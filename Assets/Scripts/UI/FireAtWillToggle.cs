using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Simple UI helper that swaps between two sprites whenever the Fire-at-Will button is clicked.
/// Attach this to ShipRepresentation (or any suitable parent) and assign the button + sprites in
/// the inspector. This script intentionally does not trigger any weapon logic yet; it only toggles
/// the visuals so gameplay can hook in later.
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

    public bool IsActive => _isActive;

    bool _isActive;
    UnityAction _cachedHandler;

    void Awake()
    {
        _isActive = startActive;
        ApplyVisual();
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
