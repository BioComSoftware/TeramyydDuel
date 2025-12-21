using UnityEngine;
using UnityEditor;

/// <summary>
/// Removes the missing CannonSelfDamage script reference from Cannon.prefab
/// </summary>
public class RemoveMissingScriptFromCannon
{
    [MenuItem("Tools/Fix/Remove Missing Script from Cannon Prefab")]
    static void RemoveMissingScript()
    {
        string prefabPath = "Assets/Prefabs/Weapons/ProjectileLaunchers/Cannon.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError($"Could not find prefab at {prefabPath}");
            return;
        }
        
        int removedCount = 0;
        
        // Check all GameObjects in the prefab hierarchy
        foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
        {
            GameObject go = child.gameObject;
            
            // Use SerializedObject to find and remove missing scripts
            SerializedObject so = new SerializedObject(go);
            SerializedProperty components = so.FindProperty("m_Component");
            
            for (int i = components.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty component = components.GetArrayElementAtIndex(i);
                SerializedProperty componentRef = component.FindPropertyRelative("component");
                
                // Check if the component reference is null (missing script)
                if (componentRef.objectReferenceValue == null)
                {
                    Debug.Log($"Removing missing script from '{go.name}'");
                    components.DeleteArrayElementAtIndex(i);
                    removedCount++;
                }
            }
            
            so.ApplyModifiedProperties();
        }
        
        if (removedCount > 0)
        {
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", 
                $"Removed {removedCount} missing script reference(s) from Cannon.prefab", 
                "OK");
            Debug.Log($"[RemoveMissingScript] Successfully removed {removedCount} missing script(s) from Cannon.prefab");
        }
        else
        {
            EditorUtility.DisplayDialog("No Changes", 
                "No missing scripts found in Cannon.prefab", 
                "OK");
        }
    }
}
