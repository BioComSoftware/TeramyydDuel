using UnityEngine;

/// <summary>
/// Wing Leveler Indicator - Shows ship's roll and pitch attitude.
/// Similar to an airplane's artificial horizon / attitude indicator.
/// 
/// Hermeneutic principle: Visual disclosure of ship's spatial orientation.
/// The wing leveler image represents the ship's current attitude (NOT trajectory).
/// 
/// ROLL: Wing image rotates to match ship roll
///   - Ship rolled 30° left → Image rotates 30° counter-clockwise
///   - Ship rolled 10° right → Image rotates 10° clockwise
/// 
/// PITCH: Wing image translates vertically to match ship pitch
///   - Ship pitched 30° nose-up → Image moves up (e.g., 90 pixels at 3px/degree)
///   - Ship pitched 30° nose-down → Image moves down (e.g., 90 pixels at 3px/degree)
/// 
/// Reads attitude from ShipCharacteristics.currentRollDegrees and currentPitchDegrees.
/// </summary>
[AddComponentMenu("Teramyyd/HUD/Wing Leveler Indicator")]
public class WingLevelerIndicator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The RectTransform of the wing leveler image (rotates and moves vertically).")]
    public RectTransform wingImageTransform;
    
    [Tooltip("Reference to ship's ShipCharacteristics component. Leave empty to auto-discover.")]
    public ShipCharacteristics shipCharacteristics;
    
    [Header("Pitch Configuration")]
    [Tooltip("Pixels to move vertically per degree of pitch. E.g., 3 means 30° pitch = 90 pixels movement.")]
    public float pixelsPerPitchDegree = 3f;
    
    [Header("Smoothing")]
    [Tooltip("How quickly indicator responds (0 = instant, higher = smoother). Typical: 5-10.")]
    public float dampingFactor = 8f;
    
    [Header("Debug")]
    [Tooltip("Enable debug logging.")]
    public bool debugLog = false;
    
    [Header("Status (Read-Only)")]
    [SerializeField] private float currentShipRollDegrees;
    [SerializeField] private float currentShipPitchDegrees;
    [SerializeField] private float currentImageRotation;
    [SerializeField] private Vector2 currentImagePosition;
    
    private Vector2 wingImageStartPosition;
    private float targetImageRotation;
    private Vector2 targetImagePosition;
    
    private void Start()
    {
        // Auto-find ShipCharacteristics if not assigned
        if (shipCharacteristics == null)
        {
            shipCharacteristics = FindFirstObjectByType<ShipCharacteristics>();
            
            if (shipCharacteristics == null)
            {
                Debug.LogError($"WingLevelerIndicator on {gameObject.name}: Cannot find ShipCharacteristics in scene!");
                enabled = false;
                return;
            }
            
            if (debugLog)
            {
                Debug.Log($"WingLevelerIndicator on {gameObject.name}: Auto-discovered ShipCharacteristics on {shipCharacteristics.gameObject.name}");
            }
        }
        
        if (wingImageTransform == null)
        {
            Debug.LogError($"WingLevelerIndicator on {gameObject.name}: wingImageTransform not assigned!");
            enabled = false;
            return;
        }
        
        // Store initial position (centered in attitude indicator)
        wingImageStartPosition = wingImageTransform.anchoredPosition;
        currentImagePosition = wingImageStartPosition;
        currentImageRotation = 0f;
        
        if (debugLog)
        {
            Debug.Log($"WingLevelerIndicator initialized: Start position = {wingImageStartPosition}, Pixels per degree = {pixelsPerPitchDegree}");
        }
    }
    
    private void Update()
    {
        if (shipCharacteristics == null || wingImageTransform == null)
            return;
        
        // Read current attitude from ship
        currentShipRollDegrees = shipCharacteristics.currentRollDegrees;
        currentShipPitchDegrees = shipCharacteristics.currentPitchDegrees;
        
        // ROLL: Rotate wing image to match ship roll
        // Ship rolled right (+) → Image rotates clockwise (+)
        // Ship rolled left (-) → Image rotates counter-clockwise (-)
        // Note: Unity UI uses Z-axis rotation where positive = counter-clockwise
        // So we negate the roll to make positive roll = clockwise rotation
        targetImageRotation = -currentShipRollDegrees;
        
        // PITCH: Move wing image vertically
        // Ship pitched nose-up (+) → Image moves UP (+Y)
        // Ship pitched nose-down (-) → Image moves DOWN (-Y)
        // Use ship's max pitch limits from ShipCharacteristics
        float maxPitch = Mathf.Max(shipCharacteristics.maxPitchUpDegrees, shipCharacteristics.maxPitchDownDegrees);
        float clampedPitch = Mathf.Clamp(currentShipPitchDegrees, -maxPitch, maxPitch);
        
        // Negate pitch to correct direction: positive pitch (nose up) = move image UP
        float pitchOffsetPixels = -clampedPitch * pixelsPerPitchDegree;
        
        targetImagePosition = wingImageStartPosition + new Vector2(0f, pitchOffsetPixels);
        
        // Apply smoothing (damping)
        if (dampingFactor > 0f)
        {
            currentImageRotation = Mathf.LerpAngle(currentImageRotation, targetImageRotation, Time.deltaTime * dampingFactor);
            currentImagePosition = Vector2.Lerp(currentImagePosition, targetImagePosition, Time.deltaTime * dampingFactor);
        }
        else
        {
            // Instant response (no damping)
            currentImageRotation = targetImageRotation;
            currentImagePosition = targetImagePosition;
        }
        
        // Apply transformations to wing image
        wingImageTransform.localRotation = Quaternion.Euler(0f, 0f, currentImageRotation);
        wingImageTransform.anchoredPosition = currentImagePosition;
        
        // Debug output
        if (debugLog && Time.frameCount % 60 == 0) // Every ~1 second at 60 FPS
        {
            Debug.Log($"WingLeveler: Roll={currentShipRollDegrees:F1}° → Rotation={currentImageRotation:F1}°, " +
                      $"Pitch={currentShipPitchDegrees:F1}° → Y Offset={pitchOffsetPixels:F1}px");
        }
    }
    
    /// <summary>
    /// Reset wing leveler to center position (level flight).
    /// Useful for testing or when resetting ship attitude.
    /// </summary>
    public void ResetToCenter()
    {
        currentImageRotation = 0f;
        currentImagePosition = wingImageStartPosition;
        targetImageRotation = 0f;
        targetImagePosition = wingImageStartPosition;
        
        if (wingImageTransform != null)
        {
            wingImageTransform.localRotation = Quaternion.identity;
            wingImageTransform.anchoredPosition = wingImageStartPosition;
        }
        
        if (debugLog)
        {
            Debug.Log($"WingLevelerIndicator reset to center position");
        }
    }
    
    /// <summary>
    /// Manually set attitude values (for testing without ship).
    /// </summary>
    public void SetTestAttitude(float rollDegrees, float pitchDegrees)
    {
        currentShipRollDegrees = rollDegrees;
        currentShipPitchDegrees = pitchDegrees;
        
        targetImageRotation = -rollDegrees;
        float maxPitch = Mathf.Max(shipCharacteristics.maxPitchUpDegrees, shipCharacteristics.maxPitchDownDegrees);
        float clampedPitch = Mathf.Clamp(pitchDegrees, -maxPitch, maxPitch);
        float pitchOffsetPixels = clampedPitch * pixelsPerPitchDegree;
        targetImagePosition = wingImageStartPosition + new Vector2(0f, pitchOffsetPixels);
        
        // Apply immediately (no damping)
        currentImageRotation = targetImageRotation;
        currentImagePosition = targetImagePosition;
        
        wingImageTransform.localRotation = Quaternion.Euler(0f, 0f, currentImageRotation);
        wingImageTransform.anchoredPosition = currentImagePosition;
        
        if (debugLog)
        {
            Debug.Log($"WingLeveler test attitude set: Roll={rollDegrees}°, Pitch={pitchDegrees}° → Offset={pitchOffsetPixels}px");
        }
    }
}
