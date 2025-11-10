using UnityEngine;

// Manages three camera views (Bridge, Follow, Overhead) using a single Camera + CameraMove.
// Switch views with F1 (Bridge), F2 (Follow), F3 (Overhead) by default.
// HUD should be a Screen Space - Overlay Canvas so it appears identically across views.
public class CameraViewManager : MonoBehaviour
{
    public enum ViewMode { Bridge, Follow, Overhead }

    [Header("References")]
    public Camera mainCamera;            // Assign Main Camera
    public CameraMove cameraMove;        // The rotation/zoom controller on the camera
    public CameraOrbitMove cameraOrbit;  // Orbit controller for follow/overhead
    public OverheadViewController overheadController; // New specialized overhead controller

    [Tooltip("Where the bridge view should be mounted (usually a child on the ship's bridge)")]
    public Transform bridgeMount;

    [Tooltip("The player's ship root transform used as the orbit center in Follow view (usually the ship root)")]
    public Transform followTarget;

    [Tooltip("Optional: a mount/anchor Transform whose position defines the initial Follow camera placement (e.g., FollowCameraMount behind/above the ship)")]
    public Transform followMount;

    [Tooltip("Center point (anchor) of the playfield for Overhead view")]
    public Transform overheadTarget; // Deprecated for overhead; kept for compatibility
    [Tooltip("Anchor position for overhead camera (top-center). If null, derived from GameFieldBounds size.")]
    public Transform overheadMount;

    [Header("Auto-Find Targets")]
    [Tooltip("If true and Follow target is not assigned, the manager will look for a child named FollowCameraFocalPoint under the ship hierarchy or anywhere in the scene.")]
    public bool autoFindFocalPointByName = true;
    [Tooltip("Name of the Transform to use as the follow focal point when auto-finding.")]
    public string followFocalPointName = "FollowCameraFocalPoint";

    [Header("Follow View Settings")]
    [Tooltip("Default orbit distance when entering Follow view (used if no followMount or mount is at target)")] 
    public float followDefaultDistance = 18f;
    [Tooltip("Default pitch angle (degrees) when entering Follow view; 0=horizon, 30=looking down 30°")] 
    public float followDefaultPitch = 25f;
    
    public enum FollowAimMode { LookAtTargetCenter, UseMountForward, LookAtTargetAhead }
    [Tooltip("How the follow view aims initially. Center = look at followTarget. MountForward = use followMount.forward. Ahead = look at a point ahead of the target.")]
    public FollowAimMode followAim = FollowAimMode.LookAtTargetCenter;
    [Tooltip("When using LookAtTargetAhead, how far ahead of the target to aim (in target.forward direction)")]
    public float followLookAhead = 10f;

    [Header("Overhead View Settings")]
    [Tooltip("Default orbit distance when entering Overhead view (tune so full field is visible at no-zoom)")] 
    public float overheadDefaultDistance = 120f;
    [Tooltip("Yaw to face when entering Overhead view")] 
    public float overheadDefaultYaw = 0f;

    [Tooltip("If true, use FOV-based zoom in all modes to keep HUD stable")]
    public bool forceFOVZoom = true;

    [Header("Input")] 
    // Deprecated: now sourced from KeyBindingConfig
    public KeyCode bridgeKey = KeyCode.F1;
    public KeyCode followKey = KeyCode.F2;
    public KeyCode overheadKey = KeyCode.F3;

    [SerializeField]
    private ViewMode currentMode = ViewMode.Bridge;

    void Reset()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null && cameraMove == null) cameraMove = mainCamera.GetComponent<CameraMove>();
        if (mainCamera != null && cameraOrbit == null) cameraOrbit = mainCamera.GetComponent<CameraOrbitMove>();
    }

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (cameraMove == null && mainCamera != null) cameraMove = mainCamera.GetComponent<CameraMove>();
        if (cameraOrbit == null && mainCamera != null) cameraOrbit = mainCamera.GetComponent<CameraOrbitMove>();
    if (overheadController == null && mainCamera != null) overheadController = mainCamera.GetComponent<OverheadViewController>();

        if (forceFOVZoom && cameraMove != null) cameraMove.useFOVZoom = true;

        // Initialize to whatever mode is selected in inspector
        ApplyMode(currentMode, true);
    }

    void Update()
    {
        var kb = KeyBindingConfig.Instance;
        if (kb != null)
        {
            if (Input.GetKeyDown(kb.bridgeView)) ApplyMode(ViewMode.Bridge);
            if (Input.GetKeyDown(kb.followView)) ApplyMode(ViewMode.Follow);
            if (Input.GetKeyDown(kb.overheadView)) ApplyMode(ViewMode.Overhead);
        }
        else
        {
            if (Input.GetKeyDown(bridgeKey)) ApplyMode(ViewMode.Bridge);
            if (Input.GetKeyDown(followKey)) ApplyMode(ViewMode.Follow);
            if (Input.GetKeyDown(overheadKey)) ApplyMode(ViewMode.Overhead);
        }
    }

    public void ApplyMode(ViewMode mode, bool force = false)
    {
        if (!force && mode == currentMode) return;
        currentMode = mode;

        if (mainCamera == null || cameraMove == null) return;

        switch (mode)
        {
            case ViewMode.Bridge:
                EnterBridge();
                break;
            case ViewMode.Follow:
                EnterFollow();
                break;
            case ViewMode.Overhead:
                EnterOverhead();
                break;
        }
    }

    void EnterBridge()
    {
        if (bridgeMount != null)
        {
            mainCamera.transform.SetParent(bridgeMount, worldPositionStays: false);
            mainCamera.transform.localPosition = Vector3.zero;
            mainCamera.transform.localRotation = Quaternion.identity;
        }

        // Enable bridge controller, disable orbit and overhead controllers
        if (cameraMove != null)
        {
            cameraMove.enabled = true;
            cameraMove.ClearOrbitTarget();
            cameraMove.useFOVZoom = forceFOVZoom || cameraMove.useFOVZoom;
            cameraMove.RebaselineFromCurrent(); // This resets position and zoom to baseline
        }
        if (cameraOrbit != null) cameraOrbit.enabled = false;
        if (overheadController != null) overheadController.enabled = false;
        
        Debug.Log("Switched to Bridge view (reset to default)");
    }

    void EnterFollow()
    {
        // Auto-assign follow target if requested and missing.
        if (followTarget == null && autoFindFocalPointByName)
        {
            TryAutoAssignFollowTarget();
        }
        if (followTarget == null)
        {
            Debug.LogWarning("CameraViewManager: Follow target not set. Assign 'followTarget' or create a GameObject named 'FollowCameraFocalPoint' under your ship and try again.");
            return;
        }
        mainCamera.transform.SetParent(null, worldPositionStays: true);

        // Determine starting position from followMount if provided; otherwise use default behind/above offset
        Vector3 startPos;
        Vector3 center = followTarget.position;
        if (followMount != null)
        {
            startPos = followMount.position;
        }
        else
        {
            float yawInit = followTarget.eulerAngles.y;
            float pitchInit = followDefaultPitch;
            Quaternion desired = Quaternion.Euler(pitchInit, yawInit, 0f);
            startPos = center + desired * Vector3.back * followDefaultDistance;
        }

        // Position camera
        mainCamera.transform.position = startPos;
        mainCamera.transform.rotation = Quaternion.LookRotation(center - startPos, Vector3.up);

        // Compute spherical angles from center to camera
        Vector3 dir = (startPos - center).normalized; // direction from center to camera
        float dist = Vector3.Distance(startPos, center);
        // Convert direction to yaw/pitch for orbit controller
        float yawAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float pitchAngle = Mathf.Asin(dir.y) * Mathf.Rad2Deg; // elevation

        // Enable orbit controller and reset to default position
        if (cameraOrbit != null)
        {
            cameraOrbit.enabled = true;
            cameraOrbit.useFOVZoom = forceFOVZoom || cameraOrbit.useFOVZoom;
            cameraOrbit.SetTarget(followTarget, dist, yawAngle, pitchAngle); // This already resets position/angle
        }
        if (cameraMove != null) cameraMove.enabled = false;
        if (overheadController != null) overheadController.enabled = false;
        
        Debug.Log("Switched to Follow view (reset to default)");
    }

    // Attempts to find a Transform named followFocalPointName under the same top-level root as the
    // bridgeMount or anywhere in the scene as a fallback.
    private void TryAutoAssignFollowTarget()
    {
        if (followTarget != null) return;
        Transform candidate = null;
        Transform searchRoot = bridgeMount != null ? GetTopmost(bridgeMount) : null;
        if (searchRoot != null)
        {
            var all = searchRoot.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (t.name == followFocalPointName) { candidate = t; break; }
            }
        }
        if (candidate == null)
        {
            var go = GameObject.Find(followFocalPointName);
            if (go != null) candidate = go.transform;
        }
        if (candidate != null) followTarget = candidate;
    }

    private Transform GetTopmost(Transform t)
    {
        while (t != null && t.parent != null) t = t.parent;
        return t;
    }

    void EnterOverhead()
    {
        mainCamera.transform.SetParent(null, worldPositionStays: true);

        // Enable specialized overhead controller, disable others
        if (cameraMove != null) cameraMove.enabled = false;
        if (cameraOrbit != null) cameraOrbit.enabled = false;

        if (overheadController == null)
        {
            overheadController = mainCamera.gameObject.AddComponent<OverheadViewController>();
        }

        // Wire ship target and initialize over ship
        overheadController.enabled = true;
        if (followTarget != null) overheadController.shipTarget = followTarget;
        overheadController.heightAboveShip = 1000f; // per spec
        overheadController.SnapToShipCenter(); // This resets position and zoom to default
        
        Debug.Log("Switched to Overhead view (reset to default)");
    }
}
