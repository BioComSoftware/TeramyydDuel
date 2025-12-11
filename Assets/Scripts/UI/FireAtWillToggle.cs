using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Simple Fire-at-Will driver. Taps a UI button to toggle sprites and, when active,
/// loops through all configured WeaponMounts to trigger TryFire on any mount that
/// currently has crew, a ready launcher, and a valid target lock.
/// </summary>
[AddComponentMenu("Teramyyd/UI/Fire At Will Toggle")]
public class FireAtWillToggle : MonoBehaviour
{
    [Tooltip("Button that should toggle the Fire-at-Will state.")]
    public Button fireAtWillButton;

    [Tooltip("Optional override for the Image that displays the toggle sprite. Defaults to the button's target graphic.")]
    public Image fireAtWillImage;

    [Tooltip("Sprite shown when Fire-at-Will is inactive.")]
    public Sprite inactiveSprite;

    [Tooltip("Sprite shown when Fire-at-Will is active.")]
    public Sprite activeSprite;

    [Tooltip("Start Fire-at-Will in the active/on state.")]
    public bool startActive = false;

    [Header("Weapon Mounts")]
    [Tooltip("Weapon mounts to auto-fire when Fire-at-Will is active.")]
    public WeaponMount[] weaponMounts;
    [Tooltip("When true, the toggle will search for WeaponMount children under Auto Populate Root when the array is empty.")]
    public bool autoPopulateMountsFromChildren = true;
    [Tooltip("Optional override for the hierarchy root used when auto-populating mounts.")]
    public Transform autoPopulateRootOverride;
    [Tooltip("Debug log")] [FormerlySerializedAs("enableDebugLogging")]
    public bool debugLog = false;

    public bool IsActive => _isActive;

    bool _isActive;
    UnityAction _cachedHandler;

    void Awake()
    {
        _isActive = startActive;
        ApplyVisual();
        // Don't rely solely on Awake for caching, as other objects might not be ready.
    }

    void Start()
    {
        // If auto-populate is enabled, force a refresh at Start to ensure we have all mounts,
        // overriding any stale data serialized in the inspector.
        if (autoPopulateMountsFromChildren)
        {
            RefreshMounts();
        }
        else
        {
            EnsureMountCache();
        }
    }

    void OnEnable()
    {
        RegisterHandler();
        ApplyVisual();
    }

    void OnDisable()
    {
        UnregisterHandler();
    }

    void RegisterHandler()
    {
        if (fireAtWillButton == null)
            return;

        if (_cachedHandler == null)
        {
            _cachedHandler = ToggleState;
            fireAtWillButton.onClick.AddListener(_cachedHandler);
        }
    }

    void UnregisterHandler()
    {
        if (fireAtWillButton != null && _cachedHandler != null)
        {
            fireAtWillButton.onClick.RemoveListener(_cachedHandler);
        }

        _cachedHandler = null;
    }

    void ToggleState()
    {
        SetActive(!_isActive);
    }

    public void SetActive(bool active)
    {
        if (_isActive == active)
        {
            ApplyVisual();
            return;
        }

        _isActive = active;
        ApplyVisual();
        if (debugLog)
        {
            LogDebug(_isActive ? "Fire-at-Will ENABLED" : "Fire-at-Will DISABLED");
        }
    }

    void ApplyVisual()
    {
        Image targetImage = fireAtWillImage;
        if (targetImage == null && fireAtWillButton != null)
        {
            targetImage = fireAtWillButton.targetGraphic as Image;
        }

        if (targetImage == null)
            return;

        Sprite desiredSprite = _isActive ? activeSprite : inactiveSprite;
        if (desiredSprite != null && targetImage.sprite != desiredSprite)
        {
            targetImage.sprite = desiredSprite;
        }
    }

    void Update()
    {
        if (!_isActive)
            return;

        // Ensure we have mounts (in case they were destroyed or list was cleared)
        if (weaponMounts == null || weaponMounts.Length == 0)
        {
            if (autoPopulateMountsFromChildren)
            {
                RefreshMounts();
            }
        }

        if (weaponMounts == null || weaponMounts.Length == 0)
        {
            LogDebug("Update skipped: no weapon mounts registered.");
            return;
        }

        for (int i = 0; i < weaponMounts.Length; i++)
        {
            var mount = weaponMounts[i];
            if (mount == null)
                continue;

            bool crewReady = mount.HasCrewReady;
            var launcher = mount.currentLauncher;
            bool launcherReady = launcher != null && launcher.IsReady;
            bool acquired = mount.IsTargetFullyAcquired;

            if (crewReady && launcherReady && acquired)
            {
                mount.TryFire();
            }
            else if (debugLog)
            {
                LogDebug($"{mount.mountId}: not auto-firing (crew={crewReady}, launcherReady={launcherReady}, targetAcquired={acquired})");
            }
        }
    }

    void EnsureMountCache()
    {
        TrimNullMounts();

        if (weaponMounts != null && weaponMounts.Length > 0)
            return;

        if (autoPopulateMountsFromChildren)
        {
            RefreshMounts();
        }
    }

    void RefreshMounts()
    {
        Transform root = autoPopulateRootOverride != null ? autoPopulateRootOverride : transform;
        
        // 1. Try to find children under the root (or self)
        WeaponMount[] found = root.GetComponentsInChildren<WeaponMount>(includeInactive: true);

        // 2. If none found, and we are using the default root (self), fall back to global search
        //    (This handles the case where the script is on a UI object but mounts are elsewhere)
        if ((found == null || found.Length == 0) && autoPopulateRootOverride == null)
        {
            found = FindAllWeaponMounts();
            LogDebug($"Auto-populate: Local search empty. Global search found {found?.Length ?? 0} mounts.");
        }
        else
        {
            LogDebug($"Auto-populate: Found {found?.Length ?? 0} mounts under root '{root.name}'.");
        }

        if (found != null && found.Length > 0)
        {
            weaponMounts = found;
        }
    }

    void TrimNullMounts()
    {
        if (weaponMounts == null || weaponMounts.Length == 0)
            return;

        int validCount = 0;
        for (int i = 0; i < weaponMounts.Length; i++)
        {
            if (weaponMounts[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == weaponMounts.Length)
            return;

        if (validCount == 0)
        {
            weaponMounts = Array.Empty<WeaponMount>();
            return;
        }

        var trimmed = new WeaponMount[validCount];
        int index = 0;
        for (int i = 0; i < weaponMounts.Length; i++)
        {
            if (weaponMounts[i] == null)
                continue;

            trimmed[index++] = weaponMounts[i];
        }

        weaponMounts = trimmed;
    }

    WeaponMount[] FindAllWeaponMounts()
    {
        WeaponMount[] mounts = null;
        try
        {
#if UNITY_2023_1_OR_NEWER
            mounts = UnityEngine.Object.FindObjectsByType<WeaponMount>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            mounts = FindObjectsOfType<WeaponMount>(includeInactive: true);
#endif
        }
        catch
        {
            mounts = Resources.FindObjectsOfTypeAll<WeaponMount>();
        }
        return mounts;
    }

    void LogDebug(string message)
    {
        if (!debugLog)
            return;

        string formatted = $"[FireAtWillToggle] {message}";
        Debug.Log(formatted, this);
        FileLogger.Log(formatted, "FireAtWill");
    }
}
