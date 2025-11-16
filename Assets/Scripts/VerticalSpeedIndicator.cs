using UnityEngine;

/// <summary>
/// Vertical Speed Indicator (VSI) - Displays rate of climb/descent.
/// Reads actual vertical velocity from ship's Rigidbody.
/// </summary>
[AddComponentMenu("Teramyyd/HUD/Vertical Speed Indicator")]
public class VerticalSpeedIndicator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The RectTransform of the VSI needle (will rotate around Z-axis).")]
    public RectTransform needleTransform;
    
    [Tooltip("Reference to ship's ShipCharacteristics component.")]
    public ShipCharacteristics shipCharacteristics;
    
    [Header("Configuration")]
    [Tooltip("The rotation in degrees where the zero marker is on the gauge (e.g., 270 = left, 0 = up).")]
    public float zeroMarkerDegrees = 270f;
    
    [Tooltip("Smoothing factor for needle movement (higher = smoother/slower).")]
    public float dampingFactor = 10f;
    
    // VSI scale: 10 m/s climb = 90° clockwise from zero marker
    // This means: degreesPerMPS = 90 / 10 = 9° per m/s
    private const float DEGREES_PER_MPS = 9f;
    
    // Track previous Y position for delta calculation
    private float previousYPosition;
    private bool isInitialized = false;
    private System.IO.StreamWriter logWriter;
    
    private void Start()
    {
        // Initialize log file
        string logPath = System.IO.Path.Combine(Application.dataPath, "VSI_Log.txt");
        logWriter = new System.IO.StreamWriter(logPath, false); // false = overwrite
        logWriter.AutoFlush = true;
        logWriter.WriteLine($"=== VSI Log Started at {System.DateTime.Now} ===");
        logWriter.WriteLine($"Zero Marker set to: {zeroMarkerDegrees}°");
        
        // Auto-find ShipCharacteristics if not assigned
        if (shipCharacteristics == null)
        {
            shipCharacteristics = FindFirstObjectByType<ShipCharacteristics>();
        }
        
        // Validate references
        if (needleTransform == null)
        {
            Debug.LogError($"VerticalSpeedIndicator: needleTransform not assigned!");
            logWriter.WriteLine("ERROR: needleTransform not assigned!");
        }
        
        if (shipCharacteristics == null)
        {
            Debug.LogError($"VerticalSpeedIndicator: Cannot find ShipCharacteristics!");
            logWriter.WriteLine("ERROR: Cannot find ShipCharacteristics!");
        }
        
        // Set needle to zero marker position
        if (needleTransform != null)
        {
            needleTransform.localRotation = Quaternion.Euler(0f, 0f, zeroMarkerDegrees);
            logWriter.WriteLine($"Needle initialized to {zeroMarkerDegrees}° (zero marker). Actual rotation: {needleTransform.localEulerAngles.z}°");
        }
        
        // Initialize previous position
        if (shipCharacteristics != null)
        {
            previousYPosition = shipCharacteristics.transform.position.y;
        }
        
        isInitialized = true;
    }
    
    private void Update()
    {
        if (needleTransform == null || shipCharacteristics == null || !isInitialized)
            return;
        
        // Get current Y position
        float currentY = shipCharacteristics.transform.position.y;
        
        // Calculate altitude change since last frame (in meters)
        float deltaY = currentY - previousYPosition;
        
        // Convert to meters per second
        float verticalSpeedMPS = deltaY / Time.deltaTime;
        
        // Calculate rotation from zero marker
        // Positive (climb) = clockwise, Negative (descent) = counter-clockwise
        // +10 m/s = +90°, -10 m/s = -90°
        float speedRotation = verticalSpeedMPS * DEGREES_PER_MPS;
        
        // Target rotation: zero marker + rotation based on speed
        // If zero marker = 270° (left):
        //   At 0 m/s: 270° (left)
        //   At +10 m/s: 270° + 90° = 360° = 0° (up) - clockwise
        //   At -10 m/s: 270° - 90° = 180° (down) - counter-clockwise
        float targetRotation = zeroMarkerDegrees + speedRotation;
        
        // Get current rotation
        float currentRotation = needleTransform.localEulerAngles.z;
        
        // Log every 60 frames to file
        if (Time.frameCount % 60 == 0)
        {
            logWriter.WriteLine($"Frame {Time.frameCount}: Speed={verticalSpeedMPS:F2} m/s, Rotation={speedRotation:F1}°, Current={currentRotation:F1}°, Target={targetRotation:F1}°");
        }
        
        // Smoothly interpolate to target rotation
        float smoothedRotation = Mathf.LerpAngle(currentRotation, targetRotation, Time.deltaTime * dampingFactor);
        
        // Apply rotation
        needleTransform.localRotation = Quaternion.Euler(0f, 0f, smoothedRotation);
        
        // Update previous position for next frame
        previousYPosition = currentY;
    }
    
    private void OnDestroy()
    {
        // Close log file
        if (logWriter != null)
        {
            logWriter.WriteLine($"=== VSI Log Ended at {System.DateTime.Now} ===");
            logWriter.Close();
        }
    }
}
