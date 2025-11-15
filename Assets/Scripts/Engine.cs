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
[System.Serializable]
public class FloatEvent : UnityEvent<float> { }

[RequireComponent(typeof(Health))]
[AddComponentMenu("Teramyyd/Ship Systems/Engine (Base)")]
public abstract class Engine : MonoBehaviour
{
    [Header("Power Generation")]
    [Tooltip("Maximum total power output per second (at 100% burn) for ALL ship systems.")]
    public float maxPowerPerSecond = 100f;
    
    [Tooltip("Damage per second when running at 100% burn rate.")]
    public float usageDamagePerSecond = 1f;
    
    [Header("Thrust Configuration")]
    [Tooltip("Force (Newtons) generated per unit of power allocated to thrust.")]
    public float forcePerUnitPower = 1000f;
    
    [Tooltip("Power units needed per ton of ship weight to accelerate at 1 m/s².")]
    public float powerPerTonPerMeterPerSecond = 1f;
    
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
    [SerializeField] protected float _currentSpeedKnots;
    [SerializeField] protected float _requestedThrustPower;
    [SerializeField] protected float _allocatedThrustPower;
    [SerializeField] protected float _actualForceNewtons;
    [SerializeField] protected float _accelerationMPS2;
    [SerializeField] protected bool _isAccelerating;
    [SerializeField] protected float _damagePerSecond;
    
    [Header("Events")]
    public FloatEvent onPowerOutputChanged;
    public FloatEvent onThrustOutputChanged;
    
    [Header("Debug")]
    public bool debugLog = false;
    
    // Constants
    protected const float KNOTS_TO_MPS = 0.514444f;
    protected const float MPS_TO_KNOTS = 1.94384f;
    
    // Component references
    protected Health healthComponent;
    protected ShipCharacteristics shipCharacteristics;
    protected Rigidbody shipRigidbody;
    protected LiftDevice liftDevice;
    
    // Public read-only properties
    public float CurrentPowerOutput => _currentPowerOutput;
    public float CurrentSpeedKnots => _currentSpeedKnots;
    public float RequestedThrustPower => _requestedThrustPower;
    public float AllocatedThrustPower => _allocatedThrustPower;
    public float ActualForceNewtons => _actualForceNewtons;
    public float CurrentDamagePerSecond => _damagePerSecond;
    
    public enum PowerPriorityMode
    {
        LiftPriority,    // Lift gets full power, thrust gets remainder
        ThrustPriority,  // Thrust gets full power, lift gets remainder
        Balanced         // Optimize distribution, maintain minimum altitude
    }
    
    protected virtual void Awake()
    {
        healthComponent = GetComponent<Health>();
        shipCharacteristics = GetComponentInParent<ShipCharacteristics>();
        liftDevice = GetComponentInParent<LiftDevice>();
        
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
        CalculatePowerOutput();
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} initialized - MaxPower: {maxPowerPerSecond}/s, ForcePerPower: {forcePerUnitPower}N, Burn: {burnRatePercent}%", "Engine");
        }
    }
    
    protected virtual void FixedUpdate()
    {
        if (shipCharacteristics == null || shipRigidbody == null)
            return;
        
        float deltaTime = Time.fixedDeltaTime;
        
        // Calculate total power output from engine
        CalculatePowerOutput();
        
        // Allocate power between thrust and lift
        CalculatePowerAllocation();
        
        // Apply thrust based on allocated power
        ApplyThrust(deltaTime);
        
        // Update current speed
        UpdateCurrentSpeed();
        
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
        _isAccelerating = Mathf.Abs(velocityError) > 0.1f;
        
        if (!_isAccelerating)
        {
            // At desired speed - only need power to overcome drag
            _requestedThrustPower = CalculateDragCompensationPower();
        }
        else
        {
            // Need to accelerate/decelerate to reach desired velocity
            // Calculate required acceleration (can be positive or negative)
            float desiredAcceleration = CalculateDesiredAcceleration(velocityError);
            
            // F = ma (force needed for desired acceleration)
            float requiredForceNewtons = shipMassKg * Mathf.Abs(desiredAcceleration);
            
            // Convert force to power units (always positive - power is directionless)
            _requestedThrustPower = requiredForceNewtons / forcePerUnitPower;
        }
        
        // Get lift device power request
        float requestedLiftPower = 0f;
        if (liftDevice != null)
        {
            requestedLiftPower = liftDevice.allocatedPowerPerSecond;
        }
        
        // Total power requested
        float totalRequested = _requestedThrustPower + requestedLiftPower;
        
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
                    _allocatedThrustPower = Mathf.Max(0f, _currentPowerOutput - requestedLiftPower);
                    break;
                
                case PowerPriorityMode.ThrustPriority:
                    _allocatedThrustPower = Mathf.Min(_requestedThrustPower, _currentPowerOutput);
                    if (liftDevice != null)
                    {
                        float remainingPower = _currentPowerOutput - _allocatedThrustPower;
                        liftDevice.allocatedPowerPerSecond = remainingPower;
                    }
                    break;
                
                case PowerPriorityMode.Balanced:
                    // Ensure lift gets at least minimum for hover
                    float minLiftPower = (liftDevice != null) ? liftDevice.minimumPowerPerSecond : 0f;
                    float guaranteedLiftPower = Mathf.Min(requestedLiftPower, minLiftPower);
                    
                    float remainingPowerAfterMinLift = _currentPowerOutput - guaranteedLiftPower;
                    
                    if (remainingPowerAfterMinLift > 0f)
                    {
                        // Distribute remaining power proportionally
                        float remainingLiftRequest = requestedLiftPower - guaranteedLiftPower;
                        float totalRemainingRequest = _requestedThrustPower + remainingLiftRequest;
                        
                        if (totalRemainingRequest > 0f)
                        {
                            float thrustRatio = _requestedThrustPower / totalRemainingRequest;
                            _allocatedThrustPower = remainingPowerAfterMinLift * thrustRatio;
                            
                            if (liftDevice != null)
                            {
                                float allocatedLiftBonus = remainingPowerAfterMinLift * (1f - thrustRatio);
                                liftDevice.allocatedPowerPerSecond = guaranteedLiftPower + allocatedLiftBonus;
                            }
                        }
                        else
                        {
                            _allocatedThrustPower = remainingPowerAfterMinLift;
                        }
                    }
                    else
                    {
                        _allocatedThrustPower = 0f;
                    }
                    break;
            }
        }
        
        if (debugLog && Time.frameCount % 60 == 0)
        {
            string movementStr = (knotsAhead > 0f) ? $"AHEAD {knotsAhead}kt" : 
                                (knotsAstern > 0f) ? $"ASTERN {knotsAstern}kt" : "STOP";
            FileLogger.Log($"{gameObject.name} - Power: {_currentPowerOutput:F1}/s, Movement: {movementStr}, CurrentSpeed: {_currentSpeedKnots:F1}kt, ThrustPower: {_allocatedThrustPower:F1}/{_requestedThrustPower:F1}, Mode: {priorityMode}", "Engine");
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
        float baseAcceleration = powerPerTonPerMeterPerSecond;
        
        // Proportional acceleration based on how far off we are
        float accelerationGain = 2.0f;
        float targetAcceleration = velocityError * accelerationGain;
        
        // Clamp to max capability
        float maxAcceleration = baseAcceleration * shipMassTons;
        return Mathf.Clamp(targetAcceleration, -maxAcceleration, maxAcceleration);
    }
    
    /// <summary>
    /// Calculate power needed to maintain speed against drag.
    /// </summary>
    protected virtual float CalculateDragCompensationPower()
    {
        // Drag force = drag coefficient * velocity
        // This is simplified; Unity's Rigidbody.drag handles the actual physics
        float currentSpeed = shipRigidbody.linearVelocity.magnitude;
        float dragForce = shipCharacteristics.dragCoefficient * currentSpeed * shipCharacteristics.shipWeightTons * 100f;
        
        return dragForce / forcePerUnitPower;
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
        _actualForceNewtons = _allocatedThrustPower * forcePerUnitPower;
        
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
    /// Update current speed in knots for display.
    /// </summary>
    protected virtual void UpdateCurrentSpeed()
    {
        if (shipCharacteristics == null || shipRigidbody == null)
            return;
        
        float speedMPS = shipRigidbody.linearVelocity.magnitude;
        
        // Determine sign based on direction relative to ship's forward (bow)
        if (shipRigidbody.linearVelocity.magnitude > 0.01f)
        {
            Vector3 shipForward = shipCharacteristics.transform.forward;
            float dot = Vector3.Dot(shipRigidbody.linearVelocity.normalized, shipForward);
            if (dot < 0f)
            {
                speedMPS = -speedMPS; // Negative for reverse (moving toward stern)
            }
        }
        
        _currentSpeedKnots = speedMPS * MPS_TO_KNOTS;
    }
    
    /// <summary>
    /// Apply continuous wear-and-tear damage to the engine.
    /// Temporal structure: Being-towards-breakdown.
    /// </summary>
    protected virtual void ApplyUsageDamage(float deltaTime)
    {
        if (healthComponent == null || _damagePerSecond <= 0f)
            return;
        
        // Accumulate fractional damage
        float damageThisFrame = _damagePerSecond * deltaTime;
        int damageToApply = Mathf.FloorToInt(damageThisFrame);
        
        if (damageToApply > 0)
        {
            healthComponent.TakeDamage(damageToApply);
            
            if (debugLog)
            {
                FileLogger.Log($"{gameObject.name} suffered {damageToApply} usage damage (Health: {healthComponent.currentHealth}/{healthComponent.maxHealth})", "Engine");
            }
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
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} all stop - engines idle", "Engine");
        }
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
}
