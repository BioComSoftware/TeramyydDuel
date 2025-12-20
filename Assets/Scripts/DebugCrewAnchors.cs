using UnityEngine;
using Teramyyd.UI;

public class DebugCrewAnchors : MonoBehaviour
{
    void Start()
    {
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
            if (mount.name.Contains("Bow"))
            {
                Debug.Log($"Mount: {mount.name}");
                
                // Check WeaponMount settings
                var so = new UnityEditor.SerializedObject(mount);
                var defMax = so.FindProperty("defaultCrewMax").intValue;
                Debug.Log($"  WeaponMount.defaultCrewMax: {defMax}");

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
                    var useOverride = builderSO.FindProperty("useOverrideAnchorCount").boolValue;
                    var overrideCount = builderSO.FindProperty("overrideAnchorCount").intValue;
                    
                    Debug.Log($"  Builder.useOverrideAnchorCount: {useOverride}");
                    Debug.Log($"  Builder.overrideAnchorCount: {overrideCount}");
                }
            }
        }
#endif
    }
}
