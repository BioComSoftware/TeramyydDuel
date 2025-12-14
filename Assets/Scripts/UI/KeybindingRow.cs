using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Teramyyd.UI
{
    /// <summary>
    /// Represents a single row in the controls settings panel showing an action and its keybinding.
    /// Handles rebinding via KeybindingManager.
    /// </summary>
    public class KeybindingRow : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] TextMeshProUGUI actionLabel;
        [SerializeField] Button rebindButton;
        [SerializeField] TextMeshProUGUI keyLabel;
        [SerializeField] TextMeshProUGUI listeningIndicator;

        [Header("Visual Feedback")]
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color listeningColor = Color.yellow;
        [SerializeField] Color conflictColor = Color.red;

        private string _actionId;
        private bool _isListening;

        public string ActionId => _actionId;
        public bool IsListening => _isListening;

        void Awake()
        {
            if (rebindButton != null)
            {
                rebindButton.onClick.AddListener(OnRebindClicked);
            }

            if (listeningIndicator != null)
            {
                listeningIndicator.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Initialize this row with action information
        /// </summary>
        public void Initialize(string actionId, string displayName, KeyCode currentKey)
        {
            _actionId = actionId;

            if (actionLabel != null)
            {
                actionLabel.text = displayName;
            }

            UpdateKeyDisplay(currentKey);
        }

        /// <summary>
        /// Update the displayed key
        /// </summary>
        public void UpdateKeyDisplay(KeyCode key)
        {
            if (keyLabel != null)
            {
                keyLabel.text = GetKeyDisplayName(key);
                keyLabel.color = normalColor;
            }
        }

        /// <summary>
        /// Start listening for a new key input
        /// </summary>
        public void StartListening()
        {
            _isListening = true;

            if (listeningIndicator != null)
            {
                listeningIndicator.gameObject.SetActive(true);
            }

            if (keyLabel != null)
            {
                keyLabel.text = "Press any key...";
                keyLabel.color = listeningColor;
            }

            if (rebindButton != null)
            {
                rebindButton.interactable = false;
            }
        }

        /// <summary>
        /// Stop listening for key input
        /// </summary>
        public void StopListening()
        {
            _isListening = false;

            if (listeningIndicator != null)
            {
                listeningIndicator.gameObject.SetActive(false);
            }

            if (rebindButton != null)
            {
                rebindButton.interactable = true;
            }
        }

        /// <summary>
        /// Show conflict warning (when another action uses this key)
        /// </summary>
        public void ShowConflict(string conflictingActionName)
        {
            if (keyLabel != null)
            {
                keyLabel.color = conflictColor;
            }

            Debug.LogWarning($"[KeybindingRow] Key conflict with action: {conflictingActionName}");
        }

        /// <summary>
        /// Clear conflict warning
        /// </summary>
        public void ClearConflict()
        {
            if (keyLabel != null)
            {
                keyLabel.color = normalColor;
            }
        }

        void OnRebindClicked()
        {
            if (KeybindingControlsPanel.Instance != null)
            {
                KeybindingControlsPanel.Instance.StartRebinding(_actionId, this);
            }
            else
            {
                Debug.LogError("[KeybindingRow] KeybindingControlsPanel instance not found!");
            }
        }

        /// <summary>
        /// Convert KeyCode to user-friendly display name
        /// </summary>
        string GetKeyDisplayName(KeyCode key)
        {
            // Special cases for better readability
            switch (key)
            {
                case KeyCode.None: return "Not Bound";
                case KeyCode.LeftArrow: return "Left Arrow";
                case KeyCode.RightArrow: return "Right Arrow";
                case KeyCode.UpArrow: return "Up Arrow";
                case KeyCode.DownArrow: return "Down Arrow";
                case KeyCode.LeftShift: return "Left Shift";
                case KeyCode.RightShift: return "Right Shift";
                case KeyCode.LeftControl: return "Left Ctrl";
                case KeyCode.RightControl: return "Right Ctrl";
                case KeyCode.LeftAlt: return "Left Alt";
                case KeyCode.RightAlt: return "Right Alt";
                case KeyCode.Space: return "Space";
                case KeyCode.Return: return "Enter";
                case KeyCode.Escape: return "Escape";
                case KeyCode.Backspace: return "Backspace";
                case KeyCode.Tab: return "Tab";
                case KeyCode.CapsLock: return "Caps Lock";
                case KeyCode.Keypad0: return "Numpad 0";
                case KeyCode.Keypad1: return "Numpad 1";
                case KeyCode.Keypad2: return "Numpad 2";
                case KeyCode.Keypad3: return "Numpad 3";
                case KeyCode.Keypad4: return "Numpad 4";
                case KeyCode.Keypad5: return "Numpad 5";
                case KeyCode.Keypad6: return "Numpad 6";
                case KeyCode.Keypad7: return "Numpad 7";
                case KeyCode.Keypad8: return "Numpad 8";
                case KeyCode.Keypad9: return "Numpad 9";
                case KeyCode.KeypadPlus: return "Numpad +";
                case KeyCode.KeypadMinus: return "Numpad -";
                case KeyCode.KeypadMultiply: return "Numpad *";
                case KeyCode.KeypadDivide: return "Numpad /";
                case KeyCode.KeypadEnter: return "Numpad Enter";
                case KeyCode.KeypadPeriod: return "Numpad .";
                case KeyCode.Mouse0: return "Left Mouse";
                case KeyCode.Mouse1: return "Right Mouse";
                case KeyCode.Mouse2: return "Middle Mouse";
                case KeyCode.Mouse3: return "Mouse 3";
                case KeyCode.Mouse4: return "Mouse 4";
                case KeyCode.Mouse5: return "Mouse 5";
                case KeyCode.Mouse6: return "Mouse 6";
                default: return key.ToString();
            }
        }

        void OnDestroy()
        {
            if (rebindButton != null)
            {
                rebindButton.onClick.RemoveListener(OnRebindClicked);
            }
        }
    }
}
