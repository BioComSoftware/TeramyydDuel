using UnityEngine;

// Simple camera rotation and movement script using arrow keys
// Attach to Main Camera or any camera object
public class CameraMove : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 50f;  // Degrees per second
    
    [Header("Movement Settings")]
    public float moveSpeed = 10f;  // Units per second for Shift+Arrow movement
    
    [Header("Optional: Orbit Around Target")]
    public Transform orbitTarget;  // Leave empty to rotate camera in place
    public float orbitDistance = 10f;  // Distance from target when orbiting
    
    private float currentYaw = 0f;
    private float currentPitch = 0f;
    
    void Start()
    {
        // Initialize rotation from current camera rotation
        Vector3 euler = transform.eulerAngles;
        currentYaw = euler.y;
        currentPitch = euler.x;
        
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
                // Ctrl + Arrow Keys: Zoom in/out (move forward/backward)
                Vector3 zoomDirection = transform.forward * vertical;  // Up = forward, Down = backward
                transform.position += zoomDirection * moveSpeed * Time.deltaTime;
                
                // Update orbit distance if orbiting
                if (orbitTarget != null)
                {
                    orbitDistance = Vector3.Distance(transform.position, orbitTarget.position);
                }
            }
            else if (shiftHeld)
            {
                // Shift + Arrow Keys: Move camera position (pan)
                Vector3 moveDirection = Vector3.zero;
                moveDirection += transform.right * horizontal;  // Left/Right movement
                moveDirection += transform.up * vertical;       // Up/Down movement
                
                transform.position += moveDirection * moveSpeed * Time.deltaTime;
                
                // Update orbit distance if orbiting
                if (orbitTarget != null)
                {
                    orbitDistance = Vector3.Distance(transform.position, orbitTarget.position);
                }
            }
            else
            {
                // Arrow Keys alone: Rotate camera
                currentYaw += horizontal * rotationSpeed * Time.deltaTime;
                currentPitch -= vertical * rotationSpeed * Time.deltaTime;
                
                // Clamp pitch to prevent flipping
                currentPitch = Mathf.Clamp(currentPitch, -89f, 89f);
                
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
