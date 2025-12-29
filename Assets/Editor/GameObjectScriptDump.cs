using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class GameObjectScriptDump
{
    const string OutputPath = "Assets/Logs/GameobjectScriptDump.txt";

    static GameObjectScriptDump()
    {
        // Subscribe to play mode state changes
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Run dump when exiting play mode
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            DumpActiveSceneScripts();
        }
    }

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
            // Removed: EditorUtility.RevealInFinder(OutputPath);
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

        // Write Transform/RectTransform details
        var rectTransform = node as RectTransform;
        if (rectTransform != null)
        {
            WriteRectTransformDetails(rectTransform, writer, indent);
        }
        else
        {
            WriteTransformDetails(node, writer, indent);
        }

        // Write all components
        var components = node.GetComponents<Component>();
        if (components != null)
        {
            foreach (var component in components)
            {
                if (component == null)
                {
                    writer.WriteLine($"{indent}    [Missing Component]");
                    continue;
                }

                // Skip Transform/RectTransform since we already wrote it
                if (component is Transform)
                    continue;

                WriteComponentDetails(component, writer, indent);
            }
        }

        for (int i = 0; i < node.childCount; i++)
        {
            WriteObjectRecursive(node.GetChild(i), writer, depth + 1);
        }
    }

    static void WriteTransformDetails(Transform transform, StreamWriter writer, string indent)
    {
        writer.WriteLine($"{indent}    [Transform] pos={Vec3Str(transform.localPosition)} rot={Vec3Str(transform.localEulerAngles)} scale={Vec3Str(transform.localScale)}");
    }

    static void WriteRectTransformDetails(RectTransform rectTransform, StreamWriter writer, string indent)
    {
        var anchorMin = rectTransform.anchorMin;
        var anchorMax = rectTransform.anchorMax;
        var anchoredPos = rectTransform.anchoredPosition;
        var sizeDelta = rectTransform.sizeDelta;
        var pivot = rectTransform.pivot;

        writer.WriteLine($"{indent}    [RectTransform]");
        writer.WriteLine($"{indent}      anchors=({anchorMin.x:F3},{anchorMin.y:F3})-({anchorMax.x:F3},{anchorMax.y:F3})");
        writer.WriteLine($"{indent}      anchoredPos=({anchoredPos.x:F1},{anchoredPos.y:F1}) sizeDelta=({sizeDelta.x:F1},{sizeDelta.y:F1})");
        writer.WriteLine($"{indent}      pivot=({pivot.x:F2},{pivot.y:F2}) localScale={Vec3Str(rectTransform.localScale)}");
    }

    static void WriteComponentDetails(Component component, StreamWriter writer, string indent)
    {
        string typeName = component.GetType().Name;
        string state = "";

        // Check if component has an enabled property
        var behaviour = component as Behaviour;
        if (behaviour != null)
        {
            state = behaviour.enabled ? "[Enabled]" : "[Disabled]";
        }

        // Write basic component info
        writer.Write($"{indent}    {state} {component.GetType().FullName}");

        // Add specific details for common component types
        if (component is Image image)
        {
            writer.Write($" | sprite={(image.sprite != null ? image.sprite.name : "None")}");
            writer.Write($" | color=({image.color.r:F2},{image.color.g:F2},{image.color.b:F2},{image.color.a:F2})");
            writer.Write($" | type={image.type}");
        }
        else if (component is Canvas canvas)
        {
            writer.Write($" | renderMode={canvas.renderMode} | sortOrder={canvas.sortingOrder}");
        }
        else if (component is CanvasScaler scaler)
        {
            writer.Write($" | uiScale={scaler.uiScaleMode} | refRes=({scaler.referenceResolution.x},{scaler.referenceResolution.y})");
        }
        else if (component is Button button)
        {
            writer.Write($" | interactable={button.interactable}");
        }
        else if (component is Text text)
        {
            writer.Write($" | text=\"{text.text.Substring(0, Math.Min(30, text.text.Length))}\" | fontSize={text.fontSize}");
        }

        writer.WriteLine();
    }

    static string Vec3Str(Vector3 v)
    {
        return $"({v.x:F2},{v.y:F2},{v.z:F2})";
    }
}
