using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Temporary script to force Canvas setup.
/// Attach this to HUD_Canvas to ensure it's properly configured at runtime.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class ForceCanvasSetup : MonoBehaviour
{
    void Awake()
    {
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        
        // Ensure Canvas Scaler exists
        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Ensure Graphic Raycaster exists
        var raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
        
        Debug.Log("ForceCanvasSetup: Canvas configured for screen overlay");
    }
}
