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
    [SerializeField] private float _maxLiftPowerPerSecond = 0f;
    [SerializeField] private bool _isIncreasingLift = false;
    [SerializeField] private bool _isReducingLift = false;

    [Header("Debug")]
    public bool debugLog = false;

    // Components
    private Image handleImage;
    private AudioSource audioSource;
    private Canvas canvas;

    // State
    private bool isDragging = false;
    private float lastLoggedPercent = -1f;

    // Public accessors
    public float CurrentRotation => _currentRotation;
    public float CurrentPercentage => _currentPercentage;
    public float AllocatedPowerPerSecond => _allocatedPowerPerSecond;
    public float MaxLiftPowerPerSecond => _maxLiftPowerPerSecond;

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

        RefreshMaxLiftCache();
        SetRotation(0f);
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

        RefreshMaxLiftCache();
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

    void RefreshMaxLiftCache()
    {
        _maxLiftPowerPerSecond = (targetLiftDevice != null) ? targetLiftDevice.MaxLiftPowerPerSecond : 0f;
    }

    void ApplyLiftRequest()
    {
        if (targetLiftDevice == null)
        {
            _allocatedPowerPerSecond = 0f;
            return;
        }

        float minPower = Mathf.Max(0f, targetLiftDevice.minimumPowerPerSecond);
        float maxPower = targetLiftDevice.MaxLiftPowerPerSecond;
        float targetPower = minPower;

        if (_isIncreasingLift)
        {
            float normalized = Mathf.Clamp01(_currentPercentage / Mathf.Max(1f, maxRotationDegrees));
            targetPower = Mathf.Lerp(minPower, maxPower, normalized);
        }
        else if (_isReducingLift)
        {
            float normalized = Mathf.Clamp01(_currentPercentage / Mathf.Max(1f, maxRotationDegrees));
            targetPower = Mathf.Lerp(minPower, 0f, normalized);
        }

        targetLiftDevice.SetPowerAllocation(targetPower);
        _allocatedPowerPerSecond = targetLiftDevice.allocatedPowerPerSecond;
    }

    void UpdateHandleColor()
    {
        if (handleImage == null)
            return;

        float blend = (_maxLiftPowerPerSecond > 0f)
            ? Mathf.Clamp01(_allocatedPowerPerSecond / _maxLiftPowerPerSecond)
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
            float minPower = (targetLiftDevice != null) ? Mathf.Max(0f, targetLiftDevice.minimumPowerPerSecond) : 0f;
            FileLogger.Log($"Lift Chadburn [{mode}] {_currentPercentage:F0}% -> {_allocatedPowerPerSecond:F1}/s (min {minPower:F1}/s, max {_maxLiftPowerPerSecond:F1}/s)", "LiftChadburn");
            lastLoggedPercent = _currentPercentage;
        }
    }
}
