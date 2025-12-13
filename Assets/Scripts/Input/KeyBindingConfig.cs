using System;
using System.IO;
using UnityEngine;

// TWO-LAYER KEYBINDING SYSTEM
// 
// LAYER 1: Developer Defaults (Inspector Fields)
// - Public fields prefixed with "default" (e.g., defaultBridgeView)
// - Set by game developer in Unity Inspector
// - These values NEVER change when players modify keybindings
// - Serve as "factory defaults" for the reset function
// - Stored in KeyBindingConfig.asset ScriptableObject
//
// LAYER 2: Runtime Player Settings (Private Fields + Properties)
// - Private fields prefixed with "_" (e.g., _bridgeView)
// - Loaded from keybindings.json at runtime
// - These are the AUTHORITATIVE values used during gameplay
// - Players can modify these through settings menu
// - Stored in Resources/keybindings.json
//
// WORKFLOW:
// 1. At startup: LoadFromJSON() reads keybindings.json into runtime (_) variables
// 2. During gameplay: All code reads from public properties (bridgeView, followView, etc.)
// 3. Player changes setting: SetRuntimeKey() updates runtime variable, SaveToJSON() writes to file
// 4. Player resets: ResetToDefaults() copies Inspector defaults to runtime variables
// 5. Inspector fields remain unchanged regardless of player actions

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

    // Weapon controls
    public string fireAllWeapons = "F";

    // UI controls
    public string instrumentZoom = "Z";

    // Ship wheel controls
    public float autoReturnSpeedDegPerSec = 90f;

    // Modifiers
    public bool snapRequiresCtrl = true;
    public bool zoomRequiresCtrl = true;
}

[CreateAssetMenu(menuName = "Teramyyd/Key Binding Config", fileName = "KeyBindingConfig")] 
public class KeyBindingConfig : ScriptableObject
{
    [Header("===== DEVELOPER DEFAULTS (Inspector Only) =====")]
    [Header("These values are set by the developer and serve as the 'factory defaults'")]
    [Header("They are NOT changed when players modify their keybindings")]
    [Space(10)]
    
    [Header("View Switching Defaults")]
    public KeyCode defaultBridgeView = KeyCode.F1;
    public KeyCode defaultFollowView = KeyCode.F2;
    public KeyCode defaultOverheadView = KeyCode.F3;

    [Header("View Snap Defaults (require Ctrl)")]
    public KeyCode defaultBridgeSnap = KeyCode.F1;
    public KeyCode defaultFollowSnap = KeyCode.F2;
    public KeyCode defaultOverheadSnap = KeyCode.F3;

    [Header("Camera Look/Pan Defaults")]
    public KeyCode defaultLookLeft = KeyCode.LeftArrow;
    public KeyCode defaultLookRight = KeyCode.RightArrow;
    public KeyCode defaultLookUp = KeyCode.UpArrow;
    public KeyCode defaultLookDown = KeyCode.DownArrow;

    [Header("Zoom Defaults (require Ctrl)")]
    public KeyCode defaultZoomIn = KeyCode.UpArrow;
    public KeyCode defaultZoomOut = KeyCode.DownArrow;

    [Header("Weapon Control Defaults")]
    public KeyCode defaultFireAllWeapons = KeyCode.F;

    [Header("UI Control Defaults")]
    public KeyCode defaultInstrumentZoom = KeyCode.Z;

    [Header("Ship Wheel Control Defaults")]
    [Tooltip("Degrees per second the wheel will spring back toward center when the player releases it (0 disables auto return).")]
    public float defaultAutoReturnSpeedDegPerSec = 90f;

    [Header("Modifier Flag Defaults")]
    public bool defaultSnapRequiresCtrl = true;
    public bool defaultZoomRequiresCtrl = true;

    [Space(20)]
    [Header("===== RUNTIME PLAYER SETTINGS (Read Only) =====")]
    [Header("These values are loaded from keybindings.json at runtime")]
    [Header("DO NOT edit these in Inspector - they will be overwritten")]
    [Space(10)]
    
    [Header("Active View Switching (Runtime)")]
    [SerializeField] private KeyCode _bridgeView;
    [SerializeField] private KeyCode _followView;
    [SerializeField] private KeyCode _overheadView;

    [Header("Active View Snap (Runtime)")]
    [SerializeField] private KeyCode _bridgeSnap;
    [SerializeField] private KeyCode _followSnap;
    [SerializeField] private KeyCode _overheadSnap;

    [Header("Active Camera Look/Pan (Runtime)")]
    [SerializeField] private KeyCode _lookLeft;
    [SerializeField] private KeyCode _lookRight;
    [SerializeField] private KeyCode _lookUp;
    [SerializeField] private KeyCode _lookDown;

    [Header("Active Zoom (Runtime)")]
    [SerializeField] private KeyCode _zoomIn;
    [SerializeField] private KeyCode _zoomOut;

    [Header("Active Weapon Controls (Runtime)")]
    [SerializeField] private KeyCode _fireAllWeapons;

    [Header("Active UI Controls (Runtime)")]
    [SerializeField] private KeyCode _instrumentZoom;

    [Header("Active Ship Wheel Controls (Runtime)")]
    [SerializeField] private float _autoReturnSpeedDegPerSec;

    [Header("Active Modifier Flags (Runtime)")]
    [SerializeField] private bool _snapRequiresCtrl;
    [SerializeField] private bool _zoomRequiresCtrl;

    // Public properties to access runtime values (authoritative source during gameplay)
    public KeyCode bridgeView => _bridgeView;
    public KeyCode followView => _followView;
    public KeyCode overheadView => _overheadView;
    public KeyCode bridgeSnap => _bridgeSnap;
    public KeyCode followSnap => _followSnap;
    public KeyCode overheadSnap => _overheadSnap;
    public KeyCode lookLeft => _lookLeft;
    public KeyCode lookRight => _lookRight;
    public KeyCode lookUp => _lookUp;
    public KeyCode lookDown => _lookDown;
    public KeyCode zoomIn => _zoomIn;
    public KeyCode zoomOut => _zoomOut;
    public KeyCode fireAllWeapons => _fireAllWeapons;
    public KeyCode instrumentZoom => _instrumentZoom;
    public float autoReturnSpeedDegPerSec => _autoReturnSpeedDegPerSec;
    public bool snapRequiresCtrl => _snapRequiresCtrl;
    public bool zoomRequiresCtrl => _zoomRequiresCtrl;

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
    /// Populates RUNTIME values only - does NOT change developer defaults in Inspector.
    /// If JSON not found, initializes runtime values from developer defaults.
    /// </summary>
    public void LoadFromJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("keybindings");
        if (jsonFile == null)
        {
            Debug.LogWarning("KeyBindingConfig: keybindings.json not found in Resources. Initializing from developer defaults.");
            InitializeFromDefaults();
            return;
        }

        try
        {
            KeyBindingData data = JsonUtility.FromJson<KeyBindingData>(jsonFile.text);
            
            // Load into RUNTIME variables (_fields) - developer defaults remain unchanged
            _bridgeView = ParseKeyCode(data.bridgeView, defaultBridgeView);
            _followView = ParseKeyCode(data.followView, defaultFollowView);
            _overheadView = ParseKeyCode(data.overheadView, defaultOverheadView);

            _bridgeSnap = ParseKeyCode(data.bridgeSnap, defaultBridgeSnap);
            _followSnap = ParseKeyCode(data.followSnap, defaultFollowSnap);
            _overheadSnap = ParseKeyCode(data.overheadSnap, defaultOverheadSnap);

            _lookLeft = ParseKeyCode(data.lookLeft, defaultLookLeft);
            _lookRight = ParseKeyCode(data.lookRight, defaultLookRight);
            _lookUp = ParseKeyCode(data.lookUp, defaultLookUp);
            _lookDown = ParseKeyCode(data.lookDown, defaultLookDown);

            _zoomIn = ParseKeyCode(data.zoomIn, defaultZoomIn);
            _zoomOut = ParseKeyCode(data.zoomOut, defaultZoomOut);

            _fireAllWeapons = ParseKeyCode(data.fireAllWeapons, defaultFireAllWeapons);

            _instrumentZoom = ParseKeyCode(data.instrumentZoom, defaultInstrumentZoom);

            _autoReturnSpeedDegPerSec = data.autoReturnSpeedDegPerSec;

            _snapRequiresCtrl = data.snapRequiresCtrl;
            _zoomRequiresCtrl = data.zoomRequiresCtrl;

            Debug.Log("KeyBindingConfig: Successfully loaded player keybindings from JSON.");
        }
        catch (Exception ex)
        {
            Debug.LogError("KeyBindingConfig: Failed to parse keybindings.json - " + ex.Message);
            InitializeFromDefaults();
        }
    }

    /// <summary>
    /// Initialize runtime values from developer defaults.
    /// Called when JSON doesn't exist or fails to load.
    /// </summary>
    private void InitializeFromDefaults()
    {
        _bridgeView = defaultBridgeView;
        _followView = defaultFollowView;
        _overheadView = defaultOverheadView;
        _bridgeSnap = defaultBridgeSnap;
        _followSnap = defaultFollowSnap;
        _overheadSnap = defaultOverheadSnap;
        _lookLeft = defaultLookLeft;
        _lookRight = defaultLookRight;
        _lookUp = defaultLookUp;
        _lookDown = defaultLookDown;
        _zoomIn = defaultZoomIn;
        _zoomOut = defaultZoomOut;
        _fireAllWeapons = defaultFireAllWeapons;
        _instrumentZoom = defaultInstrumentZoom;
        _autoReturnSpeedDegPerSec = defaultAutoReturnSpeedDegPerSec;
        _snapRequiresCtrl = defaultSnapRequiresCtrl;
        _zoomRequiresCtrl = defaultZoomRequiresCtrl;
        
        Debug.Log("KeyBindingConfig: Initialized runtime values from developer defaults.");
    }

    /// <summary>
    /// Save current RUNTIME keybindings to JSON format.
    /// Returns JSON string that can be written to keybindings.json file.
    /// </summary>
    public string SaveToJSON()
    {
        KeyBindingData data = new KeyBindingData
        {
            bridgeView = _bridgeView.ToString(),
            followView = _followView.ToString(),
            overheadView = _overheadView.ToString(),
            bridgeSnap = _bridgeSnap.ToString(),
            followSnap = _followSnap.ToString(),
            overheadSnap = _overheadSnap.ToString(),
            lookLeft = _lookLeft.ToString(),
            lookRight = _lookRight.ToString(),
            lookUp = _lookUp.ToString(),
            lookDown = _lookDown.ToString(),
            zoomIn = _zoomIn.ToString(),
            zoomOut = _zoomOut.ToString(),
            fireAllWeapons = _fireAllWeapons.ToString(),
            instrumentZoom = _instrumentZoom.ToString(),
            autoReturnSpeedDegPerSec = _autoReturnSpeedDegPerSec,
            snapRequiresCtrl = _snapRequiresCtrl,
            zoomRequiresCtrl = _zoomRequiresCtrl
        };

        return JsonUtility.ToJson(data, true);
    }

    /// <summary>
    /// Save current RUNTIME keybindings to keybindings.json file.
    /// Call this after player changes a keybinding to persist changes.
    /// </summary>
    public void SaveToJSONFile()
    {
        string json = SaveToJSON();
        string path = Path.Combine(Application.dataPath, "Resources", "keybindings.json");
        
        try
        {
            File.WriteAllText(path, json);
            Debug.Log($"KeyBindingConfig: Saved player keybindings to {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"KeyBindingConfig: Failed to write keybindings.json - {ex.Message}");
        }
    }

    /// <summary>
    /// Reset player keybindings to developer defaults.
    /// Call this when player clicks "Reset to Defaults" button.
    /// This copies Inspector values into runtime values and returns JSON to save.
    /// </summary>
    public string ResetToDefaults()
    {
        InitializeFromDefaults();
        Debug.Log("KeyBindingConfig: Reset player keybindings to developer defaults.");
        return SaveToJSON();
    }

    /// <summary>
    /// Update a specific runtime keybinding value.
    /// Use this when player rebinds a key in the settings menu.
    /// </summary>
    public void SetRuntimeKey(string keyName, KeyCode newValue)
    {
        switch (keyName)
        {
            case "bridgeView": _bridgeView = newValue; break;
            case "followView": _followView = newValue; break;
            case "overheadView": _overheadView = newValue; break;
            case "bridgeSnap": _bridgeSnap = newValue; break;
            case "followSnap": _followSnap = newValue; break;
            case "overheadSnap": _overheadSnap = newValue; break;
            case "lookLeft": _lookLeft = newValue; break;
            case "lookRight": _lookRight = newValue; break;
            case "lookUp": _lookUp = newValue; break;
            case "lookDown": _lookDown = newValue; break;
            case "zoomIn": _zoomIn = newValue; break;
            case "zoomOut": _zoomOut = newValue; break;
            case "fireAllWeapons": _fireAllWeapons = newValue; break;
            case "instrumentZoom": _instrumentZoom = newValue; break;
            default:
                Debug.LogWarning($"KeyBindingConfig: Unknown key name '{keyName}'");
                break;
        }
    }

    /// <summary>
    /// Update a runtime float value (like autoReturnSpeedDegPerSec).
    /// </summary>
    public void SetRuntimeFloat(string valueName, float newValue)
    {
        switch (valueName)
        {
            case "autoReturnSpeedDegPerSec": _autoReturnSpeedDegPerSec = newValue; break;
            default:
                Debug.LogWarning($"KeyBindingConfig: Unknown float value '{valueName}'");
                break;
        }
    }

    /// <summary>
    /// Update a runtime bool value (like snapRequiresCtrl).
    /// </summary>
    public void SetRuntimeBool(string valueName, bool newValue)
    {
        switch (valueName)
        {
            case "snapRequiresCtrl": _snapRequiresCtrl = newValue; break;
            case "zoomRequiresCtrl": _zoomRequiresCtrl = newValue; break;
            default:
                Debug.LogWarning($"KeyBindingConfig: Unknown bool value '{valueName}'");
                break;
        }
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
