using UnityEngine;
using Teramyyd.UI;

public class DebugCrewAnchors : MonoBehaviour
{
    [Tooltip("Enables debug logging to console.")]
    public bool debugLog = false;

    void Start()
    {
        if (!debugLog) return;

#if UNITY_EDITOR
        Debug.Log("--- Starting Crew Anchor Debug ---");
        var mounts = FindObjectsByType<WeaponMount>(FindObjectsSortMode.None);
        if (mounts == null || mounts.Length == 0)
        {
            Debug.Log("No WeaponMounts found in scene.");
            return;
        }
        foreach (var mount in mounts)
        {
            if (mount == null) continue;
            
            if (mount.name.Contains("Bow"))
            {
                Debug.Log($"Mount: {mount.name}");
                
                // Check WeaponMount settings
                var so = new UnityEditor.SerializedObject(mount);
                var defMax = so.FindProperty("defaultCrewMax");
                if (defMax != null)
                {
                    Debug.Log($"  WeaponMount.defaultCrewMax: {defMax.intValue}");
                }

                // Check CrewStation
                var station = mount.GetComponent<CrewStation>();
                if (station != null)
                {
                    Debug.Log($"  CrewStation.MaximumCrewAllowed: {station.MaximumCrewAllowed}");
                }

                // Check Builder
                var builder = mount.GetComponent<CrewStationAnchorRuntimeBuilder>();
                if (builder != null)
                {
                    var builderSO = new UnityEditor.SerializedObject(builder);
                    var useOverride = builderSO.FindProperty("useOverrideAnchorCount");
                    var overrideCount = builderSO.FindProperty("overrideAnchorCount");
                    
                    if (useOverride != null)
                        Debug.Log($"  Builder.useOverrideAnchorCount: {useOverride.boolValue}");
                    if (overrideCount != null)
                        Debug.Log($"  Builder.overrideAnchorCount: {overrideCount.intValue}");
                }
            }
        }
#endif
    }
}
