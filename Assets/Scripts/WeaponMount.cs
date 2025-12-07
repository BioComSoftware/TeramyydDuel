using UnityEngine;

// General weapon mount with yaw/pitch pivots and runtime mounting for ProjectileLauncher-based weapons.
public class WeaponMount : MonoBehaviour
{
    [Header("Identity")]
    public string mountId = "Mount_01";
    public string mountType = "cannon";  // accepted type (informational gate for game logic)

    [Header("Pivots")]
    [Tooltip("Yaw pivot (left/right) rotates around local Y")] public Transform yawBase;
    [Tooltip("Pitch pivot (up/down) rotates around local X; weapon is parented here")] public Transform pitchBarrel;

    [Header("Limits (degrees)")]
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

    [Header("Debug Input (temporary)")]
    public bool debugKeypadControl = false;
    public float yawSpeedDegPerSec = 60f;
    public float pitchSpeedDegPerSec = 45f;

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

    [Header("Target Acquisition Sensor")]
    [Tooltip("Trigger-like collider placed in front of the cannon barrel. When it overlaps the targeted collider, the HUD can hide the TargetNotAcquired sprite.")]
    public Collider targetAcquisitionCollider;
    [Tooltip("Optional name hint used when auto-locating the acquisition collider under the mounted weapon. Leave blank to pick the first trigger collider found.")]
    public string targetAcquisitionColliderNameHint = string.Empty;
    [Tooltip("Automatically tries to bind the acquisition collider whenever the reference is missing (useful when the weapon prefab is spawned at runtime).")]
    public bool autoAssignTargetAcquisitionCollider = true;

    [Header("Testing (optional)")]
    [Tooltip("If set with autoPopulateOnStart, this weapon prefab is mounted at Start for quick testing")] public GameObject autoPopulatePrefab;
    public bool autoPopulateOnStart = false;

    [Header("Crew Requirements")]
    [Tooltip("Crew station that operates this mount. Auto-located on the same GameObject if left empty.")]
    public CrewStation crewStation;
    [Tooltip("Creates a transient CrewStation at runtime when none is configured so the mount can participate in the crew system before dedicated mount points exist.")]
    public bool autoCreateCrewStation = true;
    [Tooltip("Crew skill focus expected when a runtime station needs to be created automatically.")]
    public CrewSkill defaultCrewSkill = CrewSkill.Gunnery;
    [Range(1, 4)] public int defaultCrewRequired = 1;
    [Range(1, 4)] public int defaultCrewMax = 2;

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
    bool _targetColliderInsideSensor;
    bool _hasLoggedSensorState;
    bool _lastLoggedSensorState;
    Health _lastLoggedTarget;
    Collider _lastLoggedTargetCollider;
    Collider _lastLoggedSensorCollider;
    bool _loggedCrewWarning;
    float _lastAccuracyScale = -1f;

    public bool HasTargetInsideAcquisitionCollider => _targetColliderInsideSensor;
    public bool HasSelectedTarget => targetingController != null && targetingController.CurrentTarget != null;
    public bool HasValidFiringSolution => _hasBallisticInterceptSolution;
    public bool CanFireAtCurrentTarget => HasSelectedTarget && _targetColliderInsideSensor && _hasBallisticInterceptSolution;
    public Health MountedWeaponHealth => weaponHealth;

    /// <summary>
    /// Attempt to fire the currently mounted weapon. Returns true if a fire command was issued.
    /// </summary>
    public bool TryFire()
    {
        if (!HasOperationalCrew())
        {
            LogDebug("TryFire blocked - no crew assigned to this mount.");
            return false;
        }

        if (currentLauncher == null)
            return false;

        if (!currentLauncher.IsReady)
            return false;

        if (!CanFireAtCurrentTarget)
            return false;

        currentLauncher.TriggerFireCommand();
        
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
        if (autoPopulateOnStart && autoPopulatePrefab != null && !isOccupied)
        {
            MountWeapon(autoPopulatePrefab);
        }
        TryResolveTargetAcquisitionCollider();
        ApplyRotations();
        SyncAimTargetsToCurrentPose();
        
        if (enableDebugLogging)
        {
            string path = GetHierarchyPath(transform);
            LogDebug($"Start complete. isOccupied={isOccupied}, path='{path}', childCount={transform.childCount}");
            // List immediate children
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var launcher = child.GetComponent<ProjectileLauncher>();
                LogDebug($"  Child {i}: {child.name}, hasLauncher={launcher != null}");
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
 
    void Update()
    {
        EnsureMountId();
        EnsureCrewStation();

        bool hasCrew = HasOperationalCrew();
        UpdateCrewAccuracyBonus(hasCrew);

        if (!hasCrew)
        {
            _hasBallisticInterceptSolution = false;
            _targetColliderInsideSensor = false;
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

            if (debugKeypadControl)
            {
                HandleDebugKeypadInput();
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

        _wasAutoTargetingActive = autoTargetingActive;
        TryResolveTargetAcquisitionCollider();
        UpdateTargetAcquisitionState();
    }

    void AutoAimTowardsTarget()
    {
        if (!isOccupied || mountedWeapon == null || pitchBarrel == null)
            return;

        if (!HasOperationalCrew())
            return;

        if (targetingController == null)
        {
            targetingController = FindObjectOfType<TargetingController>();
            if (targetingController == null)
            {
                _hasBallisticInterceptSolution = false;
                return;
                    if (isOccupied)
                    {
                        WeaponPersistenceManager.Instance.RegisterMountedWeapon(this);
                    }
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

        float yawStep = autoAimYawSpeedDegPerSec * Time.deltaTime;
        float pitchStep = autoAimPitchSpeedDegPerSec * Time.deltaTime;
        _yaw = Mathf.MoveTowards(_yaw, _aimYawTarget, yawStep);
        _pitch = Mathf.MoveTowards(_pitch, _aimPitchTarget, pitchStep);
        ApplyRotations();

        if (currentLauncher != null)
        {
            currentLauncher.SetRuntimeLaunchSpeed(_aimLaunchSpeed);
        }
    }

    void UpdateTargetAcquisitionState()
    {
        if (targetingController == null)
        {
            targetingController = FindObjectOfType<TargetingController>();
        }

        if (targetAcquisitionCollider == null)
        {
            FinalizeAcquisitionState(false, null, null, "Target acquisition collider not assigned.");
            return;
        }

        if (targetingController == null)
        {
            FinalizeAcquisitionState(false, null, null, "TargetingController not found in scene.");
            return;
        }

        Collider targetCollider = targetingController.CurrentTargetCollider;
        Health target = targetingController.CurrentTarget;
        if (target == null)
        {
            FinalizeAcquisitionState(false, null, null, "No target selected");
            return;
        }

        if (targetCollider == null)
        {
            FinalizeAcquisitionState(false, target, null, "Target lacks collider reference from TargetingController");
            return;
        }

        if (!targetCollider.enabled)
        {
            FinalizeAcquisitionState(false, target, targetCollider, $"Target collider '{targetCollider.name}' disabled");
            return;
        }

        if (!targetAcquisitionCollider.enabled)
        {
            FinalizeAcquisitionState(false, target, targetCollider, $"Sensor collider '{targetAcquisitionCollider.name}' disabled");
            return;
        }

        bool inside = false;
        string reason;

        if (Physics.ComputePenetration(
                targetAcquisitionCollider,
                targetAcquisitionCollider.transform.position,
                targetAcquisitionCollider.transform.rotation,
                targetCollider,
                targetCollider.transform.position,
                targetCollider.transform.rotation,
                out Vector3 direction,
                out float penetrationDistance))
        {
            inside = true;
            reason = $"Sensor penetration confirmed (depth={penetrationDistance:F3}m)";
        }
        else
        {
            float separation = EstimateColliderSeparation(targetAcquisitionCollider, targetCollider);
            reason = separation < float.MaxValue
                ? $"No penetration (surface separation â‰ˆ {separation:F3}m)"
                : "Unable to compute precise separation";
        }
        FinalizeAcquisitionState(inside, target, targetCollider, reason);
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
            crewStation.primarySkill = defaultCrewSkill;
            crewStation.trainingSkill = CrewSkill.None;
            crewStation.minimumCrewRequired = Mathf.Clamp(defaultCrewRequired, 1, defaultCrewMax);
            crewStation.maximumCrewAllowed = Mathf.Max(defaultCrewRequired, defaultCrewMax);
            crewStation.enforceRequirements = true;
        }

        if (crewStation != null)
        {
            // Use GameObject name to ensure uniqueness (mounts often share the same mountId like "Mount_01")
            // This ensures each weapon mount gets a unique station ID
            string expectedId = $"{gameObject.name}_crew_slot";
            
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

    bool HasOperationalCrew()
    {
        if (!CrewManager.HasInstance)
            return true;

        return CrewManager.Instance.MeetsRequirement(crewStation);
    }
    void ComputeBallisticSolution(Transform targetTransform)
    {
        _hasAimSolution = false;
        _hasBallisticInterceptSolution = false;

        Transform muzzle = (currentLauncher != null && currentLauncher.spawnPoint != null) ? currentLauncher.spawnPoint : pitchBarrel;
        if (muzzle == null)
            return;

        Vector3 origin = muzzle.position;
        Vector3 aimPoint = GetTargetAimPoint(targetTransform);
        Vector3 displacement = aimPoint - origin;
        if (displacement.sqrMagnitude < 0.0001f)
            return;

        Transform reference = yawBase != null ? (yawBase.parent != null ? yawBase.parent : yawBase) : transform;

        Vector3 gravityVector = Physics.gravity.sqrMagnitude > 0.0001f ? Physics.gravity : Vector3.down * 9.81f;
        Vector3 worldUp = -gravityVector.normalized; // up is opposite gravity
        float verticalOffset = Vector3.Dot(displacement, worldUp);
        Vector3 planar = displacement - verticalOffset * worldUp;
        float horizontalDistance = planar.magnitude;
        Vector3 planarDir;
        if (horizontalDistance > 0.0005f)
        {
            planarDir = planar / horizontalDistance;
        }
        else
        {
            Vector3 projectedForward = Vector3.ProjectOnPlane(reference.forward, worldUp);
            if (projectedForward.sqrMagnitude < 1e-6f)
            {
                projectedForward = Vector3.ProjectOnPlane(reference.up, worldUp);
            }
            planarDir = projectedForward.sqrMagnitude > 1e-6f ? projectedForward.normalized : reference.forward.normalized;
        }

        Vector3 toTargetForAiming = aimPoint - origin;
        Vector3 localDir = reference.InverseTransformDirection(toTargetForAiming.normalized);

        float desiredYawLOS = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        float halfYaw = Mathf.Max(0f, yawLimitDeg * 0.5f);
        float clampedYawLOS = Mathf.Clamp(desiredYawLOS, -halfYaw, halfYaw);

        if (currentLauncher == null)
        {
            _aimYawTarget = clampedYawLOS;
            return;
        }

        float gravityMag = Physics.gravity.magnitude;
        if (gravityMag < 0.0001f)
            gravityMag = 9.81f;

        float thetaMaxRad = Mathf.Deg2Rad * Mathf.Clamp(Mathf.Abs(pitchUpDeg), 1f, 85f);
        float drag = currentLauncher.ProjectileDrag;
        float damping = currentLauncher.ProjectileLinearDamping;
        float maxSpeed = currentLauncher.launchSpeed;

        float launchSpeed;
        float launchAngleRad;
        bool solved = BallisticsSolver.SolveWithUnityDrag(
            horizontalDistance,
            verticalOffset,
            gravityMag,
            maxSpeed,
            thetaMaxRad,
            drag,
            damping,
            out launchSpeed,
            out launchAngleRad);

        if (solved)
        {
            Vector3 worldAimDir;
            if (horizontalDistance > 0.0005f)
            {
                float cos = Mathf.Cos(launchAngleRad);
                float sin = Mathf.Sin(launchAngleRad);
                worldAimDir = (planarDir * cos + worldUp * sin).normalized;
            }
            else
            {
                if (Mathf.Abs(verticalOffset) < 0.0005f)
                {
                    worldAimDir = planarDir;
                }
                else
                {
                    worldAimDir = verticalOffset >= 0f ? worldUp : -worldUp;
                }
            }

            Vector3 localAimDir = reference.InverseTransformDirection(worldAimDir);
            float desiredYaw = Mathf.Atan2(localAimDir.x, localAimDir.z) * Mathf.Rad2Deg;
            _aimYawTarget = Mathf.Clamp(desiredYaw, -halfYaw, halfYaw);

            Vector3 yawAlignedAimDir = Quaternion.Euler(0f, -_aimYawTarget, 0f) * localAimDir;
            float desiredPitch = -Mathf.Atan2(yawAlignedAimDir.y, yawAlignedAimDir.z) * Mathf.Rad2Deg;
            _aimPitchTarget = Mathf.Clamp(desiredPitch, -Mathf.Abs(pitchDownDeg), Mathf.Abs(pitchUpDeg));
            _aimLaunchSpeed = Mathf.Clamp(launchSpeed, currentLauncher.minimumLaunchSpeed, currentLauncher.launchSpeed);
            _hasAimSolution = true;
            _hasBallisticInterceptSolution = true;
            return;
        }

        _aimYawTarget = clampedYawLOS;
        Vector3 dirAfterYaw = Quaternion.Euler(0f, -_aimYawTarget, 0f) * localDir;
        float forwardAfterYaw = dirAfterYaw.z;
        float fallbackPitch = -Mathf.Atan2(dirAfterYaw.y, forwardAfterYaw) * Mathf.Rad2Deg;
        fallbackPitch = Mathf.Clamp(fallbackPitch, -Mathf.Abs(pitchDownDeg), Mathf.Abs(pitchUpDeg));
        _aimPitchTarget = fallbackPitch;
        _aimLaunchSpeed = currentLauncher.launchSpeed;
        _hasAimSolution = true;
        _hasBallisticInterceptSolution = false;
    }

    void SyncAimTargetsToCurrentPose()
    {
        _aimYawTarget = _yaw;
        _aimPitchTarget = _pitch;
        _aimLaunchSpeed = currentLauncher != null ? currentLauncher.launchSpeed : 0f;
        _hasAimSolution = false;
        _hasBallisticInterceptSolution = false;
    }

    void EnsureMountId()
    {
        if (!string.IsNullOrEmpty(mountId))
            return;

        mountId = transform.name;
    }

    void HandleDebugKeypadInput()
    {
        float dt = Time.deltaTime;
        if (Input.GetKey(KeyCode.J)) ApplyYawDelta((invertYawDirection ? 1f : -1f) * yawSpeedDegPerSec * dt);
        if (Input.GetKey(KeyCode.L)) ApplyYawDelta((invertYawDirection ? -1f : 1f) * yawSpeedDegPerSec * dt);
        if (Input.GetKey(KeyCode.I)) ApplyPitchDelta((invertPitchDirection ? -1f : 1f) * pitchSpeedDegPerSec * dt);
        if (Input.GetKey(KeyCode.K)) ApplyPitchDelta((invertPitchDirection ? 1f : -1f) * pitchSpeedDegPerSec * dt);
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
        }
        if (enableDebugLogging) LogDebug($"Mounting {weaponPrefab.name} -> created {mountedWeapon.name}, launcher={currentLauncher}");
        TryResolveTargetAcquisitionCollider();
        
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
        UpdateCrewAccuracyBonus(HasOperationalCrew());
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
        }
        currentLauncher = null;
        isOccupied = false;

        WeaponPersistenceManager.Instance.UnregisterMountedWeapon(this);
        weapon.transform.SetParent(null);
        _lastAccuracyScale = -1f;
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

    void UpdateCrewAccuracyBonus(bool hasCrew)
    {
        if (currentLauncher == null)
            return;

        float desiredScale = 1f;
        if (hasCrew && crewStation != null)
        {
            float skill = crewStation.GetBestSkillLevel();
            if (skill > 0f)
            {
                desiredScale = CrewSkillUtility.EvaluateAccuracyScale(skill);
            }
        }

        if (_lastAccuracyScale >= 0f && Mathf.Approximately(desiredScale, _lastAccuracyScale))
            return;

        currentLauncher.SetCrewAccuracyScale(desiredScale);
        _lastAccuracyScale = desiredScale;
    }

    void FinalizeAcquisitionState(bool inside, Health target, Collider targetCollider, string reason)
    {
        bool previous = _targetColliderInsideSensor;
        _targetColliderInsideSensor = inside;

        if (!enableDebugLogging)
            return;

        bool stateChanged = !_hasLoggedSensorState || previous != inside;
        bool targetChanged = _lastLoggedTarget != target;
        bool colliderChanged = _lastLoggedTargetCollider != targetCollider;
        bool sensorChanged = _lastLoggedSensorCollider != targetAcquisitionCollider;

        if (stateChanged || targetChanged || colliderChanged || sensorChanged)
        {
            string targetName = target != null ? target.name : "null";
            string colliderName = targetCollider != null ? targetCollider.name : "null";
            string sensorName = targetAcquisitionCollider != null ? targetAcquisitionCollider.name : "null";
            bool sensorIsTrigger = targetAcquisitionCollider != null && targetAcquisitionCollider.isTrigger;
            LogDebug($"Target acquisition update â†’ inside={inside}, target={targetName}, collider={colliderName}, sensor={sensorName}, sensorIsTrigger={sensorIsTrigger}, reason={reason}");
            _lastLoggedSensorState = inside;
            _lastLoggedTarget = target;
            _lastLoggedTargetCollider = targetCollider;
            _lastLoggedSensorCollider = targetAcquisitionCollider;
            _hasLoggedSensorState = true;
        }
    }

    void LogDebug(string message)
    {
        if (!enableDebugLogging)
            return;

        string formatted = $"[WeaponMount:{mountId}] {message}";
        Debug.Log(formatted, this);
        FileLogger.Log(formatted, "WeaponMount");
    }

    float EstimateColliderSeparation(Collider sensor, Collider target)
    {
        if (sensor == null || target == null)
            return float.MaxValue;

        Vector3 sensorPoint = sensor.ClosestPoint(target.bounds.center);
        Vector3 targetPoint = target.ClosestPoint(sensorPoint);
        return Vector3.Distance(sensorPoint, targetPoint);
    }

    void TryResolveTargetAcquisitionCollider()
    {
        if (!autoAssignTargetAcquisitionCollider)
            return;

        if (targetAcquisitionCollider != null)
        {
            bool colliderDestroyed = !targetAcquisitionCollider.gameObject.scene.IsValid();
            bool belongsToThisMount = ColliderBelongsToCurrentMount(targetAcquisitionCollider);
            if (!colliderDestroyed && belongsToThisMount)
            {
                return;
            }

            if (enableDebugLogging)
            {
                string reason = colliderDestroyed ? "was destroyed" : "is not parented under this mount";
                string colliderName = colliderDestroyed ? "(destroyed)" : targetAcquisitionCollider.name;
                LogDebug($"Discarding cached acquisition collider '{colliderName}' because it {reason}.");
            }

            targetAcquisitionCollider = null;
        }

        Collider candidate = FindAcquisitionColliderCandidate();
        if (candidate != null)
        {
            targetAcquisitionCollider = candidate;
            if (enableDebugLogging)
            {
                LogDebug($"Auto-assigned target acquisition collider '{candidate.name}'");
            }
        }
    }

    Collider FindAcquisitionColliderCandidate()
    {
        if (!isOccupied || pitchBarrel == null)
            return null;

        Collider[] colliders = pitchBarrel.GetComponentsInChildren<Collider>(true);
        if (colliders == null || colliders.Length == 0)
            return null;

        Collider candidate = null;

        if (!string.IsNullOrEmpty(targetAcquisitionColliderNameHint))
        {
            foreach (var c in colliders)
            {
                if (c != null && string.Equals(c.name, targetAcquisitionColliderNameHint, System.StringComparison.OrdinalIgnoreCase))
                {
                    candidate = c;
                    break;
                }
            }
        }

        if (candidate == null)
        {
            foreach (var c in colliders)
            {
                if (c != null && c.isTrigger)
                {
                    candidate = c;
                    break;
                }
            }
        }

        if (candidate == null)
        {
            foreach (var c in colliders)
            {
                if (c != null)
                {
                    candidate = c;
                    break;
                }
            }
        }

        return candidate;
    }

    bool ColliderBelongsToCurrentMount(Collider candidate)
    {
        if (candidate == null || pitchBarrel == null)
            return false;

        Transform t = candidate.transform;
        if (t == pitchBarrel)
            return true;

        return t.IsChildOf(pitchBarrel);
    }
}
