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
    
    [Tooltip("Drag coefficient (C_D) - resistance to movement (0 = no drag, higher = more resistance).")]
    [InspectorName("Drag coefficient C₍D₎")]
    [Range(0f, 5f)]
    public float dragCoefficient = 0.5f;
    
    [Tooltip("Reference frontal area (S_ref) used for drag calculations (square meters).")]
    [InspectorName("Frontal Area S₍ref₎")]
    [Range(0.1f, 10000f)]
    public float frontalAreaSref = 50f;

    [Tooltip("Computed maximum sustainable speed in knots based on thrust vs drag (read-only).")]
    [InspectorName("Max Speed Knots (computed)")]
    [SerializeField, ReadOnlyInInspector]
    private float _maxSpeedKnots = 0f;
    
    [Header("Movement State (Read-Only)")]
    [SerializeField] private float _currentSpeedKnots = 0f;
    [SerializeField] private float _currentSpeedMetersPerSecond = 0f;
    [SerializeField] private float _horizontalSpeedKnots = 0f;
    [SerializeField] private float _horizontalSpeedMetersPerSecond = 0f;
    [SerializeField] private float _currentAltitude = 0f;
    [SerializeField] private float _verticalVelocityMPS = 0f;
    [SerializeField] private Vector3 _velocity = Vector3.zero;
    [SerializeField] private float _totalThrustAvailable = 0f;
    [SerializeField] private float _accelerationMPS2 = 0f;
    
    [Header("Position (Read-Only)")]
    [SerializeField] private float _positionX = 0f;
    [SerializeField] private float _positionY = 0f;
    [SerializeField] private float _positionZ = 0f;
    
    [Header("Attitude (Read-Only)")]
    [Tooltip("Current roll angle in degrees (positive = right wing down, negative = left wing down).")]
    [SerializeField] private float _currentRollDegrees = 0f;
    
    [Tooltip("Current pitch angle in degrees (positive = nose up, negative = nose down).")]
    [SerializeField] private float _currentPitchDegrees = 0f;
    
    [Tooltip("Current yaw angle in degrees (heading).")]
    [SerializeField] private float _currentYawDegrees = 0f;
    
    [Header("Attitude Rotation Speed")]
    [Tooltip("How fast the ship rolls to target attitude (degrees per second). Higher = faster.")]
    [Range(1f, 180f)]
    public float rollRotationSpeed = 20f;
    
    [Tooltip("How fast the ship pitches to target attitude (degrees per second). Higher = faster.")]
    [Range(1f, 180f)]
    public float pitchRotationSpeed = 15f;
    
    [Tooltip("How fast the ship yaws to target attitude (degrees per second). Higher = faster.")]
    [Range(1f, 180f)]
    public float yawRotationSpeed = 10f;
    
    [Header("Attitude Limits")]
    [Tooltip("Maximum roll angle in degrees (symmetric: port and starboard). Limits how far ship can bank.")]
    [Range(10f, 180f)]
    public float maxRollDegrees = 45f;
    
    [Tooltip("Maximum nose-up pitch angle in degrees. Limits how far ship can pitch upward.")]
    [Range(5f, 90f)]
    public float maxPitchUpDegrees = 30f;
    
    [Tooltip("Maximum nose-down pitch angle in degrees. Limits how far ship can pitch downward.")]
    [Range(5f, 90f)]
    public float maxPitchDownDegrees = 20f;
    
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
    public float HorizontalSpeedKnots => _horizontalSpeedKnots;
    public float HorizontalSpeedMetersPerSecond => _horizontalSpeedMetersPerSecond;
    public float currentAltitude => _currentAltitude;
    public float verticalVelocityMPS => _verticalVelocityMPS;
    public float currentSpeedKnots => _currentSpeedKnots; // Alias for HUD compatibility
    public float horizontalSpeedKnots => _horizontalSpeedKnots; // Alias for airspeed indicator
    public Vector3 Velocity => _velocity;
    public float TotalThrustAvailable => _totalThrustAvailable;
    public float MaxSpeedKnots => _maxSpeedKnots;
    
    // Attitude properties
    public float currentRollDegrees => _currentRollDegrees;
    public float currentPitchDegrees => _currentPitchDegrees;
    public float currentYawDegrees => _currentYawDegrees;
    
    // Target attitude (what the levers are commanding)
    private float _targetRollDegrees = 0f;
    private float _targetPitchDegrees = 0f;
    private float _targetYawDegrees = 0f;
    
    public float targetRollDegrees => _targetRollDegrees;
    public float targetPitchDegrees => _targetPitchDegrees;
    public float targetYawDegrees => _targetYawDegrees;
    
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
            rb.linearDamping = 0.1f; // Re-enable Unity damping for hidden velocity cap
            
            // DO NOT freeze rotation - we need manual attitude control
            rb.constraints = RigidbodyConstraints.None;
        }
        
        // Set mass from weight (tons to kg)
        rb.mass = shipWeightTons * 1000f;
        
        // Initialize attitude tracking
        UpdateAttitudeTracking();
        
        // Find all engines on this ship and compute theoretical max speed once
        engines.AddRange(GetComponentsInChildren<Engine>());
    }

    void Start()
    {
        ComputeMaxSpeedKnots();
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} initialized - Weight: {shipWeightTons}t, Engines: {engines.Count}, Drag: {dragCoefficient}", "ShipCharacteristics");
        }
    }
    
    void FixedUpdate()
    {
        // Update movement tracking
        UpdateMovementTracking();
        // Update attitude tracking
        UpdateAttitudeTracking();
    }
    
    /// <summary>
    /// Update velocity and speed tracking.
    /// Engine.ApplyThrust() now handles force application directly.
    /// </summary>
    void UpdateMovementTracking()
    {
        // Update velocity tracking (full 3D velocity)
        _velocity = rb.linearVelocity;
        _currentSpeedMetersPerSecond = _velocity.magnitude;
        _currentSpeedKnots = _currentSpeedMetersPerSecond * MPS_TO_KNOTS;
        
        // Update horizontal airspeed (X-Z plane only, ignoring Y-axis vertical movement)
        // This is true airspeed for aircraft/ships - movement across the ground
        Vector3 horizontalVelocity = new Vector3(_velocity.x, 0f, _velocity.z);
        _horizontalSpeedMetersPerSecond = horizontalVelocity.magnitude;
        _horizontalSpeedKnots = _horizontalSpeedMetersPerSecond * MPS_TO_KNOTS;
        
        // Update position coordinates
        Vector3 pos = transform.position;
        _positionX = pos.x;
        _positionY = pos.y;
        _positionZ = pos.z;
        
        // Update altitude (Y position in world space)
        _currentAltitude = pos.y;
        
        // Update vertical velocity (Y component of velocity)
        _verticalVelocityMPS = rb.linearVelocity.y;
        
        // Track total available thrust for display purposes
        _totalThrustAvailable = 0f;
        foreach (var engine in engines)
        {
            if (engine != null && engine.enabled)
            {
                _totalThrustAvailable += engine.ActualForceNewtons;
            }
        }
        
        // Calculate current acceleration
        float massKg = shipWeightTons * 1000f;
        _accelerationMPS2 = (massKg > 0f) ? _totalThrustAvailable / massKg : 0f;
        
        if (debugLog && Time.frameCount % 60 == 0) // Log once per second (at 60fps)
        {
            FileLogger.Log($"{gameObject.name} - Altitude: {_currentAltitude:F2}m, VertVel: {_verticalVelocityMPS:F2}m/s, TotalSpeed: {_currentSpeedKnots:F1}kt, HorizontalSpeed: {_horizontalSpeedKnots:F1}kt, Position: {transform.position}, RBVelocity: {rb.linearVelocity}", "ShipCharacteristics");
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
        ComputeMaxSpeedKnots();
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} refreshed engine list - found {engines.Count} engines", "ShipCharacteristics");
        }
    }

    /// <summary>
    /// Estimate steady-state max speed at sea level assuming thrust balances aero drag
    /// plus Unity's linear damping (which behaves like an unseen velocity cap).
    /// </summary>
    void ComputeMaxSpeedKnots()
    {
        if (engines == null || engines.Count == 0)
        {
            _maxSpeedKnots = 0f;
            return;
        }

        float totalMaxPowerPerSecond = 0f;
        foreach (var engine in engines)
        {
            if (engine != null && engine.isActiveAndEnabled)
            {
                totalMaxPowerPerSecond += Mathf.Max(0f, engine.maxPowerPerSecond);
            }
        }

        if (totalMaxPowerPerSecond <= 0f)
        {
            _maxSpeedKnots = 0f;
            return;
        }

        float thrustForce = totalMaxPowerPerSecond * Engine.FORCE_PER_POWER_UNIT;
        float rhoSeaLevel = LiftDevice.CalculateAirDensity(0f);
        float cd = Mathf.Max(0.001f, dragCoefficient);
        float area = Mathf.Max(0.1f, frontalAreaSref);
        float quadraticCoeff = 0.5f * rhoSeaLevel * cd * area; // A in Av^2 + Bv - C = 0
        float massKg = Mathf.Max(1f, shipWeightTons * 1000f);
        float linearDamping = (rb != null) ? Mathf.Max(0f, rb.linearDamping) : 0f;
        float linearCoeff = linearDamping * massKg; // B term representing Unity damping
        float maxSpeedMPS;

        if (quadraticCoeff <= 1e-6f)
        {
            // No meaningful aero drag: thrust balances only damping
            maxSpeedMPS = (linearCoeff > 0f)
                ? thrustForce / linearCoeff
                : thrustForce / massKg;
        }
        else
        {
            float discriminant = linearCoeff * linearCoeff + 4f * quadraticCoeff * thrustForce;
            maxSpeedMPS = (-linearCoeff + Mathf.Sqrt(discriminant)) / (2f * quadraticCoeff);
        }
        _maxSpeedKnots = maxSpeedMPS * MPS_TO_KNOTS;
    }
    
    // ========== ATTITUDE CONTROL METHODS ==========
    // Hermeneutic principle: Attitude is PURELY VISUAL ORIENTATION.
    // Roll/pitch/yaw do NOT affect velocity vector - ship can climb while pitched nose-down.
    // This decouples appearance (attitude) from Being (trajectory).
    
    /// <summary>
    /// Update attitude tracking - smoothly interpolate toward target attitude.
    /// CRITICAL: Yaw rotates around GLOBAL Y-axis, while roll/pitch are local to ship.
    /// </summary>
    void UpdateAttitudeTracking()
    {
        // Smoothly rotate toward target attitude at configured speed
        float rollDelta = rollRotationSpeed * Time.fixedDeltaTime;
        float pitchDelta = pitchRotationSpeed * Time.fixedDeltaTime;
        float yawDelta = yawRotationSpeed * Time.fixedDeltaTime;
        
        // Roll and pitch use MoveTowards (limited range)
        _currentRollDegrees = Mathf.MoveTowards(_currentRollDegrees, _targetRollDegrees, rollDelta);
        _currentPitchDegrees = Mathf.MoveTowards(_currentPitchDegrees, _targetPitchDegrees, pitchDelta);
        
        // Yaw: continuous rotation without wrapping or shortest-path behavior
        // Simply move current toward target at fixed speed, preserving rotation direction
        if (_currentYawDegrees < _targetYawDegrees)
        {
            _currentYawDegrees = Mathf.Min(_currentYawDegrees + yawDelta, _targetYawDegrees);
        }
        else if (_currentYawDegrees > _targetYawDegrees)
        {
            _currentYawDegrees = Mathf.Max(_currentYawDegrees - yawDelta, _targetYawDegrees);
        }
        
        // Build rotation using quaternions to ensure yaw is always around GLOBAL Y-axis
        // Order: Yaw (global Y) -> Pitch (local X) -> Roll (local Z)
        // Note: Roll is negated because positive roll (right wing down) should rotate clockwise,
        // but Unity's Z-axis rotation is counter-clockwise positive
        Quaternion yawRotation = Quaternion.AngleAxis(_currentYawDegrees, Vector3.up);
        Quaternion pitchRotation = Quaternion.AngleAxis(_currentPitchDegrees, Vector3.right);
        Quaternion rollRotation = Quaternion.AngleAxis(-_currentRollDegrees, Vector3.forward);
        
        // Apply: Global yaw first, then local pitch and roll
        transform.rotation = yawRotation * pitchRotation * rollRotation;
        
        if (debugLog && Time.frameCount % 60 == 0)
        {
            float rollDiff = Mathf.Abs(_currentRollDegrees - _targetRollDegrees);
            float pitchDiff = Mathf.Abs(_currentPitchDegrees - _targetPitchDegrees);
            float yawDiff = Mathf.Abs(_currentYawDegrees - _targetYawDegrees);
            if (rollDiff > 0.1f || pitchDiff > 0.1f || yawDiff > 0.1f)
            {
                FileLogger.Log($"{gameObject.name} attitude - Roll: {_currentRollDegrees:F1}°→{_targetRollDegrees:F1}°, Pitch: {_currentPitchDegrees:F1}°→{_targetPitchDegrees:F1}°, Yaw: {_currentYawDegrees:F1}°→{_targetYawDegrees:F1}°", "ShipCharacteristics");
            }
        }
    }
    
    /// <summary>
    /// Set ship roll attitude target (visual orientation only, no velocity change).
    /// Ship will smoothly rotate to this angle at rollRotationSpeed.
    /// Positive = right wing down, Negative = left wing down.
    /// </summary>
    public void SetRollAttitude(float rollDegrees)
    {
        _targetRollDegrees = rollDegrees;
        
        if (debugLog && Time.frameCount % 60 == 0)
        {
            FileLogger.Log($"{gameObject.name} roll target set to {rollDegrees:F1}° (current: {_currentRollDegrees:F1}°, velocity unchanged)", "ShipCharacteristics");
        }
    }
    
    /// <summary>
    /// Set ship pitch attitude target (visual orientation only, no velocity change).
    /// Ship will smoothly rotate to this angle at pitchRotationSpeed.
    /// Positive = nose up, Negative = nose down.
    /// </summary>
    public void SetPitchAttitude(float pitchDegrees)
    {
        _targetPitchDegrees = pitchDegrees;
        
        if (debugLog && Time.frameCount % 60 == 0)
        {
            FileLogger.Log($"{gameObject.name} pitch target set to {pitchDegrees:F1}° (current: {_currentPitchDegrees:F1}°, velocity unchanged)", "ShipCharacteristics");
        }
    }
    
    /// <summary>
    /// Set ship yaw attitude target (visual orientation only, no velocity change).
    /// Ship will smoothly rotate to this angle at yawRotationSpeed.
    /// </summary>
    public void SetYawAttitude(float yawDegrees)
    {
        _targetYawDegrees = yawDegrees;
        
        if (debugLog && Time.frameCount % 60 == 0)
        {
            FileLogger.Log($"{gameObject.name} yaw target set to {yawDegrees:F1}° (current: {_currentYawDegrees:F1}°, velocity unchanged)", "ShipCharacteristics");
        }
    }
    
    /// <summary>
    /// Set complete attitude targets (roll, pitch, yaw) in one operation.
    /// Ship will smoothly rotate to these angles at configured speeds.
    /// </summary>
    public void SetAttitude(float rollDegrees, float pitchDegrees, float yawDegrees)
    {
        _targetRollDegrees = rollDegrees;
        _targetPitchDegrees = pitchDegrees;
        _targetYawDegrees = yawDegrees;
        
        if (debugLog && Time.frameCount % 60 == 0)
        {
            FileLogger.Log($"{gameObject.name} attitude targets set to Roll:{rollDegrees:F1}° Pitch:{pitchDegrees:F1}° Yaw:{yawDegrees:F1}° (velocity unchanged)", "ShipCharacteristics");
        }
    }
    
    /// <summary>
    /// Reset attitude to level flight (zero roll, pitch, yaw).
    /// </summary>
    public void ResetAttitude()
    {
        SetAttitude(0f, 0f, 0f);
        
        if (debugLog)
        {
            FileLogger.Log($"{gameObject.name} attitude reset to level flight", "ShipCharacteristics");
        }
    }
    
    /// <summary>
    /// Normalize angle to -180° to +180° range.
    /// </summary>
    float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
