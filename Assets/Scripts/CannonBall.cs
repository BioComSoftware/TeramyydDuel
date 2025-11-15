using UnityEngine;

// Cannon-specific projectile with explosion + shrapnel on impact.
[AddComponentMenu("Teramyyd/Weapons/CannonBall")]
public class CannonBall : Projectile
{
    [Header("Explosion Visuals")]
    public GameObject explosionEffectPrefab;    // Optional visual explosion
    public float explosionEffectLifetime = 2f;  // Auto-destroy delay for explosion effect

    [Header("Shrapnel Settings")]
    public GameObject shrapnelPrefab;           // Prefab with Rigidbody + Collider + (optional) Projectile
    public int shrapnelCount = 16;              // Number of shrapnel pieces
    public float shrapnelSpeed = 30f;           // Initial speed for shrapnel
    [Tooltip("Small offset along the surface normal to spawn shrapnel outside colliding surface")]
    public float shrapnelSpawnOffset = 0.05f;
    [Range(0f, 1f)] [Tooltip("0 = random directions, 1 = fully biased outward along impact normal")]
    public float shrapnelNormalBias = 0.5f;

    // Set typical defaults for a cannonball when first added
    void Reset()
    {
        if (damage <= 0) damage = 25;   // Heavier impact than base default
        if (lifeTime <= 0f) lifeTime = 5f;
        if (shrapnelCount <= 0) shrapnelCount = 16;
        if (shrapnelSpeed <= 0f) shrapnelSpeed = 30f;
        if (explosionEffectLifetime <= 0f) explosionEffectLifetime = 2f;
    }

    // Handle impact: apply damage, spawn explosion VFX, emit shrapnel (physics-driven), then destroy self.
    void OnCollisionEnter(Collision collision)
    {
        GameObject other = collision.gameObject;

        // 1) Apply direct-hit damage if the target has Health
        Health targetHealth = other.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage);
        }

        // Collision contact point and normal (fallback to transform if not available)
        ContactPoint contact = (collision.contacts != null && collision.contacts.Length > 0)
            ? collision.contacts[0]
            : default;
        Vector3 hitPoint = contact.point != Vector3.zero ? contact.point : transform.position;
        Vector3 hitNormal = contact.normal != Vector3.zero ? contact.normal : -transform.forward;

        // 2) Spawn explosion visual (if provided)
        if (explosionEffectPrefab != null)
        {
            var vfx = Instantiate(explosionEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
            if (explosionEffectLifetime > 0f)
            {
                Destroy(vfx, explosionEffectLifetime);
            }
        }
        else if (hitEffectPrefab != null)
        {
            // Fallback to base hit effect if no explicit explosion prefab is assigned
            Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
        }

        // 3) Emit shrapnel pieces (physics will handle occlusion by walls/geometry)
        if (shrapnelPrefab != null && shrapnelCount > 0)
        {
            FileLogger.Log($"Spawning {shrapnelCount} shrapnel pieces at {hitPoint}", "Shrapnel");
            
            Vector3 spawnOrigin = hitPoint + hitNormal * shrapnelSpawnOffset;
            
            // Track all spawned shrapnel to disable inter-shrapnel collisions
            GameObject[] shrapnelPieces = new GameObject[shrapnelCount];
            
            for (int i = 0; i < shrapnelCount; i++)
            {
                // Random direction on unit sphere, biased toward outward normal
                Vector3 randDir = Random.onUnitSphere;
                Vector3 dir = (randDir * (1f - shrapnelNormalBias) + hitNormal * shrapnelNormalBias).normalized;

                Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
                GameObject piece = Instantiate(shrapnelPrefab, spawnOrigin, rot);
                shrapnelPieces[i] = piece;
                
                // Check if shrapnel is visible
                var renderer = piece.GetComponent<Renderer>();
                if (renderer == null)
                {
                    FileLogger.Log($"WARNING: Shrapnel {i} has NO RENDERER - will be invisible!", "Shrapnel");
                }
                else if (!renderer.enabled)
                {
                    FileLogger.Log($"WARNING: Shrapnel {i} renderer is DISABLED", "Shrapnel");
                }
                else if (renderer.sharedMaterial == null)
                {
                    FileLogger.Log($"WARNING: Shrapnel {i} has NO MATERIAL - will be invisible!", "Shrapnel");
                }
                else
                {
                    FileLogger.Log($"Shrapnel {i} renderer OK: enabled={renderer.enabled}, material={renderer.sharedMaterial.name}", "Shrapnel");
                }
                
                FileLogger.Log($"Spawned shrapnel {i}: {piece.name} at {spawnOrigin}, direction {dir}, scale={piece.transform.localScale}", "Shrapnel");

                // Ensure the shrapnel has a Rigidbody (required for physics)
                var rb = piece.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = piece.AddComponent<Rigidbody>();
                    rb.mass = 0.1f; // Light shrapnel
                    rb.useGravity = true;
                    FileLogger.Log($"Added Rigidbody to shrapnel {i}", "Shrapnel");
                }
                
                // Apply initial velocity
                rb.linearVelocity = dir * shrapnelSpeed;
                
                // Add random angular velocity (spin)
                rb.angularVelocity = Random.insideUnitSphere * 10f; // Random spin on all axes
                
                FileLogger.Log($"Set shrapnel {i} velocity to {rb.linearVelocity} (speed={shrapnelSpeed})", "Shrapnel");

                // If the shrapnel has a Projectile component, use its damage and lifetime
                var proj = piece.GetComponent<Projectile>();
                if (proj != null)
                {
                    // Use the prefab's own settings - don't override
                    FileLogger.Log($"Shrapnel {i} using Projectile settings: damage={proj.damage}, lifetime={proj.lifeTime}", "Shrapnel");
                }
                else
                {
                    // No Projectile component - add SimpleShrapnel with default values
                    var shrapnel = piece.AddComponent<SimpleShrapnel>();
                    shrapnel.damage = 5;  // Default damage if no Projectile
                    shrapnel.lifeTime = 1.5f;  // Default lifetime if no Projectile
                    FileLogger.Log($"Added SimpleShrapnel to shrapnel {i}: damage={shrapnel.damage}, lifetime={shrapnel.lifeTime}", "Shrapnel");
                }
            }
            
            // Disable collisions between all shrapnel pieces
            for (int i = 0; i < shrapnelPieces.Length; i++)
            {
                for (int j = i + 1; j < shrapnelPieces.Length; j++)
                {
                    var col1 = shrapnelPieces[i].GetComponent<Collider>();
                    var col2 = shrapnelPieces[j].GetComponent<Collider>();
                    if (col1 != null && col2 != null)
                    {
                        Physics.IgnoreCollision(col1, col2);
                    }
                }
            }
            
            FileLogger.Log("Disabled inter-shrapnel collisions", "Shrapnel");
        }
        else
        {
            if (shrapnelPrefab == null)
                FileLogger.Log("Shrapnel prefab is NULL - no shrapnel spawned", "Shrapnel");
            if (shrapnelCount <= 0)
                FileLogger.Log($"Shrapnel count is {shrapnelCount} - no shrapnel spawned", "Shrapnel");
        }

        // 4) Destroy the cannonball after impact processing
        Destroy(gameObject);
    }
}
