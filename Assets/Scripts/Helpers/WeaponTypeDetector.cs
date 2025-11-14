using UnityEngine;

// Utility to determine weapon type from mounted GameObject.
// Used by ShipHUDDisplay to map weapons to HUD sprites.
public static class WeaponTypeDetector
{
    /// <summary>
    /// Determine the weapon type string from a ProjectileLauncher.
    /// Returns a lowercase type string like "cannon", "harpoon", etc.
    /// </summary>
    public static string GetWeaponType(ProjectileLauncher launcher)
    {
        if (launcher == null)
            return "unknown";
        
        // Check by actual type (most reliable)
        if (launcher is Cannon)
            return "cannon";
        
        // Future weapon types can be added here:
        // if (launcher is Harpoon) return "harpoon";
        // if (launcher is Mortar) return "mortar";
        
        // Fallback: check GameObject name
        string name = launcher.gameObject.name.Replace("(Clone)", "").Trim().ToLower();
        if (name.Contains("cannon")) return "cannon";
        if (name.Contains("harpoon")) return "harpoon";
        if (name.Contains("mortar")) return "mortar";
        
        // Last resort: use the type name
        return launcher.GetType().Name.ToLower();
    }
    
    /// <summary>
    /// Determine weapon type from the mount's type field.
    /// Fallback when mounted weapon isn't available.
    /// </summary>
    public static string GetMountType(WeaponMount mount)
    {
        if (mount == null || string.IsNullOrEmpty(mount.mountType))
            return "unknown";
        
        return mount.mountType.ToLower();
    }
}
