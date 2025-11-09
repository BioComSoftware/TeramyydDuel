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

        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        float h = 0f;
        float v = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) h = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) h = 1f;
        if (Input.GetKey(KeyCode.UpArrow)) v = 1f;
        if (Input.GetKey(KeyCode.DownArrow)) v = -1f;

        if (ctrl && (v != 0f))
        {
            if (useFOVZoom && cam != null)
            {
                float fov = cam.fieldOfView;
                fov += -v * zoomSpeed * Time.deltaTime; // Up narrows FOV
                cam.fieldOfView = Mathf.Clamp(fov, minFOV, maxFOV);
            }
            else
            {
                distance += -v * zoomSpeed * Time.deltaTime; // Up -> closer
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }
            Reposition();
            return;
        }

        if (h != 0f || v != 0f)
        {
            yaw += h * orbitSpeed * Time.deltaTime;
            pitch += v * orbitSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            Reposition();
        }
    }

    void Reposition()
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
