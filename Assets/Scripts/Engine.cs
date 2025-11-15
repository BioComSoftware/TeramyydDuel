using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base engine class representing the hermeneutic circle:
/// Power output → Ship capabilities → Tactical choices → Power consumption → Damage accumulation
/// All engines exist in temporal flux (per-second calculations) and degradation (damage over time).
/// </summary>
[System.Serializable]
public class FloatEvent : UnityEvent<float> { }

[RequireComponent(typeof(Health))]
[AddComponentMenu("Teramyyd/Ship Systems/Engine (Base)")]
public abstract class Engine : MonoBehaviour
{
    [Header("Engine Core Specifications")]
    [Tooltip("Maximum power output per second (at 100% burn). Distributed to thrust and other ship systems.")]
    public float maxPowerPerSecond = 100f;
    
    [Tooltip("Maximum thrust output per second (at 100% burn, with 100% power available for thrust).")]
    public float maxThrustPerSecond = 50f;
    
    [Tooltip("Damage per second when running at 100% burn rate.")]
    public float usageDamagePerSecond = 1f;
    
    [Header("Operational State")]
    [Range(0f, 300f)]
    [Tooltip("Engine burn rate as percentage. 100% = normal, <100% = underburn, >100% = overburn.")]
    public float burnRatePercent = 100f;
    
    [Tooltip("Percentage of power reserved for non-thrust systems (shields, weapons, etc.). Remaining power goes to thrust.")]
    [Range(0f, 100f)]
    public float powerReservedPercent = 0f;
    
    [Header("Status (Read-Only)")]
    [SerializeField] protected float _currentPowerOutput;
    [SerializeField] protected float _availableThrustPower;
    [SerializeField] protected float _actualThrustOutput;
    [SerializeField] protected float _damagePerSecond;
    
    [Header("Events")]
    public FloatEvent onPowerOutputChanged;
    public FloatEvent onThrustOutputChanged;
    
    [Header("Debug")]
    public bool debugLog = false;
    
    // Component references
    protected Health healthComponent;
    protected ShipCharacteristics shipCharacteristics;
    
    // Public read-only properties
    public float CurrentPowerOutput => _currentPowerOutput;
    public float AvailableThrustPower => _availableThrustPower;
    public float ActualThrustOutput => _actualThrustOutput;
    public float CurrentDamagePerSecond => _damagePerSecond;
    
    protected virtual void Awake()
    {
        healthComponent = GetComponent<Health>();
        shipCharacteristics = GetComponentInParent<ShipCharacteristics>();
        
        if (healthComponent == null)
        {
            Debug.LogError($"[Engine] {gameObject.name} requires Health component!");
        }
        
        if (shipCharacteristics == null && debugLog)
        {
            Debug.LogWarning($"[Engine] {gameObject.name} has no ShipCharacteristics parent - some calculations may be limited");
        }
    }
    
    protected virtual void Start()
    {
        CalculateOutputs();
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} initialized - MaxPower: {maxPowerPerSecond}/s, MaxThrust: {maxThrustPerSecond}/s, Burn: {burnRatePercent}%", "Engine");
        }
    }
    
    protected virtual void Update()
    {
        float deltaTime = Time.deltaTime;
        
        // Recalculate outputs based on current burn rate and power allocation
        CalculateOutputs();
        
        // Apply usage damage over time
        ApplyUsageDamage(deltaTime);
    }
    
    /// <summary>
    /// The hermeneutic core: Calculate power and thrust outputs based on burn rate and power allocation.
    /// Part informs whole: Engine output → Ship capability
    /// Whole informs part: Ship power needs → Available thrust
    /// </summary>
    protected virtual void CalculateOutputs()
    {
        // Calculate actual power output based on burn rate
        float burnMultiplier = burnRatePercent / 100f;
        _currentPowerOutput = maxPowerPerSecond * burnMultiplier;
        
        // Calculate power reserved for non-thrust systems
        float reservedPower = _currentPowerOutput * (powerReservedPercent / 100f);
        
        // Remaining power available for thrust
        _availableThrustPower = _currentPowerOutput - reservedPower;
        
        // Calculate actual thrust output (limited by both available power and max thrust capability)
        float maxPossibleThrust = maxThrustPerSecond * burnMultiplier;
        float thrustFromPower = _availableThrustPower; // 1:1 relationship between power and thrust potential
        _actualThrustOutput = Mathf.Min(maxPossibleThrust, thrustFromPower);
        
        // Calculate damage rate based on burn multiplier
        _damagePerSecond = usageDamagePerSecond * burnMultiplier;
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} - Burn: {burnRatePercent}%, Power: {_currentPowerOutput:F1}/s, Reserved: {reservedPower:F1}, ThrustPower: {_availableThrustPower:F1}, Thrust: {_actualThrustOutput:F1}/s, Damage: {_damagePerSecond:F2}/s", "Engine");
        }
        
        // Notify listeners
        onPowerOutputChanged?.Invoke(_currentPowerOutput);
        onThrustOutputChanged?.Invoke(_actualThrustOutput);
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
    /// Set the burn rate percentage. Player interface for tactical choice.
    /// </summary>
    public virtual void SetBurnRate(float percentBurn)
    {
        burnRatePercent = Mathf.Clamp(percentBurn, 0f, 300f);
        CalculateOutputs();
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} burn rate set to {burnRatePercent}%", "Engine");
        }
    }
    
    /// <summary>
    /// Set the power reservation percentage. Allows player to allocate power between thrust and systems.
    /// </summary>
    public virtual void SetPowerReservation(float percentReserved)
    {
        powerReservedPercent = Mathf.Clamp(percentReserved, 0f, 100f);
        CalculateOutputs();
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} power reservation set to {powerReservedPercent}%", "Engine");
        }
    }
}
