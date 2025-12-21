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
        
        // Load prefab contents for editing
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
        
        if (prefabContents == null)
        {
            Debug.LogError($"Could not load prefab contents from {prefabPath}");
            return;
        }
        
        int removedCount = 0;
        
        try
        {
            // Check all GameObjects in the prefab hierarchy including inactive ones
            Transform[] allTransforms = prefabContents.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTransforms)
            {
                GameObject go = t.gameObject;
                
                // Use GameObjectUtility to remove missing scripts
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (count > 0)
                {
                    Debug.Log($"Removing {count} missing script(s) from '{go.name}'");
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    removedCount += count;
                }
            }
            
            if (removedCount > 0)
            {
                // Save the modified prefab
                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                
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
                Debug.Log("[RemoveMissingScript] No missing scripts found in Cannon.prefab");
            }
        }
        finally
        {
            // Always unload the prefab contents
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
