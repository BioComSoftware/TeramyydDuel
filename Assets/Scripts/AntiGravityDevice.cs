using UnityEngine;

/// <summary>
/// AntiGravityDevice: Specialized lift device using anti-gravity technology.
/// Represents technological opposition to gravitational thrownness.
/// 
/// Operates by generating a counter-gravitational field that can:
/// 1. Neutralize gravity (hover)
/// 2. Overcome gravity (ascend)
/// 3. Partially counter gravity (controlled descent)
/// </summary>
[AddComponentMenu("Teramyyd/Ship Systems/Anti-Gravity Device")]
public class AntiGravityDevice : LiftDevice
{
    [Header("Anti-Gravity Specifics")]
    [Tooltip("Field efficiency - affects power consumption (1.0 = standard, >1.0 = more efficient).")]
    [Range(0.5f, 2f)]
    public float fieldEfficiency = 1.0f;
    
    [Tooltip("Field stability - affects consistency of lift (1.0 = perfect, <1.0 = fluctuating).")]
    [Range(0.5f, 1f)]
    public float fieldStability = 1.0f;
    
    [Tooltip("Maximum safe field strength as percentage of ship weight.")]
    [Range(100f, 300f)]
    public float maxSafeFieldStrength = 150f;
    
    [Tooltip("Altitude calibration offset - added to measured altitude for display.")]
    public float altitudeCalibration = 0f;
    
    [Header("Status (Read-Only)")]
    [SerializeField] private float _currentAltitude = 0f;
    [SerializeField] private float _fieldStrengthPercent = 0f;
    [SerializeField] private bool _fieldOverload = false;
    
    public float CurrentAltitude => _currentAltitude;
    public float FieldStrengthPercent => _fieldStrengthPercent;
    public bool IsFieldOverloaded => _fieldOverload;
    
    protected override void Start()
    {
        base.Start();
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} [AntiGrav] - Efficiency: {fieldEfficiency}, Stability: {fieldStability}, MaxSafe: {maxSafeFieldStrength}%", "AntiGrav");
        }
    }
    
    protected override void CalculateLift()
    {
        // Update altitude measurement with calibration offset
        _currentAltitude = transform.position.y + altitudeCalibration;
        
        // Apply field efficiency to effective power
        float effectivePower = allocatedPowerPerSecond * fieldEfficiency;
        float originalPower = allocatedPowerPerSecond;
        allocatedPowerPerSecond = effectivePower;
        
        // Calculate base lift
        base.CalculateLift();
        
        // Restore original power value
        allocatedPowerPerSecond = originalPower;
        
        // Apply field stability fluctuation
        if (fieldStability < 1f)
        {
            float fluctuation = 1f + (Random.Range(-1f, 1f) * (1f - fieldStability) * 0.1f);
            _currentLiftForce *= fluctuation;
        }
        
        // Calculate field strength as percentage of ship weight
        if (shipCharacteristics != null)
        {
            float gravityForce = shipCharacteristics.shipWeightTons * 1000f * Physics.gravity.magnitude;
            _fieldStrengthPercent = (gravityForce > 0f) ? (_currentLiftForce / gravityForce) * 100f : 0f;
            
            // Check for field overload
            if (_fieldStrengthPercent > maxSafeFieldStrength)
            {
                _fieldOverload = true;
                ApplyOverloadDamage();
            }
            else
            {
                _fieldOverload = false;
            }
        }
        
        if (debugLog && Time.frameCount % 120 == 0) // Log every 2 seconds
        {
            FileLogger.Log($"{gameObject.name} [AntiGrav] - Altitude: {_currentAltitude:F2}m, FieldStrength: {_fieldStrengthPercent:F1}%, Overload: {_fieldOverload}, Stability: {fieldStability}", "AntiGrav");
        }
    }
    
    /// <summary>
    /// Apply additional damage when field is overloaded.
    /// Dialectic: Excess power (possibility) damages the very device enabling it (self-negation).
    /// </summary>
    void ApplyOverloadDamage()
    {
        if (healthComponent == null)
            return;
        
        // Overload damage proportional to excess field strength
        float excessPercent = _fieldStrengthPercent - maxSafeFieldStrength;
        float overloadDamageRate = (excessPercent / 100f) * usageDamagePerSecond;
        
        float damageThisFrame = overloadDamageRate * Time.fixedDeltaTime;
        int damageToApply = Mathf.FloorToInt(damageThisFrame);
        
        if (damageToApply > 0)
        {
            healthComponent.TakeDamage(damageToApply);
            
            if (debugLog)
            {
                FileLogger.Log($"{gameObject.name} taking {damageToApply} OVERLOAD damage! Field at {_fieldStrengthPercent:F1}% (max safe: {maxSafeFieldStrength}%)", "AntiGrav");
            }
        }
    }
    
    /// <summary>
    /// Emergency field boost - temporarily increase efficiency at cost of stability and damage.
    /// Tactical choice: Short-term capability vs long-term reliability.
    /// </summary>
    public void EmergencyFieldBoost()
    {
        fieldEfficiency = Mathf.Min(fieldEfficiency * 1.5f, 2f);
        fieldStability = Mathf.Max(fieldStability * 0.7f, 0.5f);
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} EMERGENCY BOOST - Efficiency: {fieldEfficiency}, Stability: {fieldStability}", "AntiGrav");
        }
    }
    
    /// <summary>
    /// Stabilize field - reduce efficiency but increase stability and reduce overload risk.
    /// </summary>
    public void StabilizeField()
    {
        fieldEfficiency = Mathf.Max(fieldEfficiency * 0.8f, 0.5f);
        fieldStability = Mathf.Min(fieldStability * 1.2f, 1f);
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} field stabilized - Efficiency: {fieldEfficiency}, Stability: {fieldStability}", "AntiGrav");
        }
    }
    
    /// <summary>
    /// Calculate minimum power needed to hover this specific ship.
    /// Utility for player/AI to determine power requirements.
    /// </summary>
    public float CalculateMinimumHoverPower()
    {
        if (shipCharacteristics == null)
            return minimumPowerPerSecond;
        
        // Account for field efficiency
        return minimumPowerPerSecond / fieldEfficiency;
    }
    
    /// <summary>
    /// Calculate power needed for a specific vertical velocity.
    /// </summary>
    public float CalculatePowerForVelocity(float targetVelocityMPS)
    {
        if (shipCharacteristics == null)
            return 0f;
        
        float shipWeightTons = shipCharacteristics.shipWeightTons;
        float powerForVelocity = shipWeightTons * powerPerTonPerMeterPerSecond * targetVelocityMPS;
        float totalPower = (minimumPowerPerSecond + powerForVelocity) / fieldEfficiency;
        
        return totalPower;
    }
}
