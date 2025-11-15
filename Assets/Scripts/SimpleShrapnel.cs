using UnityEngine;

/// <summary>
/// Simple shrapnel behavior - flies, damages on impact, then destroys itself.
/// Automatically added to shrapnel prefabs that don't have a Projectile component.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SimpleShrapnel : MonoBehaviour
{
    [HideInInspector]
    public int damage = 5;
    
    [HideInInspector]
    public float lifeTime = 1.5f;
    
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

        // Apply damage if target has Health component
        Health targetHealth = collision.gameObject.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage);
        }

        // Destroy immediately on impact
        Destroy(gameObject);
    }
}
