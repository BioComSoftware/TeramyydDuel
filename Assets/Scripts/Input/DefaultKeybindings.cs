using UnityEngine;

/// <summary>
/// Centralized location for developer to set default keybinding values in Unity Inspector.
/// Attach this component to a GameObject named "DefaultKeybindings".
/// 
/// IMPORTANT: These are DEFAULT VALUES ONLY - they never change during gameplay.
/// - Developer sets these in Unity Inspector
/// - Player runtime settings are stored in keybindings.json (authoritative during gameplay)
/// - When player clicks "Reset to Defaults", values from this script are copied to keybindings.json
/// 
/// EXCEPTION: autoReturnSpeedDegPerSec is NOT in this script - it remains in ShipWheelController
/// </summary>
public class DefaultKeybindings : MonoBehaviour
{
    [Header("===== DEVELOPER DEFAULT KEYBINDINGS =====")]
    [Header("These values serve as 'factory defaults' for player keybindings")]
    [Header("They do NOT change when players modify keybindings.json")]
    [Space(10)]
    
    [Header("View Switching Defaults")]
    [Tooltip("Default key to switch to bridge view")]
    public KeyCode defaultBridgeView = KeyCode.F1;
    [Tooltip("Default key to switch to follow view")]
    public KeyCode defaultFollowView = KeyCode.F2;
    [Tooltip("Default key to switch to overhead view")]
    public KeyCode defaultOverheadView = KeyCode.F3;

    [Header("View Snap Defaults (require Ctrl)")]
    [Tooltip("Default key to snap camera to bridge position (requires Ctrl)")]
    public KeyCode defaultBridgeSnap = KeyCode.F1;
    [Tooltip("Default key to snap camera to follow position (requires Ctrl)")]
    public KeyCode defaultFollowSnap = KeyCode.F2;
    [Tooltip("Default key to snap camera to overhead position (requires Ctrl)")]
    public KeyCode defaultOverheadSnap = KeyCode.F3;

    [Header("Camera Look/Pan Defaults")]
    [Tooltip("Default key to look/pan left")]
    public KeyCode defaultLookLeft = KeyCode.LeftArrow;
    [Tooltip("Default key to look/pan right")]
    public KeyCode defaultLookRight = KeyCode.RightArrow;
    [Tooltip("Default key to look up")]
    public KeyCode defaultLookUp = KeyCode.UpArrow;
    [Tooltip("Default key to look down")]
    public KeyCode defaultLookDown = KeyCode.DownArrow;

    [Header("Zoom Defaults (require Ctrl)")]
    [Tooltip("Default key to zoom in (requires Ctrl)")]
    public KeyCode defaultZoomIn = KeyCode.UpArrow;
    [Tooltip("Default key to zoom out (requires Ctrl)")]
    public KeyCode defaultZoomOut = KeyCode.DownArrow;

    [Header("Weapon Control Defaults")]
    [Tooltip("Default key to fire all weapons")]
    public KeyCode defaultFireAllWeapons = KeyCode.F;

    [Header("UI Control Defaults")]
    [Tooltip("Default key to zoom instrument panel")]
    public KeyCode defaultInstrumentZoom = KeyCode.Z;

    [Header("Engine Chadburn Control Defaults")]
    [Tooltip("Default key to increase engine forward speed (rotate chadburn clockwise)")]
    public KeyCode defaultEngineForward = KeyCode.W;
    [Tooltip("Default key to increase engine reverse speed (rotate chadburn counter-clockwise)")]
    public KeyCode defaultEngineReverse = KeyCode.S;
    [Tooltip("Default rotation speed in degrees per second when holding engine control keys")]
    public float defaultEngineChadburnRotationSpeed = 45f;

    [Header("Ship Wheel Control Defaults")]
    [Tooltip("Default key to turn ship wheel left (counter-clockwise)")]
    public KeyCode defaultWheelLeft = KeyCode.A;
    [Tooltip("Default key to turn ship wheel right (clockwise)")]
    public KeyCode defaultWheelRight = KeyCode.D;
    
    [Header("Lift Chadburn Control Defaults")]
    [Tooltip("Default key to increase lift (rotate chadburn clockwise)")]
    public KeyCode defaultLiftUp = KeyCode.Q;
    [Tooltip("Default key to decrease lift (rotate chadburn counter-clockwise)")]
    public KeyCode defaultLiftDown = KeyCode.E;
    [Tooltip("Default rotation speed for lift chadburn in degrees per second")]
    public float defaultLiftChadburnRotationSpeed = 45f;

    [Header("Modifier Flag Defaults")]
    [Tooltip("Whether snap commands require Ctrl modifier by default")]
    public bool defaultSnapRequiresCtrl = true;
    [Tooltip("Whether zoom commands require Ctrl modifier by default")]
    public bool defaultZoomRequiresCtrl = true;

    private static DefaultKeybindings _instance;

    /// <summary>
    /// Singleton access to DefaultKeybindings component.
    /// </summary>
    public static DefaultKeybindings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DefaultKeybindings>();
                if (_instance == null)
                {
                    Debug.LogError("DefaultKeybindings component not found in scene! Please create a GameObject named 'DefaultKeybindings' and attach this component.");
                }
            }
            return _instance;
        }
    }

    void Awake()
    {
        // Ensure singleton
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("Multiple DefaultKeybindings components found! There should only be one. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    /// <summary>
    /// Get a KeyBindingData struct populated with all default values.
    /// Used when resetting player keybindings to defaults.
    /// </summary>
    public KeyBindingData GetDefaults()
    {
        // Get autoReturnSpeedDegPerSec from ShipWheelController (not part of this component)
        float autoReturnDefault = 90f;
        ShipWheelController shipWheel = FindObjectOfType<ShipWheelController>();
        if (shipWheel != null)
        {
            autoReturnDefault = shipWheel.autoReturnSpeedDegPerSec;
        }

        return new KeyBindingData
        {
            bridgeView = defaultBridgeView.ToString(),
            followView = defaultFollowView.ToString(),
            overheadView = defaultOverheadView.ToString(),
            bridgeSnap = defaultBridgeSnap.ToString(),
            followSnap = defaultFollowSnap.ToString(),
            overheadSnap = defaultOverheadSnap.ToString(),
            lookLeft = defaultLookLeft.ToString(),
            lookRight = defaultLookRight.ToString(),
            lookUp = defaultLookUp.ToString(),
            lookDown = defaultLookDown.ToString(),
            zoomIn = defaultZoomIn.ToString(),
            zoomOut = defaultZoomOut.ToString(),
            fireAllWeapons = defaultFireAllWeapons.ToString(),
            instrumentZoom = defaultInstrumentZoom.ToString(),
            engineForward = defaultEngineForward.ToString(),
            engineReverse = defaultEngineReverse.ToString(),
            engineChadburnRotationSpeed = defaultEngineChadburnRotationSpeed,
            wheelLeft = defaultWheelLeft.ToString(),
            wheelRight = defaultWheelRight.ToString(),
            liftUp = defaultLiftUp.ToString(),
            liftDown = defaultLiftDown.ToString(),
            liftChadburnRotationSpeed = defaultLiftChadburnRotationSpeed,
            snapRequiresCtrl = defaultSnapRequiresCtrl,
            zoomRequiresCtrl = defaultZoomRequiresCtrl,
            // autoReturnSpeedDegPerSec comes from ShipWheelController component, not this component
            autoReturnSpeedDegPerSec = autoReturnDefault
        };
    }
}
