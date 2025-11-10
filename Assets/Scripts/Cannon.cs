using UnityEngine;

// Cannon-specific component that reuses the generic ProjectileLauncher behavior.
// Add this to cannon GameObjects and customize cannon-only settings here.
[AddComponentMenu("Teramyyd/Weapons/Cannon")]
public class Cannon : ProjectileLauncher
{
    // Set cannon-typical defaults when the component is first added or Reset is called
    void Reset()
    {
        if (launchSpeed < 0.01f) launchSpeed = 50f;
        if (spawnOffset < 0.01f) spawnOffset = 1f;
        if (fireKey == KeyCode.None) fireKey = KeyCode.F;
    }

    // Future: override or extend behavior specifically for cannons
    // e.g., recoil, cooldown variance, spread, sound hooks, etc.
}

