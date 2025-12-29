using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

[InitializeOnLoad]
public static class DumpHierarchy
{
    static DumpHierarchy()
    {
        // Subscribe to play mode state changes
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Run dump when exiting play mode
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            Dump();
        }
    }
    [MenuItem("Tools/Component Audit/Dump Hierarchy to Text")]
    public static void Dump()
    {
        // Absolute path to the Assets folder
        string assetsFolder = Application.dataPath;

        // Logs directory under Assets
        string logsFolder = Path.Combine(assetsFolder, "Logs");

        // Ensure the Logs directory exists
        if (!Directory.Exists(logsFolder))
        {
            Directory.CreateDirectory(logsFolder);
        }

        // Final path to the output file
        string filePath = Path.Combine(logsFolder, "HierarchyDump.txt");

        try
        {
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                var scene = SceneManager.GetActiveScene();

                if (!scene.IsValid())
                {
                    Debug.LogError("Active scene is not valid.");
                    return;
                }

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    WriteObject(root.transform, writer, 0);
                }
            }

            Debug.Log("Hierarchy dumped to: " + filePath);

            // Refresh Project window so the file appears in Unity
            AssetDatabase.Refresh();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to dump hierarchy: " + ex);
        }
    }

    private static void WriteObject(Transform t, StreamWriter writer, int indent)
    {
        writer.WriteLine(new string(' ', indent * 2) + "- " + t.name);

        foreach (Transform child in t)
        {
            WriteObject(child, writer, indent + 1);
        }
    }
}
