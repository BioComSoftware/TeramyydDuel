using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Roll control lever for ship attitude (visual orientation only).
/// Vertical (0°) = wings level. Lever rotation directly maps to ship roll angle.
/// Does NOT affect ship velocity, lift, or trajectory - purely visual attitude.
/// 
/// Hermeneutic: Decoupling of appearance (attitude) from Being (velocity vector).
/// Ship can roll while maintaining straight flight path.
/// </summary>
[AddComponentMenu("Teramyyd/UI/Roll Lever Controller")]
public class RollLeverController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [Tooltip("The lever RectTransform that rotates (pivot at bottom).")]
    public RectTransform leverTransform;

    [Tooltip("Ship to control attitude. If empty, auto-discovers ShipCharacteristics.")]
    public ShipCharacteristics targetShip;

    [Header("Roll Limits")]
    [Tooltip("Snap lever to increments (0 = smooth, 5 = snap every 5 degrees).")]
    [Range(0f, 15f)]
    public float snapIncrement = 0f;

    [Header("Visual Feedback")]
    [Tooltip("Lever color at wings level (0°).")]
    public Color levelColor = Color.white;

    [Tooltip("Lever color at maximum roll (left or right).")]
    public Color maxRollColor = new Color(1f, 0.6f, 0f, 1f); // Orange

    [Header("Audio (Optional)")]
    [Tooltip("Sound when lever moves.")]
    public AudioClip leverMoveSound;

    [Tooltip("Sound when returning to wings level.")]
    public AudioClip levelBellSound;

    [Header("Status (Read-Only)")]
    [SerializeField] private float _currentLeverAngle = 0f;
    [SerializeField] private float _currentShipRoll = 0f;
    [SerializeField] private float _maxRollDegrees = 45f; // Synced from ShipCharacteristics

    [Header("Debug")]
    public bool debugLog = false;
    
    // Public accessor for maxRollDegrees
    public float maxRollDegrees => _maxRollDegrees;

    // Components
    private Image leverImage;
    private AudioSource audioSource;
    private Canvas canvas;

    // State
    private bool isDragging = false;
    private float lastLoggedAngle = -999f;

    // Public accessors
    public float CurrentLeverAngle => _currentLeverAngle;
    public float CurrentShipRoll => _currentShipRoll;
    public bool IsRollingLeft => _currentLeverAngle < -1f;
    public bool IsRollingRight => _currentLeverAngle > 1f;
    public bool IsWingsLevel => Mathf.Abs(_currentLeverAngle) <= 1f;

    void Awake()
    {
        if (leverTransform != null)
        {
            leverImage = leverTransform.GetComponent<Image>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (leverMoveSound != null || levelBellSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D UI sound
        }

        canvas = GetComponentInParent<Canvas>();

        if (leverTransform == null)
        {
            Debug.LogError($"[RollLever] {gameObject.name} requires leverTransform reference!");
        }
    }

    void Start()
    {
        if (targetShip == null)
        {
            targetShip = FindFirstObjectByType<ShipCharacteristics>();
        }

        if (targetShip == null)
        {
            Debug.LogWarning("[RollLever] No ShipCharacteristics found. Lever is idle.");
        }
        else
        {
            // Sync max roll from ShipCharacteristics
            _maxRollDegrees = targetShip.maxRollDegrees;
            
            if (debugLog)
            {
                FileLogger.Log($"Roll Lever controlling {targetShip.gameObject.name}, max roll: ±{_maxRollDegrees}° (synced from ShipCharacteristics)", "RollLever");
            }
        }

        // Initialize at wings level
        SetLeverAngle(0f);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;

        if (debugLog)
        {
            FileLogger.Log($"Roll Lever drag start at {_currentLeverAngle:F1}°", "RollLever");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || leverTransform == null)
            return;

        // Calculate angle from lever base to mouse position
        Vector2 leverScreenPos = RectTransformUtility.WorldToScreenPoint(
            canvas != null ? canvas.worldCamera : null,
            leverTransform.position
        );

        Vector2 direction = eventData.position - leverScreenPos;
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

        // Clamp to ±maxRollDegrees
        angle = Mathf.Clamp(angle, -_maxRollDegrees, _maxRollDegrees);

        // Apply snapping if enabled
        if (snapIncrement > 0f)
        {
            angle = Mathf.Round(angle / snapIncrement) * snapIncrement;
        }

        // Only update if changed significantly
        if (Mathf.Abs(angle - _currentLeverAngle) > 0.1f)
        {
            SetLeverAngle(angle);

            // Play movement sound
            if (audioSource != null && leverMoveSound != null && !audioSource.isPlaying)
            {
                audioSource.PlayOneShot(leverMoveSound, 0.3f);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        // Play level bell if returned to wings level
        if (IsWingsLevel && audioSource != null && levelBellSound != null)
        {
            audioSource.PlayOneShot(levelBellSound, 0.5f);
        }

        if (debugLog)
        {
            FileLogger.Log($"Roll Lever drag end at {_currentLeverAngle:F1}°", "RollLever");
        }
    }

    /// <summary>
    /// Set lever angle and apply roll to ship.
    /// Lever angle maps 1:1 to ship roll angle.
    /// </summary>
    public void SetLeverAngle(float angleDegrees)
    {
        // Clamp to valid roll range
        _currentLeverAngle = Mathf.Clamp(angleDegrees, -_maxRollDegrees, _maxRollDegrees);

        // Update visual lever rotation
        if (leverTransform != null)
        {
            leverTransform.localRotation = Quaternion.Euler(0f, 0f, -_currentLeverAngle); // Negative for correct visual direction
        }

        // Apply roll to ship (attitude only, no velocity change)
        if (targetShip != null)
        {
            targetShip.SetRollAttitude(_currentLeverAngle);
            _currentShipRoll = targetShip.currentRollDegrees;
        }
        else
        {
            _currentShipRoll = 0f;
        }

        // Update visual feedback
        UpdateLeverColor();

        // Debug logging
        if (debugLog && Mathf.Abs(_currentLeverAngle - lastLoggedAngle) >= 5f)
        {
            string status = IsWingsLevel ? "WINGS LEVEL" :
                           IsRollingLeft ? $"ROLLING LEFT {Mathf.Abs(_currentLeverAngle):F1}°" :
                           $"ROLLING RIGHT {_currentLeverAngle:F1}°";
            FileLogger.Log($"Roll Lever: {status}", "RollLever");
            lastLoggedAngle = _currentLeverAngle;
        }
    }

    /// <summary>
    /// Update lever color based on roll angle.
    /// </summary>
    void UpdateLeverColor()
    {
        if (leverImage == null)
            return;

        float normalizedRoll = Mathf.Abs(_currentLeverAngle) / _maxRollDegrees;
        leverImage.color = Color.Lerp(levelColor, maxRollColor, normalizedRoll);
    }

    /// <summary>
    /// Reset lever to wings level (0°).
    /// </summary>
    public void ResetToWingsLevel()
    {
        SetLeverAngle(0f);

        if (audioSource != null && levelBellSound != null)
        {
            audioSource.PlayOneShot(levelBellSound, 0.7f);
        }

        if (debugLog)
        {
            FileLogger.Log("Roll Lever reset to WINGS LEVEL", "RollLever");
        }
    }

    /// <summary>
    /// Set lever to specific percentage of max roll (-100 to +100).
    /// Negative = left roll, Positive = right roll.
    /// </summary>
    public void SetRollPercentage(float percentage)
    {
        float angle = Mathf.Clamp(percentage, -100f, 100f) * (_maxRollDegrees / 100f);
        SetLeverAngle(angle);
    }
}
