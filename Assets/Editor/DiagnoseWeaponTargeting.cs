using UnityEngine;
using UnityEditor;

/// <summary>
/// Diagnostic tool to help troubleshoot weapon targeting issues.
/// Reports detailed information about all weapon mounts and their targeting status.
/// </summary>
public class DiagnoseWeaponTargeting : EditorWindow
{
    [MenuItem("Tools/Diagnose Weapon Targeting")]
    public static void ShowDiagnostics()
    {
        WeaponMount[] mounts = FindObjectsByType<WeaponMount>(FindObjectsSortMode.None);
        
        if (mounts == null || mounts.Length == 0)
        {
            Debug.LogWarning("[DiagnoseWeaponTargeting] No WeaponMounts found in scene!");
            return;
        }

        Debug.Log($"[DiagnoseWeaponTargeting] Found {mounts.Length} weapon mount(s):");
        Debug.Log("==========================================");

        foreach (WeaponMount mount in mounts)
        {
            string mountId = mount.mountId;
            bool hasTarget = mount.HasSelectedTarget;
            bool hasHorizontalLock = mount.HasHorizontalLock;
            bool hasFiringSolution = mount.HasValidFiringSolution;
            bool isFullyAcquired = mount.IsTargetFullyAcquired;
            bool canFire = mount.CanFireAtCurrentTarget;
            bool hasCrew = mount.HasCrewReady;
            bool isOccupied = mount.isOccupied;

            Debug.Log($"\n[Mount: {mountId}]");
            Debug.Log($"  Location: {GetPath(mount.transform)}");
            Debug.Log($"  isOccupied: {isOccupied}");
            Debug.Log($"  hasCrew: {hasCrew}");
            Debug.Log($"  hasTarget: {hasTarget}");
            Debug.Log($"  hasHorizontalLock: {hasHorizontalLock}");
            Debug.Log($"  hasFiringSolution: {hasFiringSolution}");
            Debug.Log($"  isFullyAcquired: {isFullyAcquired}");
            Debug.Log($"  canFire: {canFire}");
            
            if (mount.targetingController != null && mount.targetingController.CurrentTarget != null)
            {
                Transform target = mount.targetingController.CurrentTarget.transform;
                Debug.Log($"  Target: {target.name} at {target.position}");
                
                // Calculate distances and angles
                Transform muzzle = mount.pitchBarrel;
                if (mount.currentLauncher != null && mount.currentLauncher.spawnPoint != null)
                {
                    muzzle = mount.currentLauncher.spawnPoint;
                }
                
                if (muzzle != null)
                {
                    Vector3 displacement = target.position - muzzle.position;
                    float distance = displacement.magnitude;
                    float horizontalDistance = new Vector3(displacement.x, 0, displacement.z).magnitude;
                    float verticalOffset = displacement.y;
                    
                    Debug.Log($"  Distance: {distance:F1}m (H:{horizontalDistance:F1}m V:{verticalOffset:F1}m)");
                    
                    // Check yaw constraints
                    Transform reference = mount.yawBase != null ? mount.yawBase.parent : mount.transform;
                    if (reference == null) reference = mount.yawBase;
                    if (reference == null) reference = mount.transform;
                    
                    Vector3 localDir = reference.InverseTransformDirection(displacement.normalized);
                    float targetYaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
                    float yawLimit = mount.yawLimitDeg * 0.5f;
                    
                    Debug.Log($"  Target Yaw: {targetYaw:F1}° (Limit: ±{yawLimit:F1}°)");
                    Debug.Log($"  Yaw Status: {(Mathf.Abs(targetYaw) <= yawLimit ? "WITHIN LIMITS" : "OUTSIDE LIMITS")}");
                    
                    if (mount.currentLauncher != null)
                    {
                        Debug.Log($"  Launcher Speed: {mount.currentLauncher.launchSpeed:F1} (Min: {mount.currentLauncher.minimumLaunchSpeed:F1})");
                        Debug.Log($"  Pitch Limits: Up:{mount.pitchUpDeg:F1}° Down:{mount.pitchDownDeg:F1}°");
                    }
                }
            }
            else
            {
                Debug.Log("  Target: NONE");
            }
            
            Debug.Log($"  Debug Logging: {(mount.enableDebugLogging ? "ENABLED" : "DISABLED")}");
            Debug.Log("------------------------------------------");
        }
        
        Debug.Log("==========================================");
        Debug.Log("[DiagnoseWeaponTargeting] Diagnosis complete.");
        Debug.Log("\nTO ENABLE DETAILED LOGGING:");
        Debug.Log("1. Select each weapon mount in the scene");
        Debug.Log("2. Check 'Enable Debug Logging' in the Inspector");
        Debug.Log("3. Check Logs/game_debug.log for detailed ballistic calculations");
    }

    static string GetPath(Transform t)
    {
        if (t == null) return "null";
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
