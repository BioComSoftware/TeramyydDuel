using UnityEngine;

/// <summary>
/// Altimeter - Three rotating hands for 10s, 100s, and 1000s of meters.
/// Ontological structure: Temporal disclosure of ship's altitude through layered hands.
/// Each hand uses 0-9 scale (0 at top) and completes one rotation per decade.
/// - 10s hand: 0-100m (0 at top = 0-9m, position 1 = 10-19m, etc.)
/// - 100s hand: 0-1000m (0 at top = 0-99m, position 1 = 100-199m, etc.)
/// - 1000s hand: 0-10000m (0 at top = 0-999m, position 1 = 1000-1999m, etc.)
/// Based on real aircraft altimeter with 0-9 scale.
/// 
/// Example: 2,456 meters
/// - 1000s hand: between 2 and 3 (at ~2.456 position)
/// - 100s hand: between 4 and 5 (at ~4.56 position)
/// - 10s hand: between 5 and 6 (at ~5.6 position)
/// </summary>
[AddComponentMenu("Teramyyd/HUD/Altimeter Indicator")]
public class AltimeterIndicator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The RectTransform for the 10s of meters hand (fastest moving).")]
    public RectTransform tensHandTransform;
    
    [Tooltip("The RectTransform for the 100s of meters hand.")]
    public RectTransform hundredsHandTransform;
    
    [Tooltip("The RectTransform for the 1000s of meters hand (slowest moving).")]
    public RectTransform thousandsHandTransform;
    
    [Tooltip("Reference to ship's ShipCharacteristics component.")]
    public ShipCharacteristics shipCharacteristics;
    
    [Header("Configuration")]
    [Tooltip("Rotation at zero altitude (0° = top/12 o'clock = 0 position).")]
    public float zeroRotationDegrees = 0f;
    
    [Tooltip("Does the needle rotate clockwise (true) or counter-clockwise (false)?")]
    public bool rotateClockwise = true;
    
    [Header("Smoothing")]
    [Tooltip("How quickly the needles move (0 = instant, higher = smoother).")]
    public float dampingFactor = 5f;
    
    [Header("Status")]
    [SerializeField] private float currentAltitudeMeters;
    [SerializeField] private float tensRotation;
    [SerializeField] private float hundredsRotation;
    [SerializeField] private float thousandsRotation;
    
    private float targetTensRotation;
    private float targetHundredsRotation;
    private float targetThousandsRotation;
    
    private void Start()
    {
        // Auto-find ShipCharacteristics if not assigned
        if (shipCharacteristics == null)
        {
            shipCharacteristics = FindFirstObjectByType<ShipCharacteristics>();
        }
        
        if (tensHandTransform == null || hundredsHandTransform == null || thousandsHandTransform == null)
        {
            Debug.LogError($"AltimeterIndicator on {gameObject.name}: One or more hand transforms not assigned!");
        }
        
        if (shipCharacteristics == null)
        {
            Debug.LogError($"AltimeterIndicator on {gameObject.name}: Cannot find ShipCharacteristics!");
        }
    }
    
    private void Update()
    {
        if (shipCharacteristics == null)
            return;
        
        // Get current altitude from ship
        currentAltitudeMeters = shipCharacteristics.currentAltitude;
        
        // Calculate rotations for each hand on 0-9 scale
        // Each position represents one digit (0-9), so we map to 0-10 range (10 positions)
        
        // 10s hand: 0-100 meters maps to 0-9 scale (0-360°)
        // Position 0 = 0-9m, Position 1 = 10-19m, Position 9 = 90-99m
        float tens = (currentAltitudeMeters % 100f) / 10f; // 0.0 to 10.0 (maps to 0-9 positions)
        
        // 100s hand: 0-1000 meters maps to 0-9 scale (0-360°)
        // Position 0 = 0-99m, Position 1 = 100-199m, Position 9 = 900-999m
        float hundreds = (currentAltitudeMeters % 1000f) / 100f; // 0.0 to 10.0
        
        // 1000s hand: 0-10000 meters maps to 0-9 scale (0-360°)
        // Position 0 = 0-999m, Position 1 = 1000-1999m, Position 9 = 9000-9999m
        float thousands = (currentAltitudeMeters % 10000f) / 1000f; // 0.0 to 10.0
        
        // Convert to degrees (0-10 maps to 0-360°)
        // Note: We use 10 positions for 0-9 scale (position between 9 and 0)
        if (rotateClockwise)
        {
            targetTensRotation = zeroRotationDegrees + (tens * 36f); // 36° per digit (360/10)
            targetHundredsRotation = zeroRotationDegrees + (hundreds * 36f);
            targetThousandsRotation = zeroRotationDegrees + (thousands * 36f);
        }
        else
        {
            targetTensRotation = zeroRotationDegrees - (tens * 36f);
            targetHundredsRotation = zeroRotationDegrees - (hundreds * 36f);
            targetThousandsRotation = zeroRotationDegrees - (thousands * 36f);
        }
        
        // Smooth rotations
        if (dampingFactor > 0f)
        {
            tensRotation = Mathf.LerpAngle(tensRotation, targetTensRotation, Time.deltaTime * dampingFactor);
            hundredsRotation = Mathf.LerpAngle(hundredsRotation, targetHundredsRotation, Time.deltaTime * dampingFactor);
            thousandsRotation = Mathf.LerpAngle(thousandsRotation, targetThousandsRotation, Time.deltaTime * dampingFactor);
        }
        else
        {
            tensRotation = targetTensRotation;
            hundredsRotation = targetHundredsRotation;
            thousandsRotation = targetThousandsRotation;
        }
        
        // Apply rotations
        if (tensHandTransform != null)
            tensHandTransform.localRotation = Quaternion.Euler(0f, 0f, -tensRotation);
        
        if (hundredsHandTransform != null)
            hundredsHandTransform.localRotation = Quaternion.Euler(0f, 0f, -hundredsRotation);
        
        if (thousandsHandTransform != null)
            thousandsHandTransform.localRotation = Quaternion.Euler(0f, 0f, -thousandsRotation);
    }
    
    /// <summary>
    /// Set custom altitude value (for testing or external control).
    /// </summary>
    public void SetAltitude(float meters)
    {
        currentAltitudeMeters = meters;
    }
}
