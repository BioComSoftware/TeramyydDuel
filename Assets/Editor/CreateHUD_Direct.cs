using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simplified HUD creation using direct Unity UI references (no reflection).
/// Run this from menu: Teramyyd/Create HUD Canvas (Direct)
/// </summary>
public static class CreateHUD_Direct
{
    [MenuItem("Teramyyd/Create HUD Canvas (Direct)")]
    public static void CreateHudCanvas()
    {
        // Create Canvas
        var canvasGO = new GameObject("HUD_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        
        // Add Canvas Scaler
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Add Graphic Raycaster
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Ensure EventSystem exists
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        
        // Create full-screen panel root
        var rootPanel = new GameObject("HUD_Root");
        rootPanel.transform.SetParent(canvasGO.transform, false);
        var rootRT = rootPanel.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;
        
        // Create Settings Button (top-right)
        var settingsGO = new GameObject("SettingsButton");
        settingsGO.transform.SetParent(rootPanel.transform, false);
        var settingsRT = settingsGO.AddComponent<RectTransform>();
        settingsRT.anchorMin = new Vector2(1f, 1f);
        settingsRT.anchorMax = new Vector2(1f, 1f);
        settingsRT.pivot = new Vector2(1f, 1f);
        settingsRT.anchoredPosition = new Vector2(-20f, -20f);
        settingsRT.sizeDelta = new Vector2(60f, 60f);
        
        var button = settingsGO.AddComponent<Button>();
        var buttonImage = settingsGO.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.7f, 1f, 1f); // Light blue
        
        // Create gear text inside button
        var gearTextGO = new GameObject("GearText");
        gearTextGO.transform.SetParent(settingsGO.transform, false);
        var gearText = gearTextGO.AddComponent<Text>();
        gearText.text = "⚙";
        gearText.fontSize = 72;
        gearText.color = Color.white;
        gearText.alignment = TextAnchor.MiddleCenter;
        var gearRT = gearTextGO.GetComponent<RectTransform>();
        gearRT.anchorMin = Vector2.zero;
        gearRT.anchorMax = Vector2.one;
        gearRT.offsetMin = Vector2.zero;
        gearRT.offsetMax = Vector2.zero;
        
        Selection.activeGameObject = canvasGO;
        Debug.Log("HUD Canvas created successfully with Settings button!");
    }
    
    static T FindObjectOfType<T>() where T : Object
    {
        return Object.FindObjectOfType<T>();
    }
}
