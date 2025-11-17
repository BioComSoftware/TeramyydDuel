using UnityEngine;

/// <summary>
/// Airspeed Indicator - Single rotating hand pointing to knots on gauge face.
/// Displays HORIZONTAL airspeed only (X-Z plane movement, ignoring vertical climb/descent).
/// This represents movement across the ground/battlefield, not total 3D velocity.
/// Needle rotates continuously: 10 knots = 1 full rotation (matching 0-9 gauge scale).
/// No speed limitation - needle can rotate multiple times for higher speeds.
/// Based on real aircraft airspeed indicator behavior.
/// </summary>
[AddComponentMenu("Teramyyd/HUD/Airspeed Indicator")]
public class AirspeedIndicator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The RectTransform of the airspeed needle/hand (will rotate around Z-axis).")]
    public RectTransform needleTransform;
    
    [Tooltip("Reference to ship's ShipCharacteristics component.")]
    public ShipCharacteristics shipCharacteristics;
    
    [Header("Configuration")]
    [Tooltip("Knots per dial increment (default 10, so 10kt = '1', 20kt = '2', etc.). Needle rotates continuously for unlimited speed.")]
    public float knotsPerDialIncrement = 10f;
    
    [Tooltip("Rotation at zero airspeed (0° = top/12 o'clock).")]
    public float zeroRotationDegrees = 0f;
    
    [Tooltip("Does the needle rotate clockwise (true) or counter-clockwise (false)?")]
    public bool rotateClockwise = true;
    
    [Header("Smoothing")]
    [Tooltip("How quickly the needle moves to new position (0 = instant, higher = smoother).")]
    public float dampingFactor = 5f;
    
    [Header("Status")]
    [SerializeField] private float currentAirspeedKnots;
    [SerializeField] private float currentRotation;
    
    [Header("Debug")]
    public bool debugLog = false;
    
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
            Debug.LogError($"AirspeedIndicator on {gameObject.name}: needleTransform not assigned!");
        }
        
        if (shipCharacteristics == null)
        {
            Debug.LogError($"AirspeedIndicator on {gameObject.name}: Cannot find ShipCharacteristics!");
        }
        
        // Initialize currentRotation to current needle position to avoid sudden jumps
        if (needleTransform != null)
        {
            currentRotation = -needleTransform.localEulerAngles.z;
        }
    }
    
    private void Update()
    {
        if (shipCharacteristics == null || needleTransform == null)
            return;
        
        // Get current HORIZONTAL airspeed from ship (X-Z plane only, no vertical component)
        currentAirspeedKnots = shipCharacteristics.horizontalSpeedKnots;
        
        // Calculate target rotation
        // Each dial number represents knotsPerDialIncrement (default 10 knots)
        // 10kt = '1', 20kt = '2', 30kt = '3', etc.
        // The 0-9 dial has 10 positions, so 36° per position
        float dialPosition = currentAirspeedKnots / knotsPerDialIncrement;
        float rotationDegrees = dialPosition * 36f; // 36° per dial increment (360° / 10 positions)
        
        if (rotateClockwise)
        {
            targetRotation = zeroRotationDegrees + rotationDegrees;
        }
        else
        {
            targetRotation = zeroRotationDegrees - rotationDegrees;
        }
        
        // Smooth rotation
        if (dampingFactor > 0f)
        {
            currentRotation = Mathf.LerpAngle(currentRotation, targetRotation, Time.deltaTime * dampingFactor);
        }
        else
        {
            currentRotation = targetRotation;
        }
        
        // Apply rotation (rotate around Z-axis for UI)
        needleTransform.localRotation = Quaternion.Euler(0f, 0f, -currentRotation);
        
        if (debugLog && Time.frameCount % 60 == 0)
        {
            FileLogger.Log($"[AirspeedIndicator] HorizontalSpeed: {currentAirspeedKnots:F2}kt, DialPosition: {dialPosition:F2}, TargetRot: {targetRotation:F1}°, CurrentRot: {currentRotation:F1}°", "AirspeedIndicator");
        }
    }
    
    /// <summary>
    /// Set custom airspeed value (for testing or external control).
    /// </summary>
    public void SetAirspeed(float knots)
    {
        currentAirspeedKnots = knots;
    }
}
