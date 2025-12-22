using UnityEngine;
using UnityEngine.Serialization;

// General weapon mount with yaw/pitch pivots and runtime mounting for ProjectileLauncher-based weapons.
public class WeaponMount : MonoBehaviour
{
    [Header("Identity")]
    public string mountId = string.Empty;
    public string mountType = "cannon";  // accepted type (informational gate for game logic)

    [Header("Pivots")]
    [Tooltip("Yaw pivot (left/right) rotates around local Y")] public Transform yawBase;
    [Tooltip("Pitch pivot (up/down) rotates around local X; weapon is parented here")] public Transform pitchBarrel;
    [Tooltip("Total left+right arc; yaw clamped to +/- (yawLimitDeg/2)")] public float yawLimitDeg = 60f;
    [Tooltip("Max elevation above center")] public float pitchUpDeg = 15f;
    [Tooltip("Max depression below center")] public float pitchDownDeg = 15f;

    public enum LauncherAxis { Up, Forward, Right }
    [Header("Launcher Axis Mapping")]
    [Tooltip("Which local axis of the launcher's spawn point represents its firing direction.")]
    public LauncherAxis launcherAxis = LauncherAxis.Up;
    [Tooltip("Invert the chosen axis if your prefab fires along the negative direction (e.g., -Y).")]
    public bool invertLauncherAxis = false;

    [Header("Direction Tweaks")]
    [Tooltip("Invert yaw delta application if your mount turns the opposite of expected.")]
    public bool invertYawDirection = false;
    [Tooltip("Invert pitch delta application if your mount pitches opposite of expected.")]
    public bool invertPitchDirection = false;

    [Header("Target Tracking")]
    [Tooltip("Disable automatic targeting entirely (aiming + ballistic speed adjustments)." )]
    public bool disableAutoTargeting = false;
    [Tooltip("When enabled (and auto targeting is not disabled), the mount will continuously aim toward the current TargetingController target.")]
    public bool autoTrackTarget = true;
    [Tooltip("Reference to the shared TargetingController. Auto-discovered if left empty.")]
    public TargetingController targetingController;
    [Tooltip("Maximum yaw slew speed while auto-tracking (deg/sec).")]
    public float autoAimYawSpeedDegPerSec = 120f;
    [Tooltip("Maximum pitch slew speed while auto-tracking (deg/sec).")]
    public float autoAimPitchSpeedDegPerSec = 90f;
    [Tooltip("Degrees away from the yaw limit that still counts as 'at the edge'. Staying inside this margin means the target is reachable.")]
    [Min(0f)] public float yawEdgeBufferDeg = 0.5f;

    [Header("Lead Targeting")]
    [Tooltip("If true, calculates movement lead based on ship and target velocities just before firing.")]
    public bool useLeadTargeting = true;
    [Tooltip("Maximum number of iterations for lead targeting calculation convergence.")]
    public int maxLeadIterations = 5;
    [Tooltip("If lead calculation pushes yaw beyond limits, clamp to max yaw and fire anyway.")]
    public bool fireAtMaxYawIfLeadExceedsLimit = true;

    [Header("Crew Requirements")]
    [Tooltip("Crew station that operates this mount. Auto-located on the same GameObject if left empty.")]
    public CrewStation crewStation;
    [Tooltip("Creates a transient CrewStation at runtime when none is configured so the mount can participate in the crew system before dedicated mount points exist.")]
    public bool autoCreateCrewStation = true;

    [Header("Debug Logging")]
    [Tooltip("Writes verbose mount + targeting diagnostics to Logs/game_debug.log when enabled.")]
    public bool enableDebugLogging = false;

    // State
    public bool isOccupied { get; private set; } = false;
    private GameObject mountedWeapon;
    private Health weaponHealth;
    public ProjectileLauncher currentLauncher { get; private set; }

    float _yaw;   // signed degrees (left - / right +)
    float _pitch; // signed degrees (up + / down -)
    float _aimYawTarget;
    float _aimPitchTarget;
    float _aimLaunchSpeed;
    bool _hasAimSolution;
    bool _hasBallisticInterceptSolution;
    bool _wasAutoTargetingActive;
    bool _hasLoggedAcquisitionState;
    bool _lastLoggedHorizontalLock;
    bool _lastLoggedBallisticLock;
    bool _loggedCrewWarning;
    float _lastAccuracyScale = -1f;
    float _lastReloadScale = -1f;

    public bool HasSelectedTarget => targetingController != null && targetingController.CurrentTarget != null;
    public bool HasValidFiringSolution => _hasBallisticInterceptSolution;
    public bool HasTargetLock => HasSelectedTarget;
    public bool HasHorizontalLock => HasSelectedTarget && IsYawWithinEdgeMargin() && IsYawAlignedWithTarget();
    public bool IsTargetFullyAcquired => HasHorizontalLock && _hasBallisticInterceptSolution;
    public bool CanFireAtCurrentTarget => IsTargetFullyAcquired;
    public bool HasCrewReady => HasOperationalCrew();
    public Health MountedWeaponHealth => weaponHealth;

    /// <summary>
    /// Attempt to fire the currently mounted weapon. Returns true if a fire command was issued.
    /// </summary>
    public bool TryFire(bool ignoreTargetLock = false, bool showUnmannedMessage = false)
    {
        if (!HasOperationalCrew())
        {
            string stationId = crewStation != null ? crewStation.stationId : "(none)";
            int assigned = crewStation != null ? crewStation.AssignedCrewCount : 0;
            int required = crewStation != null ? crewStation.MinimumCrewRequired : 0;
            LogDebug($"TryFire blocked - no crew assigned (station={stationId}, assigned={assigned}, required={required}).");
            
            // Show message only when manually fired (not from Fire-at-Will or F key)
            // Delegate to the launcher so each weapon type can have custom messages
            if (showUnmannedMessage && currentLauncher != null)
            {
                currentLauncher.ShowUnmannedWeaponMessage();
            }
            
            return false;
        }

        if (currentLauncher == null)
            return false;

        if (!currentLauncher.IsReady)
            return false;

        if (!ignoreTargetLock && !IsTargetFullyAcquired)
            return false;

        // Snap to firing position immediately before firing
        // Apply lead targeting adjustments if enabled
        if (useLeadTargeting && targetingController != null && targetingController.CurrentTarget != null)
        {
            ApplyLeadTargetingAdjustment(targetingController.CurrentTarget.transform);
        }
        else
        {
            // Snap to ballistic solution (yaw and pitch) immediately before firing
            if (_hasAimSolution)
            {
                _yaw = _aimYawTarget;
                _pitch = _aimPitchTarget;
                ApplyRotations();
            }
        }

        currentLauncher.TriggerFireCommand(ignoreTargetLock);
        
        // Grant gunnery XP to all crew members assigned to this weapon
        if (crewStation != null && crewStation.AssignedCrew != null)
        {
            foreach (var crew in crewStation.AssignedCrew)
            {
                if (crew != null)
                {
                    crew.OnWeaponFired();
                }
            }
        }
        
        return true;
    }

    void Reset()
    {
        if (yawBase == null) yawBase = transform;
        if (pitchBarrel == null)
        {
            var go = new GameObject("PitchBarrel");
            go.transform.SetParent(yawBase != null ? yawBase : transform, false);
            pitchBarrel = go.transform;
        }

        EnsureMountId();
    }

    void Awake()
    {
        EnsureMountId();
        EnsureCrewStation();
    }

    void Start()
    {
        EnsureMountId();
        EnsureCrewStation();

        // Try to load from persistence
        if (WeaponPersistenceManager.Instance != null)
        {
            WeaponPersistenceManager.Instance.TryMountSavedWeapon(this);
        }

        ApplyRotations();
        SyncAimTargetsToCurrentPose();
        
        if (enableDebugLogging)
        {
            string path = GetHierarchyPath(transform);
            LogDebug($"Start complete. isOccupied={isOccupied}, path='{path}', childCount={transform.childCount}");
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var childLauncher = child.GetComponent<ProjectileLauncher>();
                LogDebug($"  Child {i}: {child.name}, hasLauncher={childLauncher != null}");
            }
        }
    }

    string GetHierarchyPath(Transform t)
    {
        if (t == null) return "null";
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    bool IsYawWithinEdgeMargin()
    {
        GetYawEdgeMetrics(out _, out _, out float effectiveLimit);
        return Mathf.Abs(_yaw) <= effectiveLimit;
    }

    bool IsYawAlignedWithTarget()
    {
        // Only consider us "locked" if we are pointing within a tight margin of the firing solution
        return Mathf.Abs(Mathf.DeltaAngle(_yaw, _aimYawTarget)) < 2f;
    }

    void GetYawEdgeMetrics(out float halfYaw, out float buffer, out float effectiveLimit)
    {
        halfYaw = Mathf.Max(0f, yawLimitDeg * 0.5f);
        buffer = Mathf.Clamp(yawEdgeBufferDeg, 0f, halfYaw);
        effectiveLimit = Mathf.Max(0f, halfYaw - buffer);
    }

    void Update()
    {
        EnsureMountId();
        EnsureCrewStation();

        bool hasCrew = HasOperationalCrew();
        UpdateCrewPerformanceBonuses(hasCrew);

        if (!hasCrew)
        {
            _hasBallisticInterceptSolution = false;
            if (enableDebugLogging && !_loggedCrewWarning)
            {
                LogDebug($"{mountId}: awaiting crew assignment before operating.");
                _loggedCrewWarning = true;
            }
            return;
        }
        _loggedCrewWarning = false;

        bool autoTargetingActive = !disableAutoTargeting && autoTrackTarget;
        if (autoTargetingActive && !_wasAutoTargetingActive)
        {
            _hasAimSolution = false;
        }
        else if (!autoTargetingActive && _wasAutoTargetingActive)
        {
            SyncAimTargetsToCurrentPose();
        }

        if (autoTargetingActive)
        {
            AutoAimTowardsTarget();
        }
        else
        {
            if (currentLauncher != null)
            {
                currentLauncher.SetRuntimeLaunchSpeed(currentLauncher.launchSpeed);
            }
        }

        // Check if mounted weapon was destroyed externally (e.g., by Health component)
        if (isOccupied && mountedWeapon == null)
        {
            WeaponPersistenceManager.Instance.UnregisterMountedWeapon(this);
            FileLogger.Log($"{mountId}: Mounted weapon was destroyed externally, clearing mount", "WeaponMount");
            
            // Clear mount state
            weaponHealth = null;
            currentLauncher = null;
            isOccupied = false;
        }

        RefreshBallisticReadiness(autoTargetingActive);
        _wasAutoTargetingActive = autoTargetingActive;
        ReportAcquisitionDiagnostics();
    }

    void AutoAimTowardsTarget()
    {
        if (!isOccupied || mountedWeapon == null || pitchBarrel == null)
            return;

        if (!HasOperationalCrew())
            return;

        if (targetingController == null)
        {
            targetingController = FindFirstObjectByType<TargetingController>();
            if (targetingController == null)
            {
                _hasBallisticInterceptSolution = false;
                return;
            }
        }

        Health target = targetingController.CurrentTarget;
        if (target == null)
        {
            _hasBallisticInterceptSolution = false;
            return;
        }

        ComputeBallisticSolution(target.transform);

        if (!_hasAimSolution)
            return;

        // DO NOT move the cannon here - calculations only
        // Cannon will snap to firing position in TryFire() immediately before firing
        // Store the solution but don't apply rotations
        
        if (currentLauncher != null)
        {
            currentLauncher.SetRuntimeLaunchSpeed(_aimLaunchSpeed);
        }
    }

    void RefreshBallisticReadiness(bool autoTargetingActive)
    {
        if (targetingController == null)
        {
            targetingController = FindFirstObjectByType<TargetingController>();
        }

        if (targetingController == null)
        {
            _hasBallisticInterceptSolution = false;
            return;
        }

        Health target = targetingController.CurrentTarget;
        if (target == null)
        {
            _hasBallisticInterceptSolution = false;
            return;
        }

        Transform muzzle = (currentLauncher != null && currentLauncher.spawnPoint != null) ? currentLauncher.spawnPoint : pitchBarrel;
        if (currentLauncher == null || muzzle == null)
        {
            _hasBallisticInterceptSolution = false;
            return;
        }

        bool solved = TrySolveBallisticArc(target.transform, out float yaw, out float pitch, out float launchSpeed);

        if (!autoTargetingActive)
        {
            _aimYawTarget = yaw;
            _aimPitchTarget = pitch;
            _aimLaunchSpeed = launchSpeed;
            _hasAimSolution = true;
        }

        _hasBallisticInterceptSolution = solved;
    }

    void ComputeBallisticSolution(Transform targetTransform)
    {
        _hasAimSolution = false;

        if (targetTransform == null)
            return;

        Transform muzzle = (currentLauncher != null && currentLauncher.spawnPoint != null) ? currentLauncher.spawnPoint : pitchBarrel;
        if (muzzle == null)
            return;

        if (currentLauncher == null)
        {
            Vector3 origin = muzzle.position;
            Vector3 aimPoint = GetTargetAimPoint(targetTransform);
            Vector3 displacement = aimPoint - origin;
            if (displacement.sqrMagnitude < 0.0001f)
                return;

            Transform reference = yawBase != null ? (yawBase.parent != null ? yawBase.parent : yawBase) : transform;
            Vector3 localDir = reference.InverseTransformDirection(displacement.normalized);
            float desiredYawLOS = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            float halfYaw = Mathf.Max(0f, yawLimitDeg * 0.5f);
            _aimYawTarget = Mathf.Clamp(desiredYawLOS, -halfYaw, halfYaw);
            return;
        }

        bool solved = TrySolveBallisticArc(targetTransform, out float yaw, out float pitch, out float launchSpeed);

        _aimYawTarget = yaw;
        _aimPitchTarget = pitch;
        _aimLaunchSpeed = launchSpeed;
        _hasAimSolution = true;
        _hasBallisticInterceptSolution = solved;
    }

    void ReportAcquisitionDiagnostics()
    {
        if (!enableDebugLogging)
        {
            _hasLoggedAcquisitionState = false;
            return;
        }

        bool horizontalLock = HasHorizontalLock;
        bool ballisticLock = _hasBallisticInterceptSolution;
        bool hasTarget = HasSelectedTarget;

        if (_hasLoggedAcquisitionState &&
            horizontalLock == _lastLoggedHorizontalLock &&
            ballisticLock == _lastLoggedBallisticLock)
        {
            return;
        }

        GetYawEdgeMetrics(out float halfYaw, out float buffer, out float effectiveLimit);
        
        // Enhanced diagnostics when target is not acquired
        string status;
        if (hasTarget && (!horizontalLock || !ballisticLock))
        {
            // Target selected but not fully acquired - log detailed reasons
            status = $"{mountId}: TARGET NOT ACQUIRED - horizontal={horizontalLock} ballistic={ballisticLock}";
            LogDebug(status);
            
            // Log specific failure reasons
            if (!horizontalLock)
            {
                float yawToTarget = _aimYawTarget;
                bool yawInLimits = Mathf.Abs(_yaw) <= effectiveLimit;
                bool yawAligned = Mathf.Abs(Mathf.DeltaAngle(_yaw, _aimYawTarget)) < 2f;
                
                LogDebug($"  → Horizontal Lock FAILED: currentYaw={_yaw:F1}° targetYaw={yawToTarget:F1}° withinLimits={yawInLimits} aligned={yawAligned} (limit=±{effectiveLimit:F1}°)");
            }
            
            if (!ballisticLock)
            {
                if (targetingController != null && targetingController.CurrentTarget != null)
                {
                    Transform target = targetingController.CurrentTarget.transform;
                    Transform muzzle = (currentLauncher != null && currentLauncher.spawnPoint != null) ? currentLauncher.spawnPoint : pitchBarrel;
                    
                    if (muzzle != null)
                    {
                        Vector3 displacement = target.position - muzzle.position;
                        float distance = displacement.magnitude;
                        float horizontalDist = new Vector3(displacement.x, 0, displacement.z).magnitude;
                        float verticalOffset = displacement.y;
                        
                        string speedInfo = currentLauncher != null ? $"speed={currentLauncher.launchSpeed:F1} min={currentLauncher.minimumLaunchSpeed:F1}" : "no launcher";
                        LogDebug($"  → Ballistic Solution FAILED: distance={distance:F1}m (H:{horizontalDist:F1}m V:{verticalOffset:F1}m) {speedInfo} pitch=±{pitchUpDeg:F1}/{pitchDownDeg:F1}°");
                    }
                }
                else
                {
                    LogDebug($"  → Ballistic Solution FAILED: No target available");
                }
            }
        }
        else
        {
            // Standard status logging
            status = $"{mountId}: acquisition horizontal={horizontalLock} ballistic={ballisticLock} yaw={_yaw:F1}deg limit={effectiveLimit:F1}deg (half={halfYaw:F1}deg buffer={buffer:F1}deg)";
            LogDebug(status);
        }

        _hasLoggedAcquisitionState = true;
        _lastLoggedHorizontalLock = horizontalLock;
        _lastLoggedBallisticLock = ballisticLock;
    }

    bool TrySolveBallisticArc(Transform targetTransform, out float yaw, out float pitch, out float launchSpeed)
    {
        yaw = _yaw;
        pitch = _pitch;
        launchSpeed = currentLauncher != null ? currentLauncher.launchSpeed : 0f;

        if (targetTransform == null || currentLauncher == null)
            return false;

        Transform muzzle = currentLauncher.spawnPoint != null ? currentLauncher.spawnPoint : pitchBarrel;
        if (muzzle == null)
            return false;

        Vector3 origin = muzzle.position;
        Vector3 aimPoint = GetTargetAimPoint(targetTransform);
        Vector3 displacement = aimPoint - origin;
        if (displacement.sqrMagnitude < 0.0001f)
            return false;

        Transform reference = yawBase != null ? (yawBase.parent != null ? yawBase.parent : yawBase) : transform;

        // ========================================================================
        // CHECK 1: Is the target's POSITION within the yaw arc of the cannon?
        // This check happens FIRST, before any ballistic calculations.
        // ========================================================================
        Vector3 toTargetForAiming = aimPoint - origin;
        Vector3 localDir = reference.InverseTransformDirection(toTargetForAiming.normalized);
        float targetPositionYaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        float halfYaw = Mathf.Max(0f, yawLimitDeg * 0.5f);

        if (Mathf.Abs(targetPositionYaw) > halfYaw)
        {
            // Target is outside the yaw arc - no valid firing solution possible
            // Set aim values to turn towards target, but return false
            yaw = Mathf.Clamp(targetPositionYaw, -halfYaw, halfYaw);
            Vector3 clampedLocalDir = Quaternion.Euler(0f, -yaw, 0f) * localDir;
            float clampedPitch = -Mathf.Atan2(clampedLocalDir.y, clampedLocalDir.z) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(clampedPitch, -Mathf.Abs(pitchDownDeg), Mathf.Abs(pitchUpDeg));
            launchSpeed = currentLauncher.launchSpeed;
            
            if (enableDebugLogging)
            {
                LogDebug($"CHECK 1 FAILED: Target outside yaw arc. TargetYaw={targetPositionYaw:F1}° Limit=±{halfYaw:F1}°");
            }
            
            return false;
        }

        if (enableDebugLogging)
        {
            LogDebug($"CHECK 1 PASSED: Target within yaw arc. TargetYaw={targetPositionYaw:F1}° Limit=±{halfYaw:F1}°");
        }

        // ========================================================================
        // CHECK 2: Calculate LPLS (Launch Pitch & Launch Speed) to intercept target
        // Strategy: Start at maximum pitch, adjust speed to hit target
        // Only reduce pitch if overshooting at minimum speed
        // ========================================================================
        
        float gravityMag = Physics.gravity.magnitude;
        if (gravityMag < 0.0001f)
            gravityMag = 9.81f;

        // Calculate yaw to point horizontally at target
        Vector3 planarDisplacement = displacement;
        planarDisplacement.y = 0f;
        float horizontalDistance = planarDisplacement.magnitude;
        
        if (horizontalDistance < 0.1f)
        {
            if (enableDebugLogging)
            {
                LogDebug($"CHECK 2 FAILED: Target too close horizontally (distance={horizontalDistance:F2}m)");
            }
            yaw = targetPositionYaw;
            pitch = 0f;
            launchSpeed = currentLauncher.launchSpeed;
            return false;
        }

        float verticalOffset = aimPoint.y - origin.y;
        
        // Calculate yaw in local space
        Vector3 horizontalDir = planarDisplacement.normalized;
        Vector3 localHorizontalDir = reference.InverseTransformDirection(horizontalDir);
        float solutionYaw = Mathf.Atan2(localHorizontalDir.x, localHorizontalDir.z) * Mathf.Rad2Deg;
        
        // Verify yaw is within limits (should be, since CHECK 1 passed)
        if (Mathf.Abs(solutionYaw) > halfYaw)
        {
            if (enableDebugLogging)
            {
                LogDebug($"CHECK 2 FAILED: Solution yaw {solutionYaw:F1}° outside limits ±{halfYaw:F1}°");
            }
            yaw = Mathf.Clamp(solutionYaw, -halfYaw, halfYaw);
            pitch = 0f;
            launchSpeed = currentLauncher.launchSpeed;
            return false;
        }

        float maxSpeed = currentLauncher.launchSpeed;
        float minSpeed = currentLauncher.minimumLaunchSpeed;
        float maxPitchUpDeg = Mathf.Abs(pitchUpDeg);
        float maxPitchDownDeg = Mathf.Abs(pitchDownDeg);
        
        // ========================================================================
        // LPLS CALCULATION: Test pitch angles in 8 equal segments from max up to max down
        // Select the solution with the LOWEST launch speed that hits the target
        // NOTE: In this coordinate system, NEGATIVE pitch values aim UP, POSITIVE pitch values aim DOWN
        // Example: If cannon can pitch ±15°, tests at: -15°, -11.25°, -7.5°, -3.75°, 0°, +3.75°, +7.5°, +11.25°, +15°
        // ========================================================================
        
        const int numSegments = 8;
        float pitchRangeTotal = maxPitchUpDeg + maxPitchDownDeg; // Total range from max up to max down
        float pitchStep = pitchRangeTotal / numSegments;
        
        bool foundValidSolution = false;
        float bestPitchDeg = -maxPitchUpDeg; // Start at max pitch (negative = up)
        float bestSpeed = float.MaxValue;
        
        // Test pitch angles from maximum upward (-maxPitchUpDeg) to maximum downward (+maxPitchDownDeg)
        for (int i = 0; i <= numSegments; i++)
        {
            float testPitchDeg = -maxPitchUpDeg + (i * pitchStep); // Start negative (up), go to positive (down)
            
            // Convert to physics angle (Unity: negative=up, positive=down; Physics: positive=up, negative=down)
            float physicsAngleRad = -testPitchDeg * Mathf.Deg2Rad;
            float sinTheta = Mathf.Sin(physicsAngleRad);
            float cosTheta = Mathf.Cos(physicsAngleRad);
            
            // Ballistic trajectory formula:
            // y = x*tan(θ) - (g*x²)/(2*v²*cos²(θ))
            // Where: y = verticalOffset, x = horizontalDistance, θ = launch angle, v = launch speed, g = gravity
            // Solving for v²: v² = (g*x²)/(2*cos²(θ)*(x*tan(θ) - y))
            //
            // But this formula fails when shooting downward at targets below. Use alternative:
            // From kinematic equations: v² = (g*x²)/(2*(x*sinθ - y*cosθ)*cosθ)
            // This works for all angles (up, down, level)
            
            float denominator = 2f * (horizontalDistance * sinTheta - verticalOffset * cosTheta) * cosTheta;
            
            // Skip if denominator is too small or negative (no solution at this angle)
            if (denominator <= 0.001f)
                continue;
            
            float requiredSpeed2 = (gravityMag * horizontalDistance * horizontalDistance) / denominator;
            
            if (requiredSpeed2 <= 0f)
                continue;
            
            float requiredSpeed = Mathf.Sqrt(requiredSpeed2);
            
            // Check if this speed is within our launcher's capabilities
            if (requiredSpeed >= minSpeed && requiredSpeed <= maxSpeed)
            {
                // Valid solution - check if it's better (lower speed) than previous best
                if (requiredSpeed < bestSpeed)
                {
                    foundValidSolution = true;
                    bestSpeed = requiredSpeed;
                    bestPitchDeg = testPitchDeg;
                }
            }
        }
        
        if (!foundValidSolution)
        {
            // No valid firing solution found at any pitch angle
            if (enableDebugLogging)
            {
                LogDebug($"CHECK 2 FAILED: No valid pitch angle found. Speed range=[{minSpeed:F1}, {maxSpeed:F1}] Pitch range=[{-maxPitchUpDeg:F1}, {maxPitchDownDeg:F1}]° H={horizontalDistance:F1}m V={verticalOffset:F1}m gravity={gravityMag:F2}");
                // Log why each pitch angle failed
                for (int i = 0; i <= numSegments; i++)
                {
                    float testPitchDeg = -maxPitchUpDeg + (i * pitchStep);
                    float physicsAngleRad = -testPitchDeg * Mathf.Deg2Rad;
                    float sinTheta = Mathf.Sin(physicsAngleRad);
                    float cosTheta = Mathf.Cos(physicsAngleRad);
                    float denominator = 2f * (horizontalDistance * sinTheta - verticalOffset * cosTheta) * cosTheta;
                    
                    if (denominator <= 0.001f)
                    {
                        LogDebug($"  Pitch {testPitchDeg:F1}°: denominator={denominator:F4} (TOO SMALL - no ballistic solution)");
                        continue;
                    }
                    
                    float requiredSpeed2 = (gravityMag * horizontalDistance * horizontalDistance) / denominator;
                    
                    if (requiredSpeed2 <= 0f)
                    {
                        LogDebug($"  Pitch {testPitchDeg:F1}°: requiredSpeed2={requiredSpeed2:F2} (NEGATIVE)");
                        continue;
                    }
                    
                    float requiredSpeed = Mathf.Sqrt(requiredSpeed2);
                    LogDebug($"  Pitch {testPitchDeg:F1}°: needs {requiredSpeed:F1} m/s (range: {minSpeed:F1}-{maxSpeed:F1})");
                }
            }
            yaw = solutionYaw;
            pitch = -maxPitchUpDeg; // Negative = pitch up (default to max)
            launchSpeed = maxSpeed;
            return false;
        }
        
        // Success! We found the optimal firing solution
        yaw = solutionYaw;
        pitch = bestPitchDeg; // Already negative (aiming up)
        launchSpeed = bestSpeed;
        
        if (enableDebugLogging)
        {
            LogDebug($"CHECK 2 PASSED: Optimal LPLS solution. Yaw={yaw:F1}° Pitch={pitch:F1}° Speed={launchSpeed:F1} | Target: H={horizontalDistance:F1}m V={verticalOffset:F1}m");
        }
        
        return true;

        // All checks failed - no valid firing solution
        // Set aim values to point towards target for visual feedback
        yaw = Mathf.Clamp(targetPositionYaw, -halfYaw, halfYaw);
        Vector3 fallbackLocalDir = Quaternion.Euler(0f, -yaw, 0f) * localDir;
        pitch = -Mathf.Atan2(fallbackLocalDir.y, fallbackLocalDir.z) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, -Mathf.Abs(pitchDownDeg), Mathf.Abs(pitchUpDeg));
        launchSpeed = currentLauncher.launchSpeed;
        
        if (enableDebugLogging)
        {
            LogDebug($"ALL CHECKS FAILED: No valid firing solution. Target unreachable.");
        }
        
        return false;
    }

    Vector3 GetTargetAimPoint(Transform targetTransform)
    {
        Collider col = targetTransform.GetComponentInChildren<Collider>();
        if (col != null)
            return col.bounds.center;

        Renderer rend = targetTransform.GetComponentInChildren<Renderer>();
        if (rend != null)
            return rend.bounds.center;

        return targetTransform.position;
    }

    /// <summary>
    /// Applies lead targeting adjustment by calculating intercept point based on velocities.
    /// Adjusts yaw and pitch just before firing to account for target and ship movement.
    /// This is separate from normal target acquisition and happens only at the moment of firing.
    /// </summary>
    void ApplyLeadTargetingAdjustment(Transform targetTransform)
    {
        if (targetTransform == null || currentLauncher == null)
        {
            // Fallback to standard ballistic pitch
            if (_hasAimSolution)
            {
                _pitch = _aimPitchTarget;
                ApplyRotations();
            }
            return;
        }

        Transform muzzle = currentLauncher.spawnPoint != null ? currentLauncher.spawnPoint : pitchBarrel;
        if (muzzle == null)
        {
            if (_hasAimSolution)
            {
                _pitch = _aimPitchTarget;
                ApplyRotations();
            }
            return;
        }

        // Get velocities
        Vector3 targetVelocity = Vector3.zero;
        Vector3 shipVelocity = Vector3.zero;

        Rigidbody targetRb = targetTransform.GetComponent<Rigidbody>();
        if (targetRb != null)
        {
            targetVelocity = targetRb.linearVelocity;
        }

        // Get ship's velocity (our firing platform)
        Transform shipRoot = transform.root;
        Rigidbody shipRb = shipRoot.GetComponent<Rigidbody>();
        if (shipRb != null)
        {
            shipVelocity = shipRb.linearVelocity;
        }

        // Calculate relative velocity
        Vector3 relativeVelocity = targetVelocity - shipVelocity;

        // If relative velocity is negligible, use standard ballistic solution
        if (relativeVelocity.sqrMagnitude < 0.01f)
        {
            if (_hasAimSolution)
            {
                _pitch = _aimPitchTarget;
                ApplyRotations();
            }
            return;
        }

        // Calculate intercept point using iterative method
        Vector3 firingPos = muzzle.position;
        Vector3 targetPos = GetTargetAimPoint(targetTransform);
        Vector3 interceptPoint = targetPos;
        float timeToTarget = 0f;
        float projectileSpeed = _aimLaunchSpeed > 0 ? _aimLaunchSpeed : currentLauncher.launchSpeed;

        for (int iteration = 0; iteration < maxLeadIterations; iteration++)
        {
            Vector3 toIntercept = interceptPoint - firingPos;
            float distance = toIntercept.magnitude;

            if (distance < 0.001f)
                break;

            // Estimate flight time accounting for ballistic arc
            float horizontalDistance = new Vector3(toIntercept.x, 0f, toIntercept.z).magnitude;
            float verticalDistance = toIntercept.y;
            
            float estimatedAngle = Mathf.Atan2(verticalDistance, horizontalDistance);
            float horizontalSpeed = projectileSpeed * Mathf.Cos(estimatedAngle);
            
            if (horizontalSpeed < 0.1f)
                break;

            timeToTarget = horizontalDistance / horizontalSpeed;

            // Predict where target will be
            Vector3 predictedTargetPos = targetPos + (relativeVelocity * timeToTarget);

            // Check convergence
            if (Vector3.Distance(interceptPoint, predictedTargetPos) < 0.1f)
            {
                interceptPoint = predictedTargetPos;
                break;
            }

            interceptPoint = predictedTargetPos;
        }

        // Calculate firing solution to intercept point
        bool solved = TrySolveBallisticArc(
            CreateTransformProxy(interceptPoint),
            out float leadYaw,
            out float leadPitch,
            out float leadSpeed
        );

        if (!solved)
        {
            // Lead solution failed, use standard ballistic solution
            if (_hasAimSolution)
            {
                _pitch = _aimPitchTarget;
                ApplyRotations();
            }
            
            if (enableDebugLogging)
            {
                LogDebug($"Lead targeting failed to find solution. Using standard ballistic pitch.");
            }
            return;
        }

        // Apply lead-adjusted angles
        float halfYaw = Mathf.Max(0f, yawLimitDeg * 0.5f);
        
        // Check if lead yaw exceeds limits
        if (Mathf.Abs(leadYaw) > halfYaw)
        {
            if (fireAtMaxYawIfLeadExceedsLimit)
            {
                // Clamp to max yaw and fire anyway
                _yaw = Mathf.Clamp(leadYaw, -halfYaw, halfYaw);
                _pitch = leadPitch;
                
                if (enableDebugLogging)
                {
                    float leadDistance = Vector3.Distance(targetPos, interceptPoint);
                    LogDebug($"Lead yaw {leadYaw:F1}° exceeds limit ±{halfYaw:F1}°. Clamping to {_yaw:F1}° and firing. Lead={leadDistance:F1}m Time={timeToTarget:F2}s");
                }
            }
            else
            {
                // Use standard solution
                if (_hasAimSolution)
                {
                    _pitch = _aimPitchTarget;
                }
                
                if (enableDebugLogging)
                {
                    LogDebug($"Lead yaw {leadYaw:F1}° exceeds limit ±{halfYaw:F1}°. Using standard ballistic solution.");
                }
            }
        }
        else
        {
            // Lead solution is within limits - apply it
            _yaw = leadYaw;
            _pitch = leadPitch;
            
            if (enableDebugLogging)
            {
                float leadDistance = Vector3.Distance(targetPos, interceptPoint);
                LogDebug($"Lead targeting applied: Yaw={_yaw:F1}° Pitch={_pitch:F1}° Lead={leadDistance:F1}m Time={timeToTarget:F2}s RelVel={relativeVelocity.magnitude:F1}m/s");
            }
        }

        // Update launch speed if needed
        if (currentLauncher != null)
        {
            currentLauncher.SetRuntimeLaunchSpeed(leadSpeed);
        }

        ApplyRotations();
    }

    /// <summary>
    /// Creates a temporary transform proxy for a position (used for lead targeting calculations).
    /// </summary>
    Transform CreateTransformProxy(Vector3 position)
    {
        GameObject proxy = new GameObject("_TempLeadTargetProxy");
        proxy.transform.position = position;
        proxy.hideFlags = HideFlags.HideAndDontSave;
        
        // Destroy after a short delay to allow the ballistic calculation to complete
        Destroy(proxy, 0.1f);
        
        return proxy.transform;
    }

    void EnsureCrewStation()
    {
        if (crewStation == null)
        {
            crewStation = GetComponent<CrewStation>();
        }

        if (crewStation == null && autoCreateCrewStation)
        {
            crewStation = gameObject.AddComponent<CrewStation>();
            crewStation.displayName = string.IsNullOrEmpty(mountId) ? name + " Crew" : mountId + " Crew";
            // All other settings will be applied by the weapon's profile.
            ApplyCrewLimitsToStation(crewStation);
        }

        if (crewStation != null)
        {
            ApplyCrewLimitsToStation(crewStation);
            string expectedId = !string.IsNullOrEmpty(mountId)
                ? $"{mountId}_crew_slot"
                : $"{gameObject.name}_crew_slot";
            
            if (crewStation.stationId != expectedId)
            {
                string oldId = crewStation.stationId;
                crewStation.stationId = expectedId;
                
                // If station was already registered under old ID, re-register it
                if (CrewManager.HasInstance && !string.IsNullOrEmpty(oldId))
                {
                    CrewManager.Instance.RegisterStation(crewStation);
                }
            }
        }
    }

    void ApplyCrewLimitsToStation(CrewStation station)
    {
        if (station == null) return;

        var profile = mountedWeapon != null 
            ? mountedWeapon.GetComponentInChildren<CrewStationRequirementProfile>() 
            : null;

        int targetMin, targetMax;
        
        if (profile != null)
        {
            targetMin = profile.MinimumCrewRequired;
            targetMax = profile.MaximumCrewAllowed;
            
            if (enableDebugLogging)
            {
                Debug.Log($"[WeaponMount:{name}] Applied crew profile from '{mountedWeapon.name}'. Min: {targetMin}, Max: {targetMax}");
            }
        }
        else
        {
            // When no weapon is mounted, or it lacks a profile, we default to a safe, single-person, non-functional state.
            // This prevents errors during initialization before persistence loads the real weapon.
            targetMin = 1;
            targetMax = 1;
            station.enforceRequirements = true;
            if (isOccupied && enableDebugLogging) // Only warn if a weapon is supposed to be there but has no profile
            {
                Debug.LogWarning($"[WeaponMount:{name}] No CrewStationRequirementProfile found on mounted weapon '{mountedWeapon.name}'. Using safe defaults.");
            }
        }
        
        // Only rebuild anchors if the limits actually changed
        bool limitsChanged = station.MinimumCrewRequired != targetMin || station.MaximumCrewAllowed != targetMax;
        
        if (profile != null)
        {
            profile.ApplyTo(station);
        }
        else
        {
            station.SetCrewLimits(targetMin, targetMax);
        }
        
        if (limitsChanged)
        {
            RequestAnchorRebuild();
        }
    }



    void RequestAnchorRebuild()
    {
        if (!Application.isPlaying)
            return;

        var builders = GetComponents<CrewStationAnchorRuntimeBuilder>();
        if (builders == null || builders.Length == 0)
            return;

        for (int i = 0; i < builders.Length; i++)
        {
            var builder = builders[i];
            if (builder != null)
            {
                builder.RebuildAnchors();
            }
        }
    }

    bool HasOperationalCrew()
    {
        if (!CrewManager.HasInstance)
            return true;

        bool meetsReq = CrewManager.Instance.MeetsRequirement(crewStation);
        
        if (enableDebugLogging && crewStation != null)
        {
            LogDebug($"HasOperationalCrew check: Station={crewStation.stationId}, AssignedCrew={crewStation.AssignedCrewCount}, MinRequired={crewStation.MinimumCrewRequired}, MeetsReq={meetsReq}");
        }
        
        return meetsReq;
    }

    void SyncAimTargetsToCurrentPose()
    {
        _aimYawTarget = _yaw;
        _aimPitchTarget = _pitch;
        _aimLaunchSpeed = currentLauncher != null ? currentLauncher.launchSpeed : 0f;
        _hasAimSolution = false;
        _hasBallisticInterceptSolution = false;
    }

    static readonly System.Collections.Generic.Dictionary<string, int> s_MountNameUsage = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
    static readonly System.Collections.Generic.HashSet<string> s_AssignedMountIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    bool _mountIdFinalized;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetMountIdRegistry()
    {
        s_MountNameUsage.Clear();
        s_AssignedMountIds.Clear();
    }
#endif

    void EnsureMountId()
    {
        if (_mountIdFinalized && !string.IsNullOrEmpty(mountId))
            return;

        string baseName = ComputeBaseMountName();
        if (string.IsNullOrEmpty(baseName))
        {
            baseName = "WeaponMount";
        }

        string uniqueId = AllocateMountId(baseName);
        mountId = uniqueId;
        _mountIdFinalized = true;
    }

    string ComputeBaseMountName()
    {
        string raw = transform != null ? transform.name : string.Empty;
        if (string.IsNullOrEmpty(raw))
            return raw;

        const string actualSuffix = "_actual";
        if (raw.EndsWith(actualSuffix, System.StringComparison.OrdinalIgnoreCase))
        {
            raw = raw.Substring(0, raw.Length - actualSuffix.Length);
        }

        return raw;
    }

    string AllocateMountId(string baseName)
    {
        if (string.IsNullOrEmpty(baseName))
            baseName = "WeaponMount";

        int nextIndex = 1;
        if (s_MountNameUsage.TryGetValue(baseName, out int count))
        {
            nextIndex = count + 1;
        }
        s_MountNameUsage[baseName] = nextIndex;

        string candidate = FormatMountId(baseName, nextIndex);
        while (s_AssignedMountIds.Contains(candidate))
        {
            nextIndex++;
            s_MountNameUsage[baseName] = nextIndex;
            candidate = FormatMountId(baseName, nextIndex);
        }

        s_AssignedMountIds.Add(candidate);
        return candidate;
    }

    static string FormatMountId(string baseName, int index)
    {
        return $"{baseName}_{index:00}";
    }

    // Mount a new weapon (ProjectileLauncher prefab recommended)
    public bool MountWeapon(GameObject weaponPrefab)
    {
        if (isOccupied || weaponPrefab == null || pitchBarrel == null)
            return false;

        mountedWeapon = Instantiate(weaponPrefab, pitchBarrel);
        mountedWeapon.transform.localPosition = Vector3.zero;
        mountedWeapon.transform.localScale = Vector3.one;

        currentLauncher = mountedWeapon.GetComponent<ProjectileLauncher>();
        if (currentLauncher != null)
        {
            currentLauncher.BindOwningMount(this);
            currentLauncher.SetCrewAccuracyScale(1f);
            currentLauncher.SetCrewReloadScale(1f);
        }
        if (enableDebugLogging) LogDebug($"Mounting {weaponPrefab.name} -> created {mountedWeapon.name}, launcher={currentLauncher}");
        
        // Align launcher spawn axis (+Y) to mount forward (+Z)
        Transform axisT = (currentLauncher != null && currentLauncher.spawnPoint != null) ? currentLauncher.spawnPoint : mountedWeapon.transform;
        Vector3 fromWorld;
        switch (launcherAxis)
        {
            case LauncherAxis.Forward: fromWorld = axisT.forward; break;
            case LauncherAxis.Right:   fromWorld = axisT.right;   break;
            default:                   fromWorld = axisT.up;      break;
        }
        if (invertLauncherAxis) fromWorld = -fromWorld;
        Vector3 toWorld = pitchBarrel.forward;   // desired world direction (+Z is cannon forward)
        if (fromWorld.sqrMagnitude > 1e-6f && toWorld.sqrMagnitude > 1e-6f)
        {
            // Map the selected launcher axis to mount forward (handles 0..180 automatically)
            Quaternion delta = Quaternion.FromToRotation(fromWorld, toWorld);
            mountedWeapon.transform.rotation = delta * mountedWeapon.transform.rotation;
        }

        // Cache health if available (on launcher or any child)
        weaponHealth = mountedWeapon.GetComponentInChildren<Health>();
        isOccupied = true;
        WeaponPersistenceManager.Instance.RegisterMountedWeapon(this);
        if (enableDebugLogging) LogDebug($"Mount complete, isOccupied={isOccupied}, health={weaponHealth}");
        SyncAimTargetsToCurrentPose();
        _lastAccuracyScale = -1f;
        _lastReloadScale = -1f;
        UpdateCrewPerformanceBonuses(HasOperationalCrew());
        ApplyCrewLimitsToStation(crewStation);
        return true;
    }

    // Remove the current weapon
    public GameObject UnmountWeapon()
    {
        if (!isOccupied || mountedWeapon == null)
            return null;

        GameObject weapon = mountedWeapon;
        mountedWeapon = null;
        weaponHealth = null;
        if (currentLauncher != null)
        {
            currentLauncher.BindOwningMount(null);
            currentLauncher.SetCrewAccuracyScale(1f);
            currentLauncher.SetCrewReloadScale(1f);
        }
        currentLauncher = null;
        isOccupied = false;

        WeaponPersistenceManager.Instance.UnregisterMountedWeapon(this);
        weapon.transform.SetParent(null);
        _lastAccuracyScale = -1f;
        _lastReloadScale = -1f;
        ApplyCrewLimitsToStation(crewStation);
        return weapon;
    }

    // Get health (if present)
    public Health GetWeaponHealth() => weaponHealth;

    // Type gate for game logic
    public bool CanMountWeaponType(string type) => mountType.ToLower() == type.ToLower();

    // Yaw/Pitch controls (developer/game adjustable)
    public void SetYawPitch(float yawDeg, float pitchDeg)
    {
        float halfYaw = Mathf.Max(0f, yawLimitDeg * 0.5f);
        _yaw = Mathf.Clamp(yawDeg, -halfYaw, halfYaw);
        _pitch = Mathf.Clamp(pitchDeg, -Mathf.Abs(pitchDownDeg), Mathf.Abs(pitchUpDeg));
        ApplyRotations();
    }

    public void ApplyYawDelta(float deltaDeg) => SetYawPitch(_yaw + (invertYawDirection ? -deltaDeg : deltaDeg), _pitch);
    public void ApplyPitchDelta(float deltaDeg) => SetYawPitch(_yaw, _pitch + (invertPitchDirection ? -deltaDeg : deltaDeg));
    public (float yawDeg, float pitchDeg) GetYawPitch() => (_yaw, _pitch);

    void ApplyRotations()
    {
        if (yawBase != null) yawBase.localRotation = Quaternion.Euler(0f, _yaw, 0f);
        if (pitchBarrel != null) pitchBarrel.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    void UpdateCrewPerformanceBonuses(bool hasCrew)
    {
        if (currentLauncher == null)
            return;

        float staffingCoverage = 0f;
        float crewRatio = 0f;
        float bestSkill = 0f;

        if (crewStation != null)
        {
            staffingCoverage = crewStation.GetStaffingRatio();
            crewRatio = crewStation.GetCrewRatio();
            if (crewStation.HasAnyCrew)
            {
                bestSkill = crewStation.GetBestSkillLevel();
            }
        }
        else if (hasCrew)
        {
            staffingCoverage = 1f;
            crewRatio = 1f;
        }

        float baseAccuracy = bestSkill > 0f
            ? CrewSkillUtility.EvaluateAccuracyScale(bestSkill)
            : 1f;
        float desiredAccuracyScale = Mathf.Lerp(1f, baseAccuracy, staffingCoverage);
        ApplyAccuracyScale(desiredAccuracyScale);

        // Simple crew-based reload: 1 crew = 1.0x, 2+ crew = 0.5x (half reload time)
        int assignedCrewCount = crewStation != null ? crewStation.AssignedCrewCount : 0;
        float desiredReloadScale = (assignedCrewCount >= 2) ? 0.5f : 1.0f;
        
        if (enableDebugLogging && (_lastReloadScale < 0f || !Mathf.Approximately(desiredReloadScale, _lastReloadScale)))
        {
            LogDebug($"Reload scale update: {assignedCrewCount} crew -> scale {desiredReloadScale}x (1.0=normal, 0.5=double speed)");
        }
        
        ApplyReloadScale(desiredReloadScale);
    }

    void ApplyAccuracyScale(float desiredScale)
    {
        if (_lastAccuracyScale >= 0f && Mathf.Approximately(desiredScale, _lastAccuracyScale))
            return;

        currentLauncher.SetCrewAccuracyScale(desiredScale);
        _lastAccuracyScale = desiredScale;
    }

    void ApplyReloadScale(float desiredScale)
    {
        if (_lastReloadScale >= 0f && Mathf.Approximately(desiredScale, _lastReloadScale))
            return;

        currentLauncher.SetCrewReloadScale(desiredScale);
        _lastReloadScale = desiredScale;
    }

    void LogDebug(string message)
    {
        if (!enableDebugLogging)
            return;

        string formatted = $"[WeaponMount:{mountId}] {message}";
        Debug.Log(formatted, this);
        FileLogger.Log(formatted, "WeaponMount");
    }
}
