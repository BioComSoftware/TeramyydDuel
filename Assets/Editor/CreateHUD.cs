using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Editor utility to create a Canvas + HUD and wire HUDController.
// It uses reflection to add UI/Text or TextMeshPro components if available so it
// doesn't hard-depend on specific UI packages at compile time.
public static class CreateHUD
{
    [MenuItem("Teramyyd/Create HUD Canvas")]
    public static void CreateHudCanvas()
    {
        // Create Canvas
    var canvasGO = new GameObject("HUD_Canvas");
    var canvas = canvasGO.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay; // stays locked to screen regardless of camera movement
    canvas.sortingOrder = 1000; // ensure HUD is drawn on top if other canvases exist

        // Add CanvasScaler if available
        var canvasScalerType = FindType("UnityEngine.UI.CanvasScaler");
        if (canvasScalerType != null)
        {
            var scaler = canvasGO.AddComponent(canvasScalerType);
            // Configure scaler for consistent sizing across resolutions
            var uiScaleModeProp = canvasScalerType.GetProperty("uiScaleMode");
            var scaleModeEnum = canvasScalerType.GetNestedType("ScaleMode");
            if (uiScaleModeProp != null && scaleModeEnum != null)
            {
                // Set to ScaleWithScreenSize if available
                var scaleWithScreenSize = System.Enum.GetValues(scaleModeEnum)
                    .Cast<object>()
                    .FirstOrDefault(v => v.ToString() == "ScaleWithScreenSize");
                if (scaleWithScreenSize != null)
                    uiScaleModeProp.SetValue(scaler, scaleWithScreenSize, null);
            }
            // Reference resolution (fallback typical 1920x1080)
            var refResProp = canvasScalerType.GetProperty("referenceResolution");
            if (refResProp != null)
                refResProp.SetValue(scaler, new Vector2(1920, 1080), null);
        }

        // Add GraphicRaycaster if available
        var grType = FindType("UnityEngine.UI.GraphicRaycaster");
        if (grType != null)
            canvasGO.AddComponent(grType);

        // Optional: pixel perfect if available
        var pixelPerfectProp = canvas.GetType().GetProperty("pixelPerfect");
        if (pixelPerfectProp != null && pixelPerfectProp.CanWrite)
        {
            pixelPerfectProp.SetValue(canvas, true, null);
        }

        // Ensure EventSystem exists
        var esType = FindType("UnityEngine.EventSystems.EventSystem");
        var simType = FindType("UnityEngine.EventSystems.StandaloneInputModule");
        if (esType != null && UnityEngine.Object.FindObjectsOfType(esType).Length == 0)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent(esType);
            if (simType != null) es.AddComponent(simType);
        }

    // Create a full-screen panel root (optional container for layout)
    var rootPanel = new GameObject("HUD_Root");
    rootPanel.transform.SetParent(canvasGO.transform, false);
    var rootRT = rootPanel.AddComponent<RectTransform>();
    rootRT.anchorMin = Vector2.zero;
    rootRT.anchorMax = Vector2.one;
    rootRT.offsetMin = Vector2.zero;
    rootRT.offsetMax = Vector2.zero;

    // Create Health Text
        var healthGO = new GameObject("HealthText");
    healthGO.transform.SetParent(rootPanel.transform, false);
        Component healthTextComp = AddTextLikeComponent(healthGO, "Health: 100");

        // Position health text (top-left)
        var rt = healthGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(10f, -10f);

        // Create Score Text
    var scoreGO = new GameObject("ScoreText");
    scoreGO.transform.SetParent(rootPanel.transform, false);
        Component scoreTextComp = AddTextLikeComponent(scoreGO, "Score: 0");
        var rt2 = scoreGO.AddComponent<RectTransform>();
        rt2.anchorMin = new Vector2(1f, 1f);
        rt2.anchorMax = new Vector2(1f, 1f);
        rt2.pivot = new Vector2(1f, 1f);
        rt2.anchoredPosition = new Vector2(-10f, -10f);

        // Create HUD controller
    var hudGO = new GameObject("HUD");
    hudGO.transform.SetParent(rootPanel.transform, false);
        var hudController = hudGO.AddComponent<HUDController>();

        // Wire player health if a GameObject named 'Player' or tagged 'Player' exists
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null)
        {
            var healthComp = player.GetComponent("Health") as Component;
            if (healthComp != null)
            {
                // assign via serialized object to avoid compile-time type
                var so = new SerializedObject(hudController);
                var prop = so.FindProperty("playerHealth");
                if (prop != null)
                {
                    prop.objectReferenceValue = (UnityEngine.Object)healthComp;
                    so.ApplyModifiedProperties();
                }
            }
        }

        // Assign text components to HUDController via SerializedObject
    var so2 = new SerializedObject(hudController);
    var htProp = so2.FindProperty("healthTextComponent");
    var stProp = so2.FindProperty("scoreTextComponent");
    var rootProp = so2.FindProperty("hudRoot");
        if (htProp != null && healthTextComp != null) htProp.objectReferenceValue = (UnityEngine.Object)healthTextComp;
        if (stProp != null && scoreTextComp != null) stProp.objectReferenceValue = (UnityEngine.Object)scoreTextComp;
    if (rootProp != null) rootProp.objectReferenceValue = rootRT;
        so2.ApplyModifiedProperties();

        // Select the new Canvas
        Selection.activeGameObject = canvasGO;
        EditorGUIUtility.PingObject(canvasGO);
        Debug.Log("Teramyyd: HUD Canvas created. Assign Player tag or name to your player GameObject for automatic wiring.");
    }

    static Component AddTextLikeComponent(GameObject go, string initialText)
    {
        // Try uGUI Text
        var textType = FindType("UnityEngine.UI.Text");
        if (textType != null)
        {
            var comp = go.AddComponent(textType);
            SetTextViaReflection(comp, initialText);
            return comp;
        }

        // Try TextMeshProUGUI
        var tmpType = FindType("TMPro.TextMeshProUGUI");
        if (tmpType != null)
        {
            var comp = go.AddComponent(tmpType);
            SetTextViaReflection(comp, initialText);
            return comp;
        }

        // Fallback: add a plain Transform (no text)
        return null;
    }

    static void SetTextViaReflection(Component comp, string text)
    {
        if (comp == null) return;
        var prop = comp.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(comp, text, null);
            return;
        }
        var field = comp.GetType().GetField("text", BindingFlags.Public | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(comp, text);
            return;
        }
        var method = comp.GetType().GetMethod("SetText", new[] { typeof(string) });
        if (method != null)
        {
            method.Invoke(comp, new object[] { text });
            return;
        }
    }

    static Type FindType(string fullname)
    {
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var t = a.GetType(fullname);
                if (t != null) return t;
            }
            catch { }
        }
        return null;
    }
}
