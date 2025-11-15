using UnityEngine;

/// <summary>
/// JetEngine: Specialized engine subclass for atmospheric/space jet propulsion.
/// Represents a specific mode of being-in-motion within the ship system.
/// Inherits the hermeneutic circle from Engine base class.
/// </summary>
[AddComponentMenu("Teramyyd/Ship Systems/Jet Engine")]
public class JetEngine : Engine
{
    [Header("Jet Engine Specifics")]
    [Tooltip("Efficiency modifier for jet propulsion (1.0 = standard, >1.0 = more efficient).")]
    public float jetEfficiency = 1.0f;
    
    [Tooltip("Heat generation per second at 100% burn.")]
    public float heatGenerationPerSecond = 10f;
    
    [SerializeField] private float _currentHeat = 0f;
    [SerializeField] private float _maxHeat = 100f;
    
    public float CurrentHeat => _currentHeat;
    public float MaxHeat => _maxHeat;
    
    protected override void Start()
    {
        base.Start();
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} [JetEngine] - Efficiency: {jetEfficiency}, HeatGen: {heatGenerationPerSecond}/s", "JetEngine");
        }
    }
    
    protected override void Update()
    {
        base.Update();
        
        // Jet-specific: Manage heat
        ManageHeat(Time.deltaTime);
    }
    
    protected override void CalculateOutputs()
    {
        // Calculate base outputs
        base.CalculateOutputs();
        
        // Apply jet efficiency modifier to thrust
        _actualThrustOutput *= jetEfficiency;
        
        // If overheating, reduce efficiency
        if (_currentHeat > _maxHeat * 0.8f)
        {
            float overheatPenalty = 1f - ((_currentHeat - _maxHeat * 0.8f) / (_maxHeat * 0.2f)) * 0.5f;
            _actualThrustOutput *= overheatPenalty;
            
            if (debugLog)
            {
                FileLogger.Log($"{gameObject.name} overheating - thrust reduced by {(1f - overheatPenalty) * 100f:F1}%", "JetEngine");
            }
        }
    }
    
    /// <summary>
    /// Manage heat generation and dissipation.
    /// Temporal-thermal dialectic: Heat accumulates (being-towards-overload) and dissipates (return-to-equilibrium).
    /// </summary>
    void ManageHeat(float deltaTime)
    {
        // Generate heat based on burn rate
        float burnMultiplier = burnRatePercent / 100f;
        float heatGenerated = heatGenerationPerSecond * burnMultiplier * deltaTime;
        
        // Natural heat dissipation (10% of max heat per second)
        float heatDissipated = (_maxHeat * 0.1f) * deltaTime;
        
        // Update current heat
        _currentHeat += heatGenerated - heatDissipated;
        _currentHeat = Mathf.Clamp(_currentHeat, 0f, _maxHeat * 1.2f); // Can overheat up to 120%
        
        // Critical overheat damage
        if (_currentHeat > _maxHeat)
        {
            float overheatDamage = (_currentHeat - _maxHeat) * 0.5f * deltaTime;
            int damageToApply = Mathf.FloorToInt(overheatDamage);
            
            if (damageToApply > 0 && healthComponent != null)
            {
                healthComponent.TakeDamage(damageToApply);
                
                if (debugLog)
                {
                    FileLogger.Log($"{gameObject.name} taking {damageToApply} overheat damage! Heat: {_currentHeat:F1}/{_maxHeat}", "JetEngine");
                }
            }
        }
    }
    
    /// <summary>
    /// Emergency heat dump - reduces heat at cost of power/thrust interruption.
    /// Player tactical choice: Trade immediate capability for thermal safety.
    /// </summary>
    public void EmergencyHeatDump()
    {
        _currentHeat *= 0.5f; // Dump 50% of heat
        burnRatePercent *= 0.3f; // Temporarily reduce burn to 30%
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} emergency heat dump - Heat: {_currentHeat:F1}, Burn reduced to {burnRatePercent:F1}%", "JetEngine");
        }
        
        CalculateOutputs();
    }
}
