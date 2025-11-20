using UnityEngine;

/// <summary>
/// Attitude Indicator - Shows ship's pitch, roll, and yaw orientation.
/// Ontological structure: Being-in-space disclosure through airplane silhouette.
/// 
/// Components:
/// 1. Airplane Silhouette: Rotates for ROLL, translates vertically for PITCH
/// 2. Yaw Triangle: Translates horizontally for YAW (left/right heading deviation)
/// 
/// Based on real aircraft attitude indicator (artificial horizon).
/// The airplane symbol represents the ship's attitude relative to the horizon.
/// </summary>
[AddComponentMenu("Teramyyd/HUD/Attitude Indicator")]
public class AttitudeIndicator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The RectTransform of the airplane silhouette (rotates for roll, moves Y for pitch).")]
    public RectTransform airplaneTransform;
    
    [Tooltip("The RectTransform of the yaw triangle indicator (moves X for yaw).")]
    public RectTransform yawTriangleTransform;
    
    [Tooltip("Reference to ship's ShipCharacteristics component.")]
    public ShipCharacteristics shipCharacteristics;
    
    [Header("Pitch Configuration")]
    [Tooltip("Maximum pitch angle shown (degrees). Typical: 30-90 degrees.")]
    public float maxPitchDegrees = 45f;
    
    [Tooltip("How many pixels the airplane moves vertically for max pitch.")]
    public float maxPitchMovementPixels = 50f;
    
    [Header("Yaw Configuration")]
    [Tooltip("Maximum yaw deviation shown (degrees). Typical: 45-90 degrees.")]
    public float maxYawDegrees = 45f;
    
    [Tooltip("How many pixels the yaw triangle moves horizontally for max yaw.")]
    public float maxYawMovementPixels = 100f;
    
    [Header("Smoothing")]
    [Tooltip("How quickly indicators respond (0 = instant, higher = smoother).")]
    public float dampingFactor = 8f;
    
    [Header("Status")]
    [SerializeField] private float currentPitchDegrees;
    [SerializeField] private float currentRollDegrees;
    [SerializeField] private float currentYawDegrees;
    [SerializeField] private Vector2 currentAirplanePosition;
    [SerializeField] private float currentAirplaneRotation;
    [SerializeField] private Vector2 currentYawPosition;
    
    private Vector2 airplaneStartPosition;
    private Vector2 yawStartPosition;
    private Vector2 targetAirplanePosition;
    private float targetAirplaneRotation;
    private Vector2 targetYawPosition;
    
    private void Start()
    {
        // Auto-find ShipCharacteristics if not assigned
        if (shipCharacteristics == null)
        {
            shipCharacteristics = FindFirstObjectByType<ShipCharacteristics>();
        }
        
        if (airplaneTransform == null)
        {
            Debug.LogError($"AttitudeIndicator on {gameObject.name}: airplaneTransform not assigned!");
        }
        else
        {
            // Store initial position
            airplaneStartPosition = airplaneTransform.anchoredPosition;
            currentAirplanePosition = airplaneStartPosition;
        }
        
        if (yawTriangleTransform == null)
        {
            Debug.LogWarning($"AttitudeIndicator on {gameObject.name}: yawTriangleTransform not assigned - yaw display disabled.");
        }
        else
        {
            // Store initial position
            yawStartPosition = yawTriangleTransform.anchoredPosition;
            currentYawPosition = yawStartPosition;
        }
        
        if (shipCharacteristics == null)
        {
            Debug.LogError($"AttitudeIndicator on {gameObject.name}: Cannot find ShipCharacteristics!");
        }
    }
    
    private void Update()
    {
        if (shipCharacteristics == null)
            return;
        
        // Get current orientation from ship
        Vector3 shipEuler = shipCharacteristics.transform.eulerAngles;
        
        // Convert to -180 to +180 range for pitch and roll
        currentPitchDegrees = Mathf.DeltaAngle(0f, shipEuler.x);
        currentRollDegrees = Mathf.DeltaAngle(0f, shipEuler.z);
        currentYawDegrees = Mathf.DeltaAngle(0f, shipEuler.y);
        
        // Update airplane indicator (pitch and roll)
        if (airplaneTransform != null)
        {
            // ROLL: Rotate the airplane sprite
            // Positive roll = right wing down, negative roll = left wing down
            targetAirplaneRotation = -currentRollDegrees; // Negative because UI rotation is opposite
            
            // PITCH: Move airplane vertically
            // Positive pitch = nose up, negative pitch = nose down
            float normalizedPitch = Mathf.Clamp(currentPitchDegrees / maxPitchDegrees, -1f, 1f);
            float pitchOffset = normalizedPitch * maxPitchMovementPixels;
            
            // Positive pitch = nose up = airplane moves DOWN (counter-intuitive but correct for attitude indicator)
            targetAirplanePosition = airplaneStartPosition + new Vector2(0f, -pitchOffset);
            
            // Smooth transitions
            if (dampingFactor > 0f)
            {
                currentAirplaneRotation = Mathf.LerpAngle(currentAirplaneRotation, targetAirplaneRotation, Time.deltaTime * dampingFactor);
                currentAirplanePosition = Vector2.Lerp(currentAirplanePosition, targetAirplanePosition, Time.deltaTime * dampingFactor);
            }
            else
            {
                currentAirplaneRotation = targetAirplaneRotation;
                currentAirplanePosition = targetAirplanePosition;
            }
            
            // Apply transformations
            airplaneTransform.localRotation = Quaternion.Euler(0f, 0f, currentAirplaneRotation);
            airplaneTransform.anchoredPosition = currentAirplanePosition;
        }
        
        // Update yaw indicator
        if (yawTriangleTransform != null)
        {
            // YAW: Move triangle horizontally
            // Positive yaw = turned right, negative yaw = turned left
            float normalizedYaw = Mathf.Clamp(currentYawDegrees / maxYawDegrees, -1f, 1f);
            float yawOffset = normalizedYaw * maxYawMovementPixels;
            
            targetYawPosition = yawStartPosition + new Vector2(yawOffset, 0f);
            
            // Smooth transition
            if (dampingFactor > 0f)
            {
                currentYawPosition = Vector2.Lerp(currentYawPosition, targetYawPosition, Time.deltaTime * dampingFactor);
            }
            else
            {
                currentYawPosition = targetYawPosition;
            }
            
            // Apply transformation
            yawTriangleTransform.anchoredPosition = currentYawPosition;
        }
    }
    
    /// <summary>
    /// Set custom orientation values (for testing or external control).
    /// </summary>
    public void SetOrientation(float pitch, float roll, float yaw)
    {
        currentPitchDegrees = pitch;
        currentRollDegrees = roll;
        currentYawDegrees = yaw;
    }
}
