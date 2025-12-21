using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor script to fix CrewMember_Default prefab's MeshCollider.
/// The MeshCollider needs to have the mesh assigned and be set to Convex
/// so crew members can take damage from Rigidbody projectiles.
/// </summary>
public class FixCrewMemberCollider : EditorWindow
{
    [MenuItem("Tools/Fix Crew Member Collider")]
    public static void FixCollider()
    {
        string prefabPath = "Assets/Prefabs/Crew/CrewMember_Default.prefab";
        
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[FixCrewMemberCollider] Could not find prefab at {prefabPath}");
            return;
        }
        
        // Load the prefab for editing
        GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
        
        MeshCollider meshCollider = instance.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            Debug.LogError("[FixCrewMemberCollider] No MeshCollider found on CrewMember_Default prefab!");
            PrefabUtility.UnloadPrefabContents(instance);
            return;
        }
        
        // Search for MeshFilter on this object and all children (nested prefab structure)
        MeshFilter meshFilter = instance.GetComponentInChildren<MeshFilter>();
        
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("[FixCrewMemberCollider] No MeshFilter or mesh found on CrewMember_Default prefab or its children!");
            PrefabUtility.UnloadPrefabContents(instance);
            return;
        }
        
        // Store info BEFORE modifying (while objects are still valid)
        bool wasConvex = meshCollider.convex;
        Mesh oldMesh = meshCollider.sharedMesh;
        string childObjectName = meshFilter.gameObject.name;
        string meshName = meshFilter.sharedMesh.name;
        
        // Assign the mesh from MeshFilter to MeshCollider and set to Convex
        meshCollider.sharedMesh = meshFilter.sharedMesh;
        meshCollider.convex = true;
        
        // Save the modified prefab
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        PrefabUtility.UnloadPrefabContents(instance);
        
        // Log results AFTER unloading (using stored values)
        Debug.Log($"[FixCrewMemberCollider] SUCCESS: Fixed MeshCollider on {prefabPath}");
        Debug.Log($"[FixCrewMemberCollider] - Found mesh on child: {childObjectName}");
        Debug.Log($"[FixCrewMemberCollider] - Assigned mesh: {meshName}");
        Debug.Log($"[FixCrewMemberCollider] - Set convex: {wasConvex} -> true");
        Debug.Log($"[FixCrewMemberCollider] - Previous mesh: {(oldMesh != null ? oldMesh.name : "NULL")}");
        Debug.Log("[FixCrewMemberCollider] Crew members should now be able to take damage from projectiles!");
    }
}
