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

        // Your cylinder's local Y points out of the barrel
        Vector3 launchDirection = spawnPoint.up.normalized;

        // Spawn slightly in front of the barrel so we don't spawn inside its collider
        Vector3 spawnPos = spawnPoint.position + launchDirection * spawnOffset;

        // Rotate projectile so its forward points along the launch direction
        Quaternion spawnRot = Quaternion.LookRotation(launchDirection, Vector3.up);

        GameObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot);

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Prefer Rigidbody.linearVelocity (newer Unity); fall back to velocity via reflection to avoid obsolete warnings
            var rbType = typeof(Rigidbody);
            var linVelProp = rbType.GetProperty("linearVelocity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (linVelProp != null && linVelProp.CanWrite)
            {
                linVelProp.SetValue(rb, launchDirection * launchSpeed, null);
            }
            else
            {
                var velProp = rbType.GetProperty("velocity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (velProp != null && velProp.CanWrite)
                {
                    velProp.SetValue(rb, launchDirection * launchSpeed, null);
                }
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

        Debug.Log($"Projectile fired! Position: {spawnPos}, Direction: {launchDirection}, Cannon Rotation: {transform.rotation.eulerAngles}");
    }
}
