using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Projects the current target selected by <see cref="TargetingController"/> into screen space and
/// positions a UI rect so it appears as a red box overlay around the target.
/// Requires a screen-space Canvas containing a RectTransform (typically an Image) that draws the box.
/// </summary>
public class TargetHighlightOverlay : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TargetingController targetingController;

    [SerializeField]
    private RectTransform highlightRect;

    [SerializeField]
    private Graphic highlightGraphic;

    [SerializeField]
    private Canvas canvas;

    [Tooltip("Optional override for the world camera used to project the target bounds.")]
    [SerializeField]
    private Camera worldCameraOverride;

    [Tooltip("Optional override for the UI camera if the canvas uses Screen Space - Camera.")]
    [SerializeField]
    private Camera uiCamera;

    [Header("Appearance")]
    [Tooltip("Extra padding (in pixels) added around the projected bounds.")]
    [SerializeField]
    private Vector2 padding = new Vector2(24f, 24f);

    [Tooltip("Minimum size (in pixels) the box is allowed to shrink to.")]
    [SerializeField]
    private float minimumSize = 32f;

    [Tooltip("When enabled the highlight hides itself if the target is behind the camera or off-screen.")]
    [SerializeField]
    private bool hideWhenOffscreen = true;

    private RectTransform _canvasRect;

    void Awake()
    {
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        _canvasRect = canvas != null ? canvas.transform as RectTransform : null;

        if (highlightRect == null)
        {
            highlightRect = GetComponent<RectTransform>();
        }

        if (highlightGraphic == null && highlightRect != null)
        {
            highlightGraphic = highlightRect.GetComponent<Graphic>();
        }

        if (targetingController == null)
        {
            targetingController = FindObjectOfType<TargetingController>();
        }

        SetHighlightVisible(false);
    }

    void LateUpdate()
    {
        UpdateHighlight();
    }

    void UpdateHighlight()
    {
        if (targetingController == null || highlightRect == null || _canvasRect == null)
        {
            SetHighlightVisible(false);
            return;
        }

        var target = targetingController.CurrentTarget;
        if (target == null)
        {
            SetHighlightVisible(false);
            return;
        }

        if (!TryGetTargetBounds(target.transform, out Bounds bounds))
        {
            // Fallback to a small box around the transform if no renderer/collider is present.
            Vector3 center = target.transform.position;
            bounds = new Bounds(center, Vector3.one);
        }

        if (!TryProjectBounds(bounds, out Vector2 screenMin, out Vector2 screenMax))
        {
            if (hideWhenOffscreen)
            {
                SetHighlightVisible(false);
                return;
            }

            // Use the target's position projected into screen space as a minimal fallback.
            Camera referenceCamera = GetWorldCamera();
            if (referenceCamera == null)
            {
                SetHighlightVisible(false);
                return;
            }

            Vector3 screenCenter = referenceCamera.WorldToScreenPoint(bounds.center);
            screenMin = screenCenter - new Vector3(minimumSize * 0.5f, minimumSize * 0.5f, 0f);
            screenMax = screenCenter + new Vector3(minimumSize * 0.5f, minimumSize * 0.5f, 0f);
        }

        UpdateRectTransform(screenMin, screenMax);
    }

    bool TryGetTargetBounds(Transform targetTransform, out Bounds bounds)
    {
        var renderers = targetTransform.GetComponentsInChildren<Renderer>();
        bool hasRenderer = false;
        bounds = new Bounds();

        foreach (var renderer in renderers)
        {
            if (!renderer.enabled)
                continue;

            if (!hasRenderer)
            {
                bounds = renderer.bounds;
                hasRenderer = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (hasRenderer)
            return true;

        var colliders = targetTransform.GetComponentsInChildren<Collider>();
        bool hasCollider = false;
        foreach (var collider in colliders)
        {
            if (!collider.enabled)
                continue;

            if (!hasCollider)
            {
                bounds = collider.bounds;
                hasCollider = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return hasCollider;
    }

    bool TryProjectBounds(Bounds bounds, out Vector2 screenMin, out Vector2 screenMax)
    {
        Camera referenceCamera = GetWorldCamera();
        screenMin = new Vector2(float.MaxValue, float.MaxValue);
        screenMax = new Vector2(float.MinValue, float.MinValue);
        bool hasVisiblePoint = false;

        if (referenceCamera == null)
            return false;

        Vector3 extents = bounds.extents;
        Vector3 center = bounds.center;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    Vector3 screenPoint = referenceCamera.WorldToScreenPoint(corner);
                    if (screenPoint.z <= 0f)
                        continue;

                    hasVisiblePoint = true;
                    Vector2 point2D = new Vector2(screenPoint.x, screenPoint.y);
                    screenMin = Vector2.Min(screenMin, point2D);
                    screenMax = Vector2.Max(screenMax, point2D);
                }
            }
        }

        return hasVisiblePoint;
    }

    void UpdateRectTransform(Vector2 screenMin, Vector2 screenMax)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenMin, uiCamera, out Vector2 localMin);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenMax, uiCamera, out Vector2 localMax);

        Vector2 localCenter = (localMin + localMax) * 0.5f;
        Vector2 localSize = new Vector2(Mathf.Abs(localMax.x - localMin.x), Mathf.Abs(localMax.y - localMin.y));

        localSize += padding;
        localSize.x = Mathf.Max(localSize.x, minimumSize);
        localSize.y = Mathf.Max(localSize.y, minimumSize);

        highlightRect.anchoredPosition = localCenter;
        highlightRect.sizeDelta = localSize;

        SetHighlightVisible(true);
    }

    Camera GetWorldCamera()
    {
        if (worldCameraOverride != null)
            return worldCameraOverride;

        if (targetingController != null && targetingController.TargetingCamera != null)
            return targetingController.TargetingCamera;

        return Camera.main;
    }

    void SetHighlightVisible(bool isVisible)
    {
        if (highlightRect == null)
            return;

        if (highlightRect != null && !highlightRect.gameObject.activeSelf)
        {
            highlightRect.gameObject.SetActive(true);
        }

        if (highlightGraphic == null && highlightRect != null)
        {
            highlightGraphic = highlightRect.GetComponent<Graphic>();
        }

        if (highlightGraphic != null)
        {
            highlightGraphic.enabled = isVisible;
        }
        else if (highlightRect != null)
        {
            var canvasRenderer = highlightRect.GetComponent<CanvasRenderer>();
            if (canvasRenderer != null)
            {
                canvasRenderer.SetAlpha(isVisible ? 1f : 0f);
            }
        }
    }

    /// <summary>
    /// Allows other systems (like UI setup scripts) to manually inject dependencies at runtime.
    /// </summary>
    public void Configure(TargetingController controller, RectTransform rect, Canvas targetCanvas)
    {
        targetingController = controller;
        highlightRect = rect;
        canvas = targetCanvas;
        _canvasRect = canvas != null ? canvas.transform as RectTransform : null;
    }
}
