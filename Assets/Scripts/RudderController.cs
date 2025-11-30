using UnityEngine;

/// <summary>
/// RudderController: Manages airship turning via lateral force applied to nose.
/// No physical rudder - uses "sideways thrust" at bow to pivot ship around center of mass.
/// Maximum rudder angle: 45Â° (hard-coded, non-configurable for gameplay balance).
/// Force magnitude scales with rudder angle and forcePerDegree setting.
/// Physics engine handles actual turn rate based on ship mass and inertia.
/// </summary>
[AddComponentMenu("Teramyyd/Ship Systems/Rudder Controller")]
public class RudderController : MonoBehaviour
{
    [Header("Rudder Configuration")]
    [Tooltip("Force (Newtons) applied per degree of rudder angle. Developer-configurable for ship tuning.")]
    public float forcePerDegree = 100f;
    
    [Tooltip("Distance from ship center to nose where lateral force is applied (meters). Affects turning leverage.")]
    public float noseOffset = 10f;
    
    [Header("Smoothing")]
    [Tooltip("How quickly rudder angle changes to match input (0 = instant, higher = smoother).")]
    public float rudderDamping = 5f;
    
    [Header("References")]
    [Tooltip("Ship's rigidbody (auto-discovered if not set)")]
    public Rigidbody shipRigidbody;
    
    [Header("Current State")]
    [SerializeField] private float _currentRudderAngle = 0f; // -45 to +45 degrees (- = port/left, + = starboard/right)
    [SerializeField] private float _targetRudderAngle = 0f;
    [SerializeField] private float _appliedForceNewtons = 0f;
    
    [Header("Debug")]
    public bool debugLog = false;
    
    // Constants
    private const float MAX_RUDDER_ANGLE = 45f; // Hard-coded maximum - not developer-changeable
    
    public float CurrentRudderAngle => _currentRudderAngle;
    public float AppliedForceNewtons => _appliedForceNewtons;
    
    private void Start()
    {
        // Auto-find rigidbody if not assigned
        if (shipRigidbody == null)
        {
            shipRigidbody = GetComponent<Rigidbody>();
        }
        
        if (shipRigidbody == null)
        {
            Debug.LogError($"RudderController on {gameObject.name}: Cannot find Rigidbody component!");
        }
        
        if (debugLog)
        {
            FileLogger.Log($"RudderController initialized - MaxRudder: {MAX_RUDDER_ANGLE}Â°, ForcePerDegree: {forcePerDegree}N/Â°, NoseOffset: {noseOffset}m", "RudderController");
        }
    }
    
    private void FixedUpdate()
    {
        if (shipRigidbody == null)
            return;
        
        // Smooth rudder angle changes
        if (rudderDamping > 0f)
        {
            _currentRudderAngle = Mathf.Lerp(_currentRudderAngle, _targetRudderAngle, Time.fixedDeltaTime * rudderDamping);
        }
        else
        {
            _currentRudderAngle = _targetRudderAngle;
        }
        
        // Apply lateral force at nose based on rudder angle
        ApplyRudderForce();
    }
    
    /// <summary>
    /// Apply lateral force to ship's nose to create turning moment
    /// </summary>
    private void ApplyRudderForce()
    {
        if (Mathf.Abs(_currentRudderAngle) < 0.1f)
        {
            _appliedForceNewtons = 0f;
            return;
        }
        
        // Calculate force magnitude from rudder angle
        _appliedForceNewtons = Mathf.Abs(_currentRudderAngle) * forcePerDegree;
        
        // Determine lateral force direction
        // Positive rudder angle (starboard) = force to right (ship's +X local axis)
        // Negative rudder angle (port) = force to left (ship's -X local axis)
        Vector3 lateralDirection = transform.right * Mathf.Sign(_currentRudderAngle);
        
        // Calculate nose position (offset forward from center)
        Vector3 nosePosition = transform.position + (transform.forward * noseOffset);
        
        // Apply force at nose position
        Vector3 forceVector = lateralDirection * _appliedForceNewtons;
        shipRigidbody.AddForceAtPosition(forceVector, nosePosition, ForceMode.Force);
        
        if (debugLog && Time.frameCount % 60 == 0)
        {
            string direction = _currentRudderAngle > 0f ? "STARBOARD" : "PORT";
            FileLogger.Log($"Rudder: {_currentRudderAngle:F1}Â° {direction}, Force: {_appliedForceNewtons:F1}N at nose, AngularVel: {shipRigidbody.angularVelocity.y:F2} rad/s", "RudderController");
        }
    }
    
    /// <summary>
    /// Set target rudder angle (-45 to +45 degrees)
    /// Negative = Port (left turn), Positive = Starboard (right turn)
    /// </summary>
    public void SetRudderAngle(float angleDegrees)
    {
        _targetRudderAngle = Mathf.Clamp(angleDegrees, -MAX_RUDDER_ANGLE, MAX_RUDDER_ANGLE);
        
        if (debugLog)
        {
            FileLogger.Log($"Rudder commanded to {_targetRudderAngle:F1}Â° (input: {angleDegrees:F1}Â°)", "RudderController");
        }
    }
    
    /// <summary>
    /// Set rudder as normalized value (-1.0 to +1.0)
    /// -1.0 = Full Port (45Â° left), +1.0 = Full Starboard (45Â° right)
    /// </summary>
    public void SetRudderNormalized(float normalizedValue)
    {
        float angleDegrees = Mathf.Clamp(normalizedValue, -1f, 1f) * MAX_RUDDER_ANGLE;
        SetRudderAngle(angleDegrees);
    }
    
    /// <summary>
    /// Center rudder (return to 0Â°)
    /// </summary>
    public void CenterRudder()
    {
        SetRudderAngle(0f);
    }
    
    /// <summary>
    /// Quick turn commands for UI buttons
    /// </summary>
    public void HardToPort()
    {
        SetRudderAngle(-MAX_RUDDER_ANGLE);
    }
    
    public void HardToStarboard()
    {
        SetRudderAngle(MAX_RUDDER_ANGLE);
    }
    
    /// <summary>
    /// Get rudder position as normalized value (-1 to +1) for UI display
    /// </summary>
    public float GetRudderNormalized()
    {
        return _currentRudderAngle / MAX_RUDDER_ANGLE;
    }
    
    /// <summary>
    /// Get maximum rudder angle (for UI display)
    /// </summary>
    public static float GetMaxRudderAngle()
    {
        return MAX_RUDDER_ANGLE;
    }
}
