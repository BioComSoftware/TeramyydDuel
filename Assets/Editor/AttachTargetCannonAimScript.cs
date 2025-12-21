using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to attach the TargetCannonAim script to the Target GameObject.
/// </summary>
public class AttachTargetCannonAimScript : MonoBehaviour
{
    [MenuItem("Tools/Target Setup/Attach TargetCannonAim Script")]
    static void AttachScript()
    {
        // Find the Target GameObject
        GameObject targetObject = GameObject.Find("Target");
        
        if (targetObject == null)
        {
            Debug.LogError("[AttachTargetCannonAimScript] Could not find 'Target' GameObject in scene!");
            EditorUtility.DisplayDialog("Error", "Could not find 'Target' GameObject in the scene.", "OK");
            return;
        }

        // Check if script is already attached
        TargetCannonAim existingScript = targetObject.GetComponent<TargetCannonAim>();
        if (existingScript != null)
        {
            Debug.LogWarning("[AttachTargetCannonAimScript] TargetCannonAim script is already attached to Target.");
            EditorUtility.DisplayDialog("Already Attached", "TargetCannonAim script is already attached to the Target GameObject.", "OK");
            return;
        }

        // Attach the script
        TargetCannonAim script = targetObject.AddComponent<TargetCannonAim>();
        
        // Auto-assign the Ship reference
        GameObject shipObject = GameObject.Find("Ship");
        if (shipObject != null)
        {
            script.ship = shipObject.transform;
            Debug.Log("[AttachTargetCannonAimScript] Auto-assigned Ship reference.");
        }
        else
        {
            Debug.LogWarning("[AttachTargetCannonAimScript] Could not find 'Ship' GameObject to auto-assign.");
        }

        // Enable debug logging by default for initial testing
        script.debugLog = true;
        script.instantRotation = true;

        // Mark scene as dirty to ensure changes are saved
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(targetObject.scene);

        Debug.Log($"[AttachTargetCannonAimScript] Successfully attached TargetCannonAim script to '{targetObject.name}' with debug logging enabled.");
        EditorUtility.DisplayDialog("Success", 
            $"TargetCannonAim script attached to '{targetObject.name}'.\n\n" +
            "Debug logging is enabled.\n" +
            "Ship reference has been auto-assigned.\n\n" +
            "Press Play to test.", "OK");
    }
}
