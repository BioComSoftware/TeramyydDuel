using UnityEngine;

/// <summary>
/// Slides the TemperatureReadout strip (UI Image) inside the masked viewport so the
/// pixel that corresponds to the jet-engine's current temperature is centered.
/// Attach this script to the TemperatureReadout GameObject.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class EngineTemperatureGauge : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("JetEngine component that exposes CurrentTemperature.")]
    public JetEngine targetEngine;

    [Header("Temperature Range (°F)")]
    [Tooltip("Minimum temperature that maps to pixelAtMinDegrees (usually 0).")]
    public float minTemperature = 0f;

    [Tooltip("Maximum temperature that maps to pixelAtMaxDegrees (e.g., 5000).")]
    public float maxTemperature = 5000f;

    [Header("Strip Pixel Positions")]
    [Tooltip("Pixel coordinate on the strip for minTemperature (0°F).")] public float pixelAtMinDegrees = 0f;
    [Tooltip("Pixel coordinate on the strip for maxTemperature (5000°F).")] public float pixelAtMaxDegrees = 815f;

    [Tooltip("Optional final tweak after calibration.")]
    public float centerOffsetX = 0f;

    private RectTransform _stripTransform;
    private Vector2 _baselineAnchoredPosition;

    private void Awake()
    {
        _stripTransform = GetComponent<RectTransform>();
        _baselineAnchoredPosition = _stripTransform.anchoredPosition;

        if (targetEngine == null)
        {
            targetEngine = FindFirstObjectByType<JetEngine>();
        }
    }

    private void LateUpdate()
    {
        if (targetEngine == null)
        {
            return;
        }

        float currentTemp = targetEngine.CurrentTemperature;
        float normalized = Mathf.InverseLerp(minTemperature, maxTemperature, currentTemp);
        float clamped = Mathf.Clamp01(normalized);
        float pixelPosition = Mathf.Lerp(pixelAtMinDegrees, pixelAtMaxDegrees, clamped);

        // Shift relative to the original anchored position so low temps stay near their initial spot.
        float anchoredX = _baselineAnchoredPosition.x - pixelPosition + centerOffsetX;
        _stripTransform.anchoredPosition = new Vector2(anchoredX, _baselineAnchoredPosition.y);
    }
}
