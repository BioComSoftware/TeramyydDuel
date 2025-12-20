using UnityEngine;

// Simple camera rotation and movement script using arrow keys
// Attach to Main Camera or any camera object
public class CameraMove : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 50f;  // Degrees per second
    
    [Header("Movement Settings")]
    public float moveSpeed = 10f;  // Units per second for Shift+Arrow movement
    
    [Header("Zoom Settings")]
    [Tooltip("When enabled, Ctrl+Up/Down adjusts Camera.fieldOfView instead of moving the camera. This keeps HUD stable.")]
    public bool useFOVZoom = true;
    public float zoomSpeed = 60f; // Degrees per second for FOV or units per second for distance
    public float minFOV = 20f;
    public float maxFOV = 70f;
    public float minOrbitDistance = 2f;
    public float maxOrbitDistance = 100f;
    
    [Header("Optional: Orbit Around Target")]
    public Transform orbitTarget;  // Leave empty to rotate camera in place
    public float orbitDistance = 10f;  // Distance from target when orbiting
    
    private float currentYaw = 0f;
    private float currentPitch = 0f;
    private float startPitch = 0f; // lower bound for pitch (can't look below start)
    private float relativePitch = 0f; // 0..maxPitchRange (degrees above start)
    [Tooltip("Maximum degrees the player can look upward from the starting pitch.")]
    public float maxPitchRange = 90f;
    private Camera cam;
    
    void Start()
    {
        // Initialize rotation from current camera rotation
        Vector3 euler = transform.eulerAngles;
    currentYaw = euler.y;
    currentPitch = euler.x;
    startPitch = currentPitch; // baseline
    relativePitch = 0f;        // at baseline
        cam = GetComponent<Camera>();
        
        // If orbiting, set initial distance
        if (orbitTarget != null)
        {
            orbitDistance = Vector3.Distance(transform.position, orbitTarget.position);
        }
    }

    // Rebaseline the pitch and yaw using the camera's current transform.
    // Call this after externally changing transform.position/rotation or parenting.
    public void RebaselineFromCurrent()
    {
        Vector3 euler = transform.eulerAngles;
        currentYaw = euler.y;
        currentPitch = euler.x;
        startPitch = currentPitch;
        relativePitch = 0f;
    }

    // Optionally set or clear an orbit target at runtime.
    public void SetOrbitTarget(Transform target, float distance)
    {
        orbitTarget = target;
        orbitDistance = distance;
    }

    public void ClearOrbitTarget()
    {
        orbitTarget = null;
    }

    // Force a one-time reposition when an orbit target is (re)assigned so that
    // subsequent rotations don't "jump" from an arbitrary manual placement.
    public void SnapOrbitPosition()
    {
        if (orbitTarget == null) return;
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 offset = rotation * Vector3.back * orbitDistance;
        transform.position = orbitTarget.position + offset;
    }
    
    void Update()
    {
        KeyBindingConfig kb = KeyBindingConfig.Instance;
        
        // Check if Shift or Ctrl is held
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        
        // Check if mouse camera control is enabled
        bool mouseControlActive = false;
        if (kb != null && Input.GetMouseButton(kb.mouseCameraButton))
        {
            mouseControlActive = true;
        }
        
        // Get input from arrow keys or mouse
        float horizontal = 0f;
        float vertical = 0f;
        
        // Mouse input when mouse button is held
        if (mouseControlActive)
        {
            float sensitivity = kb != null ? kb.mouseSensitivity : 5.0f;
            horizontal = Input.GetAxis("Mouse X") * sensitivity;
            vertical = Input.GetAxis("Mouse Y") * sensitivity;
            
            // Apply mouse Y inversion if enabled
            if (kb != null && kb.invertMouseY)
            {
                vertical = -vertical;
            }
        }
        // Keyboard arrow input (always available)
        else
        {
            if (Input.GetKey(KeyCode.LeftArrow))
                horizontal = -1f;
            if (Input.GetKey(KeyCode.RightArrow))
                horizontal = 1f;
            if (Input.GetKey(KeyCode.UpArrow))
                vertical = 1f;
            if (Input.GetKey(KeyCode.DownArrow))
                vertical = -1f;
        }
        
        // Handle zoom - keyboard or mouse wheel
        bool zoomInput = false;
        float zoomDelta = 0f;

        if (ctrlHeld && (vertical != 0f) && !mouseControlActive)
        {
            // Keyboard Ctrl+Arrow zoom
            zoomDelta = vertical;
            zoomInput = true;
        }
        else if (kb != null)
        {
            // Mouse wheel zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                float wheelSensitivity = kb.mouseWheelSensitivity;
                bool scrollForward = scroll > 0f;
                
                // Determine zoom direction based on wheel direction and settings
                string action = scrollForward ? kb.mouseWheelForward : kb.mouseWheelBackward;
                
                if (action == "ZoomIn")
                {
                    zoomDelta = wheelSensitivity;
                    zoomInput = true;
                }
                else if (action == "ZoomOut")
                {
                    zoomDelta = -wheelSensitivity;
                    zoomInput = true;
                }
            }
        }

        if (zoomInput)
        {
            // Ctrl + Arrow Keys or Mouse Wheel: Zoom in/out
            if (useFOVZoom && cam != null)
            {
                // Adjust field of view to simulate zoom without moving camera (HUD unaffected)
                float fov = cam.fieldOfView;
                fov += -zoomDelta * zoomSpeed * Time.deltaTime; // Up narrows FOV (zoom in)
                cam.fieldOfView = Mathf.Clamp(fov, minFOV, maxFOV);
            }
            else if (orbitTarget != null)
            {
                // Adjust orbit distance around target
                orbitDistance += -zoomDelta * zoomSpeed * Time.deltaTime;
                orbitDistance = Mathf.Clamp(orbitDistance, minOrbitDistance, maxOrbitDistance);
                Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
                Vector3 offset = rotation * Vector3.back * orbitDistance;
                transform.position = orbitTarget.position + offset;
                transform.LookAt(orbitTarget);
            }
            else
            {
                // Fallback: physically move camera along its forward
                Vector3 zoomDirection = transform.forward * zoomDelta;  // Up = forward, Down = backward
                transform.position += zoomDirection * moveSpeed * Time.deltaTime;
            }
        }
        else if (horizontal != 0f || vertical != 0f)
        {
            // Rotate camera only; Shift is intentionally ignored to prevent panning
            // Apply rotation speed scaling
            float speedMultiplier = mouseControlActive ? 1f : (rotationSpeed * Time.deltaTime);
            
            if (mouseControlActive)
            {
                // Mouse delta is already in screen space units
                currentYaw += horizontal;
                relativePitch += vertical;
            }
            else
            {
                // Keyboard uses speed * deltaTime
                currentYaw += horizontal * speedMultiplier;
                relativePitch += vertical * speedMultiplier;
            }

            // Adjust relative 'up' pitch: UpArrow increases upward angle up to maxPitchRange,
            // DownArrow returns toward baseline. We never go below the starting pitch.
            relativePitch = Mathf.Clamp(relativePitch, 0f, maxPitchRange);  // 0 = baseline, max = 90° up (default)
            currentPitch = startPitch - relativePitch;                      // subtract to look up when relative increases
            
            if (orbitTarget != null)
            {
                // Orbit around target
                Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
                Vector3 offset = rotation * Vector3.back * orbitDistance;
                transform.position = orbitTarget.position + offset;
                transform.LookAt(orbitTarget);
            }
            else
            {
                // Rotate camera in place
                transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            }
        }
    }
}

