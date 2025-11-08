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
    
    void Update()
    {
        // Check if Shift or Ctrl is held
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        
        // Get input from arrow keys
        float horizontal = 0f;
        float vertical = 0f;
        
        if (Input.GetKey(KeyCode.LeftArrow))
            horizontal = -1f;
        if (Input.GetKey(KeyCode.RightArrow))
            horizontal = 1f;
        if (Input.GetKey(KeyCode.UpArrow))
            vertical = 1f;
        if (Input.GetKey(KeyCode.DownArrow))
            vertical = -1f;
        
        if (horizontal != 0f || vertical != 0f)
        {
            if (ctrlHeld)
            {
                // Ctrl + Arrow Keys: Zoom in/out
                if (useFOVZoom && cam != null)
                {
                    // Adjust field of view to simulate zoom without moving camera (HUD unaffected)
                    float fov = cam.fieldOfView;
                    fov += -vertical * zoomSpeed * Time.deltaTime; // Up narrows FOV (zoom in)
                    cam.fieldOfView = Mathf.Clamp(fov, minFOV, maxFOV);
                }
                else if (orbitTarget != null)
                {
                    // Adjust orbit distance around target
                    orbitDistance += -vertical * zoomSpeed * Time.deltaTime;
                    orbitDistance = Mathf.Clamp(orbitDistance, minOrbitDistance, maxOrbitDistance);
                    Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
                    Vector3 offset = rotation * Vector3.back * orbitDistance;
                    transform.position = orbitTarget.position + offset;
                    transform.LookAt(orbitTarget);
                }
                else
                {
                    // Fallback: physically move camera along its forward
                    Vector3 zoomDirection = transform.forward * vertical;  // Up = forward, Down = backward
                    transform.position += zoomDirection * moveSpeed * Time.deltaTime;
                }
            }
            else // Rotate camera only; Shift is intentionally ignored to prevent panning
            {
                // Arrow Keys alone: Rotate camera
                currentYaw += horizontal * rotationSpeed * Time.deltaTime;

                // Adjust relative 'up' pitch: UpArrow increases upward angle up to maxPitchRange,
                // DownArrow returns toward baseline. We never go below the starting pitch.
                relativePitch += (vertical) * rotationSpeed * Time.deltaTime;   // Up increases relative upward pitch
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
}
