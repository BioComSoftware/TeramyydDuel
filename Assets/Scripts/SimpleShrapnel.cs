using UnityEngine;

/// <summary>
/// Simple shrapnel behavior - flies, damages on impact, then destroys itself.
/// Automatically added to shrapnel prefabs that don't have a Projectile component.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SimpleShrapnel : MonoBehaviour
{
    [HideInInspector]
    public float damage = 5f;
    
    [HideInInspector]
    public float lifeTime = 1.5f;
    
    [HideInInspector]
    public bool debugCollisions = false;
    
    private bool hasHit = false;

    void Start()
    {
        // Auto-destroy after lifetime
        if (lifeTime > 0f)
        {
            Destroy(gameObject, lifeTime);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Only deal damage once
        if (hasHit)
            return;
        
        hasHit = true;
        // Use collision.collider.gameObject to get the actual collider object, not the Rigidbody parent
        GameObject other = collision.collider.gameObject;

        if (debugCollisions)
        {
            Debug.Log($"[SHRAPNEL DEBUG] {gameObject.name} collided with {other.name}");
            Debug.Log($"[SHRAPNEL DEBUG] Hit GameObject path: {GetGameObjectPath(other)}");
            FileLogger.Log($"SHRAPNEL COLLISION: {gameObject.name} hit {other.name} | Path: {GetGameObjectPath(other)}", "ShrapnelDebug");
        }

        // Apply damage if target has Health component (check both object and parent)
        Health targetHealth = other.GetComponent<Health>();
        
        // If not found on the hit object, check parent (for cases where mesh collider is on child)
        if (targetHealth == null && other.transform.parent != null)
        {
            targetHealth = other.transform.parent.GetComponent<Health>();
            if (debugCollisions && targetHealth != null)
            {
                Debug.Log($"[SHRAPNEL DEBUG] Health component found on PARENT {other.transform.parent.name} instead of {other.name}");
            }
        }
        
        if (debugCollisions)
        {
            if (targetHealth != null)
            {
                string location = targetHealth.gameObject == other ? "on hit object" : "on parent";
                Debug.Log($"[SHRAPNEL DEBUG] Health component FOUND {location} ({targetHealth.gameObject.name}) | Current: {targetHealth.currentHealth:F2}/{targetHealth.maxHealth:F2}");
            }
            else
            {
                Debug.LogWarning($"[SHRAPNEL DEBUG] NO Health component found on {other.name} or its parent!");
            }
        }
        
        if (targetHealth != null)
        {
            float oldHealth = targetHealth.currentHealth;
            targetHealth.TakeDamage(damage);
            
            if (debugCollisions)
            {
                Debug.Log($"[SHRAPNEL DEBUG] Damage applied: {damage:F2} | Health: {oldHealth:F2} -> {targetHealth.currentHealth:F2}");
                FileLogger.Log($"SHRAPNEL DAMAGE to {other.name}: {damage:F2} | Health: {oldHealth:F2} -> {targetHealth.currentHealth:F2}/{targetHealth.maxHealth:F2}", "ShrapnelDebug");
            }
        }
        else if (debugCollisions)
        {
            Debug.LogWarning($"[SHRAPNEL DEBUG] NO HEALTH COMPONENT FOUND on {other.name}!");
        }

        // Destroy immediately on impact
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
