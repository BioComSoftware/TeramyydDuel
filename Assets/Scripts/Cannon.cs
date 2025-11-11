using UnityEngine;

// Cannon-specific component that reuses the generic ProjectileLauncher behavior.
// Add this to cannon GameObjects and customize cannon-only settings here.
[AddComponentMenu("Teramyyd/Weapons/Cannon")]
public class Cannon : ProjectileLauncher
{
    [Header("Audio")]
    public AudioClip fireClip;
    [Range(0f, 1f)] public float fireVolume = 1f;
    [Tooltip("Random pitch range for slight variance")] public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    private AudioSource _audio;

    // Set cannon-typical defaults when the component is first added or Reset is called
    void Reset()
    {
        if (launchSpeed < 0.01f) launchSpeed = 50f;
        if (spawnOffset < 0.01f) spawnOffset = 1f;
        if (fireKey == KeyCode.None) fireKey = KeyCode.F;

        // Ensure a 3D audio source for cannon fire
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 1f; // 3D
        _audio.rolloffMode = AudioRolloffMode.Logarithmic;
        _audio.minDistance = 2f;
        _audio.maxDistance = 50f;
    }

    // Future: override or extend behavior specifically for cannons
    // e.g., recoil, cooldown variance, spread, sound hooks, etc.

    protected override void FireProjectile()
    {
        // Play fire SFX at the muzzle if possible
        if (_audio == null) _audio = GetComponent<AudioSource>();
        if (_audio != null && fireClip != null)
        {
            if (spawnPoint != null)
            {
                _audio.transform.position = spawnPoint.position;
            }
            _audio.pitch = Mathf.Clamp(Random.Range(pitchRange.x, pitchRange.y), 0.1f, 3f);
            _audio.PlayOneShot(fireClip, fireVolume);
        }

        base.FireProjectile();
    }
}
