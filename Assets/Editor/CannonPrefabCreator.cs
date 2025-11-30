using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public class CannonPrefabCreator : MonoBehaviour
{
    [MenuItem("Tools/Create Cannon Prefab")]
    public static void CreateCannonPrefab()
    {
        // Ask user where to save the prefab
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Cannon Prefab",
            "CannonPrefab",
            "prefab",
            "Choose a location for the cannon prefab"
        );

        if (string.IsNullOrEmpty(path))
            return;

        // Create parent GameObject (blank)
        GameObject parent = new GameObject("Cannon");

        // Create the barrel as a cylinder primitive
        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "Barrel";
        barrel.transform.SetParent(parent.transform, false);

        // Make it more cannon-like via scaling
        // Y is length of the cylinder, X/Z are radius
        barrel.transform.localScale = new Vector3(0.4f, 1.0f, 0.4f);

        // Rotate so the barrel points along +Z axis
        barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // Optionally move it so the parent pivot is at the back of the cannon
        // With height=2*scale.y = 2, cylinder runs from -1 to +1 in local Y before rotation.
        // After rotation, it runs from -1 to +1 in local Z. To put the back at Z=0, shift by +1 in Z.
        barrel.transform.localPosition = new Vector3(0f, 0f, 1f);

        // Save as prefab
        bool success;
        PrefabUtility.SaveAsPrefabAsset(parent, path, out success);

        // Clean up the temporary scene object
        Object.DestroyImmediate(parent);

        if (!success)
        {
            Debug.LogError("Failed to create cannon prefab at: " + path);
        }
        else
        {
            Debug.Log("Cannon prefab created at: " + path);
        }
    }
}
#endif
