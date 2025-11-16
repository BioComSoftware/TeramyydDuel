using UnityEngine;

/// <summary>
/// Vertical Speed Indicator (VSI) - Single rotating hand showing climb/descent rate.
/// Ontological structure: Disclosure of ship's temporal trajectory (rising/falling).
/// Maps vertical velocity (-20 to +20 m/s) to clock positions.
/// 12 o'clock = 0 m/s (level flight)
/// Right side (3 o'clock) = positive (climbing)
/// Left side (9 o'clock) = negative (descending)
/// Based on real aircraft VSI/variometer.
/// </summary>
[AddComponentMenu("Teramyyd/HUD/Vertical Speed Indicator")]
public class VerticalSpeedIndicator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The RectTransform of the VSI needle/hand (will rotate around Z-axis).")]
    public RectTransform needleTransform;
    
    [Tooltip("Reference to ship's ShipCharacteristics component.")]
    public ShipCharacteristics shipCharacteristics;
    
    [Header("Configuration")]
    [Tooltip("Maximum climb rate shown on gauge (m/s). Typically ±10 or ±20.")]
    public float maxClimbRateMPS = 20f;
    
    [Tooltip("Rotation at zero vertical speed (0° = 12 o'clock).")]
    public float zeroRotationDegrees = 0f;
    
    [Tooltip("Degrees to rotate for maximum climb rate (typically 180° = half circle to the right).")]
    public float maxClimbRotationDegrees = 180f;
    
    [Tooltip("Degrees to rotate for maximum descent rate (typically -180° = half circle to the left).")]
    public float maxDescentRotationDegrees = -180f;
    
    [Header("Smoothing")]
    [Tooltip("How quickly the needle moves (0 = instant, higher = smoother). VSI is typically laggy.")]
    public float dampingFactor = 3f;
    
    [Header("Debug")]
    public bool debugLog = false;
    
    [Header("Status")]
    [SerializeField] private float currentVerticalSpeedMPS;
    [SerializeField] private float currentRotation;
    
    private float targetRotation;
    
    private void Start()
    {
        // Auto-find ShipCharacteristics if not assigned
        if (shipCharacteristics == null)
        {
            shipCharacteristics = FindFirstObjectByType<ShipCharacteristics>();
        }
        
        if (needleTransform == null)
        {
            Debug.LogError($"VerticalSpeedIndicator on {gameObject.name}: needleTransform not assigned!");
        }
        
        if (shipCharacteristics == null)
        {
            Debug.LogError($"VerticalSpeedIndicator on {gameObject.name}: Cannot find ShipCharacteristics!");
        }
        
        // Initialize needle to zero position
        currentRotation = zeroRotationDegrees;
        targetRotation = zeroRotationDegrees;
        if (needleTransform != null)
        {
            needleTransform.localRotation = Quaternion.Euler(0f, 0f, -currentRotation);
        }
        
        if (debugLog)
        {
            FileLogger.Log($"VSI Initialized - Zero: {zeroRotationDegrees}°, Climb: {maxClimbRotationDegrees}°, Descent: {maxDescentRotationDegrees}°", "VerticalSpeedIndicator");
        }
    }
    
    private void Update()
    {
        if (shipCharacteristics == null || needleTransform == null)
            return;
        
        // Get current vertical velocity from ship
        currentVerticalSpeedMPS = shipCharacteristics.verticalVelocityMPS;
        
        // Calculate target rotation
        // Clamp to max range
        float clampedVS = Mathf.Clamp(currentVerticalSpeedMPS, -maxClimbRateMPS, maxClimbRateMPS);
        
        if (clampedVS >= 0f)
        {
            // Climbing - interpolate from zero to max climb rotation
            float normalizedClimb = clampedVS / maxClimbRateMPS; // 0.0 to 1.0
            targetRotation = Mathf.Lerp(zeroRotationDegrees, maxClimbRotationDegrees, normalizedClimb);
        }
        else
        {
            // Descending - interpolate from zero to max descent rotation
            float normalizedDescent = Mathf.Abs(clampedVS) / maxClimbRateMPS; // 0.0 to 1.0
            targetRotation = Mathf.Lerp(zeroRotationDegrees, maxDescentRotationDegrees, normalizedDescent);
        }
        
        // Smooth rotation (VSI in real aircraft has lag)
        if (dampingFactor > 0f)
        {
            currentRotation = Mathf.Lerp(currentRotation, targetRotation, Time.deltaTime * dampingFactor);
        }
        else
        {
            currentRotation = targetRotation;
        }
        
        // Apply rotation (negative for UI coordinate system)
        needleTransform.localRotation = Quaternion.Euler(0f, 0f, -currentRotation);
        
        // Debug logging
        if (debugLog && Time.frameCount % 60 == 0) // Log once per second at 60fps
        {
            FileLogger.Log($"VSI: verticalSpeed={currentVerticalSpeedMPS:F2} m/s, targetRot={targetRotation:F1}°, currentRot={currentRotation:F1}°, clampedVS={clampedVS:F2}, appliedRot={-currentRotation:F1}°, settings(zero={zeroRotationDegrees}°, climb={maxClimbRotationDegrees}°, desc={maxDescentRotationDegrees}°)", "VerticalSpeedIndicator");
        }
    }
    
    /// <summary>
    /// Set custom vertical speed value (for testing or external control).
    /// </summary>
    public void SetVerticalSpeed(float metersPerSecond)
    {
        currentVerticalSpeedMPS = metersPerSecond;
    }
}
