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
    [Tooltip("Force 2D playback to debug audibility regardless of distance")] public bool force2DForDebug = false;
    [Tooltip("3D audio min distance (full volume within this radius)")] public float audioMinDistance = 2f;
    [Tooltip("3D audio max distance (audible up to this range)")] public float audioMaxDistance = 2000f;

    private AudioSource _audio;
    private Transform _audioNode; // child transform that holds the AudioSource at the muzzle

    // Ensure an AudioSource exists and is configured
    void Awake()
    {
        EnsureAudioSource();
    }

    // Set cannon-typical defaults when the component is first added or Reset is called
    void Reset()
    {
        if (launchSpeed < 0.01f) launchSpeed = 50f;
        if (spawnOffset < 0.01f) spawnOffset = 1f;
        if (fireKey == KeyCode.None) fireKey = KeyCode.F;
        EnsureAudioSource();
    }

    // Future: override or extend behavior specifically for cannons
    // e.g., recoil, cooldown variance, spread, sound hooks, etc.

    protected override void FireProjectile()
    {
        // Play fire SFX via a dedicated child AudioSource placed at the muzzle
        if (fireClip != null)
        {
            if (_audio == null) EnsureAudioSource();

            // Keep the audio node following the spawnPoint if available
            if (spawnPoint != null && _audioNode != spawnPoint)
            {
                _audioNode.SetParent(spawnPoint, worldPositionStays: false);
                _audioNode.localPosition = Vector3.zero;
                _audioNode.localRotation = Quaternion.identity;
            }

            _audio.pitch = Mathf.Clamp(Random.Range(pitchRange.x, pitchRange.y), 0.1f, 3f);
            _audio.spatialBlend = force2DForDebug ? 0f : 1f;
            _audio.minDistance = Mathf.Max(0.01f, audioMinDistance);
            _audio.maxDistance = Mathf.Max(_audio.minDistance + 1f, audioMaxDistance);
            _audio.PlayOneShot(fireClip, Mathf.Clamp01(fireVolume));
        }

        base.FireProjectile();
    }

    private void EnsureAudioSource()
    {
        if (_audioNode == null)
        {
            // Create a dedicated child to avoid moving the cannon transform for audio placement
            var nodeGO = new GameObject("CannonAudio");
            _audioNode = nodeGO.transform;
            // Parent to spawnPoint if available, otherwise to this object
            if (spawnPoint != null)
            {
                _audioNode.SetParent(spawnPoint, worldPositionStays: false);
            }
            else
            {
                _audioNode.SetParent(transform, worldPositionStays: false);
            }
            _audioNode.localPosition = Vector3.zero;
            _audioNode.localRotation = Quaternion.identity;
        }

        _audio = _audioNode.GetComponent<AudioSource>();
        if (_audio == null) _audio = _audioNode.gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.rolloffMode = AudioRolloffMode.Logarithmic;
        _audio.spatialBlend = force2DForDebug ? 0f : 1f;
        _audio.minDistance = Mathf.Max(0.01f, audioMinDistance);
        _audio.maxDistance = Mathf.Max(_audio.minDistance + 1f, audioMaxDistance);
    }

}
