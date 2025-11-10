using System;
using UnityEngine;

// Centralized keybinding configuration for camera/view controls.
// - Serializable ScriptableObject so it can be edited in Inspector and versioned as an asset.
// - Also exposes static LoadOrCreate() to ensure a singleton-like asset exists under Resources.
// - Supports JSON-based configuration for easier manual editing in VS Code.

/// <summary>
/// JSON-serializable data structure for keybindings.
/// Uses string-based key names for easier manual editing.
/// </summary>
[Serializable]
public class KeyBindingData
{
    // View switching
    public string bridgeView = "F1";
    public string followView = "F2";
    public string overheadView = "F3";

    // Snap keys
    public string bridgeSnap = "F1";
    public string followSnap = "F2";
    public string overheadSnap = "F3";

    // Look controls (arrows by default)
    public string lookLeft = "LeftArrow";
    public string lookRight = "RightArrow";
    public string lookUp = "UpArrow";
    public string lookDown = "DownArrow";

    // Zoom controls
    public string zoomIn = "UpArrow";
    public string zoomOut = "DownArrow";

    // Modifiers
    public bool snapRequiresCtrl = true;
    public bool zoomRequiresCtrl = true;
}

[CreateAssetMenu(menuName = "Teramyyd/Key Binding Config", fileName = "KeyBindingConfig")] 
public class KeyBindingConfig : ScriptableObject
{
    [Header("View Switching")]
    public KeyCode bridgeView = KeyCode.F1;
    public KeyCode followView = KeyCode.F2;
    public KeyCode overheadView = KeyCode.F3;

    [Header("View Snap (require Ctrl)")]
    public KeyCode bridgeSnap = KeyCode.F1;
    public KeyCode followSnap = KeyCode.F2;
    public KeyCode overheadSnap = KeyCode.F3;

    [Header("Camera Look/Pan")]
    public KeyCode lookLeft = KeyCode.LeftArrow;
    public KeyCode lookRight = KeyCode.RightArrow;
    public KeyCode lookUp = KeyCode.UpArrow;
    public KeyCode lookDown = KeyCode.DownArrow;

    [Header("Zoom (require Ctrl)")]
    public KeyCode zoomIn = KeyCode.UpArrow;
    public KeyCode zoomOut = KeyCode.DownArrow;

    [Header("Modifier Flags")]
    public bool snapRequiresCtrl = true;
    public bool zoomRequiresCtrl = true;

    private static KeyBindingConfig _instance;

    public static KeyBindingConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<KeyBindingConfig>("KeyBindingConfig");
#if UNITY_EDITOR
                if (_instance == null)
                {
                    _instance = CreateInstance<KeyBindingConfig>();
                    // Ensure Resources folder
                    var path = "Assets/Resources";
                    if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
                    var assetPath = System.IO.Path.Combine(path, "KeyBindingConfig.asset");
                    UnityEditor.AssetDatabase.CreateAsset(_instance, assetPath);
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.AssetDatabase.Refresh();
                    Debug.Log("Created default KeyBindingConfig at " + assetPath);
                }
#endif
                // Load from JSON if it exists
                if (_instance != null)
                {
                    _instance.LoadFromJSON();
                }
            }
            return _instance;
        }
    }

    public bool IsCtrlHeld()
    {
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }

    /// <summary>
    /// Load keybindings from JSON file in Resources folder.
    /// Call this to populate the config from JSON.
    /// </summary>
    public void LoadFromJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("keybindings");
        if (jsonFile == null)
        {
            Debug.LogWarning("KeyBindingConfig: keybindings.json not found in Resources. Using defaults.");
            return;
        }

        try
        {
            KeyBindingData data = JsonUtility.FromJson<KeyBindingData>(jsonFile.text);
            
            // View switching
            bridgeView = ParseKeyCode(data.bridgeView, KeyCode.F1);
            followView = ParseKeyCode(data.followView, KeyCode.F2);
            overheadView = ParseKeyCode(data.overheadView, KeyCode.F3);

            // Snap keys
            bridgeSnap = ParseKeyCode(data.bridgeSnap, KeyCode.F1);
            followSnap = ParseKeyCode(data.followSnap, KeyCode.F2);
            overheadSnap = ParseKeyCode(data.overheadSnap, KeyCode.F3);

            // Look controls
            lookLeft = ParseKeyCode(data.lookLeft, KeyCode.LeftArrow);
            lookRight = ParseKeyCode(data.lookRight, KeyCode.RightArrow);
            lookUp = ParseKeyCode(data.lookUp, KeyCode.UpArrow);
            lookDown = ParseKeyCode(data.lookDown, KeyCode.DownArrow);

            // Zoom controls
            zoomIn = ParseKeyCode(data.zoomIn, KeyCode.UpArrow);
            zoomOut = ParseKeyCode(data.zoomOut, KeyCode.DownArrow);

            // Modifiers
            snapRequiresCtrl = data.snapRequiresCtrl;
            zoomRequiresCtrl = data.zoomRequiresCtrl;

            Debug.Log("KeyBindingConfig: Successfully loaded keybindings from JSON.");
        }
        catch (Exception ex)
        {
            Debug.LogError("KeyBindingConfig: Failed to parse keybindings.json - " + ex.Message);
        }
    }

    /// <summary>
    /// Save current keybindings to JSON format (for editor use).
    /// Returns JSON string that can be written to file.
    /// </summary>
    public string SaveToJSON()
    {
        KeyBindingData data = new KeyBindingData
        {
            bridgeView = bridgeView.ToString(),
            followView = followView.ToString(),
            overheadView = overheadView.ToString(),
            bridgeSnap = bridgeSnap.ToString(),
            followSnap = followSnap.ToString(),
            overheadSnap = overheadSnap.ToString(),
            lookLeft = lookLeft.ToString(),
            lookRight = lookRight.ToString(),
            lookUp = lookUp.ToString(),
            lookDown = lookDown.ToString(),
            zoomIn = zoomIn.ToString(),
            zoomOut = zoomOut.ToString(),
            snapRequiresCtrl = snapRequiresCtrl,
            zoomRequiresCtrl = zoomRequiresCtrl
        };

        return JsonUtility.ToJson(data, true);
    }

    /// <summary>
    /// Parse string key name to KeyCode enum.
    /// Returns fallback value if parsing fails.
    /// </summary>
    private KeyCode ParseKeyCode(string keyName, KeyCode fallback)
    {
        if (string.IsNullOrEmpty(keyName))
            return fallback;

        try
        {
            return (KeyCode)Enum.Parse(typeof(KeyCode), keyName, true);
        }
        catch
        {
            Debug.LogWarning($"KeyBindingConfig: Invalid key name '{keyName}', using fallback {fallback}");
            return fallback;
        }
    }
}
