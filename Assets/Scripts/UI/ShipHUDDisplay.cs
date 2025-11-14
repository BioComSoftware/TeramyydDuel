using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Simplified HUD display system - finds mount markers and renders them on the ship sprite.
// Attach to the HUD Canvas GameObject.
// Designer workflow:
// 1. Add MountHUDMarker to each mount GameObject
// 2. Configure this component with ship sprite and weapon mappings
// 3. Done! Markers auto-update when weapons mount/unmount
[AddComponentMenu("Teramyyd/UI/Ship HUD Display")]
public class ShipHUDDisplay : MonoBehaviour
{
    [Header("Ship Sprite")]
    [Tooltip("The Image component showing the ship silhouette on the HUD")]
    public Image shipSpriteImage;
    
    [Tooltip("Reference to the Ship root GameObject (to find mount markers)")]
    public Transform shipGameObject;
    
    [Header("Ship Sprite Layout")]
    [Tooltip("Size of the ship sprite in pixels")]
    public Vector2 shipSpriteSize = new Vector2(300f, 700f);
    
    [Tooltip("Anchor position on screen (0-1): 0,0=bottom-left, 1,1=top-right")]
    public Vector2 screenAnchor = new Vector2(1f, 0.5f); // Right-center by default
    
    [Tooltip("Pixel offset from the anchor position")]
    public Vector2 anchorOffset = new Vector2(-30f, 75f);
    
    [Tooltip("Preserve sprite's aspect ratio when sizing")]
    public bool preserveAspect = true;
    
    [Header("Weapon Type → Sprite Mappings")]
    [Tooltip("Map weapon types (e.g., 'cannon') to their HUD sprites")]
    public WeaponSpriteMapping[] weaponSpriteMappings;
    
    [Header("Debug")]
    [Tooltip("Enable debug logging to console")]
    public bool debugLog = false;
    
    // Runtime state
    private List<MountHUDMarker> _markers = new List<MountHUDMarker>();
    private Dictionary<MountHUDMarker, Image> _markerImages = new Dictionary<MountHUDMarker, Image>();
    private Dictionary<MountHUDMarker, Image> _healthBarBackgrounds = new Dictionary<MountHUDMarker, Image>();
    private Dictionary<MountHUDMarker, Image> _healthBarGreenFills = new Dictionary<MountHUDMarker, Image>();
    private Dictionary<MountHUDMarker, Image> _healthBarRedFills = new Dictionary<MountHUDMarker, Image>();
    private Dictionary<MountHUDMarker, Image> _readyIndicators = new Dictionary<MountHUDMarker, Image>();
    private RectTransform _shipSpriteRect;
    
    void Start()
    {
        if (shipSpriteImage == null)
        {
            Debug.LogError("[ShipHUDDisplay] shipSpriteImage is not assigned! Assign the Image component showing the ship sprite.");
            enabled = false;
            return;
        }
        
        if (shipGameObject == null)
        {
            Debug.LogError("[ShipHUDDisplay] shipGameObject is not assigned! Assign the Ship root GameObject.");
            enabled = false;
            return;
        }
        
        _shipSpriteRect = shipSpriteImage.GetComponent<RectTransform>();
        
        // Apply ship sprite layout settings
        ApplyShipSpriteLayout();
        
        // Find all mount markers on the ship
        FindMarkers();
        
        // Create UI Images for each marker
        CreateMarkerImages();
        
        if (debugLog)
            Debug.Log($"[ShipHUDDisplay] Initialized with {_markers.Count} mount markers");
    }
    
    void LateUpdate()
    {
        // Update marker sprites every frame based on mount occupancy
        UpdateMarkerSprites();
        
        // Update health bars and ready indicators
        UpdateHealthBars();
        UpdateReadyIndicators();
    }
    
    /// <summary>
    /// Apply size and position settings to the ship sprite.
    /// </summary>
    void ApplyShipSpriteLayout()
    {
        if (_shipSpriteRect == null) return;
        
        // Set anchors to the specified screen position
        _shipSpriteRect.anchorMin = screenAnchor;
        _shipSpriteRect.anchorMax = screenAnchor;
        _shipSpriteRect.pivot = screenAnchor; // Pivot matches anchor for intuitive offsetting
        
        // Set position offset from anchor
        _shipSpriteRect.anchoredPosition = anchorOffset;
        
        // Set size
        _shipSpriteRect.sizeDelta = shipSpriteSize;
        
        // Set preserve aspect on Image component
        if (shipSpriteImage != null)
            shipSpriteImage.preserveAspect = preserveAspect;
        
        if (debugLog)
            Debug.Log($"[ShipHUDDisplay] Applied layout: anchor={screenAnchor}, offset={anchorOffset}, size={shipSpriteSize}");
    }
    
    /// <summary>
    /// Find all MountHUDMarker components on the ship.
    /// </summary>
    void FindMarkers()
    {
        _markers.Clear();
        var foundMarkers = shipGameObject.GetComponentsInChildren<MountHUDMarker>(true);
        _markers.AddRange(foundMarkers);
        
        if (debugLog)
        {
            Debug.Log($"[ShipHUDDisplay] Found {_markers.Count} mount markers:");
            foreach (var marker in _markers)
            {
                Debug.Log($"  - {marker.gameObject.name} at position ({marker.positionOnHUDSprite.x:F2}, {marker.positionOnHUDSprite.y:F2})");
            }
        }
    }
    
    /// <summary>
    /// Create a UI Image GameObject for each marker.
    /// </summary>
    void CreateMarkerImages()
    {
        _markerImages.Clear();
        
        foreach (var marker in _markers)
        {
            // Create Image GameObject as child of ship sprite
            GameObject markerGO = new GameObject($"Marker_{marker.gameObject.name}");
            markerGO.transform.SetParent(_shipSpriteRect, false);
            
            // Add Image component
            Image img = markerGO.AddComponent<Image>();
            img.sprite = marker.defaultSprite;
            img.raycastTarget = false; // Don't block raycasts
            
            // Set up RectTransform
            RectTransform rt = markerGO.GetComponent<RectTransform>();
            
            // Anchor to center of ship sprite
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            
            // Set size
            rt.sizeDelta = marker.iconSize;
            
            // Calculate position
            // marker.positionOnHUDSprite is normalized (0-1) within ship sprite
            // Convert to anchored position (pixels from center)
            Vector2 shipSize = _shipSpriteRect.sizeDelta;
            float x = (marker.positionOnHUDSprite.x - 0.5f) * shipSize.x;
            float y = (marker.positionOnHUDSprite.y - 0.5f) * shipSize.y;
            rt.anchoredPosition = new Vector2(x, y);
            
            // Store reference
            _markerImages[marker] = img;
            
            // Create health bar if enabled
            if (marker.showHealthBar)
            {
                CreateHealthBar(marker, markerGO.transform);
            }
            
            // Create ready indicator if enabled
            if (marker.showReadyIndicator)
            {
                CreateReadyIndicator(marker, markerGO.transform);
            }
            
            if (debugLog)
                Debug.Log($"[ShipHUDDisplay] Created marker image for {marker.gameObject.name} at ({x:F1}, {y:F1})");
        }
    }
    
    /// <summary>
    /// Get Unity's built-in UI sprite for rendering solid colors.
    /// </summary>
    Sprite GetDefaultUISprite()
    {
#if UNITY_EDITOR
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
#else
        return Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
#endif
    }

    /// <summary>
    /// Create a health bar UI element for a marker.
    /// </summary>
    void CreateHealthBar(MountHUDMarker marker, Transform parent)
    {
        if (debugLog)
            FileLogger.Log($"Creating health bar for {marker.gameObject.name}", "HealthBar");
        
        // Get the default UI sprite for rendering
        Sprite uiSprite = GetDefaultUISprite();
        
        // Background (dark gray bar)
        GameObject bgGO = new GameObject($"HealthBar_BG_{marker.gameObject.name}");
        bgGO.transform.SetParent(parent, false);
        
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = uiSprite;
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Dark semi-transparent
        bgImg.raycastTarget = false;
        
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.5f, 0.5f);
        bgRT.anchorMax = new Vector2(0.5f, 0.5f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.sizeDelta = marker.healthBarSize;
        bgRT.anchoredPosition = marker.healthBarOffset;
        
        _healthBarBackgrounds[marker] = bgImg;
        
        // Red fill (damage/background - always full) - CREATE THIS FIRST so it's behind green
        GameObject redFillGO = new GameObject($"HealthBar_RedFill_{marker.gameObject.name}");
        redFillGO.transform.SetParent(bgGO.transform, false);
        
        Image redFillImg = redFillGO.AddComponent<Image>();
        redFillImg.sprite = uiSprite; // Use Unity's default UI sprite
        redFillImg.color = marker.healthEmptyColor;
        redFillImg.raycastTarget = false;
        redFillImg.type = Image.Type.Simple; // Simple filled image
        
        RectTransform redFillRT = redFillGO.GetComponent<RectTransform>();
        redFillRT.anchorMin = Vector2.zero;
        redFillRT.anchorMax = Vector2.one;
        redFillRT.sizeDelta = Vector2.zero; // Fill parent completely
        redFillRT.anchoredPosition = Vector2.zero;
        
        _healthBarRedFills[marker] = redFillImg;
        
        if (debugLog)
            FileLogger.Log($"Red fill created - Color: {redFillImg.color}, Type: {redFillImg.type}, Sprite: {(redFillImg.sprite == null ? "null (using default)" : redFillImg.sprite.name)}", "HealthBar");
        
        // Green fill (healthy part - fills from bottom) - CREATE THIS SECOND so it's on top
        GameObject greenFillGO = new GameObject($"HealthBar_GreenFill_{marker.gameObject.name}");
        greenFillGO.transform.SetParent(bgGO.transform, false);
        
        Image greenFillImg = greenFillGO.AddComponent<Image>();
        greenFillImg.sprite = uiSprite; // Use Unity's default UI sprite
        greenFillImg.color = marker.healthFullColor;
        greenFillImg.raycastTarget = false;
        greenFillImg.type = Image.Type.Filled;
        greenFillImg.fillMethod = Image.FillMethod.Vertical;
        greenFillImg.fillOrigin = (int)Image.OriginVertical.Bottom; // Fill from bottom
        greenFillImg.fillAmount = 1f; // Start at full
        
        RectTransform greenFillRT = greenFillGO.GetComponent<RectTransform>();
        greenFillRT.anchorMin = Vector2.zero;
        greenFillRT.anchorMax = Vector2.one;
        greenFillRT.sizeDelta = Vector2.zero; // Fill parent
        greenFillRT.anchoredPosition = Vector2.zero;
        
        _healthBarGreenFills[marker] = greenFillImg;
        
        if (debugLog)
            FileLogger.Log($"Green fill created - Color: {greenFillImg.color}, Type: {greenFillImg.type}, FillMethod: {greenFillImg.fillMethod}, FillAmount: {greenFillImg.fillAmount}, Sprite: {(greenFillImg.sprite == null ? "null (using default)" : greenFillImg.sprite.name)}", "HealthBar");
        
        // Initially hide health bar (will show when weapon mounted)
        bgGO.SetActive(false);
    }
    
    /// <summary>
    /// Create a ready status indicator (circle) for a marker.
    /// </summary>
    void CreateReadyIndicator(MountHUDMarker marker, Transform parent)
    {
        GameObject circleGO = new GameObject($"ReadyIndicator_{marker.gameObject.name}");
        circleGO.transform.SetParent(parent, false);
        
        Image circleImg = circleGO.AddComponent<Image>();
        // Use a circle sprite - we'll create a simple filled circle
        // For now, use a solid color square and make it look round with transparency
        circleImg.color = marker.notReadyColor; // Start as not ready
        circleImg.raycastTarget = false;
        
        RectTransform circleRT = circleGO.GetComponent<RectTransform>();
        circleRT.anchorMin = new Vector2(0.5f, 0.5f);
        circleRT.anchorMax = new Vector2(0.5f, 0.5f);
        circleRT.pivot = new Vector2(0.5f, 0.5f);
        circleRT.sizeDelta = new Vector2(marker.readyIndicatorSize, marker.readyIndicatorSize);
        circleRT.anchoredPosition = marker.readyIndicatorOffset;
        
        _readyIndicators[marker] = circleImg;
        
        // Initially hide ready indicator (will show when weapon mounted)
        circleGO.SetActive(false);
    }
    
    /// <summary>
    /// Update health bar visibility and fill amount based on mounted weapon's Health component.
    /// </summary>
    void UpdateHealthBars()
    {
        foreach (var marker in _markers)
        {
            if (!marker.showHealthBar)
                continue;
            
            if (!_healthBarBackgrounds.TryGetValue(marker, out Image bgImg) || bgImg == null)
            {
                if (debugLog)
                    FileLogger.Log("Health bar background missing or null", "HealthBar");
                continue;
            }
            
            if (!_healthBarGreenFills.TryGetValue(marker, out Image greenFillImg) || greenFillImg == null)
            {
                if (debugLog)
                    FileLogger.Log("Green fill image missing or null", "HealthBar");
                continue;
            }
            
            if (!_healthBarRedFills.TryGetValue(marker, out Image redFillImg) || redFillImg == null)
            {
                if (debugLog)
                    FileLogger.Log("Red fill image missing or null", "HealthBar");
                continue;
            }
            
            // Check if mount is occupied
            if (!marker.IsOccupied())
            {
                // Hide health bar when empty
                if (bgImg.gameObject.activeSelf)
                    bgImg.gameObject.SetActive(false);
                continue;
            }
            
            // Get mounted weapon
            ProjectileLauncher weapon = marker.GetMountedWeapon();
            if (weapon == null)
            {
                if (bgImg.gameObject.activeSelf)
                    bgImg.gameObject.SetActive(false);
                continue;
            }
            
            // Get Health component (check root and children)
            Health health = weapon.GetComponentInChildren<Health>();
            if (health == null)
            {
                if (debugLog && bgImg.gameObject.activeSelf)
                    Debug.LogWarning($"[ShipHUDDisplay] No Health component on {weapon.gameObject.name} or its children - health bar hidden");
                if (bgImg.gameObject.activeSelf)
                    bgImg.gameObject.SetActive(false);
                continue;
            }
            
            // Show health bar
            if (!bgImg.gameObject.activeSelf)
            {
                bgImg.gameObject.SetActive(true);
                if (debugLog)
                    FileLogger.Log($"Showing health bar for {weapon.gameObject.name}, Health on: {health.gameObject.name}", "HealthBar");
            }
            
            // Calculate health percentage
            float healthPercent = health.maxHealth > 0 ? (float)health.currentHealth / health.maxHealth : 0f;
            
            // Log health bar update details
            if (debugLog)
            {
                FileLogger.Log($"Weapon: {weapon.gameObject.name} | Health: {health.currentHealth}/{health.maxHealth} = {healthPercent:F2} ({healthPercent * 100f:F0}%) | " +
                              $"GreenFill: {greenFillImg.fillAmount:F2} -> {healthPercent:F2} | " +
                              $"GreenActive: {greenFillImg.gameObject.activeSelf} | RedActive: {redFillImg.gameObject.activeSelf} | " +
                              $"GreenColor: {greenFillImg.color} | RedColor: {redFillImg.color}", "HealthBar");
            }
            
            // Update green fill (healthy part) - fills from bottom up to health percentage
            // At 100% health: green covers entire bar (red hidden underneath)
            // At 50% health: green fills bottom 50%, red visible in top 50%
            // At 0% health: green is empty, entire bar shows red
            greenFillImg.fillAmount = healthPercent;
        }
    }
    
    /// <summary>
    /// Update ready indicator visibility and color based on mounted weapon's ready status.
    /// </summary>
    void UpdateReadyIndicators()
    {
        foreach (var marker in _markers)
        {
            if (!marker.showReadyIndicator)
                continue;
            
            if (!_readyIndicators.TryGetValue(marker, out Image indicatorImg) || indicatorImg == null)
                continue;
            
            // Check if mount is occupied
            if (!marker.IsOccupied())
            {
                // Hide indicator when empty
                if (indicatorImg.gameObject.activeSelf)
                    indicatorImg.gameObject.SetActive(false);
                continue;
            }
            
            // Get mounted weapon
            ProjectileLauncher weapon = marker.GetMountedWeapon();
            if (weapon == null)
            {
                if (indicatorImg.gameObject.activeSelf)
                    indicatorImg.gameObject.SetActive(false);
                continue;
            }
            
            // Show indicator
            if (!indicatorImg.gameObject.activeSelf)
                indicatorImg.gameObject.SetActive(true);
            
            // Update color based on ready status
            bool isReady = weapon.IsReadyToFire();
            indicatorImg.color = isReady ? marker.readyColor : marker.notReadyColor;
        }
    }
    
    /// <summary>
    /// Update marker sprites based on current mount occupancy.
    /// Called every frame in LateUpdate.
    /// </summary>
    void UpdateMarkerSprites()
    {
        foreach (var marker in _markers)
        {
            if (!_markerImages.TryGetValue(marker, out Image img))
                continue;
            
            Sprite targetSprite = GetSpriteForMarker(marker);
            
            // Only update if sprite changed (avoid unnecessary assignments)
            if (img.sprite != targetSprite)
            {
                img.sprite = targetSprite;
                
                if (debugLog)
                {
                    string spriteName = targetSprite != null ? targetSprite.name : "null";
                    Debug.Log($"[ShipHUDDisplay] Updated {marker.gameObject.name} sprite to {spriteName}");
                }
            }
        }
    }
    
    /// <summary>
    /// Determine which sprite to show for a marker.
    /// </summary>
    Sprite GetSpriteForMarker(MountHUDMarker marker)
    {
        // Check if mount is occupied
        if (!marker.IsOccupied())
        {
            // Empty mount - use default sprite
            return marker.defaultSprite;
        }
        
        // Occupied - check for custom override first
        if (marker.customOccupiedSprite != null)
        {
            return marker.customOccupiedSprite;
        }
        
        // Use weapon type mapping
        ProjectileLauncher weapon = marker.GetMountedWeapon();
        if (weapon != null)
        {
            string weaponType = WeaponTypeDetector.GetWeaponType(weapon);
            Sprite sprite = GetSpriteForWeaponType(weaponType);
            
            if (sprite != null)
                return sprite;
            
            if (debugLog)
                Debug.LogWarning($"[ShipHUDDisplay] No sprite mapping found for weapon type '{weaponType}' on {marker.gameObject.name}");
        }
        
        // Fallback to default if no mapping found
        return marker.defaultSprite;
    }
    
    /// <summary>
    /// Look up sprite for a weapon type in the mappings array.
    /// Returns null if not found.
    /// </summary>
    Sprite GetSpriteForWeaponType(string weaponType)
    {
        if (weaponSpriteMappings == null || string.IsNullOrEmpty(weaponType))
            return null;
        
        string normalizedType = weaponType.ToLower();
        
        foreach (var mapping in weaponSpriteMappings)
        {
            if (mapping.weaponType.ToLower() == normalizedType)
                return mapping.sprite;
        }
        
        return null;
    }
    
    /// <summary>
    /// Refresh the marker list and recreate marker images.
    /// Call this if you add/remove mounts at runtime.
    /// </summary>
    public void RefreshMarkers()
    {
        // Clear old marker images and indicators
        foreach (var img in _markerImages.Values)
        {
            if (img != null)
                Destroy(img.gameObject);
        }
        
        foreach (var img in _healthBarBackgrounds.Values)
        {
            if (img != null)
                Destroy(img.gameObject);
        }
        
        foreach (var img in _healthBarBackgrounds.Values)
        {
            if (img != null)
                Destroy(img.gameObject);
        }
        
        _healthBarBackgrounds.Clear();
        _healthBarGreenFills.Clear();
        _healthBarRedFills.Clear();
        _readyIndicators.Clear();
        
        // Rebuild
        FindMarkers();
        CreateMarkerImages();
        
        if (debugLog)
            Debug.Log($"[ShipHUDDisplay] Refreshed markers - now tracking {_markers.Count} mounts");
    }
}

/// <summary>
/// Maps a weapon type string to a sprite asset.
/// </summary>
[Serializable]
public class WeaponSpriteMapping
{
    [Tooltip("Weapon type identifier (e.g., 'cannon', 'harpoon')")]
    public string weaponType = "cannon";
    
    [Tooltip("Sprite to show when this weapon type is mounted")]
    public Sprite sprite;
}
