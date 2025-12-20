using System;
using System.IO;
using UnityEngine;

// TWO-LAYER KEYBINDING SYSTEM (Updated)
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

    // Engine chadburn controls
    public string engineForward = "W";
    public string engineReverse = "S";
    public string engineSnapFullAhead = "Ctrl+W";
    public string engineSnapStop = "Shift+W";
    public string engineSnapFullAstern = "Ctrl+S";
    public string engineSnapStopReverse = "Shift+S";
    public float engineChadburnRotationSpeed = 45f;
    
    // Ship wheel controls
    public string wheelLeft = "A";
    public string wheelRight = "D";
    
    // Lift chadburn controls
    public string liftUp = "Q";
    public string liftDown = "E";
    public string liftSnapFullUp = "Ctrl+Q";
    public string liftSnapCenter = "Shift+Q";
    public string liftSnapFullDown = "Ctrl+E";
    public string liftSnapCenterDown = "Shift+E";
    public float liftChadburnRotationSpeed = 45f;

    // Ship wheel controls (not exposed in KeyBindingConfig Inspector)
    public float autoReturnSpeedDegPerSec = 90f;

    // Modifiers
    public bool snapRequiresCtrl = true;
    public bool zoomRequiresCtrl = true;
}

[CreateAssetMenu(menuName = "Teramyyd/Key Binding Config", fileName = "KeyBindingConfig")] 
public class KeyBindingConfig : ScriptableObject
{
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

    [Header("Active Engine Chadburn Controls (Runtime)")]
    [SerializeField] private KeyCode _engineForward;
    [SerializeField] private KeyCode _engineReverse;
    [SerializeField] private Teramyyd.UI.KeyBindingData _engineSnapFullAhead;
    [SerializeField] private Teramyyd.UI.KeyBindingData _engineSnapStop;
    [SerializeField] private Teramyyd.UI.KeyBindingData _engineSnapFullAstern;
    [SerializeField] private Teramyyd.UI.KeyBindingData _engineSnapStopReverse;
    [SerializeField] private float _engineChadburnRotationSpeed = 45f;

    [Header("Active Ship Wheel Controls (Runtime)")]
    [SerializeField] private KeyCode _wheelLeft;
    [SerializeField] private KeyCode _wheelRight;
    [Tooltip("This value is loaded from keybindings.json only - no Inspector default field.")]
    [SerializeField] private float _autoReturnSpeedDegPerSec = 90f;
    
    [Header("Active Lift Chadburn Controls (Runtime)")]
    [SerializeField] private KeyCode _liftUp;
    [SerializeField] private KeyCode _liftDown;
    [SerializeField] private Teramyyd.UI.KeyBindingData _liftSnapFullUp;
    [SerializeField] private Teramyyd.UI.KeyBindingData _liftSnapCenter;
    [SerializeField] private Teramyyd.UI.KeyBindingData _liftSnapFullDown;
    [SerializeField] private Teramyyd.UI.KeyBindingData _liftSnapCenterDown;
    [SerializeField] private float _liftChadburnRotationSpeed = 45f;

    [Header("Active Modifier Flags (Runtime)")]
    [SerializeField] private bool _snapRequiresCtrl;
    [SerializeField] private bool _zoomRequiresCtrl;

    [Header("Debug")]
    [Tooltip("Enable debug logging to Console and log file")]
    public bool debugLog = false;

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
    public KeyCode engineForward => _engineForward;
    public KeyCode engineReverse => _engineReverse;
    public Teramyyd.UI.KeyBindingData engineSnapFullAhead => _engineSnapFullAhead;
    public Teramyyd.UI.KeyBindingData engineSnapStop => _engineSnapStop;
    public Teramyyd.UI.KeyBindingData engineSnapFullAstern => _engineSnapFullAstern;
    public Teramyyd.UI.KeyBindingData engineSnapStopReverse => _engineSnapStopReverse;
    public float engineChadburnRotationSpeed => _engineChadburnRotationSpeed;
    public KeyCode wheelLeft => _wheelLeft;
    public KeyCode wheelRight => _wheelRight;
    public KeyCode liftUp => _liftUp;
    public KeyCode liftDown => _liftDown;
    public Teramyyd.UI.KeyBindingData liftSnapFullUp => _liftSnapFullUp;
    public Teramyyd.UI.KeyBindingData liftSnapCenter => _liftSnapCenter;
    public Teramyyd.UI.KeyBindingData liftSnapFullDown => _liftSnapFullDown;
    public Teramyyd.UI.KeyBindingData liftSnapCenterDown => _liftSnapCenterDown;
    public float liftChadburnRotationSpeed => _liftChadburnRotationSpeed;
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
        // Read directly from file system so we can detect runtime changes
        string keybindingsPath = Path.Combine(Application.dataPath, "Resources", "keybindings.json");
        
        if (!File.Exists(keybindingsPath))
        {
            Debug.LogWarning($"KeyBindingConfig: keybindings.json not found at {keybindingsPath}. Initializing from developer defaults.");
            InitializeFromDefaults();
            return;
        }

        try
        {
            string jsonText = File.ReadAllText(keybindingsPath);
            KeyBindingData data = JsonUtility.FromJson<KeyBindingData>(jsonText);
            DefaultKeybindings defaults = DefaultKeybindings.Instance;
            
            // Load into RUNTIME variables (_fields) - developer defaults remain unchanged
            _bridgeView = ParseKeyCode(data.bridgeView, defaults?.defaultBridgeView ?? KeyCode.F1);
            _followView = ParseKeyCode(data.followView, defaults?.defaultFollowView ?? KeyCode.F2);
            _overheadView = ParseKeyCode(data.overheadView, defaults?.defaultOverheadView ?? KeyCode.F3);

            _bridgeSnap = ParseKeyCode(data.bridgeSnap, defaults?.defaultBridgeSnap ?? KeyCode.F1);
            _followSnap = ParseKeyCode(data.followSnap, defaults?.defaultFollowSnap ?? KeyCode.F2);
            _overheadSnap = ParseKeyCode(data.overheadSnap, defaults?.defaultOverheadSnap ?? KeyCode.F3);

            _lookLeft = ParseKeyCode(data.lookLeft, defaults?.defaultLookLeft ?? KeyCode.LeftArrow);
            _lookRight = ParseKeyCode(data.lookRight, defaults?.defaultLookRight ?? KeyCode.RightArrow);
            _lookUp = ParseKeyCode(data.lookUp, defaults?.defaultLookUp ?? KeyCode.UpArrow);
            _lookDown = ParseKeyCode(data.lookDown, defaults?.defaultLookDown ?? KeyCode.DownArrow);

            _zoomIn = ParseKeyCode(data.zoomIn, defaults?.defaultZoomIn ?? KeyCode.UpArrow);
            _zoomOut = ParseKeyCode(data.zoomOut, defaults?.defaultZoomOut ?? KeyCode.DownArrow);

            _fireAllWeapons = ParseKeyCode(data.fireAllWeapons, defaults?.defaultFireAllWeapons ?? KeyCode.F);

            _instrumentZoom = ParseKeyCode(data.instrumentZoom, defaults?.defaultInstrumentZoom ?? KeyCode.Z);

            _engineForward = ParseKeyCode(data.engineForward, defaults?.defaultEngineForward ?? KeyCode.W);
            _engineReverse = ParseKeyCode(data.engineReverse, defaults?.defaultEngineReverse ?? KeyCode.S);
            _engineSnapFullAhead = ParseKeyBinding(data.engineSnapFullAhead);
            _engineSnapStop = ParseKeyBinding(data.engineSnapStop);
            _engineSnapFullAstern = ParseKeyBinding(data.engineSnapFullAstern);
            _engineSnapStopReverse = ParseKeyBinding(data.engineSnapStopReverse);
            _engineChadburnRotationSpeed = data.engineChadburnRotationSpeed;
            _wheelLeft = ParseKeyCode(data.wheelLeft, defaults?.defaultWheelLeft ?? KeyCode.A);
            _wheelRight = ParseKeyCode(data.wheelRight, defaults?.defaultWheelRight ?? KeyCode.D);
            _liftUp = ParseKeyCode(data.liftUp, defaults?.defaultLiftUp ?? KeyCode.Q);
            _liftDown = ParseKeyCode(data.liftDown, defaults?.defaultLiftDown ?? KeyCode.E);
            _liftSnapFullUp = ParseKeyBinding(data.liftSnapFullUp);
            _liftSnapCenter = ParseKeyBinding(data.liftSnapCenter);
            _liftSnapFullDown = ParseKeyBinding(data.liftSnapFullDown);
            _liftSnapCenterDown = ParseKeyBinding(data.liftSnapCenterDown);
            _liftChadburnRotationSpeed = data.liftChadburnRotationSpeed;
            
            if (debugLog)
            {
                string msg = $"KeyBindingConfig: Loaded wheelLeft={_wheelLeft}, wheelRight={_wheelRight}, liftUp={_liftUp}, liftDown={_liftDown}";
                Debug.Log(msg);
                FileLogger.Log(msg, "KeyBindings");
            }

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
        DefaultKeybindings defaults = DefaultKeybindings.Instance;
        if (defaults == null)
        {
            Debug.LogWarning("DefaultKeybindings not found! Using hardcoded fallbacks.");
            // Hardcoded fallbacks
            _bridgeView = KeyCode.F1;
            _followView = KeyCode.F2;
            _overheadView = KeyCode.F3;
            _bridgeSnap = KeyCode.F1;
            _followSnap = KeyCode.F2;
            _overheadSnap = KeyCode.F3;
            _lookLeft = KeyCode.LeftArrow;
            _lookRight = KeyCode.RightArrow;
            _lookUp = KeyCode.UpArrow;
            _lookDown = KeyCode.DownArrow;
            _zoomIn = KeyCode.UpArrow;
            _zoomOut = KeyCode.DownArrow;
            _fireAllWeapons = KeyCode.F;
            _instrumentZoom = KeyCode.Z;
            _engineForward = KeyCode.W;
            _engineReverse = KeyCode.S;
            _engineChadburnRotationSpeed = 45f;
            _wheelLeft = KeyCode.A;
            _wheelRight = KeyCode.D;
            _liftUp = KeyCode.Q;
            _liftDown = KeyCode.E;
            _liftChadburnRotationSpeed = 45f;
            _snapRequiresCtrl = true;
            _zoomRequiresCtrl = true;
        }
        else
        {
            _bridgeView = defaults.defaultBridgeView;
            _followView = defaults.defaultFollowView;
            _overheadView = defaults.defaultOverheadView;
            _bridgeSnap = defaults.defaultBridgeSnap;
            _followSnap = defaults.defaultFollowSnap;
            _overheadSnap = defaults.defaultOverheadSnap;
            _lookLeft = defaults.defaultLookLeft;
            _lookRight = defaults.defaultLookRight;
            _lookUp = defaults.defaultLookUp;
            _lookDown = defaults.defaultLookDown;
            _zoomIn = defaults.defaultZoomIn;
            _zoomOut = defaults.defaultZoomOut;
            _fireAllWeapons = defaults.defaultFireAllWeapons;
            _instrumentZoom = defaults.defaultInstrumentZoom;
            _engineForward = defaults.defaultEngineForward;
            _engineReverse = defaults.defaultEngineReverse;
            _engineChadburnRotationSpeed = defaults.defaultEngineChadburnRotationSpeed;
            _wheelLeft = defaults.defaultWheelLeft;
            _wheelRight = defaults.defaultWheelRight;
            _liftUp = defaults.defaultLiftUp;
            _liftDown = defaults.defaultLiftDown;
            _liftChadburnRotationSpeed = defaults.defaultLiftChadburnRotationSpeed;
            _snapRequiresCtrl = defaults.defaultSnapRequiresCtrl;
            _zoomRequiresCtrl = defaults.defaultZoomRequiresCtrl;
        }
        
        // autoReturnSpeedDegPerSec comes from ShipWheelController, use hardcoded default
        _autoReturnSpeedDegPerSec = 90f;
        
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
            engineForward = _engineForward.ToString(),
            engineReverse = _engineReverse.ToString(),
            engineChadburnRotationSpeed = _engineChadburnRotationSpeed,
            wheelLeft = _wheelLeft.ToString(),
            wheelRight = _wheelRight.ToString(),
            liftUp = _liftUp.ToString(),
            liftDown = _liftDown.ToString(),
            liftChadburnRotationSpeed = _liftChadburnRotationSpeed,
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
    public bool SetRuntimeKey(string keyName, KeyCode newValue)
    {
        switch (keyName)
        {
            case "bridgeView": _bridgeView = newValue; return true;
            case "followView": _followView = newValue; return true;
            case "overheadView": _overheadView = newValue; return true;
            case "bridgeSnap": _bridgeSnap = newValue; return true;
            case "followSnap": _followSnap = newValue; return true;
            case "overheadSnap": _overheadSnap = newValue; return true;
            case "lookLeft": _lookLeft = newValue; return true;
            case "lookRight": _lookRight = newValue; return true;
            case "lookUp": _lookUp = newValue; return true;
            case "lookDown": _lookDown = newValue; return true;
            case "zoomIn": _zoomIn = newValue; return true;
            case "zoomOut": _zoomOut = newValue; return true;
            case "fireAllWeapons": _fireAllWeapons = newValue; return true;
            case "instrumentZoom": _instrumentZoom = newValue; return true;
            case "engineForward": _engineForward = newValue; return true;
            case "engineReverse": _engineReverse = newValue; return true;
            case "wheelLeft": _wheelLeft = newValue; return true;
            case "wheelRight": _wheelRight = newValue; return true;
            case "liftUp": _liftUp = newValue; return true;
            case "liftDown": _liftDown = newValue; return true;
            default:
                Debug.LogWarning($"KeyBindingConfig: Unknown key name '{keyName}'");
                return false;
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
            case "engineChadburnRotationSpeed": _engineChadburnRotationSpeed = newValue; break;
            case "liftChadburnRotationSpeed": _liftChadburnRotationSpeed = newValue; break;
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
    /// Reload keybindings from JSON file immediately.
    /// Call this after player changes keybindings in settings menu.
    /// </summary>
    public void ReloadKeybindings()
    {
        Debug.Log("KeyBindingConfig: Reloading keybindings from JSON...");
        LoadFromJSON();
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

    /// <summary>
    /// Parse keybinding string with modifiers (e.g., "Ctrl+W", "Shift+Q") into KeyBindingData.
    /// </summary>
    private Teramyyd.UI.KeyBindingData ParseKeyBinding(string value)
    {
        var bindingData = new Teramyyd.UI.KeyBindingData();

        if (string.IsNullOrEmpty(value))
            return bindingData;

        // Check for modifiers
        if (value.Contains("+"))
        {
            string[] parts = value.Split('+');
            foreach (string part in parts)
            {
                string trimmed = part.Trim().ToLower();
                if (trimmed == "ctrl" || trimmed == "control")
                    bindingData.ctrl = true;
                else if (trimmed == "shift")
                    bindingData.shift = true;
                else if (trimmed == "alt")
                    bindingData.alt = true;
                else
                {
                    // Parse the actual key
                    try
                    {
                        bindingData.key = (KeyCode)Enum.Parse(typeof(KeyCode), part.Trim(), true);
                    }
                    catch
                    {
                        Debug.LogWarning($"KeyBindingConfig: Invalid key in binding '{value}'");
                        bindingData.key = KeyCode.None;
                    }
                }
            }
        }
        else
        {
            // No modifiers, just a key
            try
            {
                bindingData.key = (KeyCode)Enum.Parse(typeof(KeyCode), value, true);
            }
            catch
            {
                Debug.LogWarning($"KeyBindingConfig: Invalid key '{value}'");
                bindingData.key = KeyCode.None;
            }
        }

        return bindingData;
    }
}
