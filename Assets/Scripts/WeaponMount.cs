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

        // Snap to ballistic pitch immediately before firing
        if (_hasAimSolution)
        {
            _pitch = _aimPitchTarget;
            ApplyRotations();
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

        // Calculate line-of-sight pitch (pointing directly at target)
        Transform muzzle = (currentLauncher != null && currentLauncher.spawnPoint != null) ? currentLauncher.spawnPoint : pitchBarrel;
        if (muzzle != null)
        {
            Vector3 origin = muzzle.position;
            Vector3 aimPoint = GetTargetAimPoint(target.transform);
            Vector3 displacement = aimPoint - origin;
            
            Transform reference = yawBase != null ? (yawBase.parent != null ? yawBase.parent : yawBase) : transform;
            Vector3 localDir = reference.InverseTransformDirection(displacement.normalized);
            
            // Apply yaw rotation first
            Vector3 yawAlignedDir = Quaternion.Euler(0f, -_aimYawTarget, 0f) * localDir;
            float lineOfSightPitch = -Mathf.Atan2(yawAlignedDir.y, yawAlignedDir.z) * Mathf.Rad2Deg;
            float clampedLOSPitch = Mathf.Clamp(lineOfSightPitch, -Mathf.Abs(pitchDownDeg), Mathf.Abs(pitchUpDeg));
            
            // Smoothly track yaw and line-of-sight pitch
            float yawStep = autoAimYawSpeedDegPerSec * Time.deltaTime;
            float pitchStep = autoAimPitchSpeedDegPerSec * Time.deltaTime;
            _yaw = Mathf.MoveTowards(_yaw, _aimYawTarget, yawStep);
            _pitch = Mathf.MoveTowards(_pitch, clampedLOSPitch, pitchStep);
            ApplyRotations();
        }

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

        if (_hasLoggedAcquisitionState &&
            horizontalLock == _lastLoggedHorizontalLock &&
            ballisticLock == _lastLoggedBallisticLock)
        {
            return;
        }

        GetYawEdgeMetrics(out float halfYaw, out float buffer, out float effectiveLimit);
        string status = $"{mountId}: acquisition horizontal={horizontalLock} ballistic={ballisticLock} yaw={_yaw:F1}deg limit={effectiveLimit:F1}deg (half={halfYaw:F1}deg buffer={buffer:F1}deg)";
        LogDebug(status);

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
        float maxPitchDeg = Mathf.Abs(pitchUpDeg);
        float maxPitchRad = maxPitchDeg * Mathf.Deg2Rad;
        
        // ========================================================================
        // LPLS CALCULATION: Use maximum pitch, adjust speed to hit target
        // NOTE: In this coordinate system, NEGATIVE pitch values aim UP, POSITIVE pitch values aim DOWN
        // ========================================================================
        
        // Calculate required launch speed at maximum pitch to hit the target
        // Using projectile motion: h = R*tan(θ) - (g*R²)/(2*v²*cos²(θ))
        // Solving for v: v² = (g*R²)/(2*cos²(θ)*(R*tan(θ) - h))
        
        float cosTheta = Mathf.Cos(maxPitchRad);
        float sinTheta = Mathf.Sin(maxPitchRad);
        float tanTheta = Mathf.Tan(maxPitchRad);
        
        // Calculate the vertical term: how much higher the projectile would go at this angle
        // compared to the target's vertical offset
        float verticalTerm = horizontalDistance * tanTheta - verticalOffset;
        
        if (verticalTerm <= 0f)
        {
            // At max pitch, we still can't get high enough to clear the target's altitude
            if (enableDebugLogging)
            {
                LogDebug($"CHECK 2 FAILED: Max pitch {maxPitchDeg:F1}° cannot reach target altitude (H={horizontalDistance:F1}m V={verticalOffset:F1}m, vertical_term={verticalTerm:F3})");
            }
            yaw = solutionYaw;
            pitch = -maxPitchDeg; // Negative = pitch up
            launchSpeed = maxSpeed;
            return false;
        }
        
        float cos2 = cosTheta * cosTheta;
        float requiredSpeed2 = (gravityMag * horizontalDistance * horizontalDistance) / (2f * cos2 * verticalTerm);
        
        if (requiredSpeed2 <= 0f)
        {
            if (enableDebugLogging)
            {
                LogDebug($"CHECK 2 FAILED: Invalid speed calculation (v²={requiredSpeed2:F3})");
            }
            yaw = solutionYaw;
            pitch = -maxPitchDeg; // Negative = pitch up
            launchSpeed = maxSpeed;
            return false;
        }
        
        float requiredSpeed = Mathf.Sqrt(requiredSpeed2);
        
        // Check if required speed is within our launcher's capabilities
        if (requiredSpeed < minSpeed || requiredSpeed > maxSpeed)
        {
            if (enableDebugLogging)
            {
                LogDebug($"CHECK 2 FAILED: Required speed {requiredSpeed:F1} outside range [{minSpeed:F1}, {maxSpeed:F1}] (H={horizontalDistance:F1}m V={verticalOffset:F1}m Pitch={maxPitchDeg:F1}°)");
            }
            yaw = solutionYaw;
            pitch = -maxPitchDeg; // Negative = pitch up
            launchSpeed = Mathf.Clamp(requiredSpeed, minSpeed, maxSpeed);
            return false;
        }
        
        // Success! We have a valid firing solution
        yaw = solutionYaw;
        pitch = -maxPitchDeg; // Negative = pitch up
        launchSpeed = requiredSpeed;
        
        if (enableDebugLogging)
        {
            LogDebug($"CHECK 2 PASSED: LPLS solution. Yaw={yaw:F1}° Pitch={pitch:F1}° Speed={launchSpeed:F1} | Target: H={horizontalDistance:F1}m V={verticalOffset:F1}m | Vertical_term={verticalTerm:F3} | Formula: v²={(gravityMag * horizontalDistance * horizontalDistance):F1} / (2*{cos2:F3}*{verticalTerm:F3}) = {requiredSpeed2:F1}");
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

        return CrewManager.Instance.MeetsRequirement(crewStation);
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
