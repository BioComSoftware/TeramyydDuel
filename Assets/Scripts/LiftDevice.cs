using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base lift device class representing the hermeneutic tension between weight (thrownness-to-earth) 
/// and lift (projection-upward). Lift devices oppose gravity, creating vertical mobility.
/// 
/// Hermeneutic circle: Power consumption ↔ Altitude control ↔ Weight ↔ Tactical choice
/// Temporal structure: Continuous power drain and damage accumulation over time
/// </summary>
[RequireComponent(typeof(Health))]
[AddComponentMenu("Teramyyd/Ship Systems/Lift Device (Base)")]
public abstract class LiftDevice : MonoBehaviour
{
    // International Standard Atmosphere constants (troposphere approximation)
    protected const float SEA_LEVEL_PRESSURE = 101325f; // Pa
    protected const float SEA_LEVEL_TEMPERATURE = 288.15f; // K
    protected const float TEMPERATURE_LAPSE_RATE = 0.0065f; // K/m
    protected const float GAS_CONSTANT_AIR = 287.05f; // J/(kg·K)
    protected const float STANDARD_GRAVITY = 9.80665f; // m/s²

    [Tooltip("Power allocated to this lift device per second.")]
    [Range(0f, 1000f)]
    public float allocatedPowerPerSecond = 0f;

    [Header("Operational State")]
    [Tooltip("Is the lift device currently active?")]
    public bool isActive = true;

    [Header("Descent Control")]
    [Tooltip("Maximum commanded descent rate (m/s) while maintaining hover power.")]
    public float maxControlledDescentRate = 10f;
    
    [Tooltip("Current descent telegraph percent (0-1). 0 = hover, 1 = max controlled descent.")]
    [Range(0f, 1f)]
    [SerializeField] private float controlledDescentPercent = 0f;

    [Header("Lift Core Specifications")]
    [Tooltip("Damage per second when device is active.")]
    public float usageDamagePerSecond = 0.5f;
    
    [Header("Status (Read-Only)")]
    [SerializeField] protected float _currentLiftForce;
    [SerializeField] protected float _verticalVelocityMPS;
    [SerializeField] protected float _powerConsumption;
    [SerializeField] protected float _damagePerSecond;
    [SerializeField] protected bool _isHovering;
    
    [Header("Events")]
    public FloatEvent onLiftForceChanged;
    public UnityEvent onLiftFailure;
    
    [Header("Debug")]
    public bool debugLog = false;
    
    // Component references
    protected const float POWER_PER_TON_PER_METER_PER_SECOND = 9.8f;
    protected float HoverPowerRequirement => (shipCharacteristics != null)
        ? Mathf.Max(0f, shipCharacteristics.shipWeightTons) * POWER_PER_TON_PER_METER_PER_SECOND
        : 0f;

    protected Health healthComponent;
    protected ShipCharacteristics shipCharacteristics;
    protected Rigidbody shipRigidbody;
    
    // Public read-only properties
    public float CurrentLiftForce => _currentLiftForce;
    public float VerticalVelocityMPS => _verticalVelocityMPS;
    public float PowerConsumption => _powerConsumption;
    public bool IsHovering => _isHovering;
    public float HoverPowerPerSecond => HoverPowerRequirement;
    public float CurrentLiftAllocationPercent => (HoverPowerPerSecond > 0f)
        ? (allocatedPowerPerSecond / HoverPowerPerSecond) * 100f
        : 0f;
    
    protected virtual float ClampPowerAllocation(float requestedPower)
    {
        return Mathf.Max(0f, requestedPower);
    }
    
    protected virtual void Awake()
    {
        healthComponent = GetComponent<Health>();
        shipCharacteristics = GetComponentInParent<ShipCharacteristics>();
        
        if (healthComponent == null)
        {
            Debug.LogError($"[LiftDevice] {gameObject.name} requires Health component!");
        }
        
        if (shipCharacteristics != null)
        {
            shipRigidbody = shipCharacteristics.GetComponent<Rigidbody>();
        }
        
        if (shipCharacteristics == null && debugLog)
        {
            Debug.LogWarning($"[LiftDevice] {gameObject.name} has no ShipCharacteristics parent");
        }
    }
    
    protected virtual void Start()
    {
        // If no power allocated, default to hover power
        if (allocatedPowerPerSecond <= 0f && isActive)
        {
            SetPowerAllocation(HoverPowerRequirement);
            
            if (debugLog)
            {
                FileLogger.Log($"{gameObject.name} auto-setting power to hover requirement ({HoverPowerRequirement}/s)", "LiftDevice");
            }
        }
        else
        {
            allocatedPowerPerSecond = ClampPowerAllocation(allocatedPowerPerSecond);
        }
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} initialized - HoverPower: {HoverPowerRequirement}/s, PowerPerTon: {POWER_PER_TON_PER_METER_PER_SECOND}, AllocatedPower: {allocatedPowerPerSecond}/s, Active: {isActive}", "LiftDevice");
        }
    }
    
    protected virtual void FixedUpdate()
    {
        if (!isActive || shipCharacteristics == null || shipRigidbody == null)
            return;
        
        float deltaTime = Time.fixedDeltaTime;
        
        // Calculate lift parameters
        CalculateLift(deltaTime);
        
        // Apply direct altitude control
        ApplyAltitudeControl(deltaTime);
        
        // Apply usage damage
        ApplyUsageDamage(deltaTime);
    }
    
    /// <summary>
    /// Hermeneutic core: Calculate vertical velocity based on power allocation and ship weight.
    /// Direct altitude control - no physics forces, just move the ship up/down.
    /// </summary>
    protected virtual void CalculateLift(float deltaTime)
    {
        if (shipCharacteristics == null)
            return;
        
        allocatedPowerPerSecond = ClampPowerAllocation(allocatedPowerPerSecond);
        float resolvedPower = ResolvePowerConsumption(allocatedPowerPerSecond, deltaTime);
        float shipWeightTons = Mathf.Max(0f, shipCharacteristics.shipWeightTons);
        float hoverPower = HoverPowerRequirement;
        float powerPerMeterPerSecond = Mathf.Max(0.0001f, shipWeightTons * POWER_PER_TON_PER_METER_PER_SECOND);
        _powerConsumption = resolvedPower;
        
        // POWER = 0: Let Unity gravity handle it
        if (_powerConsumption <= 0f)
        {
            if (!shipRigidbody.useGravity)
            {
                shipRigidbody.useGravity = true;
                if (debugLog)
                {
                    FileLogger.Log($"{gameObject.name} NO POWER - Gravity enabled, ship will fall naturally", "LiftDevice");
                }
            }
            _verticalVelocityMPS = 0f;
            _isHovering = false;
            _currentLiftForce = 0f;
            return;
        }
        
        // ANY POWER > 0: Disable gravity and take direct control
        if (shipRigidbody.useGravity)
        {
            shipRigidbody.useGravity = false;
            Vector3 vel = shipRigidbody.linearVelocity;
            vel.y = 0f;
            shipRigidbody.linearVelocity = vel;
            
            if (debugLog)
            {
                FileLogger.Log($"{gameObject.name} POWER APPLIED - Gravity disabled, direct altitude control active", "LiftDevice");
            }
        }
        
        float previousVelocity = _verticalVelocityMPS;
        float powerDelta = _powerConsumption - hoverPower;
        bool hasControlledDescentCommand = controlledDescentPercent > 0f && maxControlledDescentRate > 0f && Mathf.Abs(powerDelta) <= 0.01f * Mathf.Max(1f, hoverPower);
        
        if (hasControlledDescentCommand)
        {
            _verticalVelocityMPS = -controlledDescentPercent * maxControlledDescentRate;
            _isHovering = false;
        }
        else if (_powerConsumption >= hoverPower)
        {
            float climbVelocity = powerDelta / powerPerMeterPerSecond;
            _verticalVelocityMPS = climbVelocity;
            _isHovering = Mathf.Abs(_verticalVelocityMPS) < 0.01f;
        }
        else
        {
            float powerDeficit = hoverPower - Mathf.Max(_powerConsumption, 0f);
            float descentAcceleration = (shipWeightTons > 0f) ? powerDeficit / Mathf.Max(shipWeightTons, 0.0001f) : 0f;
            _verticalVelocityMPS = previousVelocity - descentAcceleration * Time.fixedDeltaTime;
            _isHovering = false;
        }
        
        _currentLiftForce = _powerConsumption;
        float hoverReference = Mathf.Max(hoverPower, 0.0001f);
        _damagePerSecond = usageDamagePerSecond * Mathf.Clamp01(_powerConsumption / hoverReference);
        
        if (debugLog && Time.frameCount % 60 == 0)
        {
            FileLogger.Log($"{gameObject.name} - Power: {_powerConsumption:F1}/s, HoverPower: {hoverPower:F1}/s, LiftForce: {_currentLiftForce:F1}N, VertVel: {_verticalVelocityMPS:F2}m/s, Hovering: {_isHovering}, Damage: {_damagePerSecond:F2}/s", "LiftDevice");
        }
        
        onLiftForceChanged?.Invoke(_currentLiftForce);
    }

    /// <summary>
    /// Resolve the actual power the lift device can deliver this frame based on requested allocation.
    /// Derived classes can cap or modify power output (e.g., generator capacity, heat penalties).
    /// </summary>
    protected virtual float ResolvePowerConsumption(float requestedPower, float deltaTime)
    {
        return requestedPower;
    }
    
    /// <summary>
    /// Apply altitude control by setting the Rigidbody's vertical velocity directly.
    /// This creates smooth movement that instruments can track.
    /// </summary>
    protected virtual void ApplyAltitudeControl(float deltaTime)
    {
        if (shipRigidbody == null || _powerConsumption <= 0f)
            return;
        
        // Set the vertical velocity on the Rigidbody
        // This allows physics tracking while maintaining direct altitude control
        Vector3 velocity = shipRigidbody.linearVelocity;
        velocity.y = _verticalVelocityMPS;
        shipRigidbody.linearVelocity = velocity;
    }
    
    /// <summary>
    /// Apply continuous wear-and-tear damage to the lift device.
    /// Temporal structure: Being-towards-breakdown through usage.
    /// </summary>
    protected virtual void ApplyUsageDamage(float deltaTime)
    {
        if (healthComponent == null || _damagePerSecond <= 0f || !isActive)
            return;
        
        float damageThisFrame = _damagePerSecond * deltaTime;
        if (damageThisFrame <= 0f)
            return;

        healthComponent.TakeDamage(damageThisFrame);

        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} suffered {damageThisFrame:F2} usage damage (Health: {healthComponent.currentHealth:F2}/{healthComponent.maxHealth:F2})", "LiftDevice");
        }

        if (healthComponent.currentHealth <= 0f)
        {
            OnLiftFailure();
        }
    }
    
    /// <summary>
    /// Set the power allocation for this lift device.
    /// </summary>
    public virtual void SetPowerAllocation(float powerPerSecond)
    {
        allocatedPowerPerSecond = ClampPowerAllocation(powerPerSecond);
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} power allocation set to {allocatedPowerPerSecond}/s", "LiftDevice");
        }
    }
    
    /// <summary>
    /// Set lift allocation as a percentage of the hover requirement (values over 100% request extra lift).
    /// </summary>
    public virtual void SetLiftPowerPercentage(float percentage)
    {
        float clampedPercent = Mathf.Max(0f, percentage);
        float targetPower = HoverPowerPerSecond * (clampedPercent / 100f);
        SetPowerAllocation(targetPower);
    }
    
    /// <summary>
    /// Set the commanded controlled descent percentage (0-100% of maxControlledDescentRate).
    /// </summary>
    public virtual void SetControlledDescentPercent(float percent)
    {
        controlledDescentPercent = Mathf.Clamp01(percent / 100f);
    }

    /// <summary>
    /// Set the commanded controlled descent rate as a 0-1 fraction.
    /// </summary>
    public virtual void SetControlledDescentFraction(float fraction)
    {
        controlledDescentPercent = Mathf.Clamp01(fraction);
    }
    
    /// <summary>
    /// Toggle device active state.
    /// </summary>
    public virtual void SetActive(bool active)
    {
        isActive = active;
        
        if (!isActive)
        {
            _currentLiftForce = 0f;
            _verticalVelocityMPS = 0f;
            _isHovering = false;
        }
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} set to {(isActive ? "ACTIVE" : "INACTIVE")}", "LiftDevice");
        }
    }
    
    /// <summary>
    /// Called when lift device fails due to damage.
    /// </summary>
    protected virtual void OnLiftFailure()
    {
        isActive = false;
        _currentLiftForce = 0f;
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} FAILED - device offline", "LiftDevice");
        }
        
        onLiftFailure?.Invoke();
    }
    
    /// <summary>
    /// Calculate air density using a simplified ISA model based on altitude (meters).
    /// </summary>
    public static float CalculateAirDensity(float altitudeMeters)
    {
        float clampedAltitude = Mathf.Clamp(altitudeMeters, 0f, 11000f);
        float temperature = SEA_LEVEL_TEMPERATURE - TEMPERATURE_LAPSE_RATE * clampedAltitude;
        temperature = Mathf.Max(150f, temperature);
        float exponent = STANDARD_GRAVITY / (GAS_CONSTANT_AIR * TEMPERATURE_LAPSE_RATE);
        float pressure = SEA_LEVEL_PRESSURE * Mathf.Pow(temperature / SEA_LEVEL_TEMPERATURE, exponent);
        return pressure / (GAS_CONSTANT_AIR * temperature);
    }
}
