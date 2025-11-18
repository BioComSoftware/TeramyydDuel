using UnityEngine;

/// <summary>
/// ShipAttitudeController: Controls visual pitch and roll of airship.
/// CRITICAL: Attitude is PURELY COSMETIC - does NOT affect flight path, thrust direction, or physics.
/// - Forward thrust still pushes horizontally regardless of pitch
/// - Ascent/descent still moves Y-axis regardless of pitch/roll
/// - Pitch up/down tilts the ship visually but doesn't change trajectory
/// - Roll left/right tilts the ship visually but doesn't change trajectory
/// This creates the airship aesthetic where attitude and movement are independent.
/// </summary>
[AddComponentMenu("Teramyyd/Ship Systems/Ship Attitude Controller")]
public class ShipAttitudeController : MonoBehaviour
{
    [Header("Pitch Limits")]
    [Tooltip("Maximum pitch up angle (bow tilts up, positive degrees)")]
    public float maxPitchUp = 30f;
    
    [Tooltip("Maximum pitch down angle (bow tilts down, negative degrees)")]
    public float maxPitchDown = 30f;
    
    [Header("Roll Limits")]
    [Tooltip("Maximum roll angle to either side (degrees). Same limit for port and starboard.")]
    public float maxRoll = 45f;
    
    [Header("Smoothing")]
    [Tooltip("How quickly attitude changes (0 = instant, higher = smoother)")]
    public float attitudeDamping = 3f;
    
    [Header("Current Attitude")]
    [SerializeField] private float _currentPitch = 0f;  // Positive = nose up, Negative = nose down
    [SerializeField] private float _currentRoll = 0f;   // Positive = roll right, Negative = roll left
    [SerializeField] private float _targetPitch = 0f;
    [SerializeField] private float _targetRoll = 0f;
    
    [Header("Debug")]
    public bool debugLog = false;
    
    public float CurrentPitch => _currentPitch;
    public float CurrentRoll => _currentRoll;
    
    private void Start()
    {
        // Initialize current attitude from ship's rotation
        Vector3 currentEuler = transform.localEulerAngles;
        _currentPitch = NormalizeAngle(currentEuler.x);
        _currentRoll = NormalizeAngle(currentEuler.z);
        _targetPitch = _currentPitch;
        _targetRoll = _currentRoll;
        
        if (debugLog)
        {
            FileLogger.Log($"ShipAttitudeController initialized - PitchRange: [{-maxPitchDown}° to {maxPitchUp}°], RollRange: [{-maxRoll}° to {maxRoll}°]", "ShipAttitudeController");
        }
    }
    
    private void Update()
    {
        // Smooth attitude changes
        if (attitudeDamping > 0f)
        {
            _currentPitch = Mathf.Lerp(_currentPitch, _targetPitch, Time.deltaTime * attitudeDamping);
            _currentRoll = Mathf.Lerp(_currentRoll, _targetRoll, Time.deltaTime * attitudeDamping);
        }
        else
        {
            _currentPitch = _targetPitch;
            _currentRoll = _targetRoll;
        }
        
        // Apply attitude to ship transform (VISUAL ONLY - no physics)
        // Yaw (Y-axis) remains unchanged - controlled by rudder physics
        Vector3 currentEuler = transform.localEulerAngles;
        float currentYaw = NormalizeAngle(currentEuler.y);
        
        // Apply pitch (X-axis) and roll (Z-axis), preserve yaw
        transform.localRotation = Quaternion.Euler(_currentPitch, currentYaw, _currentRoll);
        
        if (debugLog && Time.frameCount % 60 == 0)
        {
            FileLogger.Log($"Attitude: Pitch {_currentPitch:F1}° (target {_targetPitch:F1}°), Roll {_currentRoll:F1}° (target {_targetRoll:F1}°)", "ShipAttitudeController");
        }
    }
    
    /// <summary>
    /// Set target pitch angle (positive = nose up, negative = nose down)
    /// </summary>
    public void SetPitch(float pitchDegrees)
    {
        // Clamp to limits
        if (pitchDegrees > 0f)
        {
            _targetPitch = Mathf.Clamp(pitchDegrees, 0f, maxPitchUp);
        }
        else
        {
            _targetPitch = Mathf.Clamp(pitchDegrees, -maxPitchDown, 0f);
        }
        
        if (debugLog)
        {
            FileLogger.Log($"Pitch commanded to {_targetPitch:F1}° (input: {pitchDegrees:F1}°)", "ShipAttitudeController");
        }
    }
    
    /// <summary>
    /// Set pitch as normalized value (-1.0 to +1.0)
    /// -1.0 = Full pitch down, +1.0 = Full pitch up
    /// </summary>
    public void SetPitchNormalized(float normalizedValue)
    {
        float clampedValue = Mathf.Clamp(normalizedValue, -1f, 1f);
        
        if (clampedValue > 0f)
        {
            // Pitch up
            _targetPitch = clampedValue * maxPitchUp;
        }
        else
        {
            // Pitch down
            _targetPitch = clampedValue * maxPitchDown;
        }
    }
    
    /// <summary>
    /// Set target roll angle (positive = roll right/starboard, negative = roll left/port)
    /// </summary>
    public void SetRoll(float rollDegrees)
    {
        _targetRoll = Mathf.Clamp(rollDegrees, -maxRoll, maxRoll);
        
        if (debugLog)
        {
            FileLogger.Log($"Roll commanded to {_targetRoll:F1}° (input: {rollDegrees:F1}°)", "ShipAttitudeController");
        }
    }
    
    /// <summary>
    /// Set roll as normalized value (-1.0 to +1.0)
    /// -1.0 = Full port (left) roll, +1.0 = Full starboard (right) roll
    /// </summary>
    public void SetRollNormalized(float normalizedValue)
    {
        _targetRoll = Mathf.Clamp(normalizedValue, -1f, 1f) * maxRoll;
    }
    
    /// <summary>
    /// Level the ship (return pitch and roll to 0°)
    /// </summary>
    public void LevelShip()
    {
        _targetPitch = 0f;
        _targetRoll = 0f;
        
        if (debugLog)
        {
            FileLogger.Log("Leveling ship - pitch and roll to 0°", "ShipAttitudeController");
        }
    }
    
    /// <summary>
    /// Get pitch as normalized value (-1 to +1) for UI display
    /// </summary>
    public float GetPitchNormalized()
    {
        if (_currentPitch > 0f)
        {
            return _currentPitch / maxPitchUp;
        }
        else
        {
            return _currentPitch / maxPitchDown;
        }
    }
    
    /// <summary>
    /// Get roll as normalized value (-1 to +1) for UI display
    /// </summary>
    public float GetRollNormalized()
    {
        return _currentRoll / maxRoll;
    }
    
    /// <summary>
    /// Normalize angle to -180 to +180 range
    /// </summary>
    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
