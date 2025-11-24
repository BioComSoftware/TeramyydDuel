using UnityEngine;

/// <summary>
/// JetEngine: Specialized engine subclass for atmospheric/space jet propulsion.
/// Extends base Engine with heat management that affects power output and efficiency.
/// Inherits full thrust/power/priority system from Engine base class.
/// </summary>
[AddComponentMenu("Teramyyd/Ship Systems/Jet Engine")]
public class JetEngine : Engine
{
    [Header("Heat Management")]
    [Tooltip("Maximum safe operating temperature.")]
    public float maxSafeTemperature = 100f;
    
    [Tooltip("Minimum operating temperature when engine is running (idle heat).")]
    public float minOperatingTemperature = 20f;
    
    [Tooltip("Heat generated PER POWER UNIT PER MINUTE. Example: 1 = 1 degree per minute for each power unit produced.")]
    public float heatPerPowerUnitPerMinute = 1f;
    
    [Tooltip("Heat dissipation per second (constant rate).")]
    public float heatDissipationRate = 10f;
    
    [Tooltip("Power output penalty per degree above safe temperature.")]
    [Range(0f, 0.1f)]
    public float heatEfficiencyPenalty = 0.01f;
    
    [Tooltip("Damage per second when overheated.")]
    public float overheatDamageRate = 5f;
    
    [Header("Heat Status (Read-Only)")]
    [SerializeField] private float _currentTemperature = 0f;
    [SerializeField] private bool _isOverheating = false;
    private float _lastActualPowerUsage = 0f;
    
    public float CurrentTemperature => _currentTemperature;
    public bool IsOverheating => _isOverheating;
    
    protected override void Start()
    {
        base.Start();
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} [JetEngine] - MaxTemp: {maxSafeTemperature}, HeatPerPowerPerMin: {heatPerPowerUnitPerMinute}, Dissipation: {heatDissipationRate}/s", "JetEngine");
        }
    }
    
    protected override void FixedUpdate()
    {
        // Use last frame's power usage to update heat before this frame's power calculation
        ManageHeat(Time.fixedDeltaTime);
        
        // Handle base power/thrust logic
        base.FixedUpdate();
        
        // Cache actual power usage from this frame for next heat update
        _lastActualPowerUsage = CalculateActualPowerUsage();
    }
    
    /// <summary>
    /// Override power calculation to include heat efficiency penalty.
    /// </summary>
    protected override void CalculatePowerOutput()
    {
        // Calculate base power from burn rate
        float burnMultiplier = burnRatePercent / 100f;
        float basePower = maxPowerPerSecond * burnMultiplier;
        
        // Apply heat efficiency penalty if overheating
        float heatPenalty = 0f;
        if (_currentTemperature > maxSafeTemperature)
        {
            float excessHeat = _currentTemperature - maxSafeTemperature;
            heatPenalty = excessHeat * heatEfficiencyPenalty;
            heatPenalty = Mathf.Clamp01(heatPenalty); // Max 100% penalty
        }
        
        _currentPowerOutput = basePower * (1f - heatPenalty);
        
        // Calculate damage rate based on burn multiplier
        _damagePerSecond = usageDamagePerSecond * burnMultiplier;
        
        onPowerOutputChanged?.Invoke(_currentPowerOutput);
        
        if (debugLog && Time.frameCount % 120 == 0)
        {
            FileLogger.Log($"{gameObject.name} - Temp: {_currentTemperature:F1}/{maxSafeTemperature}, Power: {_currentPowerOutput:F1}/{basePower:F1} (penalty: {heatPenalty * 100f:F1}%)", "JetEngine");
        }
    }
    
    /// <summary>
    /// Manage heat generation and dissipation.
    /// Heat generation scales with actual power utilization (0-1) and burn rate.
    /// - ≤100% burn: linear response (utilization × burnFraction)
    /// - >100% burn: exponential amplification to punish overburn ((burnFraction)^5)
    /// Dissipation: Constant rate, cannot go below minOperatingTemperature
    /// </summary>
    void ManageHeat(float deltaTime)
    {
        float normalizedLoad = (maxPowerPerSecond > 0f)
            ? Mathf.Clamp01(_lastActualPowerUsage / maxPowerPerSecond)
            : 0f;
        float burnFraction = Mathf.Max(burnRatePercent, 0f) / 100f;
        float heatLoadFactor = normalizedLoad;

        if (burnFraction > 1f)
        {
            float overburnMultiplier = Mathf.Pow(burnFraction, 5f);
            heatLoadFactor *= overburnMultiplier;
        }
        else
        {
            heatLoadFactor *= burnFraction;
        }

        float heatPerMinute = heatLoadFactor * heatPerPowerUnitPerMinute;
        float heatGenerated = (heatPerMinute / 60f) * deltaTime;
        
        // Constant heat dissipation
        float heatDissipated = heatDissipationRate * deltaTime;
        
        // Update current temperature
        _currentTemperature += heatGenerated - heatDissipated;
        
        // Clamp to minimum operating temperature (engine maintains idle heat when running)
        _currentTemperature = Mathf.Max(minOperatingTemperature, _currentTemperature);
        
        // Check overheat status
        _isOverheating = _currentTemperature > maxSafeTemperature;
        
        // Apply overheat damage with 5th power scaling
        if (_isOverheating && healthComponent != null)
        {
            float excessHeat = _currentTemperature - maxSafeTemperature;
            float percentOver = excessHeat / maxSafeTemperature;
            
            // 5th power damage scaling
            // Example: 10% over (110°/100°) = 1.1^5 = 1.61× damage
            //          20% over (120°/100°) = 1.2^5 = 2.49× damage
            float damageMultiplier = Mathf.Pow(1f + percentOver, 5f);
            
            float overheatDamage = overheatDamageRate * damageMultiplier * deltaTime;
            int damageToApply = Mathf.FloorToInt(overheatDamage);
            
            if (damageToApply > 0)
            {
                healthComponent.TakeDamage(damageToApply);
                
                if (debugLog)
                {
                    FileLogger.Log($"{gameObject.name} taking {damageToApply} overheat damage! Temp: {_currentTemperature:F1}/{maxSafeTemperature} ({percentOver * 100f:F1}% over, {damageMultiplier:F2}× multiplier)", "JetEngine");
                }
            }
        }
    }
    
    float CalculateActualPowerUsage()
    {
        float thrustPower = Mathf.Max(0f, _allocatedThrustPower);
        return Mathf.Min(Mathf.Max(_currentPowerOutput, 0f), thrustPower);
    }
    
    /// <summary>
    /// Emergency heat dump - reduces burn rate to cool down.
    /// Player tactical choice: Trade thrust for thermal safety.
    /// </summary>
    public void EmergencyHeatDump()
    {
        burnRatePercent = Mathf.Max(25f, burnRatePercent * 0.3f); // Drop to 30% or minimum 25%
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} emergency heat dump - Burn reduced to {burnRatePercent:F1}%", "JetEngine");
        }
    }
}
