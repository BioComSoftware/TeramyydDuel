using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Pitch control lever for ship attitude (visual orientation only).
/// Starts at 90Â° (level flight). Up (toward 0Â°) = nose up, Down (toward 180Â°) = nose down.
/// Asymmetric limits: maxPitchUpDegrees and maxPitchDownDegrees define lever range.
/// Does NOT affect ship velocity, lift, or trajectory - purely visual attitude.
/// 
/// Hermeneutic: Ship can pitch nose-down while climbing, or nose-up while descending.
/// Attitude is aesthetic overlay on physics, not causal force.
/// </summary>
[AddComponentMenu("Teramyyd/UI/Pitch Lever Controller")]
public class PitchLeverController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const float LEVEL_FLIGHT_ANGLE = 90f; // Lever at 90Â° = ship level

    [Header("References")]
    [Tooltip("The lever RectTransform that rotates (pivot at bottom).")]
    public RectTransform leverTransform;

    [Tooltip("Ship to control attitude. If empty, auto-discovers ShipCharacteristics.")]
    public ShipCharacteristics targetShip;

    [Header("Pitch Limits")]
    [Tooltip("Maximum nose-up pitch in degrees. Lever moves from 90Â° toward 0Â°.")]
    [Range(5f, 90f)]
    public float maxPitchUpDegrees = 30f;

    [Tooltip("Maximum nose-down pitch in degrees. Lever moves from 90Â° toward 180Â°.")]
    [Range(5f, 90f)]
    public float maxPitchDownDegrees = 20f;

    [Tooltip("Reverse pitch direction: When checked, lever up = nose down, lever down = nose up.")]
    public bool reversePitchDirection = false;

    [Tooltip("Snap lever to increments (0 = smooth, 5 = snap every 5 degrees).")]
    [Range(0f, 15f)]
    public float snapIncrement = 0f;

    [Header("Visual Feedback")]
    [Tooltip("Lever color at level flight (90Â°).")]
    public Color levelColor = Color.white;

    [Tooltip("Lever color at maximum nose-up pitch.")]
    public Color pitchUpColor = new Color(0.4f, 1f, 0.4f, 1f); // Green

    [Tooltip("Lever color at maximum nose-down pitch.")]
    public Color pitchDownColor = new Color(1f, 0.4f, 0.4f, 1f); // Red

    [Header("Audio (Optional)")]
    [Tooltip("Sound when lever moves.")]
    public AudioClip leverMoveSound;

    [Tooltip("Sound when returning to level flight.")]
    public AudioClip levelBellSound;

    [Header("Status (Read-Only)")]
    [SerializeField] private float _currentLeverAngle = LEVEL_FLIGHT_ANGLE;
    [SerializeField] private float _currentShipPitch = 0f;
    [SerializeField] private float _minLeverAngle = 0f; // Calculated: 90 - maxPitchUp
    [SerializeField] private float _maxLeverAngle = 0f; // Calculated: 90 + maxPitchDown

    [Header("Debug")]
    public bool debugLog = false;

    // Components
    private Image leverImage;
    private AudioSource audioSource;
    private Canvas canvas;

    // State
    private bool isDragging = false;
    private float lastLoggedAngle = -999f;

    // Public accessors
    public float CurrentLeverAngle => _currentLeverAngle;
    public float CurrentShipPitch => _currentShipPitch;
    public bool IsPitchingUp => _currentLeverAngle < (LEVEL_FLIGHT_ANGLE - 1f);
    public bool IsPitchingDown => _currentLeverAngle > (LEVEL_FLIGHT_ANGLE + 1f);
    public bool IsLevelFlight => Mathf.Abs(_currentLeverAngle - LEVEL_FLIGHT_ANGLE) <= 1f;

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
            Debug.LogError($"[PitchLever] {gameObject.name} requires leverTransform reference!");
        }

        // Calculate lever angle constraints
        RecalculateLeverLimits();
    }

    void Start()
    {
        if (targetShip == null)
        {
            targetShip = FindFirstObjectByType<ShipCharacteristics>();
        }

        if (targetShip == null)
        {
            Debug.LogWarning("[PitchLever] No ShipCharacteristics found. Lever is idle.");
        }
        else
        {
            // Recalculate limits now that we have targetShip reference
            RecalculateLeverLimits();
            
            if (debugLog)
            {
                FileLogger.Log($"Pitch Lever controlling {targetShip.gameObject.name}, pitch range: +{maxPitchUpDegrees}Â° / -{maxPitchDownDegrees}Â°", "PitchLever");
            }
        }

        // Initialize at level flight (90Â°)
        SetLeverAngle(LEVEL_FLIGHT_ANGLE);
    }

    void RecalculateLeverLimits()
    {
        // Sync pitch limits from ShipCharacteristics if available
        if (targetShip != null)
        {
            maxPitchUpDegrees = targetShip.maxPitchUpDegrees;
            maxPitchDownDegrees = targetShip.maxPitchDownDegrees;
        }
        
        // Lever range: (90 - maxPitchUp) to (90 + maxPitchDown)
        _minLeverAngle = LEVEL_FLIGHT_ANGLE - maxPitchUpDegrees;
        _maxLeverAngle = LEVEL_FLIGHT_ANGLE + maxPitchDownDegrees;

        if (debugLog)
        {
            FileLogger.Log($"Pitch Lever limits: {_minLeverAngle:F1}Â° (max nose-up) to {_maxLeverAngle:F1}Â° (max nose-down), level at {LEVEL_FLIGHT_ANGLE}Â° (synced from ShipCharacteristics)", "PitchLever");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;

        if (debugLog)
        {
            FileLogger.Log($"Pitch Lever drag start at {_currentLeverAngle:F1}Â° (ship pitch: {_currentShipPitch:F1}Â°)", "PitchLever");
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

        // Clamp to asymmetric lever range
        angle = Mathf.Clamp(angle, _minLeverAngle, _maxLeverAngle);

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

        // Play level bell if returned to level flight
        if (IsLevelFlight && audioSource != null && levelBellSound != null)
        {
            audioSource.PlayOneShot(levelBellSound, 0.5f);
        }

        if (debugLog)
        {
            FileLogger.Log($"Pitch Lever drag end at {_currentLeverAngle:F1}Â° (ship pitch: {_currentShipPitch:F1}Â°)", "PitchLever");
        }
    }

    /// <summary>
    /// Set lever angle and apply pitch to ship.
    /// Lever angle maps to ship pitch: 90Â° = level, <90Â° = nose up, >90Â° = nose down.
    /// Ship pitch = leverAngle - 90Â° (so 60Â° lever = +30Â° pitch, 110Â° lever = -20Â° pitch).
    /// </summary>
    public void SetLeverAngle(float angleDegrees)
    {
        // Clamp to valid asymmetric range
        _currentLeverAngle = Mathf.Clamp(angleDegrees, _minLeverAngle, _maxLeverAngle);

        // Update visual lever rotation
        if (leverTransform != null)
        {
            leverTransform.localRotation = Quaternion.Euler(0f, 0f, -_currentLeverAngle); // Negative for correct visual direction
        }

        // Convert lever angle to ship pitch angle
        // Lever 90Â° = 0Â° pitch (level)
        // Normal: Lever 60Â° = +30Â° pitch (nose up), Lever 110Â° = -20Â° pitch (nose down)
        // Reversed: Lever 60Â° = -30Â° pitch (nose down), Lever 110Â° = +20Â° pitch (nose up)
        float shipPitchAngle = reversePitchDirection ? 
            (_currentLeverAngle - LEVEL_FLIGHT_ANGLE) : 
            (LEVEL_FLIGHT_ANGLE - _currentLeverAngle);

        // Apply pitch to ship (attitude only, no velocity change)
        if (targetShip != null)
        {
            targetShip.SetPitchAttitude(shipPitchAngle);
            _currentShipPitch = targetShip.currentPitchDegrees;
        }
        else
        {
            _currentShipPitch = 0f;
        }

        // Update visual feedback
        UpdateLeverColor();

        // Debug logging
        if (debugLog && Mathf.Abs(_currentLeverAngle - lastLoggedAngle) >= 5f)
        {
            string status = IsLevelFlight ? "LEVEL FLIGHT" :
                           IsPitchingUp ? $"NOSE UP {shipPitchAngle:F1}Â°" :
                           $"NOSE DOWN {Mathf.Abs(shipPitchAngle):F1}Â°";
            FileLogger.Log($"Pitch Lever: {_currentLeverAngle:F1}Â° â†’ {status}", "PitchLever");
            lastLoggedAngle = _currentLeverAngle;
        }
    }

    /// <summary>
    /// Update lever color based on pitch.
    /// </summary>
    void UpdateLeverColor()
    {
        if (leverImage == null)
            return;

        if (IsLevelFlight)
        {
            leverImage.color = levelColor;
        }
        else if (IsPitchingUp)
        {
            // Blend from level to pitch-up color
            float normalizedPitch = (_currentLeverAngle - _minLeverAngle) / (LEVEL_FLIGHT_ANGLE - _minLeverAngle);
            leverImage.color = Color.Lerp(pitchUpColor, levelColor, normalizedPitch);
        }
        else // IsPitchingDown
        {
            // Blend from level to pitch-down color
            float normalizedPitch = (_currentLeverAngle - LEVEL_FLIGHT_ANGLE) / (_maxLeverAngle - LEVEL_FLIGHT_ANGLE);
            leverImage.color = Color.Lerp(levelColor, pitchDownColor, normalizedPitch);
        }
    }

    /// <summary>
    /// Reset lever to level flight (90Â°).
    /// </summary>
    public void ResetToLevelFlight()
    {
        SetLeverAngle(LEVEL_FLIGHT_ANGLE);

        if (audioSource != null && levelBellSound != null)
        {
            audioSource.PlayOneShot(levelBellSound, 0.7f);
        }

        if (debugLog)
        {
            FileLogger.Log("Pitch Lever reset to LEVEL FLIGHT", "PitchLever");
        }
    }

    /// <summary>
    /// Set lever to specific pitch percentage.
    /// -100 = max nose down, 0 = level, +100 = max nose up.
    /// </summary>
    public void SetPitchPercentage(float percentage)
    {
        float clampedPercent = Mathf.Clamp(percentage, -100f, 100f);
        
        float angle;
        if (clampedPercent >= 0f)
        {
            // Positive percentage = nose up (lever moves from 90Â° toward minLeverAngle)
            float range = LEVEL_FLIGHT_ANGLE - _minLeverAngle;
            angle = LEVEL_FLIGHT_ANGLE - (range * clampedPercent / 100f);
        }
        else
        {
            // Negative percentage = nose down (lever moves from 90Â° toward maxLeverAngle)
            float range = _maxLeverAngle - LEVEL_FLIGHT_ANGLE;
            angle = LEVEL_FLIGHT_ANGLE + (range * Mathf.Abs(clampedPercent) / 100f);
        }

        SetLeverAngle(angle);
    }

    /// <summary>
    /// Update lever limits if ship pitch constraints change at runtime.
    /// </summary>
    public void UpdatePitchLimits(float newMaxPitchUp, float newMaxPitchDown)
    {
        maxPitchUpDegrees = Mathf.Clamp(newMaxPitchUp, 5f, 90f);
        maxPitchDownDegrees = Mathf.Clamp(newMaxPitchDown, 5f, 90f);
        RecalculateLeverLimits();

        // Re-clamp current lever position to new limits
        SetLeverAngle(_currentLeverAngle);
    }
}
