using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float damage = 10f;
    public float lifeTime = 5f;

    [Header("Optional Effects")]
    public GameObject hitEffectPrefab; // Optional visual effect prefab to spawn on impact

    [Header("Debug")]
    [Tooltip("Enable detailed collision and damage logging to diagnose issues.")]
    public bool debugCollisions = false;

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
        // Use collision.collider.gameObject to get the actual collider object, not the Rigidbody parent
        GameObject other = collision.collider.gameObject;
        
        FileLogger.Log($"{gameObject.name} hit {other.name} at {collision.contacts[0].point}", "Projectile");

        if (debugCollisions)
        {
            Debug.Log($"[PROJECTILE DEBUG] {gameObject.name} collided with {other.name}");
            Debug.Log($"[PROJECTILE DEBUG] Impact point: {collision.contacts[0].point}");
            Debug.Log($"[PROJECTILE DEBUG] Hit GameObject path: {GetGameObjectPath(other)}");
            FileLogger.Log($"COLLISION DEBUG: {gameObject.name} hit {other.name} | Path: {GetGameObjectPath(other)}", "ProjectileDebug");
        }

        // 1️⃣ Attempt to find the Health component on what we hit (check both object and parent)
        Health targetHealth = other.GetComponent<Health>();
        
        // If not found on the hit object, check parent (for cases where mesh collider is on child)
        if (targetHealth == null && other.transform.parent != null)
        {
            targetHealth = other.transform.parent.GetComponent<Health>();
            if (debugCollisions && targetHealth != null)
            {
                Debug.Log($"[PROJECTILE DEBUG] Health component found on PARENT {other.transform.parent.name} instead of {other.name}");
            }
        }
        
        if (debugCollisions)
        {
            if (targetHealth != null)
            {
                string location = targetHealth.gameObject == other ? "on hit object" : "on parent";
                Debug.Log($"[PROJECTILE DEBUG] Health component FOUND {location} ({targetHealth.gameObject.name}) | Current: {targetHealth.currentHealth:F2}/{targetHealth.maxHealth:F2}");
            }
            else
            {
                Debug.LogWarning($"[PROJECTILE DEBUG] NO Health component found on {other.name} or its parent!");
            }
        }
        
        if (targetHealth != null)
        {
            float oldHealth = targetHealth.currentHealth;
            targetHealth.TakeDamage(damage);
            
            if (debugCollisions)
            {
                Debug.Log($"[PROJECTILE DEBUG] Damage applied: {damage:F2} | Health: {oldHealth:F2} -> {targetHealth.currentHealth:F2}");
                FileLogger.Log($"DAMAGE APPLIED to {other.name}: {damage:F2} damage | Health: {oldHealth:F2} -> {targetHealth.currentHealth:F2}/{targetHealth.maxHealth:F2}", "ProjectileDebug");
            }
            
            FileLogger.Log($"{gameObject.name} dealt {damage:F2} damage to {other.name}", "Projectile");
        }
        else
        {
            if (debugCollisions)
            {
                Debug.LogWarning($"[PROJECTILE DEBUG] NO HEALTH COMPONENT FOUND on {other.name}!");
            }
            FileLogger.Log($"{gameObject.name} hit {other.name} but it has no Health component", "Projectile");
        }

        // 2ï¸âƒ£ Optional: Spawn impact effect at collision point
        if (hitEffectPrefab != null)
        {
            ContactPoint contact = collision.contacts.Length > 0 ? collision.contacts[0] : default;
            Instantiate(hitEffectPrefab, contact.point != Vector3.zero ? contact.point : transform.position, Quaternion.identity);
        }

        // 3️⃣ Destroy the projectile after applying damage
        FileLogger.Log($"{gameObject.name} destroying self after impact", "Projectile");
        Destroy(gameObject);
    }

    string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }
}

