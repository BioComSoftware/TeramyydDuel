using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ShipCharacteristics: The ontological whole that contains and is constituted by its parts.
/// Represents the ship's being-in-motion through space.
/// Hermeneutic principle: Ship movement emerges from the circular relationship between
/// weight (inertia/thrownness) and thrust (projection/possibility).
/// 
/// Unity scale: 1 unit = 1 meter
/// Gameplay area: 1000x1000x1000 units (1 cubic kilometer)
/// Speed: measured in knots, where 10 knots ≈ 5 m/s in reality
/// </summary>
[AddComponentMenu("Teramyyd/Ship Systems/Ship Characteristics")]
public class ShipCharacteristics : MonoBehaviour
{
    [Header("Physical Characteristics")]
    [Tooltip("Total mass of the ship in metric tons. Affects acceleration and maneuverability.")]
    public float shipWeightTons = 100f;
    
    [Tooltip("Drag coefficient - resistance to movement (0 = no drag, higher = more resistance).")]
    [Range(0f, 5f)]
    public float dragCoefficient = 0.5f;
    
    [Header("Movement State (Read-Only)")]
    [SerializeField] private float _currentSpeedKnots = 0f;
    [SerializeField] private float _currentSpeedMetersPerSecond = 0f;
    [SerializeField] private Vector3 _velocity = Vector3.zero;
    [SerializeField] private float _totalThrustAvailable = 0f;
    [SerializeField] private float _accelerationMPS2 = 0f;
    
    [Header("Debug")]
    public bool debugLog = false;
    
    // Cached component references
    private List<Engine> engines = new List<Engine>();
    private Rigidbody rb;
    
    // Constants
    private const float KNOTS_TO_MPS = 0.514444f; // 1 knot = 0.514444 m/s
    private const float MPS_TO_KNOTS = 1.94384f;  // 1 m/s = 1.94384 knots
    
    // Public read-only properties
    public float CurrentSpeedKnots => _currentSpeedKnots;
    public float CurrentSpeedMetersPerSecond => _currentSpeedMetersPerSecond;
    public Vector3 Velocity => _velocity;
    public float TotalThrustAvailable => _totalThrustAvailable;
    
    void Awake()
    {
        // Get or add Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = true; // Enable gravity - lift devices will counteract it
            rb.angularDamping = 1f;
            rb.linearDamping = 0.1f; // Small drag for terminal velocity
            
            // Freeze rotation to maintain attitude during lift/descent
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        else
        {
            // Ensure gravity is enabled if Rigidbody already exists
            rb.useGravity = true;
            rb.linearDamping = 0.1f;
            
            // Freeze rotation to maintain attitude during lift/descent
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        
        // Set mass from weight (tons to kg)
        rb.mass = shipWeightTons * 1000f;
        
        // Find all engines on this ship
        engines.AddRange(GetComponentsInChildren<Engine>());
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} initialized - Weight: {shipWeightTons}t, Engines: {engines.Count}, Drag: {dragCoefficient}", "ShipCharacteristics");
        }
    }
    
    void FixedUpdate()
    {
        // Hermeneutic calculation: Thrust (possibility) acts upon mass (thrownness) to produce velocity (being-in-motion)
        CalculateMovement(Time.fixedDeltaTime);
    }
    
    /// <summary>
    /// The hermeneutic core of ship movement:
    /// Engines (parts) provide thrust → Ship mass (whole) resists → Velocity emerges from dialectic
    /// </summary>
    void CalculateMovement(float deltaTime)
    {
        // Aggregate total thrust from all engines
        _totalThrustAvailable = 0f;
        foreach (var engine in engines)
        {
            if (engine != null && engine.enabled)
            {
                _totalThrustAvailable += engine.ActualThrustOutput;
            }
        }
        
        // Calculate acceleration: F = ma → a = F/m
        // Thrust is force in Newtons, mass is in kg (tons * 1000)
        float massKg = shipWeightTons * 1000f;
        _accelerationMPS2 = _totalThrustAvailable / massKg;
        
        // Apply thrust in forward direction
        Vector3 thrustForce = transform.forward * _totalThrustAvailable;
        rb.AddForce(thrustForce, ForceMode.Force);
        
        // Apply drag (resistance proportional to velocity squared)
        Vector3 dragForce = -rb.linearVelocity * rb.linearVelocity.magnitude * dragCoefficient;
        rb.AddForce(dragForce, ForceMode.Force);
        
        // Update velocity tracking
        _velocity = rb.linearVelocity;
        _currentSpeedMetersPerSecond = _velocity.magnitude;
        _currentSpeedKnots = _currentSpeedMetersPerSecond * MPS_TO_KNOTS;
        
        if (debugLog && Time.frameCount % 60 == 0) // Log once per second (at 60fps)
        {
            FileLogger.Log($"{gameObject.name} - Thrust: {_totalThrustAvailable:F1}N, Accel: {_accelerationMPS2:F2}m/s², Speed: {_currentSpeedKnots:F1}kt ({_currentSpeedMetersPerSecond:F1}m/s), Engines: {engines.Count}", "ShipCharacteristics");
        }
    }
    
    /// <summary>
    /// Apply directional thrust for maneuvering.
    /// </summary>
    public void ApplyManeuveringThrust(Vector3 direction, float thrustAmount)
    {
        if (rb == null) return;
        
        Vector3 force = direction.normalized * thrustAmount;
        rb.AddForce(force, ForceMode.Force);
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} maneuvering thrust: {force} (amount: {thrustAmount:F1}N)", "ShipCharacteristics");
        }
    }
    
    /// <summary>
    /// Emergency stop - apply reverse thrust.
    /// </summary>
    public void EmergencyStop()
    {
        if (rb == null) return;
        
        // Apply reverse force proportional to current velocity
        Vector3 stopForce = -_velocity * shipWeightTons * 10f;
        rb.AddForce(stopForce, ForceMode.Force);
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} emergency stop engaged - applying {stopForce.magnitude:F1}N reverse thrust", "ShipCharacteristics");
        }
    }
    
    /// <summary>
    /// Get total power output from all engines.
    /// </summary>
    public float GetTotalPowerOutput()
    {
        float totalPower = 0f;
        foreach (var engine in engines)
        {
            if (engine != null && engine.enabled)
            {
                totalPower += engine.CurrentPowerOutput;
            }
        }
        return totalPower;
    }
    
    /// <summary>
    /// Refresh engine list (call if engines are added/removed at runtime).
    /// </summary>
    public void RefreshEngines()
    {
        engines.Clear();
        engines.AddRange(GetComponentsInChildren<Engine>());
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} refreshed engine list - found {engines.Count} engines", "ShipCharacteristics");
        }
    }
}
