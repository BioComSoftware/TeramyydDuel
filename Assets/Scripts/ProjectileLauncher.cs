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
    public KeyCode fireKey = KeyCode.F;

    [Header("Projectile Settings")]
    public float launchSpeed = 50f;       // Speed of the projectile
    public float spawnOffset = 1f;        // Distance in front of the barrel

    [Header("Accuracy (runtime adjustable)")]
    [Tooltip("Max angular deviation from the muzzle axis in degrees (cone radius). Lower = more accurate.")]
    public float angleSpreadDegrees = 5f;
    [Tooltip("Random speed variance as a percentage of launchSpeed (e.g., 5 means +/-5%). Lower = more consistent speed.")]
    public float speedJitterPercent = 5f;
    void Update()
    {
        if (Input.GetKeyDown(fireKey))
        {
            FireProjectile();
        }
    }

    protected virtual void FireProjectile()
    {
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
        float spread = Mathf.Max(0f, angleSpreadDegrees);
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
        float jitter = Mathf.Max(0f, speedJitterPercent) * 0.01f;
        float speedMul = (jitter > 0f) ? Random.Range(1f - jitter, 1f + jitter) : 1f;

        // Spawn slightly in front of the barrel so we don't spawn inside its collider
        Vector3 spawnPos = spawnPoint.position + launchDirection * spawnOffset;

        // Rotate projectile so its forward points along the launch direction
        Quaternion spawnRot = Quaternion.LookRotation(launchDirection, Vector3.up);

        GameObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot);

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Prefer Rigidbody.linearVelocity (newer Unity); fall back to velocity via reflection to avoid obsolete warnings
            Vector3 initialVelocity = launchDirection * (launchSpeed * speedMul);
            // Prefer linearVelocity when available; otherwise set velocity directly
            var rbType = typeof(Rigidbody);
            var linVelProp = rbType.GetProperty("linearVelocity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (linVelProp != null && linVelProp.CanWrite)
            {
                linVelProp.SetValue(rb, initialVelocity, null);
            }
            else
            {
                rb.velocity = initialVelocity;
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

        Debug.Log($"Projectile fired! pos={spawnPos}, dir={launchDirection}, speed={(launchSpeed * speedMul):F1}, spread={angleSpreadDegrees:F1}");
    }
}
