using UnityEngine;

/// <summary>
/// Vertical Speed Indicator (VSI) - Displays rate of climb/descent.
/// Calculates vertical speed from Y-axis position changes and teleports needle to position.
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
        logWriter.WriteLine($"Zero Marker: {zeroMarkerDegrees}°");
        logWriter.WriteLine($"Degrees per m/s: {DEGREES_PER_MPS}°");
        
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
        
        // PERMANENTLY set needle to zero marker position at start
        if (needleTransform != null)
        {
            needleTransform.localRotation = Quaternion.Euler(0f, 0f, zeroMarkerDegrees);
            logWriter.WriteLine($"Needle PERMANENTLY set to {zeroMarkerDegrees}° (zero marker)");
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
        
        // STEP 1: Get current Y position (needle is NOT moved during this step)
        float currentY = shipCharacteristics.transform.position.y;
        
        // STEP 2: Determine altitude change since last frame (in meters)
        float deltaY = currentY - previousYPosition;
        
        // STEP 3: Calculate vertical speed in meters per second (needle still NOT moved)
        float verticalSpeedMPS = deltaY / Time.deltaTime;
        
        // STEP 4: Calculate rotation based on vertical speed (still just calculating, NOT moving needle)
        // Positive (climb) = clockwise, Negative (descent) = counter-clockwise
        // +10 m/s = +90°, -10 m/s = -90°
        float rotationFromSpeed = verticalSpeedMPS * DEGREES_PER_MPS;
        
        // STEP 5: Calculate where needle SHOULD be pointing (needle completely unaffected during calculation)
        // If zero marker = 270° (left):
        //   At 0 m/s: Should point at 270° (left)
        //   At +10 m/s: Should point at 270° + 90° = 360° = 0° (up) - clockwise
        //   At -10 m/s: Should point at 270° - 90° = 180° (down) - counter-clockwise
        float targetRotationDegrees = zeroMarkerDegrees + rotationFromSpeed;
        
        // Log calculation (needle still hasn't moved)
        logWriter.WriteLine($"Frame {Time.frameCount}: DeltaY={deltaY:F3}m, Speed={verticalSpeedMPS:F2}m/s, Target={targetRotationDegrees:F1}°");
        
        // STEP 6: PERMANENTLY teleport needle to calculated position
        // This is the ONLY place the needle moves - instant teleportation to target
        needleTransform.localRotation = Quaternion.Euler(0f, 0f, targetRotationDegrees);
        
        logWriter.WriteLine($"         Needle PERMANENTLY teleported to {targetRotationDegrees:F1}°");
        
        // STEP 7: Update previous position for next frame's delta calculation
        previousYPosition = currentY;
        
        // Next Update() will repeat: calculate delta → calculate speed → calculate target → teleport
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
