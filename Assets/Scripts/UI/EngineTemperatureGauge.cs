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
    [Tooltip("Minimum temperature mapped to the start of the strip (usually 0).")]
    public float minTemperature = 0f;

    [Header("Movement Scaling")]
    [Tooltip("Degrees represented by a single pixel on the temperature strip.\nExample: 2 => every 2°F moves the strip 1px.")]
    public float degreesPerPixel = 1f;

    [Tooltip("Optional final tweak after calibration.")]
    public float centerOffsetX = 0f;

    private RectTransform _stripTransform;
    private Vector2 _baselineAnchoredPosition;
    private float _imagePixelLength;

    private void Awake()
    {
        _stripTransform = GetComponent<RectTransform>();
        _baselineAnchoredPosition = _stripTransform.anchoredPosition;
        _imagePixelLength = _stripTransform.rect.width <= 0f ? 0f : _stripTransform.rect.width;

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
        float clampedTemp = Mathf.Max(currentTemp, minTemperature);
        float degreesAboveMin = clampedTemp - minTemperature;

        float pixelPosition = 0f;
        if (degreesPerPixel > 0f)
        {
            pixelPosition = degreesAboveMin / degreesPerPixel;
        }
        pixelPosition = Mathf.Clamp(pixelPosition, 0f, _imagePixelLength);

        // Shift relative to the original anchored position so low temps stay near their initial spot.
        float anchoredX = _baselineAnchoredPosition.x - pixelPosition + centerOffsetX;
        _stripTransform.anchoredPosition = new Vector2(anchoredX, _baselineAnchoredPosition.y);
    }
}
