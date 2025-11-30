using UnityEngine;

// Cannon-specific component that reuses the generic ProjectileLauncher behavior.
// Add this to cannon GameObjects and customize cannon-only settings here.
[RequireComponent(typeof(AudioSource))]
[AddComponentMenu("Teramyyd/Weapons/Cannon")]
public class Cannon : ProjectileLauncher
{
    [Header("Audio")]
    [SerializeField] private AudioSource fireAudioSource;

    void Awake()
    {
        CacheAudioSource();
    }

    // Set cannon-typical defaults when the component is first added or Reset is called
    void Reset()
    {
        if (launchSpeed < 0.01f) launchSpeed = 50f;
        if (spawnOffset < 0.01f) spawnOffset = 1f;
        if (fireKey == KeyCode.None) fireKey = KeyCode.F;
        CacheAudioSource();
    }

    // Future: override or extend behavior specifically for cannons
    // e.g., recoil, cooldown variance, spread, sound hooks, etc.

    protected override void FireProjectile()
    {
        // Play the configured AudioSource so users can control settings directly on the component
        if (fireAudioSource == null) CacheAudioSource();
        if (fireAudioSource != null && fireAudioSource.clip != null)
        {
            fireAudioSource.Stop(); // restart to allow retriggering even if still playing
            fireAudioSource.Play();
        }

        base.FireProjectile();
    }

    private void CacheAudioSource()
    {
        if (fireAudioSource == null)
        {
            fireAudioSource = GetComponent<AudioSource>();
        }
    }

}
