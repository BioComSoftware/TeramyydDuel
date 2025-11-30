using UnityEngine;

/// <summary>
/// Zooms the instrument panel while a zoom key is held, scaling and re-centering
/// the designated RectTransform to highlight the gauges on demand.
/// Attach this to InstrumentPanel_Background.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class InstrumentPanelZoom : MonoBehaviour
{
    [Header("Zoom Controls")]
    [Tooltip("Key that triggers the zoom when held.")]
    public KeyCode zoomKey = KeyCode.Z;

    [Tooltip("Scale multiplier applied when zoomed in.")]
    public float zoomScaleMultiplier = 3f;

    [Tooltip("Anchored position applied while zoomed (use this to place the panel in frame).")]
    public Vector2 zoomAnchoredPosition = Vector2.zero;

    [Tooltip("How fast the zoom/position interpolates (higher = snappier).")]
    public float zoomLerpSpeed = 12f;

    private RectTransform _rectTransform;
    private Vector3 _originalScale;
    private Vector2 _originalAnchoredPosition;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _originalScale = _rectTransform.localScale;
        _originalAnchoredPosition = _rectTransform.anchoredPosition;
        if (zoomAnchoredPosition == Vector2.zero)
        {
            zoomAnchoredPosition = _originalAnchoredPosition;
        }
    }

    void Update()
    {
        bool zoomHeld = Input.GetKey(zoomKey);
        Vector3 targetScale = zoomHeld ? _originalScale * zoomScaleMultiplier : _originalScale;
        Vector2 targetPosition = zoomHeld ? zoomAnchoredPosition : _originalAnchoredPosition;

        float lerpFactor = 1f - Mathf.Exp(-zoomLerpSpeed * Time.unscaledDeltaTime);
        _rectTransform.localScale = Vector3.Lerp(_rectTransform.localScale, targetScale, lerpFactor);
        _rectTransform.anchoredPosition = Vector2.Lerp(_rectTransform.anchoredPosition, targetPosition, lerpFactor);
    }
}
