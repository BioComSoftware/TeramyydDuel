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
    [Tooltip("Default pitch angle (degrees) when entering Follow view; 0=horizon, 30=looking down 30Â°")] 
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

    [Header("Zoom Memory")]
    [Tooltip("Store a separate zoom level for each camera view so switching modes restores the last-used zoom for that view.")]
    public bool rememberZoomPerView = true;

    [Header("Pose Memory")]
    [Tooltip("Remember camera position/orientation for each view mode and restore it when returning to that view.")]
    public bool rememberPosePerView = true;

    [Header("Debug")]
    [Tooltip("Enables debug logging to console.")]
    public bool debugLog = false;

    private float bridgeStoredFOV = -1f;
    private float followStoredFOV = -1f;
    private float followStoredOrbitDistance = -1f;
    private float overheadStoredFOV = -1f;
    private float overheadStoredHeight = -1f;

    private bool bridgePoseStored = false;
    private Vector3 bridgeStoredLocalPos = Vector3.zero;
    private Quaternion bridgeStoredLocalRot = Quaternion.identity;
    private Vector3 bridgeStoredWorldPos = Vector3.zero;
    private Quaternion bridgeStoredWorldRot = Quaternion.identity;

    private bool followPoseStored = false;
    private float followStoredYaw = 0f;
    private float followStoredPitch = 0f;

    private bool overheadPoseStored = false;
    private Vector3 overheadStoredOffset = Vector3.zero;

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

        if (!force)
        {
            CaptureZoomState(currentMode);
            CapturePoseState(currentMode);
        }

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

            RestorePoseState(mode);
            RestoreZoomState(mode);
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
            cameraMove.RebaselineFromCurrent(); // This resets position and zoom to baseline
        }
        if (cameraOrbit != null) cameraOrbit.enabled = false;
        if (overheadController != null) overheadController.enabled = false;
        
        if (debugLog)
        {
            Debug.Log("Switched to Bridge view (reset to default)");
        }
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
            cameraOrbit.SetTarget(followTarget, dist, yawAngle, pitchAngle); // This already resets position/angle
        }
        if (cameraMove != null) cameraMove.enabled = false;
        if (overheadController != null) overheadController.enabled = false;
        
        if (debugLog)
        {
            Debug.Log("Switched to Follow view (reset to default)");
        }
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
        
        if (debugLog)
        {
            Debug.Log("Switched to Overhead view (reset to default)");
        }
    }

    void CaptureZoomState(ViewMode mode)
    {
        if (!rememberZoomPerView || mainCamera == null)
            return;

        switch (mode)
        {
            case ViewMode.Bridge:
                if (cameraMove != null && cameraMove.useFOVZoom)
                {
                    bridgeStoredFOV = mainCamera.fieldOfView;
                }
                break;
            case ViewMode.Follow:
                if (cameraOrbit != null)
                {
                    if (cameraOrbit.useFOVZoom && mainCamera != null)
                    {
                        followStoredFOV = mainCamera.fieldOfView;
                    }
                    else
                    {
                        followStoredOrbitDistance = cameraOrbit.distance;
                    }
                }
                break;
            case ViewMode.Overhead:
                if (overheadController != null)
                {
                    if (overheadController.useFOVZoom && mainCamera != null)
                    {
                        overheadStoredFOV = mainCamera.fieldOfView;
                    }
                    else
                    {
                        overheadStoredHeight = overheadController.heightAboveShip;
                    }
                }
                break;
        }
    }

    void RestoreZoomState(ViewMode mode)
    {
        if (!rememberZoomPerView || mainCamera == null)
            return;

        switch (mode)
        {
            case ViewMode.Bridge:
                if (cameraMove != null && cameraMove.useFOVZoom && bridgeStoredFOV > 0f)
                {
                    mainCamera.fieldOfView = Mathf.Clamp(bridgeStoredFOV, cameraMove.minFOV, cameraMove.maxFOV);
                }
                break;
            case ViewMode.Follow:
                if (cameraOrbit != null)
                {
                    if (cameraOrbit.useFOVZoom && followStoredFOV > 0f)
                    {
                        mainCamera.fieldOfView = Mathf.Clamp(followStoredFOV, cameraOrbit.minFOV, cameraOrbit.maxFOV);
                    }
                    else if (!cameraOrbit.useFOVZoom && followStoredOrbitDistance > 0f)
                    {
                        cameraOrbit.distance = Mathf.Clamp(followStoredOrbitDistance, cameraOrbit.minDistance, cameraOrbit.maxDistance);
                        cameraOrbit.Reposition();
                    }
                }
                break;
            case ViewMode.Overhead:
                if (overheadController != null)
                {
                    if (overheadController.useFOVZoom && overheadStoredFOV > 0f)
                    {
                        mainCamera.fieldOfView = Mathf.Clamp(overheadStoredFOV, overheadController.minFOV, overheadController.maxFOV);
                    }
                    else if (!overheadController.useFOVZoom && overheadStoredHeight > 0f)
                    {
                        float minHeight = Mathf.Max(overheadController.minHeightAboveGround, 1f);
                        float maxHeightCandidate = overheadController.maxHeightAboveShip > 0f ? overheadController.maxHeightAboveShip : overheadController.heightAboveShip;
                        float maxHeight = Mathf.Max(maxHeightCandidate, minHeight);
                        overheadController.heightAboveShip = Mathf.Clamp(overheadStoredHeight, minHeight, maxHeight);
                        overheadController.RefreshImmediate();
                    }
                }
                break;
        }
    }

    void CapturePoseState(ViewMode mode)
    {
        if (!rememberPosePerView || mainCamera == null)
            return;

        switch (mode)
        {
            case ViewMode.Bridge:
                bridgeStoredWorldPos = mainCamera.transform.position;
                bridgeStoredWorldRot = mainCamera.transform.rotation;
                if (bridgeMount != null)
                {
                    if (mainCamera.transform.parent == bridgeMount)
                    {
                        bridgeStoredLocalPos = mainCamera.transform.localPosition;
                        bridgeStoredLocalRot = mainCamera.transform.localRotation;
                    }
                    else
                    {
                        bridgeStoredLocalPos = bridgeMount.InverseTransformPoint(mainCamera.transform.position);
                        bridgeStoredLocalRot = Quaternion.Inverse(bridgeMount.rotation) * mainCamera.transform.rotation;
                    }
                }
                bridgePoseStored = true;
                break;
            case ViewMode.Follow:
                if (cameraOrbit != null)
                {
                    followStoredYaw = cameraOrbit.yaw;
                    followStoredPitch = cameraOrbit.pitch;
                    followStoredOrbitDistance = cameraOrbit.distance;
                    followPoseStored = true;
                }
                break;
            case ViewMode.Overhead:
                if (overheadController != null)
                {
                    overheadStoredOffset = overheadController.GetOffsetXZ();
                    overheadPoseStored = true;
                }
                break;
        }
    }

    void RestorePoseState(ViewMode mode)
    {
        if (!rememberPosePerView || mainCamera == null)
            return;

        switch (mode)
        {
            case ViewMode.Bridge:
                if (!bridgePoseStored)
                    break;
                if (bridgeMount != null)
                {
                    if (mainCamera.transform.parent != bridgeMount)
                    {
                        mainCamera.transform.SetParent(bridgeMount, false);
                    }
                    mainCamera.transform.localPosition = bridgeStoredLocalPos;
                    mainCamera.transform.localRotation = bridgeStoredLocalRot;
                }
                else
                {
                    mainCamera.transform.SetParent(null, true);
                    mainCamera.transform.position = bridgeStoredWorldPos;
                    mainCamera.transform.rotation = bridgeStoredWorldRot;
                }
                if (cameraMove != null)
                {
                    cameraMove.RebaselineFromCurrent();
                }
                break;
            case ViewMode.Follow:
                if (!followPoseStored || cameraOrbit == null)
                    break;
                cameraOrbit.yaw = followStoredYaw;
                cameraOrbit.pitch = Mathf.Clamp(followStoredPitch, cameraOrbit.minPitch, cameraOrbit.maxPitch);
                if (followStoredOrbitDistance > 0f)
                {
                    cameraOrbit.distance = Mathf.Clamp(followStoredOrbitDistance, cameraOrbit.minDistance, cameraOrbit.maxDistance);
                }
                cameraOrbit.Reposition();
                break;
            case ViewMode.Overhead:
                if (!overheadPoseStored || overheadController == null)
                    break;
                overheadController.SetOffsetXZ(overheadStoredOffset);
                overheadController.RefreshImmediate();
                break;
        }
    }
}
