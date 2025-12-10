using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class RunDebugOnLoad
{
    static RunDebugOnLoad()
    {
        EditorApplication.update += RunOnce;
    }

    static void RunOnce()
    {
        EditorApplication.update -= RunOnce;
        if (!Application.isPlaying) return;

        var go = new GameObject("DebugRunner");
        go.AddComponent<DebugCrewAnchors>();
    }
}
