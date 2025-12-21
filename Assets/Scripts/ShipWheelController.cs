using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Ship's Wheel Controller - Visual control for ship steering/rotation.
/// Allows player to drag a wheel to rotate the ship around its central axis.
/// 
/// Wheel Positions:
/// - 0Â° (indicator pointing up) = No rotation
/// - 1Â° to 90Â° clockwise = Rotate ship right (0.0167 to 1.5 degrees/sec)
/// - 1Â° to 90Â° counter-clockwise = Rotate ship left (0.0167 to 1.5 degrees/sec)
/// 
/// Dead zone: Â±5Â° around center = no rotation
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
    
    [Tooltip("Dead zone in degrees around center (Â±degrees) where no rotation occurs.")]
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
    [Header("Auto Return")]
    [Tooltip("DEFAULT VALUE: Degrees per second the wheel will spring back toward center (0 disables). This is the developer default - player setting comes from keybindings.json.")]
    public float autoReturnSpeedDegPerSec = 90f;
    
    [Tooltip("When true, reads from keybindings.json (player setting). When false, uses the field above (developer default only).")]
    public bool useConfigurableSpeed = true;
    
    [Header("Debug")]
    public bool debugLog = false;
    
    // Component references
    private Image wheelImage;
    private AudioSource audioSource;
    private Canvas canvas;
    
    // Dragging state
    private bool isDragging = false;
    private float lastWheelRotation = 0f;
    private bool wheelLatched = false;
    
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
        if (debugLog)
        {
            Debug.Log($"ShipWheelController.Start() called on {gameObject.name}, debugLog={debugLog}");
            FileLogger.Log($"ShipWheelController.Start() called on {gameObject.name}, debugLog={debugLog}", "ShipWheel");
        }
        
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
            FileLogger.Log($"Ship Wheel initialized - Max Rotation: Â±{maxWheelRotation}Â°, Max Speed: {maxRotationSpeed}Â°/s, Dead Zone: Â±{deadZoneDegrees}Â°", "ShipWheel");
        }
    }

    void Update()
    {
        bool keyboardActive = HandleKeyboardInput();
        if (!keyboardActive)
        {
            HandleAutoReturn();
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
        bool ctrlHeld = IsCtrlModifierHeld();
        if (!ctrlHeld && wheelLatched)
        {
            wheelLatched = false;
        }
        
        if (debugLog)
        {
            FileLogger.Log($"Ship Wheel drag started at {_currentWheelRotation:F1}Â°", "ShipWheel");
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
        bool ctrlHeld = IsCtrlModifierHeld();
        if (ctrlHeld)
        {
            wheelLatched = true;
        }
        else
        {
            wheelLatched = false;
        }
        
        // Play center click if we're in dead zone
        if (Mathf.Abs(_currentWheelRotation) <= deadZoneDegrees && audioSource != null && centerClickSound != null)
        {
            audioSource.PlayOneShot(centerClickSound, 0.5f);
        }
        
        if (debugLog)
        {
            string direction = _turningRight ? "RIGHT" : _turningLeft ? "LEFT" : "CENTER";
            FileLogger.Log($"Ship Wheel drag ended at {_currentWheelRotation:F1}Â° ({direction}, {_shipRotationSpeed:F2}Â°/s)", "ShipWheel");
        }
    }
    
    /// <summary>
    /// Set the wheel rotation and calculate ship rotation speed.
    /// Dead zone: Â±deadZoneDegrees around center = no rotation.
    /// </summary>
    /// <param name="angleDegrees">Angle in degrees: 0 = center, positive = right, negative = left</param>
    public void SetWheelRotation(float angleDegrees)
    {
        float oldRotation = _currentWheelRotation;
        
        // Clamp to valid range
        _currentWheelRotation = Mathf.Clamp(angleDegrees, -maxWheelRotation, maxWheelRotation);
        
        if (debugLog && Mathf.Abs(_currentWheelRotation - oldRotation) > 0.1f)
        {
            string msg = $"SetWheelRotation: {oldRotation:F2}° → {_currentWheelRotation:F2}°";
            Debug.Log(msg);
            FileLogger.Log(msg, "ShipWheel");
        }
        
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
            string status = _turningRight ? $"RIGHT {_shipRotationSpeed:F2}Â°/s" :
                           _turningLeft ? $"LEFT {Mathf.Abs(_shipRotationSpeed):F2}Â°/s" :
                           "CENTER";
            FileLogger.Log($"Ship Wheel: {_currentWheelRotation:F1}Â° â†’ {status}", "ShipWheel");
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
    /// Reset wheel to center position (0Â°).
    /// </summary>
    public void ResetToCenter()
    {
        wheelLatched = false;
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

    bool IsCtrlModifierHeld()
    {
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }

    /// <summary>
    /// Handle keyboard controls for ship wheel rotation.
    /// A = turn left (counter-clockwise), D = turn right (clockwise)
    /// CTRL+A/D = turn and lock position when released
    /// SHIFT+A/D = snap to zero (center)
    /// CTRL+SHIFT+A/D = snap to maximum turn
    /// </summary>
    /// <returns>True if keyboard input is actively controlling the wheel</returns>
    bool HandleKeyboardInput()
    {
        // Don't process keyboard input while mouse dragging
        if (isDragging)
        {
            if (debugLog && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D)))
            {
                string msg = "ShipWheelController: Key pressed but isDragging=true, ignoring keyboard input";
                Debug.Log(msg);
                FileLogger.Log(msg, "ShipWheel");
            }
            return false;
        }

        KeyBindingConfig kb = KeyBindingConfig.Instance;
        if (kb == null)
        {
            if (debugLog)
            {
                string msg = "ShipWheelController: KeyBindingConfig.Instance is null!";
                Debug.LogWarning(msg);
                FileLogger.Log(msg, "ShipWheel");
            }
            return false;
        }
        
        // Debug: Log when HandleKeyboardInput is processing
        if (debugLog && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D)))
        {
            string msg = $"ShipWheelController: HandleKeyboardInput active, checking keys wheelLeft={kb.wheelLeft}, wheelRight={kb.wheelRight}";
            Debug.Log(msg);
            FileLogger.Log(msg, "ShipWheel");
        }

        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift);

        // Determine effective speed (from config or inspector)
        float effectiveSpeed = useConfigurableSpeed 
            ? kb.autoReturnSpeedDegPerSec 
            : autoReturnSpeedDegPerSec;

        // Debug: Check if keys are being detected
        if (debugLog)
        {
            if (Input.GetKeyDown(kb.wheelLeft))
            {
                string msg = $"ShipWheel: Detected wheelLeft key ({kb.wheelLeft}) press";
                Debug.Log(msg);
                FileLogger.Log(msg, "ShipWheel");
            }
            if (Input.GetKeyDown(kb.wheelRight))
            {
                string msg = $"ShipWheel: Detected wheelRight key ({kb.wheelRight}) press";
                Debug.Log(msg);
                FileLogger.Log(msg, "ShipWheel");
            }
        }

        // Left turn controls (A key)
        bool leftKeyPressed = Input.GetKey(kb.wheelLeft);
        if (debugLog && Input.GetKeyDown(kb.wheelLeft))
        {
            string msg = $"ShipWheel: A key pressed, leftKeyPressed={leftKeyPressed}, ctrlHeld={ctrlHeld}, shiftHeld={shiftHeld}";
            Debug.Log(msg);
            FileLogger.Log(msg, "ShipWheel");
        }
        
        if (leftKeyPressed)
        {
            if (ctrlHeld && shiftHeld)
            {
                // CTRL+SHIFT+A: Snap to full left
                if (Input.GetKeyDown(kb.wheelLeft))
                {
                    SetWheelRotation(-maxWheelRotation);
                    wheelLatched = true;
                    if (debugLog)
                    {
                        FileLogger.Log($"Ship Wheel: CTRL+SHIFT+A - Snapped to FULL LEFT (-{maxWheelRotation}°)", "ShipWheel");
                    }
                }
            }
            else if (shiftHeld)
            {
                // SHIFT+A: Snap to center
                if (Input.GetKeyDown(kb.wheelLeft))
                {
                    SetWheelRotation(0f);
                    wheelLatched = false;
                    if (debugLog)
                    {
                        FileLogger.Log("Ship Wheel: SHIFT+A - Snapped to CENTER (0°)", "ShipWheel");
                    }
                }
            }
            else if (ctrlHeld)
            {
                // CTRL+A: Turn left and will latch when released
                float newAngle = Mathf.Max(_currentWheelRotation - effectiveSpeed * Time.deltaTime, -maxWheelRotation);
                SetWheelRotation(newAngle);
            }
            else
            {
                // A alone: Turn left continuously, will auto-return when released
                wheelLatched = false;
                float oldAngle = _currentWheelRotation;
                float newAngle = Mathf.Max(_currentWheelRotation - effectiveSpeed * Time.deltaTime, -maxWheelRotation);
                SetWheelRotation(newAngle);
                
                if (debugLog && Input.GetKeyDown(kb.wheelLeft))
                {
                    string msg = $"ShipWheel: A alone - oldAngle={oldAngle:F2}, newAngle={newAngle:F2}, effectiveSpeed={effectiveSpeed}, deltaTime={Time.deltaTime:F4}";
                    Debug.Log(msg);
                    FileLogger.Log(msg, "ShipWheel");
                }
            }
        }
        else if (Input.GetKeyUp(kb.wheelLeft))
        {
            // Key released - check if we should latch or auto-return
            if (ctrlHeld)
            {
                wheelLatched = true;
                if (debugLog)
                {
                    FileLogger.Log($"Ship Wheel: CTRL+A released - Locked at {_currentWheelRotation:F1}°", "ShipWheel");
                }
            }
            else
            {
                wheelLatched = false;
            }
        }

        // Right turn controls (D key)
        if (Input.GetKey(kb.wheelRight))
        {
            if (ctrlHeld && shiftHeld)
            {
                // CTRL+SHIFT+D: Snap to full right
                if (Input.GetKeyDown(kb.wheelRight))
                {
                    SetWheelRotation(maxWheelRotation);
                    wheelLatched = true;
                    if (debugLog)
                    {
                        FileLogger.Log($"Ship Wheel: CTRL+SHIFT+D - Snapped to FULL RIGHT ({maxWheelRotation}°)", "ShipWheel");
                    }
                }
            }
            else if (shiftHeld)
            {
                // SHIFT+D: Snap to center
                if (Input.GetKeyDown(kb.wheelRight))
                {
                    SetWheelRotation(0f);
                    wheelLatched = false;
                    if (debugLog)
                    {
                        FileLogger.Log("Ship Wheel: SHIFT+D - Snapped to CENTER (0°)", "ShipWheel");
                    }
                }
            }
            else if (ctrlHeld)
            {
                // CTRL+D: Turn right and will latch when released
                float newAngle = Mathf.Min(_currentWheelRotation + effectiveSpeed * Time.deltaTime, maxWheelRotation);
                SetWheelRotation(newAngle);
            }
            else
            {
                // D alone: Turn right continuously, will auto-return when released
                wheelLatched = false;
                float newAngle = Mathf.Min(_currentWheelRotation + effectiveSpeed * Time.deltaTime, maxWheelRotation);
                SetWheelRotation(newAngle);
            }
        }
        else if (Input.GetKeyUp(kb.wheelRight))
        {
            // Key released - check if we should latch or auto-return
            if (ctrlHeld)
            {
                wheelLatched = true;
                if (debugLog)
                {
                    FileLogger.Log($"Ship Wheel: CTRL+D released - Locked at {_currentWheelRotation:F1}°", "ShipWheel");
                }
            }
            else
            {
                wheelLatched = false;
            }
        }
        
        // Return true if either key is currently being held
        return Input.GetKey(kb.wheelLeft) || Input.GetKey(kb.wheelRight);
    }

    void HandleAutoReturn()
    {
        // Determine which speed value to use
        float effectiveSpeed = useConfigurableSpeed 
            ? KeyBindingConfig.Instance.autoReturnSpeedDegPerSec 
            : autoReturnSpeedDegPerSec;

        if (effectiveSpeed <= 0f)
            return;

        if (isDragging || wheelLatched)
            return;

        if (Mathf.Approximately(_currentWheelRotation, 0f))
            return;

        float newAngle = Mathf.MoveTowards(
            _currentWheelRotation,
            0f,
            effectiveSpeed * Time.deltaTime);

        if (!Mathf.Approximately(newAngle, _currentWheelRotation))
        {
            SetWheelRotation(newAngle);
        }
    }
}
