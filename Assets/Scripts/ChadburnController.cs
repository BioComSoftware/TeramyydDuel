using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Chadburn (Engine Order Telegraph) - Visual control for ship speed.
/// Allows player to drag a handle to set engine speed ahead or astern.
/// 
/// Handle Positions:
/// - 0Â° (up) = Full Stop
/// - 1Â° to 100Â° clockwise = 1% to 100% ahead (forward)
/// - 1Â° to 100Â° counter-clockwise (359Â° to 260Â°) = 1% to 100% astern (reverse)
/// 
/// Integrates with ship max speed and engine power to calculate requested speed.
/// </summary>
[AddComponentMenu("Teramyyd/UI/Chadburn Controller")]
public class ChadburnController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [Tooltip("The handle RectTransform that rotates (pivot at bottom).")]
    public RectTransform handleTransform;
    
    [Tooltip("The engine to control. If empty, will find first Engine in scene.")]
    public Engine targetEngine;
    
    [Header("Rotation Settings")]
    [Tooltip("Maximum rotation in degrees (clockwise for ahead, counter-clockwise for astern).")]
    [Range(10f, 180f)]
    public float maxRotationDegrees = 100f;
    
    [Tooltip("Snap rotation to increments (0 = smooth, 10 = snap every 10 degrees).")]
    [Range(0f, 45f)]
    public float snapIncrement = 0f;
    
    [Header("Visual Feedback")]
    [Tooltip("Color of handle when stopped.")]
    public Color stopColor = Color.white;
    
    [Tooltip("Color of handle when going ahead.")]
    public Color aheadColor = new Color(0f, 1f, 0f, 1f); // Green
    
    [Tooltip("Color of handle when going astern.")]
    public Color asternColor = new Color(1f, 0f, 0f, 1f); // Red
    
    [Header("Audio (Optional)")]
    [Tooltip("Sound played when handle is moved.")]
    public AudioClip handleMoveSound;
    
    [Tooltip("Sound played when handle reaches stop position.")]
    public AudioClip stopBellSound;
    
    [Header("Status (Read-Only)")]
    [SerializeField] private float _currentRotation = 0f;
    [SerializeField] private float _currentPercentage = 0f;
    [SerializeField] private float _requestedSpeedKnots = 0f;
    [SerializeField] private bool _isAhead = false;
    [SerializeField] private bool _isAstern = false;
    
    [Header("Debug")]
    public bool debugLog = false;
    
    [Header("Messages")]
    [Tooltip("Reference to the MessageBoxController for displaying unmanned warnings")]
    public MessageBoxController messageBox;
    
    [Tooltip("Messages shown when player moves chadburn but no crew assigned to engine")]
    public string[] unmannedEngineMessages = new string[]
    {
        "The engine room is unmanned!",
        "Nobody is at the engine controls.",
        "We need crew in the engine room!",
        "The engine room is empty - assign crew!",
        "There's no one manning the engine!"
    };
    
    // Component references
    private Image handleImage;
    private AudioSource audioSource;
    private Canvas canvas;
    private ShipCharacteristics shipCharacteristics;
    
    // Message throttling
    private float _lastMessageTime = -999f;
    private const float MESSAGE_COOLDOWN = 2f;
    
    // Dragging state
    private bool isDragging = false;
    private float lastRotation = 0f;
    
    // Public properties
    public float CurrentRotation => _currentRotation;
    public float CurrentPercentage => _currentPercentage;
    public float RequestedSpeedKnots => _requestedSpeedKnots;
    public bool IsAhead => _isAhead;
    public bool IsAstern => _isAstern;
    
    void Awake()
    {
        // Get handle image component for color changes
        if (handleTransform != null)
        {
            handleImage = handleTransform.GetComponent<Image>();
        }
        
        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (handleMoveSound != null || stopBellSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }
        
        // Find canvas for screen space calculations
        canvas = GetComponentInParent<Canvas>();
        
        // Validate setup
        if (handleTransform == null)
        {
            Debug.LogError($"[ChadburnController] {gameObject.name} requires handleTransform reference!");
        }
    }
    
    void Start()
    {
        // Find engine if not assigned
        if (targetEngine == null)
        {
            targetEngine = FindFirstObjectByType<Engine>();
            
            if (targetEngine != null && debugLog)
            {
                FileLogger.Log($"Chadburn auto-discovered engine: {targetEngine.gameObject.name}", "Chadburn");
            }
        }
        
        if (targetEngine == null)
        {
            Debug.LogWarning($"[ChadburnController] No engine found! Chadburn will not control anything.");
        }
        else
        {
            shipCharacteristics = targetEngine.GetComponentInParent<ShipCharacteristics>();
            if (shipCharacteristics == null)
            {
                Debug.LogWarning($"[ChadburnController] Could not find ShipCharacteristics in {targetEngine.name}'s hierarchy. Speed requests will remain zero.");
            }
        }
        
        // Initialize at stop position
        SetRotation(0f);
        
        if (debugLog)
        {
            FileLogger.Log($"Chadburn initialized - Max Rotation: ±{maxRotationDegrees}°, Engine: {(targetEngine != null ? targetEngine.gameObject.name : "None")}", "Chadburn");
        }
    }

    void Update()
    {
        // Don't process keyboard input while dragging with mouse
        if (isDragging)
            return;

        HandleKeyboardInput();
    }

    /// <summary>
    /// Handle keyboard controls for chadburn rotation.
    /// W = forward (clockwise), S = reverse (counter-clockwise)
    /// CTRL+W/S = snap to max, LEFT-SHIFT+W/S = snap to zero
    /// </summary>
    void HandleKeyboardInput()
    {
        KeyBindingConfig kb = KeyBindingConfig.Instance;
        if (kb == null)
            return;

        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool altHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        // Check for engine snap shortcuts (must check before gradual controls to avoid conflicts)
        if (Input.GetKeyDown(kb.engineSnapFullAhead.key))
        {
            bool ctrlMatch = kb.engineSnapFullAhead.ctrl == ctrlHeld;
            bool shiftMatch = kb.engineSnapFullAhead.shift == shiftHeld;
            bool altMatch = kb.engineSnapFullAhead.alt == altHeld;
            
            if (ctrlMatch && shiftMatch && altMatch)
            {
                SetRotation(maxRotationDegrees);
                if (debugLog)
                {
                    FileLogger.Log($"Chadburn: {kb.engineSnapFullAhead} - Snapped to FULL AHEAD ({maxRotationDegrees}°)", "Chadburn");
                }
                return;
            }
        }

        if (Input.GetKeyDown(kb.engineSnapStop.key))
        {
            bool ctrlMatch = kb.engineSnapStop.ctrl == ctrlHeld;
            bool shiftMatch = kb.engineSnapStop.shift == shiftHeld;
            bool altMatch = kb.engineSnapStop.alt == altHeld;
            
            if (ctrlMatch && shiftMatch && altMatch)
            {
                SetRotation(0f);
                if (debugLog)
                {
                    FileLogger.Log($"Chadburn: {kb.engineSnapStop} - Snapped to STOP (0°)", "Chadburn");
                }
                return;
            }
        }

        if (Input.GetKeyDown(kb.engineSnapFullAstern.key))
        {
            bool ctrlMatch = kb.engineSnapFullAstern.ctrl == ctrlHeld;
            bool shiftMatch = kb.engineSnapFullAstern.shift == shiftHeld;
            bool altMatch = kb.engineSnapFullAstern.alt == altHeld;
            
            if (ctrlMatch && shiftMatch && altMatch)
            {
                SetRotation(-maxRotationDegrees);
                if (debugLog)
                {
                    FileLogger.Log($"Chadburn: {kb.engineSnapFullAstern} - Snapped to FULL ASTERN (-{maxRotationDegrees}°)", "Chadburn");
                }
                return;
            }
        }

        if (Input.GetKeyDown(kb.engineSnapStopReverse.key))
        {
            bool ctrlMatch = kb.engineSnapStopReverse.ctrl == ctrlHeld;
            bool shiftMatch = kb.engineSnapStopReverse.shift == shiftHeld;
            bool altMatch = kb.engineSnapStopReverse.alt == altHeld;
            
            if (ctrlMatch && shiftMatch && altMatch)
            {
                SetRotation(0f);
                if (debugLog)
                {
                    FileLogger.Log($"Chadburn: {kb.engineSnapStopReverse} - Snapped to STOP (0°)", "Chadburn");
                }
                return;
            }
        }

        // Check if any snap combination is currently held - if so, skip gradual controls
        bool snapComboHeld = false;
        
        // Check if engineSnapFullAhead combo is held
        if (Input.GetKey(kb.engineSnapFullAhead.key))
        {
            bool ctrlMatch = kb.engineSnapFullAhead.ctrl == ctrlHeld;
            bool shiftMatch = kb.engineSnapFullAhead.shift == shiftHeld;
            bool altMatch = kb.engineSnapFullAhead.alt == altHeld;
            if (ctrlMatch && shiftMatch && altMatch) snapComboHeld = true;
        }
        
        // Check if engineSnapStop combo is held
        if (Input.GetKey(kb.engineSnapStop.key))
        {
            bool ctrlMatch = kb.engineSnapStop.ctrl == ctrlHeld;
            bool shiftMatch = kb.engineSnapStop.shift == shiftHeld;
            bool altMatch = kb.engineSnapStop.alt == altHeld;
            if (ctrlMatch && shiftMatch && altMatch) snapComboHeld = true;
        }
        
        // Check if engineSnapFullAstern combo is held
        if (Input.GetKey(kb.engineSnapFullAstern.key))
        {
            bool ctrlMatch = kb.engineSnapFullAstern.ctrl == ctrlHeld;
            bool shiftMatch = kb.engineSnapFullAstern.shift == shiftHeld;
            bool altMatch = kb.engineSnapFullAstern.alt == altHeld;
            if (ctrlMatch && shiftMatch && altMatch) snapComboHeld = true;
        }
        
        // Check if engineSnapStopReverse combo is held
        if (Input.GetKey(kb.engineSnapStopReverse.key))
        {
            bool ctrlMatch = kb.engineSnapStopReverse.ctrl == ctrlHeld;
            bool shiftMatch = kb.engineSnapStopReverse.shift == shiftHeld;
            bool altMatch = kb.engineSnapStopReverse.alt == altHeld;
            if (ctrlMatch && shiftMatch && altMatch) snapComboHeld = true;
        }

        // Only process gradual controls if no snap combo is held
        if (!snapComboHeld)
        {
            // Forward controls - gradual rotation
            if (Input.GetKey(kb.engineForward))
            {
                float rotationDelta = kb.engineChadburnRotationSpeed * Time.deltaTime;
                float newRotation = Mathf.Min(_currentRotation + rotationDelta, maxRotationDegrees);
                SetRotation(newRotation);
            }

            // Reverse controls - gradual rotation
            if (Input.GetKey(kb.engineReverse))
            {
                float rotationDelta = kb.engineChadburnRotationSpeed * Time.deltaTime;
                float newRotation = Mathf.Max(_currentRotation - rotationDelta, -maxRotationDegrees);
                SetRotation(newRotation);
            }
        }
    }
    
    /// <summary>
    /// Called when player starts dragging the handle.
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        lastRotation = _currentRotation;
        
        if (debugLog)
        {
            FileLogger.Log($"Chadburn drag started at {_currentRotation:F1}Â°", "Chadburn");
        }
    }
    
    /// <summary>
    /// Called continuously while player drags the handle.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || handleTransform == null)
            return;
        
        // Get handle center position in screen space
        Vector2 handleScreenPos = RectTransformUtility.WorldToScreenPoint(
            canvas != null ? canvas.worldCamera : null,
            handleTransform.position
        );
        
        // Calculate angle from handle center to mouse position
        Vector2 direction = eventData.position - handleScreenPos;
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        
        // Apply rotation constraints (-maxRotation to +maxRotation)
        angle = Mathf.Clamp(angle, -maxRotationDegrees, maxRotationDegrees);
        
        // Apply snapping if enabled
        if (snapIncrement > 0f)
        {
            angle = Mathf.Round(angle / snapIncrement) * snapIncrement;
        }
        
        // Only update if rotation changed significantly
        if (Mathf.Abs(angle - _currentRotation) > 0.1f)
        {
            SetRotation(angle);
            
            // Play movement sound
            if (audioSource != null && handleMoveSound != null)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.PlayOneShot(handleMoveSound, 0.3f);
                }
            }
        }
    }
    
    /// <summary>
    /// Called when player stops dragging the handle.
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        
        // Play bell if we're at stop position (within dead zone)
        if (Mathf.Abs(_currentRotation) <= 5f && audioSource != null && stopBellSound != null)
        {
            audioSource.PlayOneShot(stopBellSound, 0.5f);
        }
        
        if (debugLog)
        {
            FileLogger.Log($"Chadburn drag ended at {_currentRotation:F1}Â° ({_currentPercentage:F0}% {(_isAhead ? "AHEAD" : _isAstern ? "ASTERN" : "STOP")})", "Chadburn");
        }
    }
    
    /// <summary>
    /// Set the handle rotation and update engine commands.
    /// Dead zone: Â±5Â° around center is considered STOP.
    /// </summary>
    /// <param name="angleDegrees">Angle in degrees: 0 = stop, positive = ahead, negative = astern</param>
    public void SetRotation(float angleDegrees)
    {
        // Clamp to valid range
        _currentRotation = Mathf.Clamp(angleDegrees, -maxRotationDegrees, maxRotationDegrees);
        
        // Update visual rotation
        if (handleTransform != null)
        {
            handleTransform.localRotation = Quaternion.Euler(0f, 0f, -_currentRotation); // Negative for clockwise = forward
        }
        
        // Apply 5Â° dead zone around center
        const float DEAD_ZONE = 5f;
        float effectiveRotation = _currentRotation;
        
        if (Mathf.Abs(_currentRotation) <= DEAD_ZONE)
        {
            // Within dead zone - treat as stop
            effectiveRotation = 0f;
        }
        
        float normalizedRotation = (maxRotationDegrees > 0f)
            ? Mathf.Clamp01(Mathf.Abs(effectiveRotation) / maxRotationDegrees)
            : 0f;
        _currentPercentage = normalizedRotation * 100f;
        
        // Determine direction (outside dead zone)
        _isAhead = _currentRotation > DEAD_ZONE;
        _isAstern = _currentRotation < -DEAD_ZONE;
        
        // Calculate target knots and throttle limit
        float throttleFraction = Mathf.Clamp01(_currentPercentage / 100f);
        _requestedSpeedKnots = (shipCharacteristics != null)
            ? shipCharacteristics.MaxSpeedKnots * throttleFraction
            : 0f;
        
        if (targetEngine != null)
        {
            // Check if engine is unmanned when trying to change speed
            bool tryingToMove = Mathf.Abs(_currentRotation) > DEAD_ZONE;
            bool engineUnmanned = targetEngine.crewStation != null && targetEngine.crewStation.AssignedCrewCount == 0;
            
            if (tryingToMove && engineUnmanned && Time.time - _lastMessageTime > MESSAGE_COOLDOWN)
            {
                ShowUnmannedMessage();
                _lastMessageTime = Time.time;
            }
            
            // Send commands to engine
            if (_isAhead)
            {
                targetEngine.SetThrottlePercent(throttleFraction);
                targetEngine.SetKnotsAhead(_requestedSpeedKnots);
            }
            else if (_isAstern)
            {
                targetEngine.SetThrottlePercent(throttleFraction);
                targetEngine.SetKnotsAstern(_requestedSpeedKnots);
            }
            else
            {
                targetEngine.SetThrottlePercent(0f);
                targetEngine.AllStop();
            }
        }
        else
        {
            _requestedSpeedKnots = 0f;
        }
        
        // Update handle color
        UpdateHandleColor();
        
        if (debugLog && Mathf.Abs(_currentRotation - lastRotation) > 5f) // Log every 5 degrees
        {
            string status = _isAhead ? $"AHEAD {_requestedSpeedKnots:F1}kt" :
                           _isAstern ? $"ASTERN {_requestedSpeedKnots:F1}kt" :
                           "STOP";
            FileLogger.Log($"Chadburn: {_currentRotation:F1}Â° â†’ {status} ({_currentPercentage:F0}%)", "Chadburn");
            lastRotation = _currentRotation;
        }
    }
    
    /// <summary>
    /// Update handle color based on current state.
    /// </summary>
    void UpdateHandleColor()
    {
        if (handleImage == null)
            return;
        
        if (_isAhead)
        {
            handleImage.color = Color.Lerp(stopColor, aheadColor, _currentPercentage / 100f);
        }
        else if (_isAstern)
        {
            handleImage.color = Color.Lerp(stopColor, asternColor, _currentPercentage / 100f);
        }
        else
        {
            handleImage.color = stopColor;
        }
    }
    
    /// <summary>
    /// Reset handle to stop position (0Â°).
    /// </summary>
    public void ResetToStop()
    {
        SetRotation(0f);
        
        if (audioSource != null && stopBellSound != null)
        {
            audioSource.PlayOneShot(stopBellSound, 0.7f);
        }
        
        if (debugLog)
        {
            FileLogger.Log("Chadburn reset to STOP", "Chadburn");
        }
    }
    
    /// <summary>
    /// Set to full ahead (maximum forward speed).
    /// </summary>
    public void SetFullAhead()
    {
        SetRotation(maxRotationDegrees);
    }
    
    /// <summary>
    /// Set to full astern (maximum reverse speed).
    /// </summary>
    public void SetFullAstern()
    {
        SetRotation(-maxRotationDegrees);
    }
    
    /// <summary>
    /// Set to specific percentage ahead (0-100%).
    /// </summary>
    public void SetPercentageAhead(float percentage)
    {
        float angle = Mathf.Clamp(percentage, 0f, 100f) * (maxRotationDegrees / 100f);
        SetRotation(angle);
    }
    
    /// <summary>
    /// Set to specific percentage astern (0-100%).
    /// </summary>
    public void SetPercentageAstern(float percentage)
    {
        float angle = -Mathf.Clamp(percentage, 0f, 100f) * (maxRotationDegrees / 100f);
        SetRotation(angle);
    }
    
    /// <summary>
    /// Display a random message indicating the engine is unmanned.
    /// </summary>
    private void ShowUnmannedMessage()
    {
        if (messageBox != null && unmannedEngineMessages != null && unmannedEngineMessages.Length > 0)
        {
            messageBox.ShowRandomMessage(unmannedEngineMessages);
        }
    }
    
}
