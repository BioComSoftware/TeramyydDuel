using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Ship-level Fire-at-Will controller. Handles the UI toggle sprite swap and, when active,
/// continually asks all configured weapon mounts to fire as soon as they are ready and have a valid
/// target lock. Attach this to ShipRepresentation and assign the UI button plus weapon mounts.
/// </summary>
[AddComponentMenu("Teramyyd/UI/Fire At Will Controller")]
public class FireAtWillController : MonoBehaviour
{
    [Header("Toggle UI")]
    [Tooltip("Button that toggles Fire-at-Will mode on/off.")]
    public Button fireAtWillButton;

    [Tooltip("Optional override for the Image whose sprite should change when toggled. Defaults to the button's target graphic.")]
    public Image fireAtWillImage;

    [Tooltip("Sprite shown when Fire-at-Will is inactive.")]
    public Sprite inactiveSprite;

    [Tooltip("Sprite shown when Fire-at-Will is active.")]
    public Sprite activeSprite;

    [Tooltip("Start Fire-at-Will in the active/on state.")]
    public bool startActive = false;

    [Header("Weapon Mounts")]
    [Tooltip("Weapon mounts that should receive Fire-at-Will commands.")]
    public WeaponMount[] weaponMounts;

    [Tooltip("When enabled, automatically populate weaponMounts from children of this object.")]
    public bool autoPopulateMountsFromChildren = true;

    public bool IsActive => _isActive;

    bool _isActive;
    UnityAction _cachedHandler;

    void Awake()
    {
        _isActive = startActive;
        ApplyVisual();

        if ((weaponMounts == null || weaponMounts.Length == 0) && autoPopulateMountsFromChildren)
        {
            weaponMounts = GetComponentsInChildren<WeaponMount>(includeInactive: true);
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

    void Update()
    {
        if (!_isActive)
            return;

        if (weaponMounts == null || weaponMounts.Length == 0)
            return;

        foreach (var mount in weaponMounts)
        {
            if (mount == null)
                continue;

            mount.TryFire();
        }
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
        _isActive = !_isActive;
        ApplyVisual();
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
}
