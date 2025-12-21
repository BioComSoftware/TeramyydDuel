using UnityEngine;
using UnityEditor;
using System.Reflection;

/// <summary>
/// Editor utility to disable all debug logging flags in the scene.
/// </summary>
public class DisableAllDebugFlags : MonoBehaviour
{
    [MenuItem("Tools/Debug/Disable All Debug Flags")]
    static void DisableDebugFlags()
    {
        int count = 0;
        
        // Find all MonoBehaviours in the scene
        MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>(true);
        
        foreach (MonoBehaviour mb in allMonoBehaviours)
        {
            if (mb == null) continue;
            
            // Use reflection to find fields named "debugLog", "debug", "Debug", "logVerbose", etc.
            System.Type type = mb.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (FieldInfo field in fields)
            {
                // Check if it's a boolean field with a debug-related name
                if (field.FieldType == typeof(bool))
                {
                    string fieldName = field.Name.ToLower();
                    if (fieldName.Contains("debug") || fieldName.Contains("log") || fieldName == "verbose")
                    {
                        bool currentValue = (bool)field.GetValue(mb);
                        if (currentValue == true)
                        {
                            field.SetValue(mb, false);
                            Debug.Log($"[DisableAllDebugFlags] Disabled {type.Name}.{field.Name} on '{mb.gameObject.name}'");
                            count++;
                            
                            // Mark the object as dirty so changes are saved
                            EditorUtility.SetDirty(mb);
                        }
                    }
                }
            }
        }
        
        // Mark all scenes as dirty to ensure changes are saved
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        
        string message = count > 0 
            ? $"Disabled {count} debug flag(s) across all MonoBehaviours.\n\nSave the scene to persist changes."
            : "No debug flags were found that were set to true.";
            
        EditorUtility.DisplayDialog("Disable Debug Flags", message, "OK");
        Debug.Log($"[DisableAllDebugFlags] Complete. {count} debug flags disabled.");
    }
}
