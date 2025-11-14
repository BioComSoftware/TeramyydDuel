using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
            
            if (debugLog)
                Debug.Log($"[ShipHUDDisplay] Created marker image for {marker.gameObject.name} at ({x:F1}, {y:F1})");
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
        // Clear old marker images
        foreach (var img in _markerImages.Values)
        {
            if (img != null)
                Destroy(img.gameObject);
        }
        
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
