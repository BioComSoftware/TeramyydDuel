using UnityEngine;

// Overhead view: follows ship at a fixed height with persistent X/Z offset and simple panning.
// - Starts directly above the ship at heightAboveShip.
// - Always travels with the ship, maintaining a user-adjustable offset on X/Z.
// - Arrow keys pan the view by modifying the offset relative to the ship:
//   Up    -> negative Z (ship appears to move down)
//   Down  -> positive Z (ship appears to move up)
//   Right -> positive X (ship appears to move left)
//   Left  -> negative X (ship appears to move right)
// - Ctrl+F3 snaps back directly above the ship (zero offset).
public class OverheadViewController : MonoBehaviour
{
    [Header("Target")]
    public Transform shipTarget;            // Assign the ship root

    [Header("View")]
    public float heightAboveShip = 1000f;   // World-space height above the ship
    public float panSpeed = 200f;           // Units/sec for panning
    [Tooltip("Extra distance added to camera far clip beyond heightAboveShip to ensure ship/ground are rendered.")] public float farClipPadding = 300f;

    [Header("Zoom")]
    [Tooltip("Use FOV-based zoom; if disabled, height-based zoom is used.")] public bool useFOVZoom = true;
    [Tooltip("Minimum field of view when zooming in.")] public float minFOV = 5f;
    [Tooltip("Maximum field of view (baseline). Will be captured at Start if 0.")] public float maxFOV = 70f;
    public float zoomSpeed = 60f;           // FOV degrees/sec
    private float baseFOV;                  // "No zoom" FOV to reset to

    [Header("Height Zoom (if useFOVZoom = false)")]
    [Tooltip("Minimum world-space height above ground the camera may reach when zoomed in.")] public float minHeightAboveGround = 50f;
    [Tooltip("Maximum height above the ship (baseline). If 0, Start will use initial heightAboveShip value.")] public float maxHeightAboveShip = 0f;
    [Tooltip("Units per second when height-zooming.")] public float heightZoomSpeed = 400f;
    private float baseHeight;               // Baseline heightAboveShip for reset

    [Header("Controls")]
    public KeyCode snapKey = KeyCode.F3;    // Use with Ctrl (overridden by KeyBindingConfig if present)

    private Vector3 offsetXZ = Vector3.zero; // Only x/z used; y handled by heightAboveShip
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        // Auto-find ship if not assigned
        if (shipTarget == null)
        {
            var shipGo = GameObject.Find("Ship");
            if (shipGo != null) shipTarget = shipGo.transform;
            else
            {
                var mount = GameObject.Find("OverheadCameraMount");
                if (mount != null)
                {
                    // Prefer parent (Ship) if exists
                    shipTarget = mount.transform.parent != null ? mount.transform.parent : mount.transform;
                }
            }
            if (shipTarget == null)
            {
                Debug.LogWarning("OverheadViewController: shipTarget not assigned and no 'Ship' or 'OverheadCameraMount' found. Overhead view will not update.");
            }
        }
        // Cache baseline values
        if (cam != null)
        {
            if (maxFOV <= 0f) maxFOV = cam.fieldOfView; // capture if unset
            baseFOV = maxFOV;
        }
        if (maxHeightAboveShip <= 0f) maxHeightAboveShip = heightAboveShip;
        baseHeight = maxHeightAboveShip;
        // Initialize directly above ship
        SnapToShipCenter();
        ApplyRotation();
        ConfigureClipPlanes();
    }

    void Update()
    {
        if (shipTarget == null) return;

        // Ctrl+F3 snaps back above ship (zero offset)
        var kb = KeyBindingConfig.Instance; 
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        KeyCode effectiveSnap = kb ? kb.overheadSnap : snapKey;
        bool snapNeedsCtrl = kb ? kb.snapRequiresCtrl : true;
        bool zoomNeedsCtrl = kb ? kb.zoomRequiresCtrl : true;

        if ((zoomNeedsCtrl && ctrl) || (!zoomNeedsCtrl))
        {
            if (Input.GetKeyDown(effectiveSnap) && (!snapNeedsCtrl || ctrl))
            {
                SnapToShipCenter();
                if (cam != null) cam.fieldOfView = baseFOV; // reset zoom to no-zoom
                return;
            }

            // Zoom in/out (FOV or height)
            float zoomDir = 0f;
            if (Input.GetKey(KeyCode.UpArrow)) zoomDir = 1f;    // zoom in
            if (Input.GetKey(KeyCode.DownArrow)) zoomDir = -1f; // zoom out
            if (zoomDir != 0f)
            {
                if (useFOVZoom && cam != null)
                {
                    float fov = cam.fieldOfView;
                    fov += -zoomDir * zoomSpeed * Time.deltaTime; // Up narrows = zoom in
                    cam.fieldOfView = Mathf.Clamp(fov, minFOV, maxFOV);
                }
                else
                {
                    // Height-based zoom: reduce heightAboveShip toward minHeightAboveGround, increase toward baseHeight
                    float shipY = shipTarget.position.y;
                    float currentWorldHeight = shipY + heightAboveShip;
                    float targetWorldHeight;
                    if (zoomDir > 0f) // zoom in -> get closer
                        targetWorldHeight = Mathf.Max(minHeightAboveGround, currentWorldHeight - heightZoomSpeed * Time.deltaTime);
                    else // zoom out -> move farther
                        targetWorldHeight = Mathf.Min(shipY + baseHeight, currentWorldHeight + heightZoomSpeed * Time.deltaTime);
                    // Convert back to relative heightAboveShip
                    heightAboveShip = targetWorldHeight - shipY;
                }
            }
            if (zoomNeedsCtrl && ctrl) return; // block panning if we're in modifier-required zoom mode
        }

        // Pan with arrows: adjust offset relative to ship
        float h = 0f, v = 0f;
    if (Input.GetKey(KeyCode.LeftArrow))  h -= 1f;
    if (Input.GetKey(KeyCode.RightArrow)) h += 1f;
    if (Input.GetKey(KeyCode.UpArrow))    v -= 1f;
    if (Input.GetKey(KeyCode.DownArrow))  v += 1f;

        if (h != 0f || v != 0f)
        {
            offsetXZ.x += h * panSpeed * Time.deltaTime;
            offsetXZ.z += v * panSpeed * Time.deltaTime;
        }
    }

    void LateUpdate()
    {
        if (shipTarget == null) return;
        // Keep camera riding with the ship plus offset at the desired height
        Vector3 shipPos = shipTarget.position;
        transform.position = new Vector3(shipPos.x + offsetXZ.x, shipPos.y + heightAboveShip, shipPos.z + offsetXZ.z);
        ApplyRotation();
        ConfigureClipPlanes();
    }

    public void SnapToShipCenter()
    {
        if (shipTarget == null) return;
        offsetXZ = Vector3.zero;
        Vector3 shipPos = shipTarget.position;
        // Reset zoom states
        if (useFOVZoom && cam != null) cam.fieldOfView = baseFOV;
        else heightAboveShip = baseHeight;
        transform.position = new Vector3(shipPos.x, shipPos.y + heightAboveShip, shipPos.z);
    }

    private void ApplyRotation()
    {
        // Always straight down, zero roll
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void ConfigureClipPlanes()
    {
        if (cam == null) return;
        // Ensure far clip comfortably exceeds vertical distance to ship + any offset
        float requiredFar = heightAboveShip + farClipPadding;
        if (cam.farClipPlane < requiredFar) cam.farClipPlane = requiredFar;
        // Optional: keep near clip modest
        if (cam.nearClipPlane > 0.3f) cam.nearClipPlane = 0.3f;
    }
}
