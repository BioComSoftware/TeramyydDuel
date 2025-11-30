using UnityEngine;

/// <summary>
/// Slides a lift-device temperature strip so the pixel that matches the
/// AntiGravityDevice's current temperature is centered inside the viewport.
/// Attach to the LiftdeviceTemperatureReadout GameObject.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class LiftDeviceTemperatureGauge : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("AntiGravityDevice providing CurrentTemperature readings.")]
    public AntiGravityDevice targetLiftDevice;

    [Header("Temperature Range (Â°F)")]
    [Tooltip("Minimum temperature mapped to the start of the strip (usually 0).")]
    public float minTemperature = 0f;

    [Header("Movement Scaling")]
    [Tooltip("Degrees represented by a single pixel on the temperature strip.")] 
    public float degreesPerPixel = 1f;

    [Tooltip("Optional fine-tuning offset applied after calibration.")]
    public float centerOffsetX = 0f;

    private RectTransform _stripTransform;
    private Vector2 _baselineAnchoredPosition;
    private float _imagePixelLength;

    private void Awake()
    {
        _stripTransform = GetComponent<RectTransform>();
        _baselineAnchoredPosition = _stripTransform.anchoredPosition;
        _imagePixelLength = _stripTransform.rect.width <= 0f ? 0f : _stripTransform.rect.width;

        if (targetLiftDevice == null)
        {
            targetLiftDevice = FindFirstObjectByType<AntiGravityDevice>();
        }
    }

    private void LateUpdate()
    {
        if (targetLiftDevice == null)
        {
            return;
        }

        float currentTemp = targetLiftDevice.CurrentTemperature;
        float clampedTemp = Mathf.Max(currentTemp, minTemperature);
        float degreesAboveMin = clampedTemp - minTemperature;

        float pixelPosition = 0f;
        if (degreesPerPixel > 0f)
        {
            pixelPosition = degreesAboveMin / degreesPerPixel;
        }
        pixelPosition = Mathf.Clamp(pixelPosition, 0f, _imagePixelLength);

        float anchoredX = _baselineAnchoredPosition.x - pixelPosition + centerOffsetX;
        _stripTransform.anchoredPosition = new Vector2(anchoredX, _baselineAnchoredPosition.y);
    }
}
