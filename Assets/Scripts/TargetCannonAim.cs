using UnityEngine;

/// <summary>
/// Rotates the Target GameObject to aim its Cannon child at the Ship using ballistic trajectory calculation.
/// Calculates the required pitch angle to hit the ship's center accounting for gravity and projectile speed.
/// Attach this script to the Target GameObject (parent).
/// </summary>
public class TargetCannonAim : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The ship to aim at. Will auto-find 'Ship' GameObject if not assigned.")]
    public Transform ship;

    [Header("Cannon Reference")]
    [Tooltip("The cannon child object containing the ProjectileLauncher. Will auto-find if not assigned.")]
    public Transform cannon;

    [Header("Rotation Settings")]
    [Tooltip("If true, rotation will be instant. If false, uses smooth rotation.")]
    public bool instantRotation = true;

    [Tooltip("Rotation speed in degrees per second when using smooth rotation.")]
    public float rotationSpeed = 90f;

    [Header("Ballistics")]
    [Tooltip("Gravity magnitude used for ballistic calculations. Should match Physics.gravity.magnitude.")]
    public float gravity = 9.81f;

    [Tooltip("If true, uses high-angle trajectory when low-angle is impossible. If false, clamps to maximum possible range.")]
    public bool useHighAngleWhenNeeded = false;

    [Tooltip("If true, calculates lead targeting based on ship and Target velocities for more accurate hits.")]
    public bool useLeadTargeting = true;

    [Tooltip("Maximum number of iterations for lead targeting calculation convergence.")]
    public int maxLeadIterations = 5;

    [Header("Auto-Fire Settings")]
    [Tooltip("If true, automatically fires the cannon at the ship once this Target has been targeted by the player.")]
    public bool autoFireWhenTargeted = true;

    [Tooltip("If true, disables automatic firing. Target will aim but not fire (useful for testing aim calculations).")]
    public bool disableFiring = false;

    [Tooltip("Minimum time between auto-fire attempts (in seconds). Prevents excessive firing checks.")]
    public float autoFireCheckInterval = 0.1f;

    [Header("Debug")]
    [Tooltip("Enable debug logging for this script.")]
    public bool debugLog = false;

    private ProjectileLauncher _projectileLauncher;
    private float _launchSpeed;
    private bool _isTargeted = false;
    private float _nextAutoFireCheckTime = 0f;
    private TargetingController _targetingController;

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

        // Auto-find the Cannon child if not assigned
        if (cannon == null)
        {
            cannon = transform.Find("Cannon");
            if (cannon == null)
            {
                Debug.LogError("[TargetCannonAim] Could not find 'Cannon' child object! Please assign manually.");
                FileLogger.Log("Could not find 'Cannon' child object! Please assign manually.", "TargetCannonAim");
            }
            else if (debugLog)
            {
                Debug.Log($"[TargetCannonAim] ✓ Auto-found Cannon child");
                FileLogger.Log("✓ Auto-found Cannon child", "TargetCannonAim");
            }
        }

        // Find the ProjectileLauncher component
        if (cannon != null)
        {
            _projectileLauncher = cannon.GetComponentInChildren<ProjectileLauncher>();
            if (_projectileLauncher == null)
            {
                Debug.LogError("[TargetCannonAim] Could not find ProjectileLauncher component on Cannon or its children!");
                FileLogger.Log("Could not find ProjectileLauncher component on Cannon or its children!", "TargetCannonAim");
            }
            else
            {
                _launchSpeed = _projectileLauncher.launchSpeed;
                if (debugLog)
                {
                    Debug.Log($"[TargetCannonAim] ✓ Found ProjectileLauncher with launchSpeed = {_launchSpeed}");
                    FileLogger.Log($"✓ Found ProjectileLauncher with launchSpeed = {_launchSpeed}", "TargetCannonAim");
                }
            }
        }

        // Set gravity from Physics settings
        gravity = Mathf.Abs(Physics.gravity.y);

        // Find TargetingController to subscribe to targeting events
        _targetingController = FindObjectOfType<TargetingController>();
        if (_targetingController != null)
        {
            _targetingController.onTargetAcquired.AddListener(OnTargetAcquired);
            if (debugLog)
            {
                Debug.Log("[TargetCannonAim] ✓ Subscribed to TargetingController.onTargetAcquired");
                FileLogger.Log("✓ Subscribed to TargetingController.onTargetAcquired", "TargetCannonAim");
            }
        }
        else if (debugLog)
        {
            Debug.LogWarning("[TargetCannonAim] Could not find TargetingController in scene. Auto-fire won't activate until targeted.");
            FileLogger.Log("Could not find TargetingController in scene. Auto-fire won't activate until targeted.", "TargetCannonAim");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (_targetingController != null)
        {
            _targetingController.onTargetAcquired.RemoveListener(OnTargetAcquired);
        }
    }

    /// <summary>
    /// Called when the player targets something. Checks if this Target was targeted.
    /// </summary>
    private void OnTargetAcquired(Health targetedHealth)
    {
        if (targetedHealth == null)
        {
            return;
        }

        // Check if the targeted object is this Target or one of its children
        Health ourHealth = GetComponentInChildren<Health>();
        if (ourHealth != null && targetedHealth == ourHealth)
        {
            _isTargeted = true;
            if (debugLog)
            {
                Debug.Log("[TargetCannonAim] ✓ This Target has been targeted by the player! Auto-fire ENABLED.");
                FileLogger.Log("✓ This Target has been targeted by the player! Auto-fire ENABLED.", "TargetCannonAim");
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

        if (_projectileLauncher == null)
        {
            Debug.LogError("[TargetCannonAim] Start() - ProjectileLauncher is null! Cannot calculate firing solution.");
            FileLogger.Log("Start() - ProjectileLauncher is null! Cannot calculate firing solution.", "TargetCannonAim");
        }
    }

    void Update()
    {
        if (ship == null || _projectileLauncher == null)
        {
            if (debugLog)
            {
                Debug.LogError("[TargetCannonAim] Update() - Missing ship or ProjectileLauncher! Cannot aim!");
                FileLogger.Log("Update() - Missing ship or ProjectileLauncher! Cannot aim!", "TargetCannonAim");
            }
            return;
        }

        // Update launch speed in case it changed at runtime
        _launchSpeed = _projectileLauncher.launchSpeed;

        // Get velocities for lead targeting
        Vector3 targetVelocity = Vector3.zero;
        Vector3 firingVelocity = Vector3.zero;

        if (useLeadTargeting)
        {
            // Get ship velocity
            Rigidbody shipRb = ship.GetComponent<Rigidbody>();
            if (shipRb != null)
            {
                targetVelocity = shipRb.linearVelocity;
            }

            // Get Target's own velocity
            Rigidbody targetRb = GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                firingVelocity = targetRb.linearVelocity;
            }
        }

        // Calculate the firing solution with lead targeting
        bool hasValidSolution = CalculateFiringSolutionWithLead(
            transform.position,
            ship.position,
            firingVelocity,
            targetVelocity,
            _launchSpeed,
            gravity,
            out float yawAngle,
            out float pitchAngle,
            out Vector3 interceptPoint
        );

        if (!hasValidSolution)
        {
            if (debugLog)
            {
                Debug.LogWarning($"[TargetCannonAim] No valid firing solution! Target may be out of range. Distance: {Vector3.Distance(transform.position, ship.position):F1}m");
                FileLogger.Log($"No valid firing solution! Target may be out of range. Distance: {Vector3.Distance(transform.position, ship.position):F1}m", "TargetCannonAim");
            }
            return;
        }

        // Create target rotation from calculated angles
        // NOTE: Negate pitch because this coordinate system uses NEGATIVE pitch to aim UP
        Quaternion targetRotation = Quaternion.Euler(-pitchAngle, yawAngle, 0f);

        // Apply rotation (instant or smooth)
        if (instantRotation)
        {
            transform.rotation = targetRotation;
            
            if (debugLog)
            {
                float lead = Vector3.Distance(ship.position, interceptPoint);
                Debug.Log($"[TargetCannonAim] Instant rotation: Yaw={yawAngle:F1}° Pitch={pitchAngle:F1}° (applied as {-pitchAngle:F1}°) Lead={lead:F1}m");
                FileLogger.Log($"Instant rotation: Yaw={yawAngle:F1}° Pitch={pitchAngle:F1}° (applied as {-pitchAngle:F1}°) Lead={lead:F1}m", "TargetCannonAim");
            }
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // Auto-fire if targeted and enabled
        if (autoFireWhenTargeted && _isTargeted && hasValidSolution && !disableFiring)
        {
            AttemptAutoFire();
        }
    }

    /// <summary>
    /// Attempts to fire the cannon automatically if conditions are met.
    /// </summary>
    private void AttemptAutoFire()
    {
        // Rate-limit fire attempts
        if (Time.time < _nextAutoFireCheckTime)
        {
            return;
        }

        _nextAutoFireCheckTime = Time.time + autoFireCheckInterval;

        // Check if the cannon is ready to fire
        if (_projectileLauncher != null && _projectileLauncher.IsReadyToFire())
        {
            // Fire the cannon using reflection to call the protected FireProjectile method
            var fireMethod = typeof(ProjectileLauncher).GetMethod("FireProjectile", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (fireMethod != null)
            {
                fireMethod.Invoke(_projectileLauncher, null);
                
                if (debugLog)
                {
                    Debug.Log("[TargetCannonAim] 🔥 AUTO-FIRED cannon at ship!");
                    FileLogger.Log("🔥 AUTO-FIRED cannon at ship!", "TargetCannonAim");
                }
            }
            else
            {
                Debug.LogError("[TargetCannonAim] Could not find FireProjectile method on ProjectileLauncher!");
                FileLogger.Log("Could not find FireProjectile method on ProjectileLauncher!", "TargetCannonAim");
            }
        }
    }

    /// <summary>
    /// Calculates the firing solution with lead targeting, accounting for both ship and Target movement.
    /// Uses iterative approach to find the intercept point where projectile meets the moving target.
    /// </summary>
    /// <param name="firingPos">Current position of the cannon</param>
    /// <param name="targetPos">Current position of the target (ship center)</param>
    /// <param name="firingVelocity">Velocity of the firing platform (Target)</param>
    /// <param name="targetVelocity">Velocity of the target (ship)</param>
    /// <param name="projectileSpeed">Launch speed of the projectile</param>
    /// <param name="gravityMagnitude">Gravity magnitude (positive)</param>
    /// <param name="yawAngle">Output: Horizontal angle in degrees</param>
    /// <param name="pitchAngle">Output: Vertical angle in degrees (positive = up)</param>
    /// <param name="interceptPoint">Output: Calculated intercept point in world space</param>
    /// <returns>True if a valid solution exists, false otherwise</returns>
    private bool CalculateFiringSolutionWithLead(
        Vector3 firingPos,
        Vector3 targetPos,
        Vector3 firingVelocity,
        Vector3 targetVelocity,
        float projectileSpeed,
        float gravityMagnitude,
        out float yawAngle,
        out float pitchAngle,
        out Vector3 interceptPoint)
    {
        yawAngle = 0f;
        pitchAngle = 0f;
        interceptPoint = targetPos;

        // If lead targeting is disabled, use static target position
        if (!useLeadTargeting)
        {
            return CalculateFiringSolution(firingPos, targetPos, projectileSpeed, gravityMagnitude, out yawAngle, out pitchAngle);
        }

        // Calculate relative velocity (target velocity relative to firing platform)
        Vector3 relativeVelocity = targetVelocity - firingVelocity;

        // If relative velocity is negligible, no lead needed
        if (relativeVelocity.sqrMagnitude < 0.01f)
        {
            return CalculateFiringSolution(firingPos, targetPos, projectileSpeed, gravityMagnitude, out yawAngle, out pitchAngle);
        }

        // Iteratively calculate intercept point
        Vector3 estimatedInterceptPoint = targetPos;
        float timeToTarget = 0f;

        for (int iteration = 0; iteration < maxLeadIterations; iteration++)
        {
            // Calculate distance to estimated intercept point
            Vector3 toIntercept = estimatedInterceptPoint - firingPos;
            float distance = toIntercept.magnitude;

            if (distance < 0.001f)
            {
                break; // Too close
            }

            // Estimate time for projectile to reach intercept point
            // Using simple distance/speed for initial estimate, then refine with gravity
            float horizontalDistance = new Vector3(toIntercept.x, 0f, toIntercept.z).magnitude;
            float verticalDistance = toIntercept.y;

            // Estimate flight time accounting for gravity (simplified ballistic time)
            // t ≈ distance / (projectile speed * cos(estimated angle))
            float estimatedAngle = Mathf.Atan2(verticalDistance, horizontalDistance);
            float horizontalSpeed = projectileSpeed * Mathf.Cos(estimatedAngle);
            
            if (horizontalSpeed < 0.1f)
            {
                // Projectile speed too low for this trajectory
                break;
            }

            timeToTarget = horizontalDistance / horizontalSpeed;

            // Calculate where the target will be at that time
            Vector3 predictedTargetPos = targetPos + (relativeVelocity * timeToTarget);

            // Check convergence
            if (Vector3.Distance(estimatedInterceptPoint, predictedTargetPos) < 0.1f)
            {
                // Converged
                interceptPoint = predictedTargetPos;
                break;
            }

            // Update estimate
            estimatedInterceptPoint = predictedTargetPos;
            interceptPoint = estimatedInterceptPoint;
        }

        // Calculate firing solution to the intercept point
        bool hasSolution = CalculateFiringSolution(
            firingPos, 
            interceptPoint, 
            projectileSpeed, 
            gravityMagnitude, 
            out yawAngle, 
            out pitchAngle
        );

        if (hasSolution && debugLog)
        {
            float leadDistance = Vector3.Distance(targetPos, interceptPoint);
            Debug.Log($"[TargetCannonAim] Lead calculation: Time={timeToTarget:F2}s Lead={leadDistance:F1}m RelVel={relativeVelocity.magnitude:F1}m/s");
            FileLogger.Log($"Lead calculation: Time={timeToTarget:F2}s Lead={leadDistance:F1}m RelVel={relativeVelocity.magnitude:F1}m/s", "TargetCannonAim");
        }

        return hasSolution;
    }

    /// <summary>
    /// Calculates the firing solution to hit a target using projectile motion physics.
    /// </summary>
    /// <param name="firingPos">Position of the cannon</param>
    /// <param name="targetPos">Position of the target (ship center)</param>
    /// <param name="projectileSpeed">Launch speed of the projectile</param>
    /// <param name="gravityMagnitude">Gravity magnitude (positive)</param>
    /// <param name="yawAngle">Output: Horizontal angle in degrees</param>
    /// <param name="pitchAngle">Output: Vertical angle in degrees (positive = up)</param>
    /// <returns>True if a valid solution exists, false otherwise</returns>
    private bool CalculateFiringSolution(
        Vector3 firingPos,
        Vector3 targetPos,
        float projectileSpeed,
        float gravityMagnitude,
        out float yawAngle,
        out float pitchAngle)
    {
        yawAngle = 0f;
        pitchAngle = 0f;

        // Calculate displacement vector
        Vector3 displacement = targetPos - firingPos;
        
        // Calculate horizontal direction (yaw)
        Vector3 horizontalDisplacement = new Vector3(displacement.x, 0f, displacement.z);
        float horizontalDistance = horizontalDisplacement.magnitude;
        
        if (horizontalDistance < 0.001f)
        {
            // Target is directly above or below - can't calculate horizontal angle
            yawAngle = transform.eulerAngles.y; // Keep current yaw
            pitchAngle = displacement.y > 0 ? 90f : -90f;
            return false;
        }

        // Calculate yaw angle
        yawAngle = Mathf.Atan2(horizontalDisplacement.x, horizontalDisplacement.z) * Mathf.Rad2Deg;

        // Calculate pitch angle using ballistic trajectory formula
        float verticalDisplacement = displacement.y;
        
        // Ballistic equation: tan(θ) = [v² ± sqrt(v⁴ - g(gx² + 2yv²))] / (gx)
        // Where: v = projectileSpeed, g = gravity, x = horizontalDistance, y = verticalDisplacement
        
        float v2 = projectileSpeed * projectileSpeed;
        float v4 = v2 * v2;
        float gx = gravityMagnitude * horizontalDistance;
        float gx2 = gravityMagnitude * horizontalDistance * horizontalDistance;
        
        float discriminant = v4 - gravityMagnitude * (gx2 + 2f * verticalDisplacement * v2);
        
        if (discriminant < 0f)
        {
            // Target is out of range - no solution exists
            if (debugLog)
            {
                float maxRange = (v2 / gravityMagnitude) * Mathf.Sqrt(1f + (2f * gravityMagnitude * verticalDisplacement / v2));
                Debug.LogWarning($"[TargetCannonAim] Target out of range! Distance: {horizontalDistance:F1}m, Max Range: {maxRange:F1}m");
                FileLogger.Log($"Target out of range! Distance: {horizontalDistance:F1}m, Max Range: {maxRange:F1}m", "TargetCannonAim");
            }
            return false;
        }
        
        // Two solutions: low angle (direct fire) and high angle (lob)
        float sqrtDiscriminant = Mathf.Sqrt(discriminant);
        float angle1 = Mathf.Atan((v2 - sqrtDiscriminant) / gx) * Mathf.Rad2Deg; // Low angle
        float angle2 = Mathf.Atan((v2 + sqrtDiscriminant) / gx) * Mathf.Rad2Deg; // High angle
        
        // Prefer low angle (direct fire) unless specified otherwise
        if (useHighAngleWhenNeeded && angle1 < -45f)
        {
            pitchAngle = angle2;
            if (debugLog)
            {
                Debug.Log($"[TargetCannonAim] Using high-angle trajectory: {pitchAngle:F1}° (low-angle was {angle1:F1}°)");
                FileLogger.Log($"Using high-angle trajectory: {pitchAngle:F1}° (low-angle was {angle1:F1}°)", "TargetCannonAim");
            }
        }
        else
        {
            pitchAngle = angle1;
        }
        
        if (debugLog)
        {
            Debug.Log($"[TargetCannonAim] Firing solution: Yaw={yawAngle:F1}° Pitch={pitchAngle:F1}° Distance={horizontalDistance:F1}m Height={verticalDisplacement:F1}m");
            FileLogger.Log($"Firing solution: Yaw={yawAngle:F1}° Pitch={pitchAngle:F1}° Distance={horizontalDistance:F1}m Height={verticalDisplacement:F1}m", "TargetCannonAim");
        }
        
        return true;
    }
}
