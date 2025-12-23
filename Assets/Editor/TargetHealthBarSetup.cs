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

        // Create background
        GameObject bgObject = new GameObject("Background");
        bgObject.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObject.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        RectTransform bgRect = bgObject.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Create fill
        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(canvasObj.transform, false);
        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.color = Color.green;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        // Assign to health bar script
        healthBar.healthFillImage = fillImage;
        healthBar.backgroundImage = bgImage;

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
