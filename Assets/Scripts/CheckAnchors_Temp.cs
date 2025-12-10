using UnityEngine;
using UnityEditor;

public class CheckAnchors : MonoBehaviour
{
    static void Run()
    {
        var builders = Resources.FindObjectsOfTypeAll<CrewStationAnchorRuntimeBuilder>();
        foreach (var builder in builders)
        {
            if (builder.gameObject.scene.name == null) continue; // Skip prefabs
            
            Debug.Log($"Builder on {builder.gameObject.name}: Override={GetOverride(builder)}");
        }
    }

    static int GetOverride(CrewStationAnchorRuntimeBuilder builder)
    {
        var so = new SerializedObject(builder);
        return so.FindProperty("overrideAnchorCount").intValue;
    }
}
