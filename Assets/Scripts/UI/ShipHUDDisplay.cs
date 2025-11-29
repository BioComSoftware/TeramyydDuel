using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Minimal HUD binder that swaps mount icons to match the weapon currently installed on a ship mount.
/// Designed for the new ShipRepresentation/ShipOutline hierarchy where designers lay out icons manually.
/// </summary>
[AddComponentMenu("Teramyyd/UI/Ship HUD Display (Manual)")]
public class ShipHUDDisplay : MonoBehaviour
{
    static Sprite s_SolidSprite;
    static Texture2D s_SolidTexture;

    [Header("Weapon Type → Sprite Mappings")]
    [Tooltip("Map weapon type strings (e.g., 'cannon') to HUD sprites.")]
    public WeaponSpriteMapping[] weaponSpriteMappings;

    [Header("Icon Bindings")]
    [Tooltip("All ShipHUDMountDisplay components (auto-populated from mountDisplayRoot when empty).")]
    public ShipHUDMountDisplay[] mountDisplays;
    [Tooltip("If true, ShipHUDDisplay will search mountDisplayRoot (or itself) for ShipHUDMountDisplay components when mountDisplays is empty.")]
    public bool autoPopulateMountDisplays = true;
    [Tooltip("Optional override for where ShipHUDDisplay should search for ShipHUDMountDisplay components.")]
    public Transform mountDisplayRoot;

    [Header("Debug Logging")]
    [Tooltip("Writes HUD binding state changes to Logs/game_debug.log when enabled.")]
    public bool enableDebugLogging;

    void LateUpdate()
    {
        EnsureMountDisplayCache();
        if (mountDisplays == null || mountDisplays.Length == 0)
            return;

        foreach (var binding in mountDisplays)
        {
            if (binding == null || binding.iconImage == null || binding.weaponMount == null)
                continue;

            SetupFireButton(binding);
            UpdateBinding(binding);
        }
    }

    void EnsureMountDisplayCache()
    {
        TrimNullMountDisplays();

        if (!autoPopulateMountDisplays)
            return;

        Transform root = mountDisplayRoot != null ? mountDisplayRoot : transform;
        if (root == null)
            return;

        ShipHUDMountDisplay[] discovered = root.GetComponentsInChildren<ShipHUDMountDisplay>(includeInactive: true);
        if (!MountDisplayListsMatch(mountDisplays, discovered))
        {
            mountDisplays = discovered;
        }
    }

    void TrimNullMountDisplays()
    {
        if (mountDisplays == null || mountDisplays.Length == 0)
            return;

        int valid = 0;
        for (int i = 0; i < mountDisplays.Length; i++)
        {
            if (mountDisplays[i] != null)
            {
                valid++;
            }
        }

        if (valid == mountDisplays.Length)
            return;

        if (valid == 0)
        {
            mountDisplays = Array.Empty<ShipHUDMountDisplay>();
            return;
        }

        var trimmed = new ShipHUDMountDisplay[valid];
        int index = 0;
        for (int i = 0; i < mountDisplays.Length; i++)
        {
            if (mountDisplays[i] == null)
                continue;

            trimmed[index++] = mountDisplays[i];
        }

        mountDisplays = trimmed;
    }

    bool MountDisplayListsMatch(ShipHUDMountDisplay[] current, ShipHUDMountDisplay[] discovered)
    {
        if (current == null || current.Length == 0)
            return discovered == null || discovered.Length == 0;

        if (discovered == null || discovered.Length != current.Length)
            return false;

        for (int i = 0; i < current.Length; i++)
        {
            if (current[i] != discovered[i])
                return false;
        }

        return true;
    }

    void UpdateBinding(ShipHUDMountDisplay binding)
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

                if (enableDebugLogging)
                {
                    string spriteName = mapped != null ? mapped.name : "(none)";
                    LogDebug($"{binding.weaponMount.name} mounts {weaponType}, sprite={spriteName}");
                }
        }

        if (binding.iconImage.sprite != targetSprite)
        {
            binding.iconImage.sprite = targetSprite;
        }

        UpdateReadyIndicator(binding, launcher);

        bool previousIndicator = binding.cachedTargetNotAcquiredVisible;
        if (binding.manageTargetNotAcquiredIndicator)
        {
            bool indicatorVisible = true; // default to visible when we lack data
            WeaponMount mount = binding.weaponMount;
            if (mount != null && mount.HasSelectedTarget)
            {
                bool insideSensor = mount.HasTargetInsideAcquisitionCollider;
                bool hasFiringSolution = mount.HasValidFiringSolution;
                indicatorVisible = !(insideSensor && hasFiringSolution);
            }

            binding.cachedTargetNotAcquiredVisible = indicatorVisible;

            if (enableDebugLogging && previousIndicator != indicatorVisible)
            {
                string mountName = mount != null ? mount.mountId : "(null mount)";
                bool hasTarget = mount != null && mount.HasSelectedTarget;
                bool insideSensor = mount != null && mount.HasTargetInsideAcquisitionCollider;
                bool hasSolution = mount != null && mount.HasValidFiringSolution;
                LogDebug($"TargetNotAcquired → {(indicatorVisible ? "VISIBLE" : "HIDDEN")} for {mountName} (HasTarget={hasTarget}, InsideSensor={insideSensor}, HasSolution={hasSolution})");
            }
        }

        UpdateTargetNotAcquiredIndicator(binding);
        UpdateHealthBar(binding, launcher);
        UpdateFireButtonState(binding);
    }

    void UpdateTargetNotAcquiredIndicator(ShipHUDMountDisplay binding)
    {
        if (!binding.manageTargetNotAcquiredIndicator || binding.targetNotAcquiredImage == null)
            return;

        bool desiredActive = binding.cachedTargetNotAcquiredVisible;
        GameObject indicatorObject = binding.targetNotAcquiredImage.gameObject;
        if (indicatorObject.activeSelf != desiredActive)
        {
            indicatorObject.SetActive(desiredActive);
        }
    }

    public void SetTargetNotAcquiredVisible(WeaponMount mount, bool visible)
    {
        EnsureMountDisplayCache();

        if (mount == null || mountDisplays == null)
            return;

        foreach (var binding in mountDisplays)
        {
            if (binding == null || binding.weaponMount != mount)
                continue;

            binding.cachedTargetNotAcquiredVisible = visible;
            UpdateTargetNotAcquiredIndicator(binding);
            return;
        }

        if (enableDebugLogging)
        {
            LogDebug($"No ShipHUDMountDisplay found for weapon mount '{mount.name}' while setting TargetNotAcquired visibility.");
        }
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

    void UpdateReadyIndicator(ShipHUDMountDisplay binding, ProjectileLauncher launcher)
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

    void LogDebug(string message)
    {
        if (!enableDebugLogging)
            return;

        string formatted = $"[ShipHUDDisplay] {message}";
        Debug.Log(formatted, this);
        FileLogger.Log(formatted, "ShipHUD");
    }

    void UpdateHealthBar(ShipHUDMountDisplay binding, ProjectileLauncher launcher)
    {
        if (!binding.manageHealthBar || binding.healthBarContainer == null)
            return;

        EnsureHealthBarRuntime(binding);

        bool hasWeapon = launcher != null && binding.weaponMount != null;
        Health weaponHealth = hasWeapon ? binding.weaponMount.MountedWeaponHealth : null;

        if (weaponHealth == null)
        {
            if (binding.healthBarContainer.gameObject.activeSelf)
            {
                binding.healthBarContainer.gameObject.SetActive(false);
            }
            binding.cachedHealthPercent = -1f;
            return;
        }

        if (!binding.healthBarContainer.gameObject.activeSelf)
        {
            binding.healthBarContainer.gameObject.SetActive(true);
        }

        float percent = weaponHealth.maxHealth > 0f ? Mathf.Clamp01(weaponHealth.currentHealth / weaponHealth.maxHealth) : 0f;
        if (Mathf.Approximately(percent, binding.cachedHealthPercent))
            return;

        binding.cachedHealthPercent = percent;
        ApplyHealthBarWidths(binding, percent);
    }

    void SetupFireButton(ShipHUDMountDisplay binding)
    {
        if (binding == null)
            return;

        if (binding.fireButton == null)
        {
            CleanupFireButton(binding);
            return;
        }

        if (binding.cachedFireButton != null && binding.cachedFireButton != binding.fireButton)
        {
            CleanupFireButton(binding);
        }

        if (binding.weaponMount == null)
        {
            binding.fireButton.interactable = false;
            CleanupFireButton(binding);
            return;
        }

        if (binding.cachedFireHandler == null)
        {
            WeaponMount mountRef = binding.weaponMount;
            binding.cachedFireHandler = () => FireSingleMount(mountRef);
            binding.cachedFireButton = binding.fireButton;
            binding.cachedFireButton.onClick.AddListener(binding.cachedFireHandler);
        }
    }

    void CleanupFireButton(ShipHUDMountDisplay binding)
    {
        if (binding == null)
            return;

        if (binding.cachedFireButton != null && binding.cachedFireHandler != null)
        {
            binding.cachedFireButton.onClick.RemoveListener(binding.cachedFireHandler);
        } 

        binding.cachedFireButton = null;
        binding.cachedFireHandler = null;
    }

    void UpdateFireButtonState(ShipHUDMountDisplay binding)
    {
        if (binding == null || binding.fireButton == null)
            return;

        bool interactable = false;
        WeaponMount mount = binding.weaponMount;
        if (mount != null)
        {
            ProjectileLauncher launcher = mount.currentLauncher;
            if (launcher != null)
            {
                interactable = launcher.IsReady && mount.CanFireAtCurrentTarget;
            }
        }

        if (binding.fireButton.interactable != interactable)
        {
            binding.fireButton.interactable = interactable;
        }
    }

    void FireSingleMount(WeaponMount mount)
    {
        if (mount == null)
            return;

        mount.TryFire();
    }

    void EnsureHealthBarRuntime(ShipHUDMountDisplay binding)
    {
        if (binding.healthBarContainer == null)
            return;

        if (binding.healthBarBackground == null)
        {
            Image background = binding.healthBarContainer.GetComponent<Image>();
            if (background == null)
            {
                background = binding.healthBarContainer.gameObject.AddComponent<Image>();
            }
            background.sprite = GetSolidSprite();
            background.type = Image.Type.Simple;
            background.color = binding.healthBarBackgroundColor;
            binding.healthBarBackground = background;
        }

        if (binding.healthBarGreenFill == null)
        {
            binding.healthBarGreenFill = CreateHealthFillImage(binding.healthBarContainer, "HealthFill_Green", binding.healthBarHealthyColor, leftAnchored: true);
        }

        if (binding.healthBarRedFill == null)
        {
            binding.healthBarRedFill = CreateHealthFillImage(binding.healthBarContainer, "HealthFill_Red", binding.healthBarDamagedColor, leftAnchored: false);
        }
    }

    Image CreateHealthFillImage(RectTransform parent, string name, Color tint, bool leftAnchored)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = GetSolidSprite();
        img.type = Image.Type.Simple;
        img.color = tint;

        RectTransform rect = img.rectTransform;
        rect.anchorMin = leftAnchored ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
        rect.anchorMax = leftAnchored ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
        rect.pivot = leftAnchored ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(0f, parent.rect.height);

        return img;
    }

    void ApplyHealthBarWidths(ShipHUDMountDisplay binding, float percent)
    {
        float totalWidth = binding.healthBarContainer.rect.width;
        if (totalWidth <= 0f)
        {
            totalWidth = binding.healthBarContainer.sizeDelta.x;
        }
        totalWidth = Mathf.Max(totalWidth, 1f);

        float greenWidth = totalWidth * percent;
        float redWidth = totalWidth - greenWidth;

        SetRectWidth(binding.healthBarGreenFill.rectTransform, greenWidth);
        SetRectWidth(binding.healthBarRedFill.rectTransform, redWidth);

        binding.healthBarGreenFill.enabled = greenWidth > 0.01f;
        binding.healthBarRedFill.enabled = redWidth > 0.01f;
    }

    void SetRectWidth(RectTransform rect, float width)
    {
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0f, width));
    }

    Sprite GetSolidSprite()
    {
        if (s_SolidSprite != null)
            return s_SolidSprite;

        s_SolidTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "ShipHUDDisplay_SolidTex",
            hideFlags = HideFlags.HideAndDontSave
        };
        s_SolidTexture.SetPixel(0, 0, Color.white);
        s_SolidTexture.Apply();
        s_SolidSprite = Sprite.Create(s_SolidTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        s_SolidSprite.name = "ShipHUDDisplay_SolidSprite";
        return s_SolidSprite;
    }
    }

    [Serializable]
    public class WeaponSpriteMapping
    {
        public string weaponType = "cannon";
        public Sprite sprite;
    }
