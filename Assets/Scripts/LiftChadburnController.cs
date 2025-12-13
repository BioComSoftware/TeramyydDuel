using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Telegraph-style controller dedicated to lift allocation.
/// Mirrors the Chadburn controller UX but drives LiftDevice power percentages.
/// </summary>
[AddComponentMenu("Teramyyd/UI/Lift Chadburn Controller")]
public class LiftChadburnController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const float DEAD_ZONE = 5f;
    
    [Header("References")]
    [Tooltip("The handle RectTransform that rotates (pivot at bottom).")]
    public RectTransform handleTransform;

    [Tooltip("Lift device to command. If empty, will look for AntiGravityDevice, then LiftDevice.")]
    public LiftDevice targetLiftDevice;

    [Header("Rotation Settings")]
    [Tooltip("Maximum rotation in degrees (clockwise or counter-clockwise).")]
    [Range(10f, 180f)]
    public float maxRotationDegrees = 100f;

    [Tooltip("Snap rotation to increments (0 = smooth, 10 = snap every 10 degrees).")]
    [Range(0f, 45f)]
    public float snapIncrement = 0f;

    [Header("Visual Feedback")]
    [Tooltip("Handle color when no lift is requested.")]
    public Color idleColor = Color.white;

    [Tooltip("Handle color when full lift is requested.")]
    public Color fullLiftColor = new Color(0.4f, 0.8f, 1f, 1f);

    [Header("Audio (Optional)")]
    [Tooltip("Sound played while handle is moving.")]
    public AudioClip handleMoveSound;

    [Tooltip("Sound played when handle returns to idle.")]
    public AudioClip idleBellSound;

    [Header("Status (Read-Only)")]
    [SerializeField] private float _currentRotation = 0f;
    [SerializeField] private float _currentPercentage = 0f;
    [SerializeField] private float _allocatedPowerPerSecond = 0f;
    [SerializeField] private bool _isIncreasingLift = false;
    [SerializeField] private bool _isReducingLift = false;

    [Header("Power Mapping")]
    [Tooltip("How many multiples of hover power a full ASCEND command requests above hover (engine output still limits actual power).")]
    public float ascendPowerMultiple = 5f;
    
    [Tooltip("Fallback hover draw (units/s) if ship weight is unknown.")]
    public float hoverPowerFallback = 100f;

    [Header("Debug")]
    public bool debugLog = false;

    [Header("Messages")]
    [Tooltip("Reference to the MessageBoxController for displaying unmanned warnings")]
    public MessageBoxController messageBox;
    
    [Tooltip("Messages shown when player moves lift chadburn but no crew assigned to lift")]
    public string[] unmannedLiftMessages = new string[]
    {
        "The lift controls are unmanned!",
        "Nobody is at the lift station.",
        "We need crew at the lift controls!",
        "The lift station is empty - assign crew!",
        "There's no one manning the lift!"
    };

    // Components
    private Image handleImage;
    private AudioSource audioSource;
    private Canvas canvas;

    // State
    private bool isDragging = false;
    private float lastLoggedPercent = -1f;
    
    // Message throttling
    private float _lastMessageTime = -999f;
    private const float MESSAGE_COOLDOWN = 2f;

    // Public accessors
    public float CurrentRotation => _currentRotation;
    public float CurrentPercentage => _currentPercentage;
    public float AllocatedPowerPerSecond => _allocatedPowerPerSecond;

    void Awake()
    {
        if (handleTransform != null)
        {
            handleImage = handleTransform.GetComponent<Image>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (handleMoveSound != null || idleBellSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        canvas = GetComponentInParent<Canvas>();

        if (handleTransform == null)
        {
            Debug.LogError($"[LiftChadburn] {gameObject.name} requires handleTransform reference!");
        }
    }

    void Start()
    {
        if (targetLiftDevice == null)
        {
            targetLiftDevice = FindFirstObjectByType<AntiGravityDevice>();
        }

        if (targetLiftDevice == null)
        {
            targetLiftDevice = FindFirstObjectByType<LiftDevice>();
        }

        if (targetLiftDevice == null)
        {
            Debug.LogWarning("[LiftChadburn] No LiftDevice found in scene. Controller is idle.");
        }
        else if (debugLog)
        {
            FileLogger.Log($"Lift Chadburn controlling {targetLiftDevice.gameObject.name}", "LiftChadburn");
        }

        SetRotation(0f);
    }

    void Update()
    {
        HandleKeyboardInput();
    }

    /// <summary>
    /// Handle keyboard controls for lift chadburn rotation.
    /// Q = increase lift (clockwise), E = decrease lift (counter-clockwise)
    /// CTRL+Q/E = snap to max, LEFT-SHIFT+Q/E = snap to zero
    /// </summary>
    void HandleKeyboardInput()
    {
        // Don't process keyboard input while mouse dragging
        if (isDragging)
            return;

        KeyBindingConfig kb = KeyBindingConfig.Instance;
        if (kb == null)
            return;

        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift);

        // Up controls (Q key - clockwise/increase lift)
        if (Input.GetKey(kb.liftUp))
        {
            if (ctrlHeld && shiftHeld)
            {
                // CTRL+SHIFT+Q: Snap to max lift
                if (Input.GetKeyDown(kb.liftUp))
                {
                    SetRotation(maxRotationDegrees);
                    if (debugLog)
                    {
                        FileLogger.Log($"Lift Chadburn: CTRL+SHIFT+Q - Snapped to MAX LIFT ({maxRotationDegrees}°)", "LiftChadburn");
                    }
                }
            }
            else if (shiftHeld)
            {
                // SHIFT+Q: Snap to zero
                if (Input.GetKeyDown(kb.liftUp))
                {
                    SetRotation(0f);
                    if (debugLog)
                    {
                        FileLogger.Log("Lift Chadburn: SHIFT+Q - Snapped to IDLE (0°)", "LiftChadburn");
                    }
                }
            }
            else if (ctrlHeld)
            {
                // CTRL+Q: Rotate clockwise continuously at speed
                float newAngle = Mathf.Min(_currentRotation + kb.liftChadburnRotationSpeed * Time.deltaTime, maxRotationDegrees);
                SetRotation(newAngle);
            }
            else
            {
                // Q alone: Rotate clockwise continuously
                float newAngle = Mathf.Min(_currentRotation + kb.liftChadburnRotationSpeed * Time.deltaTime, maxRotationDegrees);
                SetRotation(newAngle);
            }
        }

        // Down controls (E key - counter-clockwise/decrease lift)
        if (Input.GetKey(kb.liftDown))
        {
            if (ctrlHeld && shiftHeld)
            {
                // CTRL+SHIFT+E: Snap to max reverse (minimum rotation)
                if (Input.GetKeyDown(kb.liftDown))
                {
                    SetRotation(-maxRotationDegrees);
                    if (debugLog)
                    {
                        FileLogger.Log($"Lift Chadburn: CTRL+SHIFT+E - Snapped to MAX REVERSE (-{maxRotationDegrees}°)", "LiftChadburn");
                    }
                }
            }
            else if (shiftHeld)
            {
                // SHIFT+E: Snap to zero
                if (Input.GetKeyDown(kb.liftDown))
                {
                    SetRotation(0f);
                    if (debugLog)
                    {
                        FileLogger.Log("Lift Chadburn: SHIFT+E - Snapped to IDLE (0°)", "LiftChadburn");
                    }
                }
            }
            else if (ctrlHeld)
            {
                // CTRL+E: Rotate counter-clockwise continuously at speed
                float newAngle = Mathf.Max(_currentRotation - kb.liftChadburnRotationSpeed * Time.deltaTime, -maxRotationDegrees);
                SetRotation(newAngle);
            }
            else
            {
                // E alone: Rotate counter-clockwise continuously
                float newAngle = Mathf.Max(_currentRotation - kb.liftChadburnRotationSpeed * Time.deltaTime, -maxRotationDegrees);
                SetRotation(newAngle);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;

        if (debugLog)
        {
            FileLogger.Log($"Lift Chadburn drag start at {_currentPercentage:F1}% ({_allocatedPowerPerSecond:F1}/s)", "LiftChadburn");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || handleTransform == null)
            return;

        Vector2 handleScreenPos = RectTransformUtility.WorldToScreenPoint(
            canvas != null ? canvas.worldCamera : null,
            handleTransform.position
        );

        Vector2 direction = eventData.position - handleScreenPos;
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        angle = Mathf.Clamp(angle, -maxRotationDegrees, maxRotationDegrees);

        if (snapIncrement > 0f)
        {
            angle = Mathf.Round(angle / snapIncrement) * snapIncrement;
        }

        if (Mathf.Abs(angle - _currentRotation) > 0.1f)
        {
            SetRotation(angle);

            if (audioSource != null && handleMoveSound != null && !audioSource.isPlaying)
            {
                audioSource.PlayOneShot(handleMoveSound, 0.3f);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        if (!_isIncreasingLift && !_isReducingLift && audioSource != null && idleBellSound != null)
        {
            audioSource.PlayOneShot(idleBellSound, 0.5f);
        }

        if (debugLog)
        {
            FileLogger.Log($"Lift Chadburn drag end at {_currentPercentage:F1}% ({_allocatedPowerPerSecond:F1}/s)", "LiftChadburn");
        }
    }

    /// <summary>
    /// Update the handle rotation and send lift allocation to the target device.
    /// </summary>
    public void SetRotation(float angleDegrees)
    {
        _currentRotation = Mathf.Clamp(angleDegrees, -maxRotationDegrees, maxRotationDegrees);

        if (handleTransform != null)
        {
            handleTransform.localRotation = Quaternion.Euler(0f, 0f, -_currentRotation);
        }

        float magnitude = Mathf.Abs(_currentRotation);

        if (magnitude <= DEAD_ZONE)
        {
            _currentPercentage = 0f;
            _isIncreasingLift = false;
            _isReducingLift = false;
        }
        else if (_currentRotation > DEAD_ZONE)
        {
            _isIncreasingLift = true;
            _isReducingLift = false;
            _currentPercentage = Mathf.Clamp(magnitude, 0f, maxRotationDegrees);
        }
        else
        {
            _isIncreasingLift = false;
            _isReducingLift = true;
            _currentPercentage = Mathf.Clamp(magnitude, 0f, maxRotationDegrees);
        }

        ApplyLiftRequest();
        UpdateHandleColor();
        MaybeLogStatus();
    }

    /// <summary>
    /// Convenience setter for scripts/UI buttons to jump to a specific percent.
    /// </summary>
    public void SetPercentage(float percentage)
    {
        float angle = Mathf.Clamp(percentage, 0f, 100f) * (maxRotationDegrees / 100f);
        SetRotation(angle);
    }

    /// <summary>
    /// Reset handle to idle (0% lift allocation).
    /// </summary>
    public void ResetToIdle()
    {
        SetRotation(0f);

        if (audioSource != null && idleBellSound != null)
        {
            audioSource.PlayOneShot(idleBellSound, 0.7f);
        }
    }

    void ApplyLiftRequest()
    {
        if (targetLiftDevice == null)
        {
            _allocatedPowerPerSecond = 0f;
            return;
        }
        
        // Check if lift is unmanned when trying to change altitude
        bool tryingToChangeLift = _isIncreasingLift || _isReducingLift;
        bool liftUnmanned = targetLiftDevice.crewStation != null && targetLiftDevice.crewStation.AssignedCrewCount == 0;
        
        if (tryingToChangeLift && liftUnmanned && Time.time - _lastMessageTime > MESSAGE_COOLDOWN)
        {
            ShowUnmannedMessage();
            _lastMessageTime = Time.time;
        }

        float hoverPower = Mathf.Max(targetLiftDevice.HoverPowerPerSecond, 0f);
        float referenceHover = (hoverPower > 0f) ? hoverPower : Mathf.Max(hoverPowerFallback, 0f);
        float extraLiftRange = referenceHover * Mathf.Max(0f, ascendPowerMultiple);
        float targetPower = referenceHover;
        float descentFraction = 0f;

        if (_isIncreasingLift)
        {
            float normalized = Mathf.Clamp01(_currentPercentage / Mathf.Max(1f, maxRotationDegrees));
            targetPower = referenceHover + (extraLiftRange * normalized);
            descentFraction = 0f;
        }
        else if (_isReducingLift)
        {
            float normalized = Mathf.Clamp01(_currentPercentage / Mathf.Max(1f, maxRotationDegrees));
            targetPower = referenceHover;
            descentFraction = normalized;
        }
        else
        {
            descentFraction = 0f;
        }

        targetLiftDevice.SetPowerAllocation(targetPower);
        targetLiftDevice.SetControlledDescentFraction(descentFraction);
        _allocatedPowerPerSecond = targetLiftDevice.allocatedPowerPerSecond;
    }

    void UpdateHandleColor()
    {
        if (handleImage == null)
            return;

        float hoverPower = (targetLiftDevice != null) ? targetLiftDevice.HoverPowerPerSecond : 0f;
        float referenceHover = (hoverPower > 0f) ? hoverPower : Mathf.Max(hoverPowerFallback, 0f);
        float maxRequestedPower = referenceHover + referenceHover * Mathf.Max(0f, ascendPowerMultiple);
        float blend = (maxRequestedPower > 0f)
            ? Mathf.Clamp01(_allocatedPowerPerSecond / maxRequestedPower)
            : Mathf.Clamp01(_currentPercentage / 100f);

        handleImage.color = Color.Lerp(idleColor, fullLiftColor, blend);
    }

    void MaybeLogStatus()
    {
        if (!debugLog)
            return;

        if (Mathf.Abs(_currentPercentage - lastLoggedPercent) >= 5f)
        {
            string mode = (!_isIncreasingLift && !_isReducingLift) ? "HOVER" :
                          _isIncreasingLift ? "ASCEND" : "DESCEND";
            float hoverPower = (targetLiftDevice != null) ? Mathf.Max(0f, targetLiftDevice.HoverPowerPerSecond) : 0f;
            float referenceHover = (hoverPower > 0f) ? hoverPower : Mathf.Max(hoverPowerFallback, 0f);
            float maxRequestedPower = referenceHover + referenceHover * Mathf.Max(0f, ascendPowerMultiple);
            FileLogger.Log($"Lift Chadburn [{mode}] {_currentPercentage:F0}% -> {_allocatedPowerPerSecond:F1}/s (hover {hoverPower:F1}/s, req<=~{maxRequestedPower:F1}/s)", "LiftChadburn");
            lastLoggedPercent = _currentPercentage;
        }
    }
    
    /// <summary>
    /// Display a random message indicating the lift is unmanned.
    /// </summary>
    private void ShowUnmannedMessage()
    {
        if (messageBox != null && unmannedLiftMessages != null && unmannedLiftMessages.Length > 0)
        {
            messageBox.ShowRandomMessage(unmannedLiftMessages);
        }
    }
}
