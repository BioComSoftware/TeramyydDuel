using UnityEngine;

// Attach this to the Ship root for quick testing. On Start, it finds all
// ProjectileLauncherMounts in children and mounts the provided prefab into any
// empty mount.
public class AutoPopulateLauncherMounts : MonoBehaviour
{
    public GameObject launcherPrefab;
    public bool runOnStart = true;

    void Start()
    {
        if (!runOnStart || launcherPrefab == null) return;
        var mounts = GetComponentsInChildren<ProjectileLauncherMount>(includeInactive: true);
        foreach (var m in mounts)
        {
            if (m != null && !m.isOccupied)
            {
                m.Mount(launcherPrefab);
            }
        }
    }
}

