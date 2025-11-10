using UnityEngine;

// Cannon-specific projectile that reuses the generic Projectile behavior.
// Extend here if you need cannon-only effects (e.g., heavier damage, decals, sounds).
[AddComponentMenu("Teramyyd/Weapons/CannonBall")]
public class CannonBall : Projectile
{
    // Set typical defaults for a cannonball when first added
    void Reset()
    {
        if (damage <= 0) damage = 25;   // Heavier impact than base default
        if (lifeTime <= 0f) lifeTime = 5f;
    }
}

