using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Ship's Wheel Controller - Visual control for ship steering/rotation.
/// Allows player to drag a wheel to rotate the ship around its central axis.
/// 
/// Wheel Positions:
/// - 0° (indicator pointing up) = No rotation
/// - 1° to 90° clockwise = Rotate ship right (0.0167 to 1.5 degrees/sec)
/// - 1° to 90° counter-clockwise = Rotate ship left (0.0167 to 1.5 degrees/sec)
/// 
/// Dead zone: ±5° around center = no rotation
/// </summary>
[AddComponentMenu("Teramyyd/UI/Ship Wheel Controller")]
public class ShipWheelController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [Tooltip("The wheel RectTransform that rotates (pivot at center).")]
    public RectTransform wheelTransform;
    
    [Tooltip("The ship to control. If empty, will find ship with ShipCharacteristics.")]
    public Transform targetShip;
    
    [Header("Rotation Settings")]
    [Tooltip("Maximum wheel rotation in degrees (clockwise/counter-clockwise).")]
    [Range(45f, 180f)]
    public float maxWheelRotation = 90f;
    
    [Tooltip("Dead zone in degrees around center (±degrees) where no rotation occurs.")]
    [Range(0f, 15f)]
    public float deadZoneDegrees = 5f;
    
    [Tooltip("Ship rotation speed (degrees/sec) at maximum wheel turn.")]
    [Range(0.5f, 10f)]
    public float maxRotationSpeed = 1.5f;
    
    [Tooltip("Snap wheel rotation to increments (0 = smooth, 15 = snap every 15 degrees).")]
    [Range(0f, 45f)]
    public float snapIncrement = 0f;
    
    [Header("Visual Feedback")]
    [Tooltip("Color of wheel when centered (no rotation).")]
    public Color centerColor = Color.white;
    
    [Tooltip("Color of wheel when turning right.")]
    public Color rightTurnColor = new Color(0f, 0.8f, 1f, 1f); // Cyan
    
    [Tooltip("Color of wheel when turning left.")]
    public Color leftTurnColor = new Color(1f, 0.8f, 0f, 1f); // Orange
    
    [Header("Audio (Optional)")]
    [Tooltip("Sound played when wheel is moved.")]
    public AudioClip wheelTurnSound;
    
    [Tooltip("Sound played when wheel returns to center.")]
    public AudioClip centerClickSound;
    
    [Header("Status (Read-Only)")]
    [SerializeField] private float _currentWheelRotation = 0f;
    [SerializeField] private float _shipRotationSpeed = 0f;
    [SerializeField] private bool _turningRight = false;
    [SerializeField] private bool _turningLeft = false;
    
    [Header("Debug")]
    public bool debugLog = false;
    
    // Component references
    private Image wheelImage;
    private AudioSource audioSource;
    private Canvas canvas;
    
    // Dragging state
    private bool isDragging = false;
    private float lastWheelRotation = 0f;
    
    // Public properties
    public float CurrentWheelRotation => _currentWheelRotation;
    public float ShipRotationSpeed => _shipRotationSpeed;
    public bool TurningRight => _turningRight;
    public bool TurningLeft => _turningLeft;
    
    void Awake()
    {
        // Get wheel image component for color changes
        if (wheelTransform != null)
        {
            wheelImage = wheelTransform.GetComponent<Image>();
        }
        
        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (wheelTurnSound != null || centerClickSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }
        
        // Find canvas for screen space calculations
        canvas = GetComponentInParent<Canvas>();
        
        // Validate setup
        if (wheelTransform == null)
        {
            Debug.LogError($"[ShipWheelController] {gameObject.name} requires wheelTransform reference!");
        }
    }
    
    void Start()
    {
        // Find ship if not assigned
        if (targetShip == null)
        {
            ShipCharacteristics shipChar = FindFirstObjectByType<ShipCharacteristics>();
            if (shipChar != null)
            {
                targetShip = shipChar.transform;
                
                if (debugLog)
                {
                    FileLogger.Log($"Ship Wheel auto-discovered ship: {targetShip.gameObject.name}", "ShipWheel");
                }
            }
        }
        
        if (targetShip == null)
        {
            Debug.LogWarning($"[ShipWheelController] No ship found! Wheel will not control anything.");
        }
        
        // Initialize at center position
        SetWheelRotation(0f);
        
        if (debugLog)
        {
            FileLogger.Log($"Ship Wheel initialized - Max Rotation: ±{maxWheelRotation}°, Max Speed: {maxRotationSpeed}°/s, Dead Zone: ±{deadZoneDegrees}°", "ShipWheel");
        }
    }
    
    void FixedUpdate()
    {
        // Apply rotation to ship via ShipCharacteristics (if available)
        if (targetShip != null && Mathf.Abs(_shipRotationSpeed) > 0.01f)
        {
            ShipCharacteristics shipChar = targetShip.GetComponent<ShipCharacteristics>();
            
            if (shipChar != null)
            {
                // _shipRotationSpeed is already calculated as a percentage of ship's yawRotationSpeed
                // in SetWheelRotation(), so we can use it directly
                float yawChange = _shipRotationSpeed * Time.fixedDeltaTime;
                
                // Add to current yaw target (continuous rotation - no normalization)
                // This allows unlimited rotation in either direction
                float newYaw = shipChar.targetYawDegrees + yawChange;
                
                shipChar.SetYawAttitude(newYaw);
            }
            else
            {
                // Fallback: Direct rotation if no ShipCharacteristics
                float rotationThisFrame = _shipRotationSpeed * Time.fixedDeltaTime;
                targetShip.Rotate(Vector3.up, rotationThisFrame, Space.World);
            }
        }
    }
    
    /// <summary>
    /// Called when player starts dragging the wheel.
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        lastWheelRotation = _currentWheelRotation;
        
        if (debugLog)
        {
            FileLogger.Log($"Ship Wheel drag started at {_currentWheelRotation:F1}°", "ShipWheel");
        }
    }
    
    /// <summary>
    /// Called continuously while player drags the wheel.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || wheelTransform == null)
            return;
        
        // Get wheel center position in screen space
        Vector2 wheelScreenPos = RectTransformUtility.WorldToScreenPoint(
            canvas != null ? canvas.worldCamera : null,
            wheelTransform.position
        );
        
        // Calculate angle from wheel center to mouse position
        Vector2 direction = eventData.position - wheelScreenPos;
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        
        // Apply rotation constraints (-maxWheelRotation to +maxWheelRotation)
        angle = Mathf.Clamp(angle, -maxWheelRotation, maxWheelRotation);
        
        // Apply snapping if enabled
        if (snapIncrement > 0f)
        {
            angle = Mathf.Round(angle / snapIncrement) * snapIncrement;
        }
        
        // Only update if rotation changed significantly
        if (Mathf.Abs(angle - _currentWheelRotation) > 0.1f)
        {
            SetWheelRotation(angle);
            
            // Play turning sound
            if (audioSource != null && wheelTurnSound != null)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.PlayOneShot(wheelTurnSound, 0.3f);
                }
            }
        }
    }
    
    /// <summary>
    /// Called when player stops dragging the wheel.
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        
        // Play center click if we're in dead zone
        if (Mathf.Abs(_currentWheelRotation) <= deadZoneDegrees && audioSource != null && centerClickSound != null)
        {
            audioSource.PlayOneShot(centerClickSound, 0.5f);
        }
        
        if (debugLog)
        {
            string direction = _turningRight ? "RIGHT" : _turningLeft ? "LEFT" : "CENTER";
            FileLogger.Log($"Ship Wheel drag ended at {_currentWheelRotation:F1}° ({direction}, {_shipRotationSpeed:F2}°/s)", "ShipWheel");
        }
    }
    
    /// <summary>
    /// Set the wheel rotation and calculate ship rotation speed.
    /// Dead zone: ±deadZoneDegrees around center = no rotation.
    /// </summary>
    /// <param name="angleDegrees">Angle in degrees: 0 = center, positive = right, negative = left</param>
    public void SetWheelRotation(float angleDegrees)
    {
        // Clamp to valid range
        _currentWheelRotation = Mathf.Clamp(angleDegrees, -maxWheelRotation, maxWheelRotation);
        
        // Update visual rotation
        if (wheelTransform != null)
        {
            wheelTransform.localRotation = Quaternion.Euler(0f, 0f, -_currentWheelRotation); // Negative for visual correctness
        }
        
        // Apply dead zone
        float effectiveRotation = _currentWheelRotation;
        
        if (Mathf.Abs(_currentWheelRotation) <= deadZoneDegrees)
        {
            // Within dead zone - no rotation
            effectiveRotation = 0f;
            _shipRotationSpeed = 0f;
            _turningRight = false;
            _turningLeft = false;
        }
        else
        {
            // Outside dead zone - calculate rotation speed as percentage of ship's max yaw speed
            // Wheel at maxWheelRotation = 100% of ship's yawRotationSpeed
            // Wheel at half maxWheelRotation = 50% of ship's yawRotationSpeed
            float wheelPercent = Mathf.Abs(effectiveRotation) / maxWheelRotation;
            
            // Get ship's max yaw rotation speed
            ShipCharacteristics shipChar = targetShip != null ? targetShip.GetComponent<ShipCharacteristics>() : null;
            float shipMaxYawSpeed = shipChar != null ? shipChar.yawRotationSpeed : maxRotationSpeed;
            
            // Calculate actual turn rate as percentage of ship's max capability
            _shipRotationSpeed = wheelPercent * shipMaxYawSpeed;
            
            // Apply direction
            if (effectiveRotation > 0f)
            {
                // Positive rotation = turn right (clockwise when viewed from above)
                _turningRight = true;
                _turningLeft = false;
            }
            else
            {
                // Negative rotation = turn left (counter-clockwise when viewed from above)
                _shipRotationSpeed = -_shipRotationSpeed;
                _turningRight = false;
                _turningLeft = true;
            }
        }
        
        // Update wheel color
        UpdateWheelColor();
        
        if (debugLog && Mathf.Abs(_currentWheelRotation - lastWheelRotation) > 5f) // Log every 5 degrees
        {
            string status = _turningRight ? $"RIGHT {_shipRotationSpeed:F2}°/s" :
                           _turningLeft ? $"LEFT {Mathf.Abs(_shipRotationSpeed):F2}°/s" :
                           "CENTER";
            FileLogger.Log($"Ship Wheel: {_currentWheelRotation:F1}° → {status}", "ShipWheel");
            lastWheelRotation = _currentWheelRotation;
        }
    }
    
    /// <summary>
    /// Update wheel color based on current state.
    /// </summary>
    void UpdateWheelColor()
    {
        if (wheelImage == null)
            return;
        
        if (_turningRight)
        {
            float intensity = Mathf.Abs(_currentWheelRotation) / maxWheelRotation;
            wheelImage.color = Color.Lerp(centerColor, rightTurnColor, intensity);
        }
        else if (_turningLeft)
        {
            float intensity = Mathf.Abs(_currentWheelRotation) / maxWheelRotation;
            wheelImage.color = Color.Lerp(centerColor, leftTurnColor, intensity);
        }
        else
        {
            wheelImage.color = centerColor;
        }
    }
    
    /// <summary>
    /// Reset wheel to center position (0°).
    /// </summary>
    public void ResetToCenter()
    {
        SetWheelRotation(0f);
        
        if (audioSource != null && centerClickSound != null)
        {
            audioSource.PlayOneShot(centerClickSound, 0.7f);
        }
        
        if (debugLog)
        {
            FileLogger.Log("Ship Wheel reset to CENTER", "ShipWheel");
        }
    }
    
    /// <summary>
    /// Set to maximum right turn.
    /// </summary>
    public void SetFullRight()
    {
        SetWheelRotation(maxWheelRotation);
    }
    
    /// <summary>
    /// Set to maximum left turn.
    /// </summary>
    public void SetFullLeft()
    {
        SetWheelRotation(-maxWheelRotation);
    }
    
    /// <summary>
    /// Set wheel to specific angle.
    /// </summary>
    /// <param name="degrees">Positive = right, negative = left</param>
    public void SetAngle(float degrees)
    {
        SetWheelRotation(degrees);
    }
}
