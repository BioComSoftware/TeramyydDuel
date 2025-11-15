using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public int damage = 10;
    public float lifeTime = 5f;

    [Header("Optional Effects")]
    public GameObject hitEffectPrefab; // Optional visual effect prefab to spawn on impact

    void Start()
    {
        // Destroy automatically after a certain time, even if it doesn't hit anything
        if (lifeTime > 0f)
        {
            Destroy(gameObject, lifeTime);
        }
    }

        // We're using standard (non-trigger) colliders, so use OnCollisionEnter
    void OnCollisionEnter(Collision collision)
    {
        GameObject other = collision.gameObject;
        
        FileLogger.Log($"{gameObject.name} hit {other.name} at {collision.contacts[0].point}", "Projectile");

        // 1️⃣ Attempt to find the Health component on what we hit
        Health targetHealth = other.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage);
            FileLogger.Log($"{gameObject.name} dealt {damage} damage to {other.name}", "Projectile");
        }
        else
        {
            FileLogger.Log($"{gameObject.name} hit {other.name} but it has no Health component", "Projectile");
        }

        // 2️⃣ Optional: Spawn impact effect at collision point
        if (hitEffectPrefab != null)
        {
            ContactPoint contact = collision.contacts.Length > 0 ? collision.contacts[0] : default;
            Instantiate(hitEffectPrefab, contact.point != Vector3.zero ? contact.point : transform.position, Quaternion.identity);
        }

        // 3️⃣ Destroy the projectile after applying damage
        FileLogger.Log($"{gameObject.name} destroying self after impact", "Projectile");
        Destroy(gameObject);
    }
}

