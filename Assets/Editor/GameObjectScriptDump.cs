using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameObjectScriptDump
{
    const string OutputPath = "Assets/Logs/GameobjectScriptDump.txt";

    [MenuItem("Tools/Component Audit/Dump GameObject Script Map", priority = 201)]
    public static void DumpActiveSceneScripts()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("[GameObjectScriptDump] Active scene is not valid.");
            return;
        }

        var directory = Path.GetDirectoryName(OutputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            using (var writer = new StreamWriter(OutputPath, false, Encoding.UTF8))
            {
                writer.WriteLine($"GameObject Script Dump - Scene: {scene.name} - UTC {DateTime.UtcNow:O}");
                writer.WriteLine(new string('-', 80));

                var roots = scene.GetRootGameObjects();
                Array.Sort(roots, (a, b) => string.CompareOrdinal(a.name, b.name));
                foreach (var root in roots)
                {
                    WriteObjectRecursive(root.transform, writer, 0);
                }
            }

            Debug.Log($"[GameObjectScriptDump] Wrote script map to {OutputPath}");
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(OutputPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameObjectScriptDump] Failed to write script map: {ex}");
        }
    }

    static void WriteObjectRecursive(Transform node, StreamWriter writer, int depth)
    {
        if (node == null || writer == null)
            return;

        string indent = new string(' ', depth * 2);
        writer.WriteLine($"{indent}- {node.name}");

        var scripts = node.GetComponents<MonoBehaviour>();
        if (scripts != null)
        {
            foreach (var behaviour in scripts)
            {
                writer.WriteLine($"{indent}    {DescribeBehaviour(behaviour)}");
            }
        }

        for (int i = 0; i < node.childCount; i++)
        {
            WriteObjectRecursive(node.GetChild(i), writer, depth + 1);
        }
    }

    static string DescribeBehaviour(MonoBehaviour behaviour)
    {
        if (behaviour == null)
            return "[Missing Script]";

        string state = behaviour.enabled ? "[Enabled]" : "[Disabled]";
        return $"{state} {behaviour.GetType().FullName}";
    }
}
