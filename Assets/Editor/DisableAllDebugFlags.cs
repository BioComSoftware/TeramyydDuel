using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;

/// <summary>
/// Editor utility to disable all debug logging flags in the scene and prefabs.
/// </summary>
public class DisableAllDebugFlags : MonoBehaviour
{
    [MenuItem("Tools/Debug/Disable All Debug Flags")]
    static void DisableDebugFlags()
    {
        int sceneCount = 0;
        int prefabCount = 0;
        
        // Process all MonoBehaviours in the scene
        MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>(true);
        
        foreach (MonoBehaviour mb in allMonoBehaviours)
        {
            if (mb == null) continue;
            
            if (DisableDebugFlagsOnObject(mb))
            {
                sceneCount++;
                EditorUtility.SetDirty(mb);
            }
        }
        
        // Process all prefabs in the project
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab == null) continue;
            
            bool prefabModified = false;
            MonoBehaviour[] components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            
            foreach (MonoBehaviour mb in components)
            {
                if (mb == null) continue;
                
                if (DisableDebugFlagsOnObject(mb))
                {
                    prefabCount++;
                    prefabModified = true;
                }
            }
            
            if (prefabModified)
            {
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
            }
        }
        
        // Mark all scenes as dirty to ensure changes are saved
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        
        string message = $"Disabled debug flags:\n" +
                        $"• Scene objects: {sceneCount}\n" +
                        $"• Prefab components: {prefabCount}\n\n" +
                        $"Save the scene and prefabs to persist changes.";
            
        if (sceneCount == 0 && prefabCount == 0)
        {
            message = "No debug flags were found that were set to true.";
        }
            
        EditorUtility.DisplayDialog("Disable Debug Flags", message, "OK");
        Debug.Log($"[DisableAllDebugFlags] Complete. Scene: {sceneCount}, Prefabs: {prefabCount} debug flags disabled.");
    }
    
    static bool DisableDebugFlagsOnObject(MonoBehaviour mb)
    {
        bool modified = false;
        
        // Use reflection to find fields named "debugLog", "debug", "Debug", "logVerbose", etc.
        System.Type type = mb.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        
        foreach (FieldInfo field in fields)
        {
            // Check if it's a boolean field with a debug-related name
            if (field.FieldType == typeof(bool))
            {
                string fieldName = field.Name.ToLower();
                bool isDebugField = fieldName == "debuglog" || 
                                   fieldName == "debug" || 
                                   fieldName == "enabledebug" ||
                                   fieldName == "enabledebuglogging" ||
                                   fieldName == "logverbose" || 
                                   fieldName == "verbose" ||
                                   fieldName.StartsWith("debug") ||
                                   fieldName.EndsWith("debug") ||
                                   fieldName.Contains("debuglog");
                
                if (isDebugField)
                {
                    bool currentValue = (bool)field.GetValue(mb);
                    if (currentValue == true)
                    {
                        field.SetValue(mb, false);
                        Debug.Log($"[DisableAllDebugFlags] Disabled {type.Name}.{field.Name} on '{mb.gameObject.name}'");
                        modified = true;
                    }
                }
            }
        }
        
        return modified;
    }
}
