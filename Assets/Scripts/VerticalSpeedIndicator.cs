using UnityEngine;

/// <summary>
/// Simplified vertical speed indicator: sample altitude, compute the required
/// angle, and set the needle instantly. No interpolation or easing.
/// </summary>
[AddComponentMenu("Teramyyd/HUD/Vertical Speed Indicator")]
public class VerticalSpeedIndicator : MonoBehaviour
{
    [Header("References")]
    public RectTransform needleTransform;
    public ShipCharacteristics shipCharacteristics;

    [Header("Dial Setup")]
    [Tooltip("Dial angle (degrees) used for 0 m/s. 270 = pointing left.")]
    public float zeroRotationDegrees = 270f;

    [Tooltip("Degrees to rotate for each meter/second of vertical speed.")]
    public float degreesPerMeterPerSecond = 9f;

    [Header("Sampling")]
    [Tooltip("Seconds between samples. Set to 0 for per-frame updates.")]
    public float sampleIntervalSeconds = 0.5f;

    [Header("Runtime (Read-Only)")]
    [SerializeField] private float currentVerticalSpeed;
    [SerializeField] private float lastSampleAltitude;
    [SerializeField] private float lastSampleTime;

    private bool hasSample;

    private void Start()
    {
        if (shipCharacteristics == null)
        {
            shipCharacteristics = FindFirstObjectByType<ShipCharacteristics>();
        }

        if (needleTransform == null)
        {
            needleTransform = GetComponentInChildren<RectTransform>();
        }

        if (shipCharacteristics == null)
        {
            Debug.LogError($"VerticalSpeedIndicator on {gameObject.name}: ShipCharacteristics is missing.");
        }

        if (needleTransform == null)
        {
            Debug.LogError($"VerticalSpeedIndicator on {gameObject.name}: Needle Transform is missing.");
        }
    }

    private void Update()
    {
        if (shipCharacteristics == null || needleTransform == null)
            return;

        float now = Time.time;
        float currentAltitude = shipCharacteristics.currentAltitude;

        if (!hasSample)
        {
            lastSampleAltitude = currentAltitude;
            lastSampleTime = now;
            hasSample = true;
            PointNeedle(zeroRotationDegrees);
            return;
        }

        float elapsed = now - lastSampleTime;
        bool shouldSample = sampleIntervalSeconds <= 0f || elapsed >= sampleIntervalSeconds;
        if (!shouldSample)
            return; // Hold the last position until next sample window.

        if (elapsed <= Mathf.Epsilon)
            return;

        currentVerticalSpeed = (currentAltitude - lastSampleAltitude) / elapsed;
        float targetRotation = zeroRotationDegrees + (currentVerticalSpeed * degreesPerMeterPerSecond);
        PointNeedle(targetRotation);

        lastSampleAltitude = currentAltitude;
        lastSampleTime = now;
    }

    private void PointNeedle(float rotationDegrees)
    {
        needleTransform.localRotation = Quaternion.Euler(0f, 0f, -rotationDegrees);
    }
}
