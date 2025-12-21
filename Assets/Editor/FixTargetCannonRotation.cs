using UnityEngine;
using UnityEditor;

/// <summary>
/// Fix the Cannon child rotation on the Target GameObject
/// </summary>
public class FixTargetCannonRotation : MonoBehaviour
{
    [MenuItem("Tools/Target Setup/Fix Cannon Local Rotation")]
    static void FixCannonRotation()
    {
        GameObject targetObj = GameObject.Find("Target");
        if (targetObj == null)
        {
            Debug.LogError("[FixTargetCannonRotation] Target GameObject not found!");
            EditorUtility.DisplayDialog("Error", "Target GameObject not found in scene!", "OK");
            return;
        }

        Transform cannonTransform = targetObj.transform.Find("Cannon");
        if (cannonTransform == null)
        {
            Debug.LogError("[FixTargetCannonRotation] Cannon child not found under Target!");
            EditorUtility.DisplayDialog("Error", "Cannon child not found under Target!", "OK");
            return;
        }

        Vector3 oldRotation = cannonTransform.localEulerAngles;
        
        // Reset local rotation to zero
        cannonTransform.localEulerAngles = Vector3.zero;
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(targetObj.scene);
        
        Debug.Log($"[FixTargetCannonRotation] Changed Cannon local rotation from {oldRotation} to {Vector3.zero}");
        EditorUtility.DisplayDialog("Success", 
            $"Cannon local rotation fixed!\n\n" +
            $"Old rotation: {oldRotation}\n" +
            $"New rotation: {Vector3.zero}\n\n" +
            "Save the scene and test in Play mode.", "OK");
    }
}
