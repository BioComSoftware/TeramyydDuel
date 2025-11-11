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
    public float shrapnelLifeTime = 1.5f;       // Lifetime of shrapnel projectiles
    public int shrapnelDamage = 5;              // Damage per shrapnel hit
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
        if (shrapnelLifeTime <= 0f) shrapnelLifeTime = 1.5f;
        if (shrapnelDamage <= 0) shrapnelDamage = 5;
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
            Vector3 spawnOrigin = hitPoint + hitNormal * shrapnelSpawnOffset;
            for (int i = 0; i < shrapnelCount; i++)
            {
                // Random direction on unit sphere, biased toward outward normal
                Vector3 randDir = Random.onUnitSphere;
                Vector3 dir = (randDir * (1f - shrapnelNormalBias) + hitNormal * shrapnelNormalBias).normalized;

                Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
                GameObject piece = Instantiate(shrapnelPrefab, spawnOrigin, rot);

                // If the shrapnel has a Projectile component, configure its damage and lifetime
                var proj = piece.GetComponent<Projectile>();
                if (proj != null)
                {
                    proj.damage = shrapnelDamage;
                    if (shrapnelLifeTime > 0f) proj.lifeTime = shrapnelLifeTime;
                }

                // Apply initial velocity via Rigidbody if present
                var rb = piece.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = dir * shrapnelSpeed;
                }
            }
        }

        // 4) Destroy the cannonball after impact processing
        Destroy(gameObject);
    }
}
