using UnityEngine;
using UnityEngine.Serialization;

// Attach this to the Cannon parent GameObject (the empty one you rotate)
public class ProjectileLauncher : MonoBehaviour
{
    [Header("References")]
    public GameObject projectilePrefab;   // Cannonball prefab (must have Rigidbody + Collider)
    public Transform spawnPoint;          // Your Cylinder (its Y axis points out of the barrel)
    public ParticleSystem muzzleSmoke;    // Optional: smoke effect when firing
    [FormerlySerializedAs("Muxxleblast")] public ParticleSystem MuzzleBlast;    // Optional: muzzle blast effect (user-assignable)

    [Header("Input")]
    [Tooltip("Deprecated: Use KeyBindingConfig.fireAllWeapons instead. This field is kept for backward compatibility.")]
    public KeyCode fireKey = KeyCode.F;
    [Tooltip("When true, uses KeyBindingConfig.fireAllWeapons. When false, uses the fireKey field.")]
    public bool useConfigurableKey = true;

    [Header("Projectile Settings")]
    [Tooltip("Maximum launch speed. Runtime systems may lower the actual muzzle speed, but it will never exceed this value.")]
    public float launchSpeed = 50f;
    [Tooltip("Minimum practical launch speed when auto-adjusting.")]
    public float minimumLaunchSpeed = 5f;
    public float spawnOffset = 1f;        // Distance in front of the barrel

    [Header("Accuracy (runtime adjustable)")]
    [Tooltip("Max angular deviation from the muzzle axis in degrees (cone radius). Lower = more accurate.")]
    public float angleSpreadDegrees = 5f;
    [Tooltip("Random speed variance as a percentage of launchSpeed (e.g., 5 means +/-5%). Lower = more consistent speed.")]
    public float speedJitterPercent = 5f;
    [Tooltip("Disable all accuracy error so projectiles leave exactly along the barrel axis at the computed speed.")]
    public bool disableAccuracyError = false;
    
    [Header("Projectile Physics (optional overrides)")]
    [Tooltip("Override for Rigidbody.drag used in ballistic calculations. Leave negative to auto-read from the projectile prefab.")]
    public float projectileDragOverride = -1f;
    [Tooltip("Override for Rigidbody.linearDamping (Unity 6+) used in ballistic calculations. Leave negative to auto-read from the projectile prefab.")]
    public float projectileLinearDampingOverride = -1f;

    [Header("Reload Settings")]
    [Tooltip("Time in seconds before weapon can fire again after firing")]
    [Min(0.05f)] public float reloadTime = 2f;
    
    [Tooltip("Can this weapon fire immediately at start, or does it need to reload first?")]
    public bool startReady = true;
    
    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool debugLog = false;

    [Header("Mount Integration")]
    [Tooltip("When true, this launcher ignores fire input unless its owning WeaponMount has a valid target lock (sensor overlap + firing solution).")]
    public bool requireValidMountTargetLock = true;
    
    // Runtime state
    private float _nextFireTime = 0f;
    private float _runtimeLaunchSpeed;
    private float _cachedProjectileDrag;
    private float _cachedProjectileLinearDamping;
    private WeaponMount _owningMount;
    private float _crewAccuracyScale = 1f;
    private float _crewReloadScale = 1f;

    public event System.Action<ProjectileLauncher> ProjectileFired;

    /// <summary>
    /// True when the launcher has finished reloading and can fire again.
    /// </summary>
    public bool IsReady => IsReadyToFire();
    
    /// <summary>
    /// Check if this weapon is ready to fire (not reloading).
    /// </summary>
    public bool IsReadyToFire()
    {
        return Time.time >= _nextFireTime;
    }
    
    /// <summary>
    /// Get remaining reload time in seconds. Returns 0 if ready.
    /// </summary>
    public float GetRemainingReloadTime()
    {
        float remaining = _nextFireTime - Time.time;
        return remaining > 0f ? remaining : 0f;
    }
    
    void Awake()
    {
        CacheProjectilePhysicsSpecs();
    }

    void Start()
    {
        // If weapon doesn't start ready, set initial reload time
        if (!startReady)
        {
            _nextFireTime = Time.time + GetScaledReloadDuration();
        }

        _runtimeLaunchSpeed = Mathf.Clamp(launchSpeed, minimumLaunchSpeed, launchSpeed);
    }

    void OnValidate()
    {
        minimumLaunchSpeed = Mathf.Clamp(minimumLaunchSpeed, 0.1f, Mathf.Max(0.1f, launchSpeed));
        if (!Application.isPlaying)
        {
            _runtimeLaunchSpeed = Mathf.Clamp(launchSpeed, minimumLaunchSpeed, launchSpeed);
        }
        CacheProjectilePhysicsSpecs();
    }
    
    void Update()
    {
        KeyCode effectiveFireKey = fireKey;
        if (useConfigurableKey)
        {
            var kb = KeyBindingConfig.Instance;
            if (kb != null)
            {
                effectiveFireKey = kb.fireAllWeapons;
            }
        }

        if (Input.GetKeyDown(effectiveFireKey))
        {
            if (_owningMount != null)
            {
                _owningMount.TryFire();
                return;
            }

            if (!IsFireCommandAllowed(ignoreTargetLock: false))
            {
                if (debugLog)
                {
                    string reason = _owningMount == null
                        ? "blocked (no owning mount)"
                        : "blocked (target lock invalid)";
                    Debug.Log($"[ProjectileLauncher] Fire command {reason}.");
                }
                return;
            }

            FireProjectile();
        }
    }

    public void BindOwningMount(WeaponMount mount)
    {
        _owningMount = mount;
    }

    /// <summary>
    /// Scales spread/jitter errors by the provided factor (0 = perfect accuracy, 1 = default values).
    /// </summary>
    public void SetCrewAccuracyScale(float scale)
    {
        _crewAccuracyScale = Mathf.Clamp(scale, 0f, 1f);
    }

    /// <summary>
    /// Scales reload duration (values &lt; 1 speed up, values &gt; 1 slow down).
    /// </summary>
    public void SetCrewReloadScale(float scale)
    {
        float clamped = Mathf.Clamp(scale, 0.25f, 4f);
        if (Mathf.Approximately(clamped, _crewReloadScale))
            return;

        float previousDuration = GetScaledReloadDuration();
        float remaining = IsReadyToFire() ? 0f : Mathf.Clamp(_nextFireTime - Time.time, 0f, previousDuration);
        float progress = previousDuration > 0.0001f
            ? Mathf.Clamp01((previousDuration - remaining) / previousDuration)
            : 1f;

        _crewReloadScale = clamped;

        if (remaining > 0f)
        {
            float newDuration = GetScaledReloadDuration();
            float newRemaining = Mathf.Max(0f, newDuration * (1f - progress));
            _nextFireTime = Time.time + newRemaining;
        }
    }

    float GetScaledReloadDuration()
    {
        return Mathf.Max(0.05f, reloadTime * _crewReloadScale);
    }

    /// <summary>
    /// Allows external callers (e.g., HUD buttons) to issue a fire command while honoring target lock rules.
    /// </summary>
    public void TriggerFireCommand(bool ignoreTargetLock = false)
    {
        if (!IsFireCommandAllowed(ignoreTargetLock))
        {
            if (debugLog)
            {
                string reason = _owningMount == null ? "fire blocked (no mount)" : "fire blocked (target lock invalid)";
                Debug.Log($"[ProjectileLauncher] {reason}.");
            }
            return;
        }

        FireProjectile();
    }

    bool IsFireCommandAllowed(bool ignoreTargetLock = false)
    {
        if (ignoreTargetLock || !requireValidMountTargetLock)
            return true;

        if (_owningMount == null)
            return true;

        return _owningMount.CanFireAtCurrentTarget;
    }

    protected virtual void FireProjectile()
    {
        // Check if weapon is ready to fire
        if (!IsReadyToFire())
        {
            if (debugLog)
                Debug.Log($"[ProjectileLauncher] Weapon not ready. Reloading... ({GetRemainingReloadTime():F1}s remaining)");
            return;
        }
        
        if (projectilePrefab == null)
        {
            Debug.LogWarning("ProjectileLauncher: No projectile prefab assigned!");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("ProjectileLauncher: No spawnPoint assigned!");
            return;
        }

        // Play muzzle smoke effect
        if (muzzleSmoke != null)
        {
            muzzleSmoke.Play();
        }

        // Play muzzle blast effect (if assigned)
        if (MuzzleBlast != null)
        {
            MuzzleBlast.Play();
        }

        // Base direction: spawnPoint's local +Y (up) points out of the barrel
        Vector3 launchDirection = spawnPoint.up.normalized;

        // Apply angular spread (cone around the base direction)
        float spread = disableAccuracyError ? 0f : Mathf.Max(0f, angleSpreadDegrees * _crewAccuracyScale);
        
        if (debugLog)
        {
            Debug.Log($"[ProjectileLauncher] Fire accuracy: angleSpread={angleSpreadDegrees:F2}°, crewScale={_crewAccuracyScale:F3}, finalSpread={spread:F2}°, disableError={disableAccuracyError}");
        }
        
        if (spread > 0f)
        {
            // Build an orthonormal basis around the axis
            Vector3 axis = launchDirection;
            Vector3 ortho = Vector3.Cross(axis, Vector3.up);
            if (ortho.sqrMagnitude < 1e-6f) ortho = Vector3.Cross(axis, Vector3.right);
            ortho.Normalize();
            Vector3 ortho2 = Vector3.Cross(axis, ortho);

            float phi = Random.Range(0f, 360f);           // around-axis angle
            float tilt = Random.Range(0f, spread);        // degrees away from axis
            Vector3 rotAxis = (Mathf.Cos(phi * Mathf.Deg2Rad) * ortho + Mathf.Sin(phi * Mathf.Deg2Rad) * ortho2).normalized;
            launchDirection = (Quaternion.AngleAxis(tilt, rotAxis) * axis).normalized;
        }

        // Apply speed jitter (percent of launchSpeed)
        float jitter = disableAccuracyError ? 0f : Mathf.Max(0f, speedJitterPercent * _crewAccuracyScale) * 0.01f;
        float speedMul = (jitter > 0f) ? Random.Range(1f - jitter, 1f + jitter) : 1f;
        float baseSpeed = Mathf.Clamp(_runtimeLaunchSpeed, minimumLaunchSpeed, launchSpeed);
        float finalSpeed = Mathf.Clamp(baseSpeed * speedMul, minimumLaunchSpeed, launchSpeed);

        // Spawn slightly in front of the barrel so we don't spawn inside its collider
        Vector3 spawnPos = spawnPoint.position + launchDirection * spawnOffset;

        // Rotate projectile so its forward points along the launch direction
        Quaternion spawnRot = Quaternion.LookRotation(launchDirection, Vector3.up);

        GameObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot);

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Prefer Rigidbody.linearVelocity (newer Unity); fall back to velocity via reflection to avoid obsolete warnings
            Vector3 initialVelocity = launchDirection * finalSpeed;
            var rbType = typeof(Rigidbody);
            var linVelProp = rbType.GetProperty("linearVelocity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (linVelProp != null && linVelProp.CanWrite)
            {
                linVelProp.SetValue(rb, initialVelocity, null);
            }
            else
            {
                rb.linearVelocity = initialVelocity;
            }
        }
        else
        {
            Debug.LogWarning("ProjectileLauncher: Projectile prefab has no Rigidbody component!");
        }

        // Optional: ignore collision with the cannon itself, in case colliders still overlap
        Collider projCol = proj.GetComponent<Collider>();
        Collider cannonCol = spawnPoint.GetComponentInParent<Collider>();
        if (projCol != null && cannonCol != null)
        {
            Physics.IgnoreCollision(projCol, cannonCol);
        }

        // Set reload time
        _nextFireTime = Time.time + GetScaledReloadDuration();

        ProjectileFired?.Invoke(this);

        Debug.Log($"Projectile fired! pos={spawnPos}, dir={launchDirection}, speed={finalSpeed:F1}, spread={angleSpreadDegrees:F1}");
    }

    /// <summary>
    /// Adjusts the runtime muzzle speed while clamping to [minimumLaunchSpeed, launchSpeed].
    /// </summary>
    public void SetRuntimeLaunchSpeed(float desiredSpeed)
    {
        float min = Mathf.Max(0.1f, minimumLaunchSpeed);
        _runtimeLaunchSpeed = Mathf.Clamp(desiredSpeed, min, launchSpeed);
    }

    public float GetRuntimeLaunchSpeed() => _runtimeLaunchSpeed;

    public float ProjectileDrag => _cachedProjectileDrag;
    public float ProjectileLinearDamping => _cachedProjectileLinearDamping;

    void CacheProjectilePhysicsSpecs()
    {
        if (projectileDragOverride >= 0f)
        {
            _cachedProjectileDrag = projectileDragOverride;
        }
        if (projectileLinearDampingOverride >= 0f)
        {
            _cachedProjectileLinearDamping = projectileLinearDampingOverride;
        }

        if (projectilePrefab == null)
        {
            if (projectileDragOverride < 0f) _cachedProjectileDrag = 0f;
            if (projectileLinearDampingOverride < 0f) _cachedProjectileLinearDamping = 0f;
            return;
        }

        if (projectileDragOverride < 0f || projectileLinearDampingOverride < 0f)
        {
            if (projectilePrefab.TryGetComponent<Rigidbody>(out var rb))
            {
                if (projectileDragOverride < 0f)
                {
                    _cachedProjectileDrag = rb.linearDamping;
                }

                if (projectileLinearDampingOverride < 0f)
                {
                    _cachedProjectileLinearDamping = ReadLinearDamping(rb);
                }
            }
            else
            {
                if (projectileDragOverride < 0f) _cachedProjectileDrag = 0f;
                if (projectileLinearDampingOverride < 0f) _cachedProjectileLinearDamping = 0f;
            }
        }
    }

    float ReadLinearDamping(Rigidbody rb)
    {
        var property = typeof(Rigidbody).GetProperty("linearDamping", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (property != null && property.CanRead)
        {
            object value = property.GetValue(rb, null);
            if (value is float f)
                return f;
        }

        return rb.linearDamping; // fallback for older Unity versions
    }
    
    /// <summary>
    /// Display a random message indicating this weapon is unmanned.
    /// Called by WeaponMount when player attempts manual fire with no crew.
    /// Override in child classes to customize messages per weapon type.
    /// </summary>
    public virtual void ShowUnmannedWeaponMessage()
    {
        // Base implementation does nothing - override in child classes
    }
}
