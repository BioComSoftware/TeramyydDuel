using UnityEngine;
using UnityEditor;

/// <summary>
/// Diagnose why TargetCannonAim isn't working
/// </summary>
public class DiagnoseTarget : MonoBehaviour
{
    [MenuItem("Tools/Target Setup/Fix Ship Reference")]
    static void FixShipReference()
    {
        GameObject targetObj = GameObject.Find("Target");
        if (targetObj == null)
        {
            Debug.LogError("[FixShipReference] Target not found!");
            EditorUtility.DisplayDialog("Error", "Target GameObject not found!", "OK");
            return;
        }

        TargetCannonAim script = targetObj.GetComponent<TargetCannonAim>();
        if (script == null)
        {
            Debug.LogError("[FixShipReference] TargetCannonAim script not found!");
            EditorUtility.DisplayDialog("Error", "TargetCannonAim script not attached to Target!", "OK");
            return;
        }

        GameObject shipObj = GameObject.Find("Ship");
        if (shipObj == null)
        {
            Debug.LogError("[FixShipReference] Ship not found!");
            EditorUtility.DisplayDialog("Error", "Ship GameObject not found!", "OK");
            return;
        }

        script.ship = shipObj.transform;
        script.debugLog = true;
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(targetObj.scene);
        
        Debug.Log($"[FixShipReference] ✓ Assigned Ship to TargetCannonAim. Ship position: {shipObj.transform.position}");
        EditorUtility.DisplayDialog("Success", 
            $"Ship reference has been assigned!\n\n" +
            $"Ship Position: {shipObj.transform.position}\n" +
            $"Debug logging enabled.\n\n" +
            "Save the scene and press Play to test.", "OK");
    }

    [MenuItem("Tools/Target Setup/Diagnose Target")]
    static void Diagnose()
    {
        GameObject targetObj = GameObject.Find("Target");
        
        if (targetObj == null)
        {
            Debug.LogError("[DiagnoseTarget] Target GameObject not found!");
            EditorUtility.DisplayDialog("Error", "Target GameObject not found in scene!", "OK");
            return;
        }

        Debug.Log($"[DiagnoseTarget] Target found: {targetObj.name}");
        Debug.Log($"[DiagnoseTarget] Target active: {targetObj.activeInHierarchy}");
        Debug.Log($"[DiagnoseTarget] Target activeSelf: {targetObj.activeSelf}");
        Debug.Log($"[DiagnoseTarget] Target position: {targetObj.transform.position}");
        
        TargetCannonAim script = targetObj.GetComponent<TargetCannonAim>();
        if (script == null)
        {
            Debug.LogError("[DiagnoseTarget] TargetCannonAim script NOT found on Target!");
            EditorUtility.DisplayDialog("Problem Found", "TargetCannonAim script is NOT attached to Target GameObject!", "OK");
            return;
        }

        Debug.Log($"[DiagnoseTarget] TargetCannonAim script found!");
        Debug.Log($"[DiagnoseTarget] Script enabled: {script.enabled}");
        Debug.Log($"[DiagnoseTarget] Ship assigned: {(script.ship != null ? script.ship.name : "NULL")}");
        Debug.Log($"[DiagnoseTarget] Instant rotation: {script.instantRotation}");
        Debug.Log($"[DiagnoseTarget] Debug log: {script.debugLog}");
        
        GameObject shipObj = GameObject.Find("Ship");
        if (shipObj == null)
        {
            Debug.LogError("[DiagnoseTarget] Ship GameObject not found!");
        }
        else
        {
            Debug.Log($"[DiagnoseTarget] Ship found at: {shipObj.transform.position}");
        }

        string message = $"Target: {(targetObj.activeSelf ? "ACTIVE" : "INACTIVE")}\n" +
                        $"Script: {(script != null ? "ATTACHED" : "MISSING")}\n" +
                        $"Script Enabled: {(script != null ? script.enabled.ToString() : "N/A")}\n" +
                        $"Ship Ref: {(script != null && script.ship != null ? script.ship.name : "NULL")}\n\n" +
                        "Check Console for detailed logs.";
        
        EditorUtility.DisplayDialog("Target Diagnosis", message, "OK");
    }
}
