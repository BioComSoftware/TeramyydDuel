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
    [Header("Lift Core Specifications")]
    [Tooltip("Minimum power per second required to maintain hover (zero vertical velocity).")]
    public float minimumPowerPerSecond = 10f;
    
    [Tooltip("Additional power per ton required to lift 1 meter per second.")]
    public float powerPerTonPerMeterPerSecond = 1f;
    
    [Tooltip("Damage per second when device is active.")]
    public float usageDamagePerSecond = 0.5f;
    
    [Header("Operational State")]
    [Tooltip("Power allocated to this lift device per second.")]
    [Range(0f, 1000f)]
    public float allocatedPowerPerSecond = 0f;
    
    [Tooltip("Is the lift device currently active?")]
    public bool isActive = true;
    
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
    protected Health healthComponent;
    protected ShipCharacteristics shipCharacteristics;
    protected Rigidbody shipRigidbody;
    
    // Public read-only properties
    public float CurrentLiftForce => _currentLiftForce;
    public float VerticalVelocityMPS => _verticalVelocityMPS;
    public float PowerConsumption => _powerConsumption;
    public bool IsHovering => _isHovering;
    
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
        // If no power allocated, default to minimum power for hover
        if (allocatedPowerPerSecond <= 0f && isActive)
        {
            allocatedPowerPerSecond = minimumPowerPerSecond;
            
            if (debugLog)
            {
                FileLogger.Log($"{gameObject.name} auto-setting power to minimum ({minimumPowerPerSecond}/s) for hover", "LiftDevice");
            }
        }
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} initialized - MinPower: {minimumPowerPerSecond}/s, PowerPerTon: {powerPerTonPerMeterPerSecond}, AllocatedPower: {allocatedPowerPerSecond}/s, Active: {isActive}", "LiftDevice");
        }
    }
    
    protected virtual void FixedUpdate()
    {
        if (!isActive || shipCharacteristics == null || shipRigidbody == null)
            return;
        
        float deltaTime = Time.fixedDeltaTime;
        
        // Calculate lift parameters
        CalculateLift();
        
        // Apply direct altitude control
        ApplyAltitudeControl(deltaTime);
        
        // Apply usage damage
        ApplyUsageDamage(deltaTime);
    }
    
    /// <summary>
    /// Hermeneutic core: Calculate vertical velocity based on power allocation and ship weight.
    /// Direct altitude control - no physics forces, just move the ship up/down.
    /// </summary>
    protected virtual void CalculateLift()
    {
        if (shipCharacteristics == null)
            return;
        
        float shipWeightTons = shipCharacteristics.shipWeightTons;
        _powerConsumption = allocatedPowerPerSecond;
        
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
            // Zero out any existing velocity from gravity
            Vector3 vel = shipRigidbody.linearVelocity;
            vel.y = 0f;
            shipRigidbody.linearVelocity = vel;
            
            if (debugLog)
            {
                FileLogger.Log($"{gameObject.name} POWER APPLIED - Gravity disabled, direct altitude control active", "LiftDevice");
            }
        }
        
        // Calculate power ratio relative to minimum (hover power)
        float powerRatio = _powerConsumption / minimumPowerPerSecond;
        
        if (powerRatio >= 1.0f)
        {
            // HOVER or ASCEND
            // Power beyond minimum goes to climbing
            float excessPower = _powerConsumption - minimumPowerPerSecond;
            
            // Calculate climb velocity: velocity = excessPower / (shipWeightTons * powerPerTonPerMeterPerSecond)
            // Example: 30 tons, PPTPMPS=1, power=30 → excess=0 → velocity = 0 (hover) ✓
            // Example: 30 tons, PPTPMPS=1, power=45 → excess=15 → velocity = 15/30 = 0.5 m/s ✓
            // Example: 30 tons, PPTPMPS=1, power=60 → excess=30 → velocity = 30/30 = 1.0 m/s ✓
            // Example: 30 tons, PPTPMPS=1, power=120 → excess=90 → velocity = 90/30 = 3.0 m/s ✓
            float powerPerMeterPerSecond = shipWeightTons * powerPerTonPerMeterPerSecond;
            
            if (powerPerMeterPerSecond > 0f)
            {
                _verticalVelocityMPS = excessPower / powerPerMeterPerSecond;
            }
            else
            {
                _verticalVelocityMPS = 0f;
            }
            
            _isHovering = (Mathf.Abs(_verticalVelocityMPS) < 0.01f);
            _currentLiftForce = _powerConsumption; // Just for display purposes
        }
        else
        {
            // DESCEND (controlled fall)
            // Power < minimum → descend at rate proportional to power deficit
            // Fall rate = 9.82 m/s (gravity constant)
            // Descent velocity = fallRate * (1 - powerRatio)
            // Example: 30 tons, power=15, min=30, ratio=0.5 → descent = 9.82 * 0.5 = 4.91 m/s ✓
            // Example: 30 tons, power=7.5, min=30, ratio=0.25 → descent = 9.82 * 0.75 = 7.365 m/s ✓
            // Example: 30 tons, power=0.01, min=30, ratio≈0 → descent = 9.82 * 1.0 ≈ 9.82 m/s ✓
            
            const float GRAVITY_FALL_RATE = 9.82f; // m/s
            float descentRate = GRAVITY_FALL_RATE * (1f - powerRatio);
            _verticalVelocityMPS = -descentRate; // Negative for descent
            _isHovering = false;
            _currentLiftForce = _powerConsumption; // Just for display purposes
        }
        
        // Calculate damage based on power consumption ratio
        _damagePerSecond = usageDamagePerSecond * powerRatio;
        
        if (debugLog && Time.frameCount % 60 == 0)
        {
            FileLogger.Log($"{gameObject.name} - Power: {_powerConsumption:F1}/s, LiftForce: {_currentLiftForce:F1}N, VertVel: {_verticalVelocityMPS:F2}m/s, Hovering: {_isHovering}, Damage: {_damagePerSecond:F2}/s", "LiftDevice");
        }
        
        onLiftForceChanged?.Invoke(_currentLiftForce);
    }
    
    /// <summary>
    /// Apply direct altitude control by moving the ship vertically at calculated velocity.
    /// No physics forces - direct position manipulation.
    /// </summary>
    protected virtual void ApplyAltitudeControl(float deltaTime)
    {
        if (shipRigidbody == null || _powerConsumption <= 0f)
            return;
        
        // Move ship directly based on vertical velocity
        if (Mathf.Abs(_verticalVelocityMPS) > 0.001f)
        {
            Vector3 newPosition = shipRigidbody.position;
            newPosition.y += _verticalVelocityMPS * deltaTime;
            shipRigidbody.MovePosition(newPosition);
        }
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
        int damageToApply = Mathf.FloorToInt(damageThisFrame);
        
        if (damageToApply > 0)
        {
            healthComponent.TakeDamage(damageToApply);
            
            if (debugLog)
            {
                FileLogger.Log($"{gameObject.name} suffered {damageToApply} usage damage (Health: {healthComponent.currentHealth}/{healthComponent.maxHealth})", "LiftDevice");
            }
            
            // Check for critical failure
            if (healthComponent.currentHealth <= 0)
            {
                OnLiftFailure();
            }
        }
    }
    
    /// <summary>
    /// Set the power allocation for this lift device.
    /// </summary>
    public virtual void SetPowerAllocation(float powerPerSecond)
    {
        allocatedPowerPerSecond = Mathf.Max(0f, powerPerSecond);
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} power allocation set to {allocatedPowerPerSecond}/s", "LiftDevice");
        }
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
}
