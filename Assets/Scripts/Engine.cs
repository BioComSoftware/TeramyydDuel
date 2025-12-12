using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base engine class: Hermeneutic integration of power generation, thrust, and lift coordination.
/// Power (possibility) → Force (actuality) → Motion (being-in-the-world)
/// 
/// Ontological structure:
/// - Engine generates power from fuel/burn (thrownness into energy)
/// - Power distributed to thrust and lift (care structure / priority)
/// - Thrust creates motion (projection into space)
/// - All systems degrade over time (being-towards-breakdown)
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(CrewStationRequirementProfile))]
[AddComponentMenu("Teramyyd/Ship Systems/Engine (Base)")]
public abstract class Engine : MonoBehaviour
{
    [Header("Power Generation")]
    [Tooltip("Maximum total power output per second (at 100% burn) for ALL ship systems.")]
    public float maxPowerPerSecond = 100f;
    
    [Tooltip("Damage per second when running at 100% burn rate.")]
    public float usageDamagePerSecond = 1f;
    
    [Header("Thrust Configuration")]
    [Header("Operational State")]
    [Range(0f, 300f)]
    [Tooltip("Engine burn rate as percentage. 100% = normal, <100% = underburn, >100% = overburn.")]
    public float burnRatePercent = 100f;
    
    [Header("Player Controls")]
    [Tooltip("Desired speed FORWARD (positive Z-axis) in knots. Set to 0 if moving astern.")]
    public float knotsAhead = 0f;
    
    [Tooltip("Desired speed BACKWARD (negative Z-axis) in knots. Set to 0 if moving ahead.")]
    public float knotsAstern = 0f;
    
    [Tooltip("Power priority mode for thrust vs lift allocation.")]
    public PowerPriorityMode priorityMode = PowerPriorityMode.Balanced;
    
    [Header("Status (Read-Only)")]
    [SerializeField] protected float _currentPowerOutput;
    [SerializeField] protected float _requestedThrustPower;
    [SerializeField] protected float _allocatedThrustPower;
    [SerializeField] protected float _actualForceNewtons;
    [SerializeField] protected float _accelerationMPS2;
    [SerializeField] protected bool _isAccelerating;
    [SerializeField] protected float _damagePerSecond;
    [SerializeField] protected float _throttlePercent = 1f;
    
    [Header("Events")]
    public FloatEvent onPowerOutputChanged;
    public FloatEvent onThrustOutputChanged;
    
    [Header("Debug")]
    public bool debugLog = false;

    [Header("Crew Requirements")]
    [Tooltip("Crew station responsible for operating this engine. Auto-created at runtime.")]
    public CrewStation crewStation;
    
    CrewStationRequirementProfile _crewProfile;
    
    // Constants
    public const float KNOTS_TO_MPS = 0.514444f;
    public const float MPS_TO_KNOTS = 1.94384f;
    public const float POWER_PER_TON_PER_MPS = 1f; // Acceleration capability per ton
    public const float FORCE_PER_POWER_UNIT = 1000f; // Each power unit translates to 1000N of thrust
    
    // Component references
    protected Health healthComponent;
    protected ShipCharacteristics shipCharacteristics;
    protected Rigidbody shipRigidbody;
    protected LiftDevice liftDevice;
    
    // Public read-only properties
    public float CurrentPowerOutput => _currentPowerOutput;
    public float RequestedThrustPower => _requestedThrustPower;
    public float AllocatedThrustPower => _allocatedThrustPower;
    public float ActualForceNewtons => _actualForceNewtons;
    public float CurrentDamagePerSecond => _damagePerSecond;
    public float CurrentThrottlePercent => _throttlePercent;
    
    public enum PowerPriorityMode
    {
        LiftPriority,    // Lift gets full power, thrust gets remainder
        ThrustPriority,  // Thrust gets full power, lift gets remainder
        Balanced         // Optimize distribution, maintain minimum altitude
    }
    
    protected virtual void Awake()
    {
        healthComponent = GetComponent<Health>();
        _crewProfile = GetComponent<CrewStationRequirementProfile>();
        shipCharacteristics = GetComponentInParent<ShipCharacteristics>();
        liftDevice = GetComponentInParent<LiftDevice>();
        EnsureCrewStation();
        
        if (shipCharacteristics != null)
        {
            shipRigidbody = shipCharacteristics.GetComponent<Rigidbody>();
        }
        
        if (healthComponent == null)
        {
            Debug.LogError($"[Engine] {gameObject.name} requires Health component!");
        }
        
        if (shipCharacteristics == null)
        {
            Debug.LogError($"[Engine] {gameObject.name} requires ShipCharacteristics parent!");
        }
    }
    
    protected virtual void Start()
    {
        EnsureCrewStation(); // Re-apply profile settings after all components initialized
        CalculatePowerOutput();
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} initialized - MaxPower: {maxPowerPerSecond}/s, ForcePerPower: {FORCE_PER_POWER_UNIT}N, AccelPerTon: {POWER_PER_TON_PER_MPS}, Burn: {burnRatePercent}%", "Engine");
        }
    }
    
    protected virtual void FixedUpdate()
    {
        if (shipCharacteristics == null || shipRigidbody == null)
            return;
        
        // ENGINE BEHAVIOR: If engine is undermanned, stop producing thrust
        if (!HasOperationalCrew())
        {
            _currentPowerOutput = 0f;
            _requestedThrustPower = 0f;
            _allocatedThrustPower = 0f;
            _actualForceNewtons = 0f;
            _accelerationMPS2 = 0f;
            _isAccelerating = false;
            
            // Ship decelerates naturally via physics (aerodynamic drag)
            return;
        }

        float deltaTime = Time.fixedDeltaTime;
        
        // Calculate total power output from engine
        CalculatePowerOutput();
        
        // Allocate power between thrust and lift
        CalculatePowerAllocation();
        
        // Apply thrust based on allocated power
        ApplyThrust(deltaTime);
        
        // Apply usage damage
        ApplyUsageDamage(deltaTime);
    }
    
    /// <summary>
    /// Calculate total power output based on burn rate.
    /// Subclasses (e.g., JetEngine) can override to add heat effects.
    /// </summary>
    protected virtual void CalculatePowerOutput()
    {
        float burnMultiplier = burnRatePercent / 100f;
        _currentPowerOutput = maxPowerPerSecond * burnMultiplier;
        
        // Calculate damage rate based on burn multiplier
        _damagePerSecond = usageDamagePerSecond * burnMultiplier;
        
        onPowerOutputChanged?.Invoke(_currentPowerOutput);
    }
    
    /// <summary>
    /// Hermeneutic power allocation: Mediates between lift and thrust based on priority mode.
    /// Uses AHEAD (positive Z) and ASTERN (negative Z) controls.
    /// CRITICAL: Ship's transform.forward = positive Z-axis (bow direction)
    ///          Ship's -transform.forward = negative Z-axis (stern direction)
    /// </summary>
    protected virtual void CalculatePowerAllocation()
    {
        float shipMassTons = shipCharacteristics.shipWeightTons;
        float shipMassKg = shipMassTons * 1000f;
        
        // Determine desired velocity based on ahead/astern controls
        // AHEAD = positive Z-axis motion, ASTERN = negative Z-axis motion
        float desiredVelocityMPS = 0f;
        
        if (knotsAhead > 0f)
        {
            // Moving AHEAD (forward, positive Z-axis)
            desiredVelocityMPS = knotsAhead * KNOTS_TO_MPS;
        }
        else if (knotsAstern > 0f)
        {
            // Moving ASTERN (backward, negative Z-axis)
            desiredVelocityMPS = -(knotsAstern * KNOTS_TO_MPS);
        }
        else
        {
            // Both are 0 - stop the ship
            desiredVelocityMPS = 0f;
        }
        
        // Get current velocity along ship's Z-axis (positive = forward, negative = backward)
        Vector3 shipForward = shipCharacteristics.transform.forward;
        float currentVelocityMPS = Vector3.Dot(shipRigidbody.linearVelocity, shipForward);
        
        // Calculate velocity error (how much we need to change)
        float velocityError = desiredVelocityMPS - currentVelocityMPS;
        const float SPEED_TOLERANCE_MPS = 0.2f;
        float desiredSpeedAbs = Mathf.Abs(desiredVelocityMPS);
        float currentSpeedAbs = Mathf.Abs(currentVelocityMPS);
        bool wantsMovement = desiredSpeedAbs > SPEED_TOLERANCE_MPS;
        bool withinTolerance = wantsMovement
            ? Mathf.Abs(velocityError) <= SPEED_TOLERANCE_MPS
            : currentSpeedAbs <= SPEED_TOLERANCE_MPS;
        
        _isAccelerating = !withinTolerance;
        
        if (!_isAccelerating)
        {
            // At desired speed - only need power to overcome drag at the greater of desired/current speed
            float sustainSpeedMPS = Mathf.Max(Mathf.Abs(desiredVelocityMPS), Mathf.Abs(currentVelocityMPS));
            _requestedThrustPower = CalculateDragCompensationPower(sustainSpeedMPS);
        }
        else
        {
            // Need to accelerate/decelerate to reach desired velocity
            // Request maximum available power to accelerate as fast as possible
            // The physics will naturally limit us to max speed when thrust equals drag
            _requestedThrustPower = _currentPowerOutput;
        }
        
        // Respect throttle limit (percentage of current power output)
        float throttledPowerCap = _currentPowerOutput * Mathf.Clamp01(_throttlePercent);
        _requestedThrustPower = Mathf.Min(_requestedThrustPower, throttledPowerCap);
        
        // Lift devices now generate power independently, so thrust allocation ignores their demand
        float totalRequested = _requestedThrustPower;
        
        // Allocate based on priority mode
        if (totalRequested <= _currentPowerOutput)
        {
            // Enough power for both
            _allocatedThrustPower = _requestedThrustPower;
        }
        else
        {
            // Not enough power - apply priority mode
            switch (priorityMode)
            {
                case PowerPriorityMode.LiftPriority:
                case PowerPriorityMode.ThrustPriority:
                case PowerPriorityMode.Balanced:
                    _allocatedThrustPower = Mathf.Min(_requestedThrustPower, _currentPowerOutput);
                    break;
            }
        }
        
        if (debugLog && Time.frameCount % 30 == 0)
        {
            string movementStr = (knotsAhead > 0f) ? $"AHEAD {knotsAhead:F1}kt" :
                                (knotsAstern > 0f) ? $"ASTERN {knotsAstern:F1}kt" : "STOP";
            float desiredSpeedKnots = desiredVelocityMPS * MPS_TO_KNOTS;
            float currentSpeedKnots = currentVelocityMPS * MPS_TO_KNOTS;
            float liftDemand = (liftDevice != null) ? liftDevice.allocatedPowerPerSecond : 0f;
            string accelState = _isAccelerating ? "ACCEL" : "SUSTAIN";
            FileLogger.Log(
                $"{gameObject.name} [{accelState}] Cmd:{movementStr} ({desiredSpeedKnots:F1}kt) Cur:{currentSpeedKnots:F1}kt Err:{velocityError:F2}m/s, Throttle:{_throttlePercent * 100f:F0}%, LiftReq:{liftDemand:F1}/s, Thrust {_allocatedThrustPower:F1}/{_requestedThrustPower:F1}, Mode:{priorityMode}",
                "Engine");
        }
    }
    
    /// <summary>
    /// Calculate desired acceleration based on velocity error.
    /// Positive error = need to accelerate forward (positive Z)
    /// Negative error = need to accelerate backward (negative Z)
    /// </summary>
    protected virtual float CalculateDesiredAcceleration(float velocityError)
    {
        float shipMassTons = shipCharacteristics.shipWeightTons;
        
        // Base acceleration capability
        float baseAcceleration = POWER_PER_TON_PER_MPS;
        
        // Proportional acceleration based on how far off we are
        float accelerationGain = 2.0f;
        float targetAcceleration = velocityError * accelerationGain;
        
        // Clamp to max capability
        float maxAcceleration = baseAcceleration * shipMassTons;
        return Mathf.Clamp(targetAcceleration, -maxAcceleration, maxAcceleration);
    }
    
    /// <summary>
    /// Calculate power needed to maintain a given speed against drag (aero + Unity damping).
    /// </summary>
    /// <param name="sustainSpeedMPS">Speed to sustain in meters per second. If &lt;= 0, uses current speed.</param>
    protected virtual float CalculateDragCompensationPower(float sustainSpeedMPS = -1f)
    {
        float effectiveSpeed = sustainSpeedMPS;
        if (effectiveSpeed <= 0f && shipRigidbody != null)
        {
            effectiveSpeed = shipRigidbody.linearVelocity.magnitude;
        }
        effectiveSpeed = Mathf.Max(0f, effectiveSpeed);
        
        // Aerodynamic drag force (0.5 * rho * Cd * Area * v^2)
        float aeroDragForce = CalculateAerodynamicDrag(effectiveSpeed);
        
        // Unity's linear damping force behaves like F = damping Ã— mass Ã— velocity
        float linearDampingForce = 0f;
        if (shipRigidbody != null)
        {
            linearDampingForce = shipRigidbody.linearDamping * shipRigidbody.mass * effectiveSpeed;
        }
        
        float totalDragForce = aeroDragForce + linearDampingForce;
        return totalDragForce / FORCE_PER_POWER_UNIT;
    }
    
    /// <summary>
    /// Apply thrust force to ship based on allocated power.
    /// Uses Unity physics (AddForce) for momentum-based motion.
    /// AHEAD motion = positive Z-axis (ship's transform.forward)
    /// ASTERN motion = negative Z-axis (-ship's transform.forward)
    /// 
    /// If moving forward and astern requested: must overcome forward inertia first
    /// If moving backward and ahead requested: must overcome backward inertia first
    /// </summary>
    protected virtual void ApplyThrust(float deltaTime)
    {
        // Convert allocated power to force magnitude
        _actualForceNewtons = _allocatedThrustPower * FORCE_PER_POWER_UNIT;
        
        if (_actualForceNewtons > 0.1f && shipCharacteristics != null)
        {
            // Ship's Z-axis: forward = +Z (bow), backward = -Z (stern)
            Vector3 shipForward = shipCharacteristics.transform.forward;
            
            // Current velocity along ship's Z-axis (positive = ahead, negative = astern)
            float currentVelocityMPS = Vector3.Dot(shipRigidbody.linearVelocity, shipForward);
            
            // Determine desired velocity from player controls
            float desiredVelocityMPS = 0f;
            
            if (knotsAhead > 0f)
            {
                // Player wants AHEAD motion (positive Z)
                desiredVelocityMPS = knotsAhead * KNOTS_TO_MPS;
            }
            else if (knotsAstern > 0f)
            {
                // Player wants ASTERN motion (negative Z)
                desiredVelocityMPS = -(knotsAstern * KNOTS_TO_MPS);
            }
            
            // Determine thrust direction based on what we need to achieve
            Vector3 thrustDirection;
            
            if (Mathf.Abs(desiredVelocityMPS) < 0.1f)
            {
                // Want to STOP - apply braking opposite to current motion
                if (currentVelocityMPS > 0.1f)
                {
                    // Moving ahead, thrust astern to stop
                    thrustDirection = -shipForward;
                }
                else if (currentVelocityMPS < -0.1f)
                {
                    // Moving astern, thrust ahead to stop
                    thrustDirection = shipForward;
                }
                else
                {
                    // Already stopped
                    thrustDirection = Vector3.zero;
                }
            }
            else if (desiredVelocityMPS > 0f)
            {
                // Want to move AHEAD (positive Z-axis)
                if (currentVelocityMPS < desiredVelocityMPS)
                {
                    // Not going fast enough ahead (or moving astern) - thrust AHEAD
                    thrustDirection = shipForward;
                }
                else
                {
                    // Going too fast ahead - thrust ASTERN to slow down
                    thrustDirection = -shipForward;
                }
            }
            else // desiredVelocityMPS < 0f
            {
                // Want to move ASTERN (negative Z-axis)
                if (currentVelocityMPS > desiredVelocityMPS)
                {
                    // Not going fast enough astern (or moving ahead) - thrust ASTERN
                    thrustDirection = -shipForward;
                }
                else
                {
                    // Going too fast astern - thrust AHEAD to slow down
                    thrustDirection = shipForward;
                }
            }
            
            // Apply force to ship's rigidbody
            Vector3 forceVector = thrustDirection * _actualForceNewtons;
            shipRigidbody.AddForce(forceVector, ForceMode.Force);
            
            // Calculate actual acceleration
            float shipMassKg = shipCharacteristics.shipWeightTons * 1000f;
            _accelerationMPS2 = _actualForceNewtons / shipMassKg;
            
            if (debugLog && Time.frameCount % 60 == 0)
            {
                string directionStr = (thrustDirection == shipForward) ? "AHEAD" : 
                                     (thrustDirection == -shipForward) ? "ASTERN" : "STOP";
                FileLogger.Log($"{gameObject.name} - Thrust: {_actualForceNewtons:F1}N {directionStr}, CurrentVel: {currentVelocityMPS:F2}m/s ({currentVelocityMPS * MPS_TO_KNOTS:F1}kt), DesiredVel: {desiredVelocityMPS:F2}m/s ({desiredVelocityMPS * MPS_TO_KNOTS:F1}kt), Ahead: {knotsAhead}kt, Astern: {knotsAstern}kt", "Engine");
            }
        }
        else
        {
            _actualForceNewtons = 0f;
            _accelerationMPS2 = 0f;
        }
        
        onThrustOutputChanged?.Invoke(_actualForceNewtons);
    }
    
    /// <summary>
    /// Apply continuous wear-and-tear damage to the engine.
    /// Temporal structure: Being-towards-breakdown.
    /// </summary>
    protected virtual void ApplyUsageDamage(float deltaTime)
    {
        if (healthComponent == null || _damagePerSecond <= 0f)
            return;
        
        float damageThisFrame = _damagePerSecond * deltaTime;
        if (damageThisFrame <= 0f)
            return;

        healthComponent.TakeDamage(damageThisFrame);

        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} suffered {damageThisFrame:F2} usage damage (Health: {healthComponent.currentHealth:F2}/{healthComponent.maxHealth:F2})", "Engine");
        }
    }
    
    /// <summary>
    /// Set desired speed AHEAD (forward, positive Z-axis) in knots.
    /// Automatically clears astern setting.
    /// </summary>
    public virtual void SetKnotsAhead(float knots)
    {
        knotsAhead = Mathf.Max(0f, knots);
        knotsAstern = 0f;
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} set to {knotsAhead:F1} knots AHEAD", "Engine");
        }
    }
    
    /// <summary>
    /// Set desired speed ASTERN (backward, negative Z-axis) in knots.
    /// Automatically clears ahead setting.
    /// </summary>
    public virtual void SetKnotsAstern(float knots)
    {
        knotsAstern = Mathf.Max(0f, knots);
        knotsAhead = 0f;
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} set to {knotsAstern:F1} knots ASTERN", "Engine");
        }
    }
    
    /// <summary>
    /// Stop all thrust (clears both ahead and astern).
    /// </summary>
    public virtual void AllStop()
    {
        knotsAhead = 0f;
        knotsAstern = 0f;
        _throttlePercent = 0f;
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} all stop - engines idle", "Engine");
        }
    }
    
    /// <summary>
    /// Limit thrust allocation to a percentage of current power output.
    /// </summary>
    public virtual void SetThrottlePercent(float throttlePercent)
    {
        _throttlePercent = Mathf.Clamp01(throttlePercent);
    }
    
    /// <summary>
    /// Set power priority mode. Player interface.
    /// Controls how power is distributed between thrust and lift.
    /// </summary>
    public virtual void SetPriorityMode(PowerPriorityMode mode)
    {
        priorityMode = mode;
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} priority mode set to {priorityMode}", "Engine");
        }
    }
    
    /// <summary>
    /// Set the burn rate percentage. Player interface for tactical choice.
    /// </summary>
    public virtual void SetBurnRate(float percentBurn)
    {
        burnRatePercent = Mathf.Clamp(percentBurn, 0f, 300f);
        CalculatePowerOutput();
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} burn rate set to {burnRatePercent}%", "Engine");
        }
    }
    
    protected virtual void EnsureCrewStation()
    {
        if (crewStation == null)
        {
            crewStation = GetComponent<CrewStation>();
        }

        if (crewStation == null)
        {
            crewStation = gameObject.AddComponent<CrewStation>();
            crewStation.displayName = gameObject.name + " Engine Crew";
            crewStation.stationId = gameObject.name + "_EngineCrew";
        }

        // Apply settings from CrewStationRequirementProfile
        if (_crewProfile != null)
        {
            int previousMax = crewStation.MaximumCrewAllowed;
            _crewProfile.ApplyTo(crewStation);
            
            // Trigger anchor rebuild if crew limits changed
            if (Application.isPlaying && crewStation.MaximumCrewAllowed != previousMax)
            {
                RequestAnchorRebuild();
            }
        }
        else
        {
            Debug.LogWarning($"[Engine:{name}] No CrewStationRequirementProfile found. Engine will not function properly.");
        }
    }
    
    void RequestAnchorRebuild()
    {
        if (!Application.isPlaying)
            return;

        var builders = GetComponents<CrewStationAnchorRuntimeBuilder>();
        foreach (var builder in builders)
        {
            if (builder != null && builder.enabled)
            {
                builder.RebuildAnchors();
            }
        }
    }

    protected bool HasOperationalCrew()
    {
        if (!CrewManager.HasInstance)
            return true;

        return CrewManager.Instance.MeetsRequirement(crewStation);
    }
    
    /// <summary>
    /// Returns the best PowerEngineering skill level among assigned crew.
    /// Hook for future skill-based bonuses.
    /// </summary>
    protected float GetBestEngineeringSkill()
    {
        if (crewStation == null)
            return 0f;
            
        return crewStation.GetBestSkillLevel();
    }
    
    /// <summary>
    /// Returns crew staffing ratio (0-1+). Hook for future multi-crew bonuses.
    /// </summary>
    protected float GetCrewStaffingRatio()
    {
        if (crewStation == null)
            return 0f;
            
        return crewStation.GetStaffingRatio();
    }

    float CalculateAerodynamicDrag(float speedMPS)
    {
        if (shipCharacteristics == null)
        {
            return 0f;
        }
        
        float rho = LiftDevice.CalculateAirDensity(Mathf.Max(0f, shipCharacteristics.currentAltitude));
        float cd = Mathf.Max(0f, shipCharacteristics.dragCoefficient);
        float area = Mathf.Max(0.1f, shipCharacteristics.frontalAreaSref);
        return 0.5f * rho * cd * area * speedMPS * speedMPS;
    }
}
