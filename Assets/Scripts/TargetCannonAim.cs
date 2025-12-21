using UnityEngine;

/// <summary>
/// Rotates the Target GameObject to aim its Cannon child at the Ship.
/// Attach this script to the Target GameObject (parent).
/// </summary>
public class TargetCannonAim : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The ship to aim at. Will auto-find 'Ship' GameObject if not assigned.")]
    public Transform ship;

    [Header("Rotation Settings")]
    [Tooltip("If true, rotation will be instant. If false, uses smooth rotation.")]
    public bool instantRotation = true;

    [Tooltip("Rotation speed in degrees per second when using smooth rotation.")]
    public float rotationSpeed = 90f;

    [Header("Debug")]
    [Tooltip("Enable debug logging for this script.")]
    public bool debugLog = false;

    void Awake()
    {
        if (debugLog)
        {
            Debug.Log("[TargetCannonAim] Awake() called - script is running!");
            FileLogger.Log("Awake() called - script is running!", "TargetCannonAim");
        }
        
        // Auto-find the Ship if not assigned
        if (ship == null)
        {
            if (debugLog)
            {
                Debug.Log("[TargetCannonAim] Ship is null, attempting to find 'Ship' GameObject...");
                FileLogger.Log("Ship is null, attempting to find 'Ship' GameObject...", "TargetCannonAim");
            }
            
            GameObject shipObject = GameObject.Find("Ship");
            if (shipObject != null)
            {
                ship = shipObject.transform;
                if (debugLog)
                {
                    Debug.Log($"[TargetCannonAim] ✓ Auto-found Ship at position {ship.position}");
                    FileLogger.Log($"✓ Auto-found Ship at position {ship.position}", "TargetCannonAim");
                }
            }
            else
            {
                Debug.LogError("[TargetCannonAim] ✗ Could not find 'Ship' GameObject in scene!");
                FileLogger.Log("✗ Could not find 'Ship' GameObject in scene!", "TargetCannonAim");
                
                if (debugLog)
                {
                    // List all root GameObjects to help diagnose
                    GameObject[] allObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                    Debug.Log($"[TargetCannonAim] Root GameObjects in scene ({allObjects.Length}):");
                    FileLogger.Log($"Root GameObjects in scene ({allObjects.Length}):", "TargetCannonAim");
                    foreach (GameObject obj in allObjects)
                    {
                        Debug.Log($"  - {obj.name}");
                        FileLogger.Log($"  - {obj.name}", "TargetCannonAim");
                    }
                }
            }
        }
        else
        {
            if (debugLog)
            {
                Debug.Log($"[TargetCannonAim] Ship already assigned: {ship.name}");
                FileLogger.Log($"Ship already assigned: {ship.name}", "TargetCannonAim");
            }
        }
    }

    void Start()
    {
        if (debugLog)
        {
            Debug.Log("[TargetCannonAim] ========== START() CALLED ==========");
            FileLogger.Log("========== START() CALLED ==========", "TargetCannonAim");
        }
        
        if (ship != null)
        {
            if (debugLog)
            {
                Debug.Log($"[TargetCannonAim] Start() - Ready to track Ship at {ship.position}");
                FileLogger.Log($"Start() - Ready to track Ship at {ship.position}", "TargetCannonAim");
            }
        }
        else
        {
            Debug.LogError("[TargetCannonAim] Start() - Ship is still null! Cannot aim.");
            FileLogger.Log("Start() - Ship is still null! Cannot aim.", "TargetCannonAim");
        }
    }

    void Update()
    {
        if (ship == null)
        {
            if (debugLog)
            {
                Debug.LogError("[TargetCannonAim] Update() - Ship is NULL! Cannot aim!");
                FileLogger.Log("Update() - Ship is NULL! Cannot aim!", "TargetCannonAim");
            }
            return;
        }

        // Calculate direction from Target to Ship
        Vector3 directionToShip = ship.position - transform.position;

        // Zero out the Y component to keep rotation level (prevents tilting up/down)
        directionToShip.y = 0f;

        // Skip if direction is too small (avoid flickering when directly above/below)
        if (directionToShip.sqrMagnitude < 0.001f)
        {
            if (debugLog)
            {
                Debug.LogWarning("[TargetCannonAim] Direction too small, skipping rotation");
                FileLogger.Log("Direction too small, skipping rotation", "TargetCannonAim");
            }
            return;
        }

        // Calculate the target rotation
        Quaternion targetRotation = Quaternion.LookRotation(directionToShip);

        // Apply rotation (instant or smooth)
        if (instantRotation)
        {
            transform.rotation = targetRotation;
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}
