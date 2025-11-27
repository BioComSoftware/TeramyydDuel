using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal HUD binder that swaps mount icons to match the weapon currently installed on a ship mount.
/// Designed for the new ShipRepresentation/ShipOutline hierarchy where designers lay out icons manually.
/// </summary>
[AddComponentMenu("Teramyyd/UI/Ship HUD Display (Manual)")]
public class ShipHUDDisplay : MonoBehaviour
{
    [Header("Weapon Type → Sprite Mappings")]
    [Tooltip("Map weapon type strings (e.g., 'cannon') to HUD sprites.")]
    public WeaponSpriteMapping[] weaponSpriteMappings;

    [Header("Icon Bindings")]
    [Tooltip("Manual bindings between in-world WeaponMounts and their HUD icons (e.g., ShipOutline/Bow_weapon_mount).")]
    public MountIconBinding[] mountIcons;

    [Header("Debug")]
    public bool debugLog;

    void LateUpdate()
    {
        if (mountIcons == null || mountIcons.Length == 0)
            return;

        foreach (var binding in mountIcons)
        {
            if (binding == null || binding.iconImage == null || binding.weaponMount == null)
                continue;

            UpdateBinding(binding);
        }
    }

    void UpdateBinding(MountIconBinding binding)
    {
        Sprite targetSprite = binding.emptySprite;
        ProjectileLauncher launcher = binding.weaponMount.currentLauncher;

        if (launcher != null)
        {
            string weaponType = WeaponTypeDetector.GetWeaponType(launcher);
            Sprite mapped = GetSpriteForWeaponType(weaponType);

            if (mapped != null)
            {
                targetSprite = mapped;
            }

                if (debugLog)
                {
                    string spriteName = mapped != null ? mapped.name : "(none)";
                    Debug.Log($"[ShipHUDDisplay] {binding.weaponMount.name} mounts {weaponType}, sprite={spriteName}");
                }
        }

        if (binding.iconImage.sprite != targetSprite)
        {
            binding.iconImage.sprite = targetSprite;
        }

        UpdateReadyIndicator(binding, launcher);
    }

    Sprite GetSpriteForWeaponType(string weaponType)
    {
        if (weaponSpriteMappings == null || string.IsNullOrEmpty(weaponType))
            return null;

        string normalized = weaponType.ToLowerInvariant();
        foreach (var mapping in weaponSpriteMappings)
        {
            if (mapping == null || string.IsNullOrEmpty(mapping.weaponType))
                continue;

            if (mapping.weaponType.ToLowerInvariant() == normalized)
                return mapping.sprite;
        }

        return null;
    }

    void UpdateReadyIndicator(MountIconBinding binding, ProjectileLauncher launcher)
    {
        if (!binding.manageReadyIndicator || binding.readyIndicatorImage == null)
            return;

        if (launcher == null)
        {
            if (binding.hideReadyIndicatorWhenNoWeapon)
            {
                if (binding.readyIndicatorImage.gameObject.activeSelf)
                    binding.readyIndicatorImage.gameObject.SetActive(false);
            }
            else
            {
                if (!binding.readyIndicatorImage.gameObject.activeSelf)
                    binding.readyIndicatorImage.gameObject.SetActive(true);

                if (binding.notReadySprite != null)
                    binding.readyIndicatorImage.sprite = binding.notReadySprite;
            }
            return;
        }

        if (!binding.readyIndicatorImage.gameObject.activeSelf)
            binding.readyIndicatorImage.gameObject.SetActive(true);

        bool isReady = launcher.IsReady;
        Sprite desired = isReady ? binding.readySprite : binding.notReadySprite;
        if (desired != null && binding.readyIndicatorImage.sprite != desired)
        {
            binding.readyIndicatorImage.sprite = desired;
        }
    }
}

[Serializable]
public class WeaponSpriteMapping
{
    public string weaponType = "cannon";
    public Sprite sprite;
}

[Serializable]
public class MountIconBinding
{
    [Tooltip("Weapon mount in the scene (e.g., Ship/Model/Deck/Bow_weapon_mount).")]
    public WeaponMount weaponMount;

    [Tooltip("Image under ShipRepresentation/ShipOutline corresponding to this mount.")]
    public Image iconImage;

    [Tooltip("Sprite used when the mount is empty or no mapping exists.")]
    public Sprite emptySprite;

    [Header("Ready Indicator")]
    public bool manageReadyIndicator = false;
    [Tooltip("Image (e.g., GreenStoplight) that represents ready state for this mount.")]
    public Image readyIndicatorImage;
    [Tooltip("Sprite shown when the mounted weapon is ready (e.g., green light).")]
    public Sprite readySprite;
    [Tooltip("Sprite shown while the weapon is not ready/reloading (e.g., red light).")]
    public Sprite notReadySprite;
    [Tooltip("Hide the ready indicator entirely when no weapon is mounted.")]
    public bool hideReadyIndicatorWhenNoWeapon = true;
}
