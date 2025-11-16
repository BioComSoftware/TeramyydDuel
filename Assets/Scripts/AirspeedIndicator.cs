using UnityEngine;

/// <summary>
/// Airspeed Indicator - Single rotating hand pointing to knots on gauge face.
/// Ontological structure: Ready-to-hand disclosure of ship's velocity magnitude.
/// Maps airspeed (0-10+ knots) to 0-9 scale positions (0° = top/12 o'clock = 0).
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
    [Tooltip("Maximum airspeed value on the gauge (typically 10 knots for 0-9 scale, or custom max).")]
    public float maxAirspeedKnots = 10f;
    
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
    }
    
    private void Update()
    {
        if (shipCharacteristics == null || needleTransform == null)
            return;
        
        // Get current airspeed from ship
        currentAirspeedKnots = shipCharacteristics.currentSpeedKnots;
        
        // Calculate target rotation
        // Map airspeed (0 to maxAirspeedKnots) to full rotation (0° to 360°)
        float normalizedSpeed = Mathf.Clamp01(currentAirspeedKnots / maxAirspeedKnots);
        float rotationRange = 360f;
        
        if (rotateClockwise)
        {
            targetRotation = zeroRotationDegrees + (normalizedSpeed * rotationRange);
        }
        else
        {
            targetRotation = zeroRotationDegrees - (normalizedSpeed * rotationRange);
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
    }
    
    /// <summary>
    /// Set custom airspeed value (for testing or external control).
    /// </summary>
    public void SetAirspeed(float knots)
    {
        currentAirspeedKnots = knots;
    }
}
