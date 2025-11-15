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
    
    [Tooltip("Heat generation per second at 100% burn.")]
    public float heatGenerationRate = 10f;
    
    [Tooltip("Heat dissipation per second when idle.")]
    public float heatDissipationRate = 5f;
    
    [Tooltip("Power output penalty per degree above safe temperature.")]
    [Range(0f, 0.1f)]
    public float heatEfficiencyPenalty = 0.01f;
    
    [Tooltip("Damage per second when overheated.")]
    public float overheatDamageRate = 5f;
    
    [Header("Heat Status (Read-Only)")]
    [SerializeField] private float _currentTemperature = 0f;
    [SerializeField] private bool _isOverheating = false;
    
    public float CurrentTemperature => _currentTemperature;
    public bool IsOverheating => _isOverheating;
    
    protected override void Start()
    {
        base.Start();
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} [JetEngine] - MaxTemp: {maxSafeTemperature}, HeatGen: {heatGenerationRate}/s, Dissipation: {heatDissipationRate}/s", "JetEngine");
        }
    }
    
    protected override void FixedUpdate()
    {
        // Manage heat BEFORE calculating power (heat affects power output)
        ManageHeat(Time.fixedDeltaTime);
        
        // Call base to handle power/thrust/allocation
        base.FixedUpdate();
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
    /// Heat accumulates during operation and dissipates when idle/low burn.
    /// </summary>
    void ManageHeat(float deltaTime)
    {
        // Generate heat based on burn rate
        float burnMultiplier = burnRatePercent / 100f;
        float heatGenerated = heatGenerationRate * burnMultiplier * deltaTime;
        
        // Dissipate heat (scales with how far we are from max temp)
        float dissipationEfficiency = _currentTemperature / (maxSafeTemperature * 2f); // More efficient when hotter
        float heatDissipated = heatDissipationRate * (1f + dissipationEfficiency) * deltaTime;
        
        // Update current temperature
        _currentTemperature += heatGenerated - heatDissipated;
        _currentTemperature = Mathf.Max(0f, _currentTemperature);
        
        // Check overheat status
        _isOverheating = _currentTemperature > maxSafeTemperature;
        
        // Apply overheat damage
        if (_isOverheating && healthComponent != null)
        {
            float excessHeat = _currentTemperature - maxSafeTemperature;
            float overheatDamage = overheatDamageRate * (excessHeat / maxSafeTemperature) * deltaTime;
            int damageToApply = Mathf.FloorToInt(overheatDamage);
            
            if (damageToApply > 0)
            {
                healthComponent.TakeDamage(damageToApply);
                
                if (debugLog)
                {
                    FileLogger.Log($"{gameObject.name} taking {damageToApply} overheat damage! Temp: {_currentTemperature:F1}/{maxSafeTemperature}", "JetEngine");
                }
            }
        }
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
