using UnityEngine;
using UnityEditor; 

/// <summary>
/// Quick tool to enable/disable debug logging on all WeaponMounts in the scene.
/// This helps diagnose targeting issues during gameplay.
/// </summary>
public class ToggleWeaponMountDebugging : EditorWindow
{
    [MenuItem("Tools/Targeting Debug/Enable All Weapon Mount Logging")]
    public static void EnableAllDebugLogging()
    {
        WeaponMount[] mounts = FindObjectsByType<WeaponMount>(FindObjectsSortMode.None);
        
        if (mounts == null || mounts.Length == 0)
        {
            Debug.LogWarning("[ToggleWeaponMountDebugging] No WeaponMounts found in scene!");
            return;
        }

        int enabledCount = 0;
        foreach (WeaponMount mount in mounts)
        {
            if (!mount.enableDebugLogging)
            {
                mount.enableDebugLogging = true;
                enabledCount++;
                EditorUtility.SetDirty(mount);
            }
        }

        Debug.Log($"[ToggleWeaponMountDebugging] Enabled debug logging on {enabledCount} weapon mount(s). Total: {mounts.Length}");
        Debug.Log("→ Logs will be written to Logs/game_debug.log during Play mode");
        Debug.Log("→ Watch for 'TARGET NOT ACQUIRED' messages with detailed failure reasons");
    }

    [MenuItem("Tools/Targeting Debug/Disable All Weapon Mount Logging")]
    public static void DisableAllDebugLogging()
    {
        WeaponMount[] mounts = FindObjectsByType<WeaponMount>(FindObjectsSortMode.None);
        
        if (mounts == null || mounts.Length == 0)
        {
            Debug.LogWarning("[ToggleWeaponMountDebugging] No WeaponMounts found in scene!");
            return;
        }

        int disabledCount = 0;
        foreach (WeaponMount mount in mounts)
        {
            if (mount.enableDebugLogging)
            {
                mount.enableDebugLogging = false;
                disabledCount++;
                EditorUtility.SetDirty(mount);
            }
        }

        Debug.Log($"[ToggleWeaponMountDebugging] Disabled debug logging on {disabledCount} weapon mount(s). Total: {mounts.Length}");
    }

    [MenuItem("Tools/Targeting Debug/Show Current Debug Status")]
    public static void ShowDebugStatus()
    {
        WeaponMount[] mounts = FindObjectsByType<WeaponMount>(FindObjectsSortMode.None);
        
        if (mounts == null || mounts.Length == 0)
        {
            Debug.LogWarning("[ToggleWeaponMountDebugging] No WeaponMounts found in scene!");
            return;
        }

        int enabledCount = 0;
        int disabledCount = 0;

        Debug.Log($"[ToggleWeaponMountDebugging] Weapon Mount Debug Status:");
        Debug.Log("==========================================");

        foreach (WeaponMount mount in mounts)
        {
            string status = mount.enableDebugLogging ? "ENABLED" : "DISABLED";
            Debug.Log($"  {mount.mountId}: {status}");
            
            if (mount.enableDebugLogging)
                enabledCount++;
            else
                disabledCount++;
        }

        Debug.Log("==========================================");
        Debug.Log($"Total: {mounts.Length} mounts ({enabledCount} enabled, {disabledCount} disabled)");
    }
}
