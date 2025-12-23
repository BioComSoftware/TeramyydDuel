using UnityEngine;
using UnityEngine.UI;

/// <summary> 
/// Displays a health bar above the Target object that maintains constant screen size
/// regardless of camera distance. Shows green when full health, transitions to red
/// as health decreases (e.g., 50% health = half green, half red).
/// 
/// Attach this to a Canvas child of the Target object.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class TargetHealthBar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Health component to monitor. Will auto-find on parent if not assigned.")]
    public Health targetHealth;

    [Tooltip("The Image component for the health fill. Will auto-find if not assigned.")]
    public Image healthFillImage;

    [Tooltip("Optional background image for the health bar.")]
    public Image backgroundImage;

    [Header("Appearance")]
    [Tooltip("Color when at full health")]
    public Color fullHealthColor = Color.green;

    [Tooltip("Color when at zero health")]
    public Color emptyHealthColor = Color.red;

    [Tooltip("Width of the health bar relative to target size (1.0 = same width as target)")]
    public float barWidthMultiplier = 1f;

    [Tooltip("Height of the health bar as a fraction of width (default: 0.1 = 1/10th of width)")]
    public float barHeightRatio = 0.1f;

    [Tooltip("Offset above the target object (in world units)")]
    public Vector3 worldOffset = new Vector3(0, 3f, 0);
    
    [Tooltip("Auto-detect target size from renderer bounds")]
    public bool autoDetectTargetSize = true;

    [Header("Scaling")]
    [Tooltip("Multiplier for how much the bar scales with distance (0 = no scaling, 1 = full scaling)")]
    public float distanceScaling = 0.5f;

    [Header("Visibility")]
    [Tooltip("Hide the health bar when at full health")]
    public bool hideWhenFullHealth = false;

    [Tooltip("Hide the health bar when target is dead")]
    public bool hideWhenDead = true;

    private Canvas _canvas;
    private RectTransform _rectTransform;
    private Camera _mainCamera;
    private RectTransform _fillRectTransform;
    private float _targetWidth;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _rectTransform = GetComponent<RectTransform>();
        _mainCamera = Camera.main;

        // Configure canvas for world space
        _canvas.renderMode = RenderMode.WorldSpace;

        // Auto-find health component on parent
        if (targetHealth == null)
        {
            targetHealth = GetComponentInParent<Health>();
            if (targetHealth == null)
            {
                Debug.LogError("[TargetHealthBar] No Health component found on parent!");
            }
        }
        
        // Auto-detect target size
        if (autoDetectTargetSize && targetHealth != null)
        {
            Renderer targetRenderer = targetHealth.GetComponentInChildren<Renderer>();
            if (targetRenderer != null)
            {
                _targetWidth = targetRenderer.bounds.size.x;
                Debug.Log($"[TargetHealthBar] Auto-detected target width: {_targetWidth}");
            }
            else
            {
                _targetWidth = 4f; // Default fallback
                Debug.LogWarning("[TargetHealthBar] Could not detect target size, using default 4 units");
            }
        }
        else
        {
            _targetWidth = 4f; // Default
        }

        // Auto-find fill image if not assigned
        if (healthFillImage == null)
        {
            healthFillImage = transform.Find("Fill")?.GetComponent<Image>();
            if (healthFillImage == null)
            {
                Debug.LogWarning("[TargetHealthBar] No Fill Image assigned or found. Health bar will not display.");
            }
        }

        if (healthFillImage != null)
        {
            _fillRectTransform = healthFillImage.GetComponent<RectTransform>();
        }

        // Set initial size based on target width
        float worldBarWidth = _targetWidth * barWidthMultiplier;
        float worldBarHeight = worldBarWidth * barHeightRatio;
        _rectTransform.sizeDelta = new Vector2(worldBarWidth, worldBarHeight);
        _rectTransform.localScale = Vector3.one; // Use world units directly
    }

    void LateUpdate()
    {
        if (targetHealth == null || _mainCamera == null)
        {
            return;
        }

        // Update visibility
        bool shouldShow = ShouldShowHealthBar();
        _canvas.enabled = shouldShow;
        if (!shouldShow)
        {
            return;
        }

        // Position the health bar above the target
        transform.position = targetHealth.transform.position + worldOffset;

        // Make the health bar face the camera
        transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                        _mainCamera.transform.rotation * Vector3.up);

        // Optional distance scaling (subtle)
        if (distanceScaling > 0)
        {
            float distance = Vector3.Distance(_mainCamera.transform.position, transform.position);
            float scale = 1f + (distance / 50f - 1f) * distanceScaling;
            scale = Mathf.Clamp(scale, 0.5f, 2f);
            transform.localScale = Vector3.one * scale;
        }

        // Update health fill
        UpdateHealthDisplay();
    }

    void UpdateHealthDisplay()
    {
        if (targetHealth == null || healthFillImage == null)
        {
            return;
        }

        // Calculate health percentage
        float healthPercentage = Mathf.Clamp01(targetHealth.currentHealth / targetHealth.maxHealth);

        // Update fill amount (using filled image type)
        if (healthFillImage.type == Image.Type.Filled)
        {
            healthFillImage.fillAmount = healthPercentage;
        }
        else
        {
            // If not using filled type, adjust the width of the fill rect
            if (_fillRectTransform != null)
            {
                _fillRectTransform.anchorMax = new Vector2(healthPercentage, 1f);
            }
        }

        // Interpolate color from red to green based on health
        healthFillImage.color = Color.Lerp(emptyHealthColor, fullHealthColor, healthPercentage);
    }

    bool ShouldShowHealthBar()
    {
        if (targetHealth == null)
        {
            return false;
        }

        // Hide when dead
        if (hideWhenDead && targetHealth.currentHealth <= 0)
        {
            return false;
        }

        // Hide when full health
        if (hideWhenFullHealth && targetHealth.currentHealth >= targetHealth.maxHealth)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Creates a simple health bar UI hierarchy under this canvas
    /// </summary>
    public void CreateDefaultHealthBarUI()
    {
        // Create background
        GameObject bgObject = new GameObject("Background");
        bgObject.transform.SetParent(transform, false);
        backgroundImage = bgObject.AddComponent<Image>();
        backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        RectTransform bgRect = bgObject.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Create fill
        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(transform, false);
        healthFillImage = fillObject.AddComponent<Image>();
        healthFillImage.color = fullHealthColor;
        healthFillImage.type = Image.Type.Filled;
        healthFillImage.fillMethod = Image.FillMethod.Horizontal;
        healthFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        
        _fillRectTransform = fillObject.GetComponent<RectTransform>();
        _fillRectTransform.anchorMin = Vector2.zero;
        _fillRectTransform.anchorMax = Vector2.one;
        _fillRectTransform.sizeDelta = Vector2.zero;

        Debug.Log("[TargetHealthBar] Created default health bar UI");
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Update rect transform size when values change in inspector
        if (_rectTransform != null && _targetWidth > 0)
        {
            float worldBarWidth = _targetWidth * barWidthMultiplier;
            float worldBarHeight = worldBarWidth * barHeightRatio;
            _rectTransform.sizeDelta = new Vector2(worldBarWidth, worldBarHeight);
        }
    }
#endif
}
