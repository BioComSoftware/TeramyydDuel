using UnityEngine;

// Camera orbit controller: orbits freely around a target using arrow keys.
// Left/Right: yaw  (unclamped 360)
// Up/Down:   pitch (clamped between minPitch and maxPitch in degrees)
// Ctrl+Up/Down: zoom (FOV or distance depending on useFOVZoom)
// This is specialized for Follow / Overhead style orbital views.
public class CameraOrbitMove : MonoBehaviour
{
    [Header("Orbit Target")]
    public Transform target;             // Center to orbit around
    public float distance = 25f;         // Current orbit radius
    public float minDistance = 5f;
    public float maxDistance = 200f;

    [Header("Angles (degrees)")]
    public float yaw = 0f;               // Horizontal angle around Y
    public float pitch = 20f;            // Vertical angle
    public float minPitch = -89f;        // Allow full looking up/down nearly vertical
    public float maxPitch = 89f;

    [Header("Speeds")]
    public float orbitSpeed = 60f;       // Degrees per second
    public float zoomSpeed = 60f;        // For FOV or distance

    [Header("Zoom Mode")]
    public bool useFOVZoom = true;       // True -> change FOV, False -> change distance
    public float minFOV = 20f;
    public float maxFOV = 70f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    public void SetTarget(Transform t, float startDistance, float startYaw, float startPitch)
    {
        target = t;
        distance = startDistance;
        yaw = startYaw;
        pitch = startPitch;
        Reposition();
    }

    void Update()
    {
        if (target == null) return;

        KeyBindingConfig kb = KeyBindingConfig.Instance;
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        // Check if mouse camera control is enabled
        bool mouseControlActive = false;
        if (kb != null && Input.GetMouseButton(kb.mouseCameraButton))
        {
            mouseControlActive = true;
        }

        float h = 0f;
        float v = 0f;

        // Mouse input when mouse button is held
        if (mouseControlActive)
        {
            float sensitivity = kb != null ? kb.mouseSensitivity : 5.0f;
            h = Input.GetAxis("Mouse X") * sensitivity;
            v = Input.GetAxis("Mouse Y") * sensitivity;
            
            // Apply mouse Y inversion if enabled
            if (kb != null && kb.invertMouseY)
            {
                v = -v;
            }
        }
        // Keyboard arrow input (always available)
        else
        {
            if (Input.GetKey(KeyCode.LeftArrow)) h = -1f;
            if (Input.GetKey(KeyCode.RightArrow)) h = 1f;
            if (Input.GetKey(KeyCode.UpArrow)) v = 1f;
            if (Input.GetKey(KeyCode.DownArrow)) v = -1f;
        }

        // Handle zoom - keyboard or mouse wheel
        bool zoomInput = false;
        float zoomDelta = 0f;

        if (ctrl && (v != 0f) && !mouseControlActive)
        {
            // Keyboard Ctrl+Arrow zoom
            zoomDelta = -v;
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
                    zoomDelta = wheelSensitivity; // Zoom in
                    zoomInput = true;
                }
                else if (action == "ZoomOut")
                {
                    zoomDelta = -wheelSensitivity; // Zoom out
                    zoomInput = true;
                }
            }
        }

        if (zoomInput)
        {
            if (useFOVZoom && cam != null)
            {
                float fov = cam.fieldOfView;
                fov += -zoomDelta * zoomSpeed * Time.deltaTime; // Positive delta narrows FOV
                cam.fieldOfView = Mathf.Clamp(fov, minFOV, maxFOV);
            }
            else
            {
                distance += -zoomDelta * zoomSpeed * Time.deltaTime; // Positive delta -> closer
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }
            // Defer actual reposition to LateUpdate so we always follow a moving target
            return;
        }

        if (h != 0f || v != 0f)
        {
            // Apply rotation speed scaling for keyboard input
            float speedMultiplier = mouseControlActive ? 1f : (orbitSpeed * Time.deltaTime);
            
            if (mouseControlActive)
            {
                // Mouse delta is already in screen space units, scale it appropriately
                yaw += h;
                pitch += v;
            }
            else
            {
                // Keyboard uses speed * deltaTime
                yaw += h * speedMultiplier;
                pitch += v * speedMultiplier;
            }
            
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    // Always reposition after all updates so the camera tracks a moving target even when no input occurs.
    void LateUpdate()
    {
        if (target == null) return;
        Reposition();
    }

    public void Reposition()
    {
        if (target == null) return;
        // Spherical to Cartesian conversion
        float radYaw = Mathf.Deg2Rad * yaw;
        float radPitch = Mathf.Deg2Rad * pitch;
        Vector3 dir;
        dir.x = Mathf.Cos(radPitch) * Mathf.Sin(radYaw);
        dir.y = Mathf.Sin(radPitch);
        dir.z = Mathf.Cos(radPitch) * Mathf.Cos(radYaw);
        Vector3 camPos = target.position + dir * distance;
        transform.position = camPos;
        transform.rotation = Quaternion.LookRotation(target.position - camPos, Vector3.up);
    }
}
