using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Editor helper to quickly create a health bar for the Target object
/// </summary>
public class TargetHealthBarSetup
{
    [MenuItem("GameObject/UI/Create Target Health Bar", false, 10)]
    static void CreateTargetHealthBar(MenuCommand menuCommand)
    {
        // Get the selected GameObject (should be the Target)
        GameObject target = Selection.activeGameObject;
        
        if (target == null)
        {
            EditorUtility.DisplayDialog("No Target Selected", 
                "Please select the Target GameObject in the hierarchy first.", "OK");
            return;
        }

        // Check if it has a Health component
        Health health = target.GetComponent<Health>();
        if (health == null)
        {
            health = target.GetComponentInChildren<Health>();
            if (health == null)
            {
                if (!EditorUtility.DisplayDialog("No Health Component", 
                    "The selected object doesn't have a Health component. Create health bar anyway?", 
                    "Yes", "Cancel"))
                {
                    return;
                }
            }
        }

        // Create Canvas GameObject
        GameObject canvasObj = new GameObject("HealthBarCanvas");
        canvasObj.transform.SetParent(target.transform, false);
        canvasObj.transform.localPosition = Vector3.zero;

        // Add Canvas component
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Add CanvasScaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        // Add GraphicRaycaster (optional, for interactions)
        canvasObj.AddComponent<GraphicRaycaster>();

        // Set canvas size - will be auto-adjusted by script based on target size
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(4f, 0.4f); // Initial size, will be recalculated (4 * 0.1 = 0.4)
        canvasRect.localScale = Vector3.one; // Use world units directly

        // Add TargetHealthBar script
        TargetHealthBar healthBar = canvasObj.AddComponent<TargetHealthBar>();
        healthBar.targetHealth = health;
        healthBar.healthBarContainer = canvasRect;

        // The script will auto-create the green and red fill images in EnsureHealthBarImages()
        // No need to manually create them here - just let the script handle it

        // Select the created canvas
        Selection.activeGameObject = canvasObj;

        // Mark scene as dirty
        EditorUtility.SetDirty(canvasObj);
        
        Debug.Log($"[TargetHealthBarSetup] Created health bar for {target.name}");
    }

    [MenuItem("GameObject/UI/Create Target Health Bar", true)]
    static bool ValidateCreateTargetHealthBar()
    {
        // Only show menu item if something is selected
        return Selection.activeGameObject != null;
    }
}
