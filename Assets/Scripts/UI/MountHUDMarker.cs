using UnityEngine;

// Simple component attached to each weapon mount GameObject.
// Defines where and how this mount appears on the ship HUD.
// Designer-friendly: just add to mount, set sprite and position, done!
[AddComponentMenu("Teramyyd/UI/Mount HUD Marker")]
public class MountHUDMarker : MonoBehaviour
{
    [Header("HUD Icon Appearance")]
    [Tooltip("Sprite shown when this mount is empty (e.g., Mount.png)")]
    public Sprite defaultSprite;
    
    [Tooltip("Size of the icon in pixels on the HUD")]
    public Vector2 iconSize = new Vector2(20f, 20f);
    
    [Header("Position on Ship HUD Sprite")]
    [Tooltip("Normalized position (0-1) on the ship sprite. Example: (0.5, 0.9) = top center")]
    public Vector2 positionOnHUDSprite = new Vector2(0.5f, 0.5f);
    
    [Header("Optional Custom Occupied Sprite")]
    [Tooltip("If set, always use this sprite when occupied (overrides weapon type mapping)")]
    public Sprite customOccupiedSprite;
    
    // Runtime cached references
    private WeaponMount _weaponMount;
    
    /// <summary>
    /// Get the WeaponMount component on this same GameObject.
    /// Cached for performance.
    /// </summary>
    public WeaponMount GetWeaponMount()
    {
        if (_weaponMount == null)
            _weaponMount = GetComponent<WeaponMount>();
        return _weaponMount;
    }
    
    /// <summary>
    /// Check if this mount is currently occupied.
    /// </summary>
    public bool IsOccupied()
    {
        var mount = GetWeaponMount();
        return mount != null && mount.isOccupied;
    }
    
    /// <summary>
    /// Get the currently mounted weapon's launcher component.
    /// Returns null if not occupied.
    /// </summary>
    public ProjectileLauncher GetMountedWeapon()
    {
        var mount = GetWeaponMount();
        if (mount == null || !mount.isOccupied)
            return null;
        return mount.currentLauncher;
    }
    
    void OnValidate()
    {
        // Clamp position to valid range
        positionOnHUDSprite.x = Mathf.Clamp01(positionOnHUDSprite.x);
        positionOnHUDSprite.y = Mathf.Clamp01(positionOnHUDSprite.y);
        
        // Ensure icon size is positive
        if (iconSize.x < 1f) iconSize.x = 20f;
        if (iconSize.y < 1f) iconSize.y = 20f;
    }
}
