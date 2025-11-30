using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Holds all HUD references for a single weapon mount icon.
/// </summary>
[AddComponentMenu("Teramyyd/UI/Ship HUD Mount Display")]
public class ShipHUDMountDisplay : MonoBehaviour
{
    const string DefaultIconSpritePath = "Assets/UI/Icons/Mount.png";
#if UNITY_EDITOR
    static bool s_WarnedMissingDefaultSprite;
#endif

    [Tooltip("Weapon mount in the world (e.g., Ship/Bow_weapon_mount/Weapon_mount).")]
    public WeaponMount weaponMount;

    [Tooltip("Icon Image under ShipRepresentation/ShipOutline representing this mount.")]
    public Image iconImage;

    [Tooltip("Sprite shown when no weapon is mounted or no type mapping exists.")]
    public Sprite emptySprite;

    [Header("Ready Indicator")]
    public bool manageReadyIndicator = true;
    [Tooltip("Image (e.g., Stoplight) used to show ready/not-ready states.")]
    public Image readyIndicatorImage;
    [Tooltip("Sprite shown when the weapon is ready.")]
    public Sprite readySprite;
    [Tooltip("Sprite shown while the weapon is reloading or unavailable.")]
    public Sprite notReadySprite;
    [Tooltip("Hide the indicator entirely when no weapon is mounted.")]
    public bool hideReadyIndicatorWhenNoWeapon = true;

    [Header("Target Acquisition Indicator")]
    public bool manageTargetNotAcquiredIndicator = true;
    [Tooltip("Red crosshair overlay shown when no target is acquired.")]
    public Image targetNotAcquiredImage;

    [Header("Health Bar")]
    public bool manageHealthBar = true;
    public RectTransform healthBarContainer;
    public Color healthBarHealthyColor = new Color(0.1f, 0.9f, 0.15f);
    public Color healthBarDamagedColor = new Color(0.95f, 0.1f, 0.1f);
    public Color healthBarBackgroundColor = new Color(0f, 0f, 0f, 0.5f);
    public Color healthBarDisabledColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);

    [Header("Fire Button")]
    public Button fireButton;

    [NonSerialized] internal bool cachedTargetNotAcquiredVisible = true;
    [NonSerialized] internal Image healthBarBackground;
    [NonSerialized] internal Image healthBarGreenFill;
    [NonSerialized] internal Image healthBarRedFill;
    [NonSerialized] internal float cachedHealthPercent = -1f;
    [NonSerialized] internal UnityAction cachedFireHandler;
    [NonSerialized] internal Button cachedFireButton;

    void Reset()
    {
        AutoAssignAllReferences(forceWeaponMountRefresh: true);
    }

    void OnValidate()
    {
        AutoAssignAllReferences(forceWeaponMountRefresh: true);
    }

    void Awake()
    {
        AutoAssignAllReferences(forceWeaponMountRefresh: true);
    }

    void OnEnable()
    {
        if (Application.isPlaying)
        {
            AutoAssignAllReferences(forceWeaponMountRefresh: true);
        }
    }

    void AutoAssignAllReferences(bool forceWeaponMountRefresh)
    {
        EnforceTargetIndicatorDefaults();
        EnforceHealthBarDefaults();
        AutoAssignIconImage();
        AutoAssignEmptySprite();
        AutoAssignReadyIndicators();
        AutoAssignTargetNotAcquiredIndicator();
        AutoAssignHealthBarContainer();
        AutoAssignWeaponMount(forceWeaponMountRefresh);
        AutoAssignFireButton();
    }

    void AutoAssignIconImage()
    {
        if (iconImage != null)
            return;

        iconImage = GetComponent<Image>();
    }

    void AutoAssignEmptySprite()
    {
        if (emptySprite != null)
            return;

#if UNITY_EDITOR
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultIconSpritePath);
        if (sprite != null)
        {
            emptySprite = sprite;
            return;
        }

        if (!s_WarnedMissingDefaultSprite)
        {
            Debug.LogWarning($"[ShipHUDMountDisplay] Default icon sprite not found at '{DefaultIconSpritePath}'. Please assign Empty Sprite manually.", this);
            s_WarnedMissingDefaultSprite = true;
        }
#endif
    }

    void AutoAssignReadyIndicators()
    {
        if (!manageReadyIndicator)
            return;

        Transform readyRoot = transform.Find("ReadyIndicators");
        if (readyRoot == null)
            return;

        Image greenImage = readyRoot.Find("GreenStoplight")?.GetComponent<Image>();
        Image redImage = readyRoot.Find("RedStoplight")?.GetComponent<Image>();

        if (greenImage != null)
        {
            bool shouldOverride = readyIndicatorImage == null || (redImage != null && readyIndicatorImage == redImage);
            if (shouldOverride)
            {
                readyIndicatorImage = greenImage;
            }
        }

        if (readySprite == null && greenImage != null)
        {
            readySprite = greenImage.sprite;
        }

        if (notReadySprite == null && redImage != null)
        {
            notReadySprite = redImage.sprite;
        }

        if (readyIndicatorImage != null && readySprite != null)
        {
            // Ensure scene view default reflects the ready sprite (green) when idle.
            readyIndicatorImage.sprite = readySprite;
        }

        if (redImage != null && readyIndicatorImage != null && redImage.gameObject != readyIndicatorImage.gameObject)
        {
            if (redImage.gameObject.activeSelf)
            {
                redImage.gameObject.SetActive(false);
            }
        }
    }

    void AutoAssignTargetNotAcquiredIndicator()
    {
        if (targetNotAcquiredImage != null)
            return;

        Transform indicator = FindDescendant(transform, "TargetNotAquired") ?? FindDescendant(transform, "TargetNotAcquired");
        if (indicator == null)
            return;

        Image indicatorImage = indicator.GetComponent<Image>() ?? indicator.GetComponentInChildren<Image>(true);
        if (indicatorImage != null)
        {
            targetNotAcquiredImage = indicatorImage;
        }
    }

    void AutoAssignHealthBarContainer()
    {
        if (healthBarContainer != null)
            return;

        Transform healthbarTransform = FindDescendant(transform, "Healthbar") ?? FindDescendant(transform, "HealthBar");
        if (healthbarTransform == null)
            return;

        RectTransform rect = healthbarTransform as RectTransform ?? healthbarTransform.GetComponent<RectTransform>();
        if (rect != null)
        {
            healthBarContainer = rect;
        }
    }

    void EnforceTargetIndicatorDefaults()
    {
        if (!manageTargetNotAcquiredIndicator && targetNotAcquiredImage == null)
        {
            manageTargetNotAcquiredIndicator = true;
        }
    }

    void EnforceHealthBarDefaults()
    {
        if (!manageHealthBar && healthBarContainer == null)
        {
            manageHealthBar = true;
        }
    }

    void AutoAssignWeaponMount(bool forceRefresh)
    {
        if (!forceRefresh && WeaponMountMatchesExpected(weaponMount))
            return;

        string mountNodeName = transform.name;
        if (string.IsNullOrEmpty(mountNodeName))
            return;

        GameObject ship = GameObject.Find("Ship");
        if (ship == null)
            return;

        Transform blankMount = ship.transform.Find(mountNodeName);
        if (blankMount == null)
            return;

        Transform actualMount = blankMount.Find("Weapon_mount");
        if (actualMount == null)
            return;

        WeaponMount resolved = actualMount.GetComponent<WeaponMount>() ?? actualMount.GetComponentInChildren<WeaponMount>(true);
        if (resolved != null)
        {
            weaponMount = resolved;
        }
    }

    void AutoAssignFireButton()
    {
        if (fireButton != null)
            return;

        Transform fireTransform = FindDescendant(transform, "FIRE") ?? FindDescendant(transform, "FireButton");
        if (fireTransform == null)
            return;

        Button button = fireTransform.GetComponent<Button>() ?? fireTransform.GetComponentInChildren<Button>(true);
        if (button != null)
        {
            fireButton = button;
        }
    }

    bool WeaponMountMatchesExpected(WeaponMount candidate)
    {
        if (candidate == null)
            return false;

        Transform parent = candidate.transform.parent;
        if (parent == null)
            return false;

        return string.Equals(parent.name, transform.name, StringComparison.Ordinal);
    }

    static Transform FindDescendant(Transform root, string childName)
    {
        if (root == null)
            return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root)
                continue;

            if (string.Equals(child.name, childName, StringComparison.Ordinal))
                return child;
        }

        return null;
    }
}
