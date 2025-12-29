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

    [Tooltip("Container for the health bar (usually the canvas itself).")]
    public RectTransform healthBarContainer;

    [Tooltip("Optional background image for the health bar.")]
    public Image backgroundImage;

    [Tooltip("Green fill image (left-anchored).")]
    public Image healthBarGreenFill;

    [Tooltip("Red fill image (right-anchored).")]
    public Image healthBarRedFill;

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

    [Header("Debug")]
    [Tooltip("Enable debug logging to console and file")]
    public bool debugLog = false;

    private Canvas _canvas;
    private RectTransform _rectTransform;
    private Camera _mainCamera;
    private float _targetWidth;
    private float _cachedHealthPercent = -1f;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _rectTransform = GetComponent<RectTransform>();
        _mainCamera = Camera.main;

        // Configure canvas for world space
        _canvas.renderMode = RenderMode.WorldSpace;

        // Auto-find health component on parent or siblings
        if (targetHealth == null)
        {
            // First try parent (Target object)
            targetHealth = GetComponentInParent<Health>();
            
            // If not found, try siblings (Sphere child of Target)
            if (targetHealth == null && transform.parent != null)
            {
                targetHealth = transform.parent.GetComponentInChildren<Health>();
            }
            
            if (targetHealth == null)
            {
                Debug.LogError("[TargetHealthBar] No Health component found on parent or siblings!");
            }
            else
            {
                if (debugLog)
                {
                    Debug.Log($"[TargetHealthBar] Found Health component on: {targetHealth.gameObject.name}");
                    FileLogger.Log($"Found Health component on: {targetHealth.gameObject.name}", "TargetHealthBar");
                }
            }
        }
        
        // Auto-detect target size
        if (autoDetectTargetSize && targetHealth != null)
        {
            Renderer targetRenderer = targetHealth.GetComponentInChildren<Renderer>();
            if (targetRenderer != null)
            {
                _targetWidth = targetRenderer.bounds.size.x;
                if (debugLog)
                {
                    Debug.Log($"[TargetHealthBar] Auto-detected target width: {_targetWidth}");
                }
            }
            else
            {
                _targetWidth = 4f; // Default fallback
                if (debugLog)
                {
                    Debug.LogWarning("[TargetHealthBar] Could not detect target size, using default 4 units");
                }
            }
        }
        else
        {
            _targetWidth = 4f; // Default
        }

        // Set container reference
        if (healthBarContainer == null)
        {
            healthBarContainer = _rectTransform;
        }

        // Ensure health bar images are created
        EnsureHealthBarImages();

        // Set initial size based on target width
        float worldBarWidth = _targetWidth * barWidthMultiplier;
        float worldBarHeight = worldBarWidth * barHeightRatio;
        _rectTransform.sizeDelta = new Vector2(worldBarWidth, worldBarHeight);
        _rectTransform.localScale = Vector3.one; // Use world units directly

        // Subscribe to health change events
        if (targetHealth != null)
        {
            targetHealth.onHealthChanged.AddListener(OnHealthChanged);
            if (debugLog)
            {
                Debug.Log($"[TargetHealthBar] Subscribed to health events for {targetHealth.gameObject.name}");
                FileLogger.Log($"Subscribed to health events for {targetHealth.gameObject.name}, current health: {targetHealth.currentHealth}/{targetHealth.maxHealth}", "TargetHealthBar");
            }
            // Update display immediately with current health
            UpdateHealthDisplay();
        }
    }

    void EnsureHealthBarImages()
    {
        // Create background if needed
        if (backgroundImage == null && healthBarContainer != null)
        {
            backgroundImage = healthBarContainer.GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = healthBarContainer.gameObject.AddComponent<Image>();
            }
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Dark background
        }

        // Create green fill (left-anchored)
        if (healthBarGreenFill == null && healthBarContainer != null)
        {
            healthBarGreenFill = CreateHealthFillImage(healthBarContainer, "HealthFill_Green", fullHealthColor, leftAnchored: true);
        }

        // Create red fill (right-anchored)
        if (healthBarRedFill == null && healthBarContainer != null)
        {
            healthBarRedFill = CreateHealthFillImage(healthBarContainer, "HealthFill_Red", emptyHealthColor, leftAnchored: false);
        }
    }

    Image CreateHealthFillImage(RectTransform parent, string name, Color tint, bool leftAnchored)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.type = Image.Type.Simple;
        img.color = tint;

        RectTransform rect = img.rectTransform;
        rect.anchorMin = leftAnchored ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
        rect.anchorMax = leftAnchored ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
        rect.pivot = leftAnchored ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(0f, 0f); // Let anchors control the height

        return img;
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (targetHealth != null)
        {
            targetHealth.onHealthChanged.RemoveListener(OnHealthChanged);
        }
    }

    void OnHealthChanged(float newHealth)
    {
        if (debugLog)
        {
            Debug.Log($"[TargetHealthBar] OnHealthChanged called: newHealth={newHealth}, maxHealth={targetHealth.maxHealth}, percentage={newHealth / targetHealth.maxHealth}");
            FileLogger.Log($"OnHealthChanged: newHealth={newHealth}, maxHealth={targetHealth.maxHealth}, percentage={newHealth / targetHealth.maxHealth}", "TargetHealthBar");
        }
        // Update display when health changes
        UpdateHealthDisplay();
    }

    void LateUpdate()
    {
        // Check if target still exists (not destroyed)
        if (targetHealth == null || targetHealth.gameObject == null || _mainCamera == null)
        {
            // Target was destroyed, hide and clean up
            if (_canvas != null)
            {
                _canvas.enabled = false;
            }
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
    }

    void UpdateHealthDisplay()
    {
        if (targetHealth == null || healthBarContainer == null)
        {
            return;
        }

        // Calculate health percentage
        float healthPercentage = Mathf.Clamp01(targetHealth.currentHealth / targetHealth.maxHealth);

        // Skip update if percentage hasn't changed
        if (Mathf.Approximately(healthPercentage, _cachedHealthPercent))
        {
            return;
        }

        _cachedHealthPercent = healthPercentage;

        if (debugLog)
        {
            Debug.Log($"[TargetHealthBar] UpdateHealthDisplay: currentHealth={targetHealth.currentHealth}, maxHealth={targetHealth.maxHealth}, percentage={healthPercentage}");
            FileLogger.Log($"UpdateHealthDisplay: currentHealth={targetHealth.currentHealth}, maxHealth={targetHealth.maxHealth}, percentage={healthPercentage}", "TargetHealthBar");
        }

        // Apply health bar widths (same as cannon health bars)
        ApplyHealthBarWidths(healthPercentage);
    }

    void ApplyHealthBarWidths(float percent)
    {
        if (healthBarContainer == null || healthBarGreenFill == null || healthBarRedFill == null)
        {
            return;
        }

        float totalWidth = healthBarContainer.rect.width;
        if (totalWidth <= 0f)
        {
            totalWidth = healthBarContainer.sizeDelta.x;
        }
        totalWidth = Mathf.Max(totalWidth, 1f);

        float greenWidth = totalWidth * percent;
        float redWidth = totalWidth - greenWidth;

        SetRectWidth(healthBarGreenFill.rectTransform, greenWidth);
        SetRectWidth(healthBarRedFill.rectTransform, redWidth);

        healthBarGreenFill.enabled = greenWidth > 0.01f;
        healthBarRedFill.enabled = redWidth > 0.01f;

        if (debugLog)
        {
            Debug.Log($"[TargetHealthBar] Set widths: green={greenWidth}, red={redWidth} (total={totalWidth})");
            FileLogger.Log($"Set widths: green={greenWidth}, red={redWidth}, total={totalWidth}", "TargetHealthBar");
        }
    }

    void SetRectWidth(RectTransform rect, float width)
    {
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0f, width));
    }

    bool ShouldShowHealthBar()
    {
        if (targetHealth == null)
        {
            return false;
        }

        // Always hide when dead (hardcoded)
        if (targetHealth.currentHealth <= 0)
        {
            return false;
        }

        // Hide when full health (optional)
        if (hideWhenFullHealth && targetHealth.currentHealth >= targetHealth.maxHealth)
        {
            return false;
        }

        return true;
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
