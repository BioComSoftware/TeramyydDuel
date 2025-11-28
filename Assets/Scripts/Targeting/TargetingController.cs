using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles player targeting input: hold the modifier key (default T) and left-click
/// to select any object that has a Health component, excluding the player's own ship hierarchy.
/// Attach this to a player controller object that has access to the player's ship reference.
/// </summary>
public class TargetingController : MonoBehaviour
{
    [System.Serializable]
    public class HealthEvent : UnityEvent<Health> { }

    [Header("References")]
    [Tooltip("Player's ship root. Any Health components under this transform cannot be targeted.")]
    public ShipCharacteristics playerShip;

    [Tooltip("Optional explicit camera for raycasts. Defaults to Camera.main if empty.")]
    public Camera targetingCamera;

    [Header("Input")]
    [Tooltip("Key that must be held to enable targeting clicks.")]
    public KeyCode targetingModifierKey = KeyCode.T;

    [Tooltip("How far the targeting raycast should travel.")]
    public float maxTargetingDistance = 10000f;

    [Tooltip("Physics layers considered for targeting.")]
    public LayerMask targetingLayers = Physics.DefaultRaycastLayers;

    [Header("Events")]
    [Tooltip("Invoked whenever a new valid target is acquired.")]
    public HealthEvent onTargetAcquired;

    public enum FiringSolutionRate
    {
        EveryFixedUpdate = 1,        // ~50 Hz
        Every2ndFixedUpdate = 2,     // ~25 Hz
        Every5thFixedUpdate = 5,     // ~10 Hz
        Every10thFixedUpdate = 10,   // ~5 Hz
        Every20thFixedUpdate = 20    // ~2.5 Hz
    }

    [Header("Firing Solution Update")]
    [Tooltip("Controls how often dependent systems recompute firing solutions. Values are multiples of FixedUpdate (~50 Hz)." )]
    public FiringSolutionRate firingSolutionRate = FiringSolutionRate.EveryFixedUpdate;

    [Header("Debug Logging")]
    [Tooltip("Writes targeting clicks + modifier state changes to Logs/game_debug.log when enabled.")]
    public bool enableDebugLogging = false;

    private Health _currentTarget;
    private int _fixedUpdateAccumulator; 
    private int _solverVersion;
    private bool _modifierActive;
    private bool _lastModifierSample;
    private Collider _currentTargetCollider;

    public Health CurrentTarget => _currentTarget;
    public Collider CurrentTargetCollider => _currentTargetCollider;
    public Camera TargetingCamera => targetingCamera;
    public int SolverVersion => _solverVersion;

    void Awake()
    {
        EnsureLayerMask();
        _modifierActive = targetingModifierKey == KeyCode.None;
        _lastModifierSample = false;

        if (targetingCamera == null)
        {
            targetingCamera = Camera.main;
            if (targetingCamera == null)
            {
                LogDebug("Awake: No camera tagged MainCamera. Assign a camera reference.");
            }
        }
    }

    void Update()
    {
        bool mouseDown = Input.GetMouseButtonDown(0);
        bool modifierRequired = targetingModifierKey != KeyCode.None;

        UpdateModifierState(modifierRequired);

        if (!modifierRequired)
        {
            _modifierActive = true;
        }

        if (!_modifierActive)
        {
            if (mouseDown)
            {
                LogDebug("Ignored click because targeting modifier key was not held.");
            }
            return;
        }

        if (!mouseDown)
            return;

        if (targetingCamera == null)
        {
            Debug.LogWarning("[TargetingController] No camera assigned for targeting.");
            LogDebug("Cannot raycast because no targeting camera is assigned.");
            return;
        }

        Ray ray = targetingCamera.ScreenPointToRay(Input.mousePosition);
        int layerMask = GetEffectiveLayerMask();
        if (!Physics.Raycast(ray, out RaycastHit hit, maxTargetingDistance, layerMask, QueryTriggerInteraction.Ignore))
        {
            LogDebug("Raycast missed all targets.");
            return;
        }

        Health health = hit.collider.GetComponentInParent<Health>();
        if (health == null)
        {
            LogDebug($"Clicked object '{hit.collider.name}' has no Health component.");
            return;
        }

        if (IsPlayerShipObject(health.transform))
        {
            LogDebug("Ignoring click on player's own ship.");
            return;
        }

        AcquireTarget(health, hit.collider);
    }

    void FixedUpdate()
    {
        int interval = Mathf.Max(1, (int)firingSolutionRate);
        _fixedUpdateAccumulator++;
        if (_fixedUpdateAccumulator >= interval)
        {
            _fixedUpdateAccumulator = 0;
            _solverVersion++;
        }
    }

    void UpdateModifierState(bool modifierRequired)
    {
        if (!modifierRequired)
        {
            _modifierActive = true;
            return;
        }

        bool pressedThisFrame = Input.GetKeyDown(targetingModifierKey);
        bool releasedThisFrame = Input.GetKeyUp(targetingModifierKey);

        if (pressedThisFrame)
        {
            _modifierActive = true;
            TrackModifierState(true);
        }
        else if (releasedThisFrame)
        {
            _modifierActive = false;
            TrackModifierState(false);
        }
    }

    void TrackModifierState(bool isPressed)
    {
        if (!enableDebugLogging)
            return;

        if (isPressed != _lastModifierSample)
        {
            _lastModifierSample = isPressed;
            LogDebug($"Modifier key {(isPressed ? "pressed" : "released")}.");
        }
    }

    void AcquireTarget(Health health, Collider hitCollider)
    {
        if (_currentTarget == health && _currentTargetCollider == hitCollider)
            return;

        _currentTarget = health;
        _currentTargetCollider = hitCollider;
        onTargetAcquired?.Invoke(_currentTarget);
        LogDebug($"Target acquired: {_currentTarget.name}");
    }

    bool IsPlayerShipObject(Transform candidate)
    {
        if (playerShip == null)
            return false;

        return candidate.IsChildOf(playerShip.transform);
    }

    void EnsureLayerMask()
    {
        if (targetingLayers.value == 0)
        {
            targetingLayers = Physics.DefaultRaycastLayers;
            LogDebug("Layer mask was 'Nothing'. Defaulting to Physics.DefaultRaycastLayers.");
        }
    }

    int GetEffectiveLayerMask()
    {
        int mask = targetingLayers.value;
        if (mask == 0)
        {
            mask = Physics.DefaultRaycastLayers;
        }
        return mask;
    }

    void LogDebug(string message)
    {
        if (!enableDebugLogging)
            return;

        string formatted = $"[TargetingController] {message}";
        Debug.Log(formatted, this);
        FileLogger.Log(formatted, "TargetingController");
    }
}
