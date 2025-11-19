using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Yaw Triangle Indicator - Shows ship's slip angle (sideways drift).
/// Rotates to indicate the direction of actual movement relative to ship's heading.
/// 
/// Rotation:
/// - 0° (pointing down) = Moving straight forward (no slip)
/// - 90° (pointing right) = Moving 100% sideways to the right
/// - 45° (pointing down-right) = Moving equally forward and right (45° slip)
/// - -90° (pointing left) = Moving 100% sideways to the left
/// - -45° (pointing down-left) = Moving equally forward and left (-45° slip)
/// 
/// Pivot: Top of triangle (0.5, 1) for rotation around top point.
/// </summary>
[AddComponentMenu("Teramyyd/UI/Yaw Triangle Indicator")]
public class YawTriangleIndicator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The RectTransform of the yaw triangle (this object).")]
    public RectTransform triangleTransform;
    
    [Tooltip("Ship to track. If empty, will find ShipCharacteristics in scene.")]
    public ShipCharacteristics shipCharacteristics;
    
    [Header("Status (Read-Only)")]
    [SerializeField] private float _currentSlipAngle = 0f;
    [SerializeField] private float _currentRotation = 0f;
    
    [Header("Debug")]
    public bool debugLog = false;
    
    // Public properties
    public float CurrentSlipAngle => _currentSlipAngle;
    
    void Awake()
    {
        // Use this object's RectTransform if not assigned
        if (triangleTransform == null)
        {
            triangleTransform = GetComponent<RectTransform>();
        }
        
        // Validate pivot point
        if (triangleTransform != null && triangleTransform.pivot != new Vector2(0.5f, 1f))
        {
            Debug.LogWarning($"[YawTriangleIndicator] {gameObject.name} pivot should be (0.5, 1) for rotation around top. Current: {triangleTransform.pivot}");
        }
    }
    
    void Start()
    {
        // Find ship if not assigned
        if (shipCharacteristics == null)
        {
            shipCharacteristics = FindFirstObjectByType<ShipCharacteristics>();
            
            if (shipCharacteristics != null && debugLog)
            {
                FileLogger.Log($"YawTriangle auto-discovered ship: {shipCharacteristics.gameObject.name}", "YawTriangle");
            }
        }
        
        if (shipCharacteristics == null)
        {
            Debug.LogWarning($"[YawTriangleIndicator] No ShipCharacteristics found! Triangle will not update.");
        }
        
        if (debugLog)
        {
            FileLogger.Log($"YawTriangle initialized", "YawTriangle");
        }
    }
    
    void Update()
    {
        if (shipCharacteristics == null || triangleTransform == null)
            return;
        
        // Get ship's velocity (actual movement direction)
        Vector3 velocity = shipCharacteristics.Velocity;
        
        // Get ship's forward direction (heading)
        Vector3 shipForward = shipCharacteristics.transform.forward;
        
        // Calculate slip angle in horizontal plane only (ignore vertical component)
        Vector3 velocityHorizontal = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 forwardHorizontal = new Vector3(shipForward.x, 0f, shipForward.z).normalized;
        
        // Check if ship is moving
        if (velocityHorizontal.magnitude > 0.1f)
        {
            // Normalize velocity direction
            Vector3 velocityDirection = velocityHorizontal.normalized;
            
            // Calculate slip angle using signed angle
            // Positive angle = slipping right, negative = slipping left
            _currentSlipAngle = Vector3.SignedAngle(forwardHorizontal, velocityDirection, Vector3.up);
            
            // Set rotation directly (no smoothing)
            // 0° slip = 0° rotation (pointing down)
            // +90° slip (right) = +90° rotation (pointing right)
            // -90° slip (left) = -90° rotation (pointing left)
            _currentRotation = _currentSlipAngle;
        }
        else
        {
            // Ship not moving - reset to center
            _currentSlipAngle = 0f;
            _currentRotation = 0f;
        }
        
        // Apply rotation instantly (teleport - no smoothing)
        triangleTransform.localRotation = Quaternion.Euler(0f, 0f, _currentRotation); // Positive rotation
        
        if (debugLog && Time.frameCount % 60 == 0) // Log once per second at 60fps
        {
            string slipDirection = _currentSlipAngle > 5f ? "RIGHT" :
                                  _currentSlipAngle < -5f ? "LEFT" :
                                  "STRAIGHT";
            FileLogger.Log($"YawTriangle - Slip: {_currentSlipAngle:F1}°, Rotation: {_currentRotation:F1}°, Direction: {slipDirection}", "YawTriangle");
        }
    }
    
    /// <summary>
    /// Manually set the yaw triangle rotation (for testing).
    /// </summary>
    /// <param name="angleDegrees">Angle in degrees (0 = straight, +90 = right, -90 = left)</param>
    public void SetRotation(float angleDegrees)
    {
        _currentRotation = angleDegrees;
        
        if (triangleTransform != null)
        {
            triangleTransform.localRotation = Quaternion.Euler(0f, 0f, angleDegrees);
        }
    }
}
