using UnityEngine;

// Cannon-specific component that reuses the generic ProjectileLauncher behavior.
// Add this to cannon GameObjects and customize cannon-only settings here.
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CrewStationRequirementProfile))]
[AddComponentMenu("Teramyyd/Weapons/Cannon")]
public class Cannon : ProjectileLauncher
{
    [Header("Audio")]
    [SerializeField] private AudioSource fireAudioSource;
    
    [Header("Messages")]
    [Tooltip("Reference to the MessageBoxController for displaying unmanned warnings")]
    public MessageBoxController messageBox;
    
    [Tooltip("Messages shown when player tries to manually fire this cannon with no crew assigned")]
    public string[] unmannedCannonMessages = new string[]
    {
        "This cannon has no crew assigned!",
        "The cannon is unmanned.",
        "We need crew at this gun!",
        "Nobody is manning this weapon!",
        "Assign crew to this cannon first!"
    };
    
    [Header("Usage wear and tear")]
    [Tooltip("Health component that receives self-damage each time the cannon fires. Leave empty to auto-detect on the mesh child.")]
    [SerializeField] private Health wearTarget;
    [Tooltip("Amount of health removed every time this cannon fires.")]
    [Min(0f)] public float damagePerShot = 0.5f;
    CrewStationRequirementProfile _crewProfile;

    void Awake()
    {
        CacheAudioSource();
        CacheCrewProfile();
        CacheWearTarget();
    }

    // Set cannon-typical defaults when the component is first added or Reset is called
    void Reset()
    {
        if (launchSpeed < 0.01f) launchSpeed = 50f;
        if (spawnOffset < 0.01f) spawnOffset = 1f;
        if (fireKey == KeyCode.None) fireKey = KeyCode.F;
        CacheAudioSource();
        CacheCrewProfile();
        CacheWearTarget();
    }

    void OnEnable()
    {
        ProjectileFired += HandleSelfWear;
    }

    void OnDisable()
    {
        ProjectileFired -= HandleSelfWear;
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

    void CacheCrewProfile()
    {
        if (_crewProfile == null)
        {
            _crewProfile = GetComponent<CrewStationRequirementProfile>();
        }
    }

    void CacheWearTarget()
    {
        if (wearTarget != null)
            return;

        wearTarget = GetComponentInChildren<Health>();
        if (wearTarget == null)
        {
            wearTarget = GetComponent<Health>();
        }
    }

    void HandleSelfWear(ProjectileLauncher launcher)
    {
        if (launcher != this || damagePerShot <= 0f)
            return;

        if (wearTarget == null)
        {
            CacheWearTarget();
        }

        if (wearTarget != null)
        {
            wearTarget.TakeDamage(damagePerShot);
        }
    }
    
    /// <summary>
    /// Display a random cannon-specific message when trying to fire unmanned.
    /// </summary>
    public override void ShowUnmannedWeaponMessage()
    {
        if (messageBox != null && unmannedCannonMessages != null && unmannedCannonMessages.Length > 0)
        {
            messageBox.ShowRandomMessage(unmannedCannonMessages);
        }
    }
}
