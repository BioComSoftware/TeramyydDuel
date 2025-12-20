using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Teramyyd.UI
{
    /// <summary>
    /// Represents a single row in the controls settings panel showing an action and its keybinding.
    /// Handles rebinding with modifier key support (Ctrl, Shift, Alt).
    /// </summary>
    public class KeybindingRow : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] TextMeshProUGUI actionLabel;
        [SerializeField] TMP_Dropdown modifierDropdown;
        [SerializeField] Button keyButton;
        [SerializeField] TextMeshProUGUI keyButtonText;
        [SerializeField] GameObject listeningIndicator;

        [Header("Visual Feedback")]
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color listeningColor = Color.yellow;
        [SerializeField] Color conflictColor = Color.red;

        private string _actionId;
        private KeyBindingData _currentBinding;
        private ControlsSettingsPanel _panel;
        private bool _isListening;

        public string ActionId => _actionId;
        public bool IsListening => _isListening;

        public void Initialize(string actionId, string displayName, KeyBindingData binding, ControlsSettingsPanel panel)
        {
            _actionId = actionId;
            _currentBinding = binding;
            _panel = panel;

            if (actionLabel != null)
                actionLabel.text = displayName;

            SetupModifierDropdown();
            UpdateDisplay(binding);

            if (keyButton != null)
                keyButton.onClick.AddListener(OnKeyButtonClicked);

            if (listeningIndicator != null)
                listeningIndicator.SetActive(false);
        }

        private void OnDestroy()
        {
            if (keyButton != null)
                keyButton.onClick.RemoveListener(OnKeyButtonClicked);
        }

        private void SetupModifierDropdown()
        {
            if (modifierDropdown == null)
                return;

            modifierDropdown.ClearOptions();
            modifierDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "None",
                "Ctrl",
                "Shift",
                "Alt",
                "Ctrl+Shift",
                "Ctrl+Alt",
                "Shift+Alt",
                "Ctrl+Shift+Alt"
            });

            // Set current modifier
            int index = GetModifierIndex(_currentBinding);
            modifierDropdown.SetValueWithoutNotify(index);

            modifierDropdown.onValueChanged.AddListener(OnModifierChanged);
        }

        private int GetModifierIndex(KeyBindingData data)
        {
            if (data.ctrl && data.shift && data.alt) return 7;
            if (data.shift && data.alt) return 6;
            if (data.ctrl && data.alt) return 5;
            if (data.ctrl && data.shift) return 4;
            if (data.alt) return 3;
            if (data.shift) return 2;
            if (data.ctrl) return 1;
            return 0;
        }

        private void OnModifierChanged(int index)
        {
            // Update the current binding with new modifiers
            _currentBinding.ctrl = (index == 1 || index == 4 || index == 5 || index == 7);
            _currentBinding.shift = (index == 2 || index == 4 || index == 6 || index == 7);
            _currentBinding.alt = (index == 3 || index == 5 || index == 6 || index == 7);

            UpdateDisplay(_currentBinding);
        }

        public void UpdateDisplay(KeyBindingData binding)
        {
            _currentBinding = binding;

            if (keyButtonText != null)
            {
                keyButtonText.text = GetKeyDisplayName(binding.key);
                keyButtonText.color = normalColor;
            }

            if (modifierDropdown != null)
            {
                int index = GetModifierIndex(binding);
                modifierDropdown.SetValueWithoutNotify(index);
            }
        }

        private void OnKeyButtonClicked()
        {
            if (_panel != null)
            {
                _panel.StartListening(this);
            }
        }

        public void StartListening()
        {
            _isListening = true;

            if (listeningIndicator != null)
                listeningIndicator.SetActive(true);

            if (keyButtonText != null)
            {
                keyButtonText.text = "Press any key...";
                keyButtonText.color = listeningColor;
            }

            if (keyButton != null)
                keyButton.interactable = false;
        }

        public void StopListening()
        {
            _isListening = false;

            if (listeningIndicator != null)
                listeningIndicator.SetActive(false);

            if (keyButtonText != null)
            {
                keyButtonText.text = GetKeyDisplayName(_currentBinding.key);
                keyButtonText.color = normalColor;
            }

            if (keyButton != null)
                keyButton.interactable = true;
        }

        public void ShowConflict(string conflictingActionName)
        {
            if (keyButtonText != null)
            {
                keyButtonText.text = $"Conflict: {conflictingActionName}";
                keyButtonText.color = conflictColor;
            }

            Invoke(nameof(ResetConflictDisplay), 2f);
        }

        private void ResetConflictDisplay()
        {
            if (keyButtonText != null)
            {
                keyButtonText.text = GetKeyDisplayName(_currentBinding.key);
                keyButtonText.color = normalColor;
            }
        }

        public bool IsCtrlSelected()
        {
            if (modifierDropdown == null) return false;
            int index = modifierDropdown.value;
            return (index == 1 || index == 4 || index == 5 || index == 7);
        }

        public bool IsShiftSelected()
        {
            if (modifierDropdown == null) return false;
            int index = modifierDropdown.value;
            return (index == 2 || index == 4 || index == 6 || index == 7);
        }

        public bool IsAltSelected()
        {
            if (modifierDropdown == null) return false;
            int index = modifierDropdown.value;
            return (index == 3 || index == 5 || index == 6 || index == 7);
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
                case KeyCode.F1: return "F1";
                case KeyCode.F2: return "F2";
                case KeyCode.F3: return "F3";
                case KeyCode.F4: return "F4";
                case KeyCode.F5: return "F5";
                case KeyCode.F6: return "F6";
                case KeyCode.F7: return "F7";
                case KeyCode.F8: return "F8";
                case KeyCode.F9: return "F9";
                case KeyCode.F10: return "F10";
                case KeyCode.F11: return "F11";
                case KeyCode.F12: return "F12";
                case KeyCode.Alpha0: return "0";
                case KeyCode.Alpha1: return "1";
                case KeyCode.Alpha2: return "2";
                case KeyCode.Alpha3: return "3";
                case KeyCode.Alpha4: return "4";
                case KeyCode.Alpha5: return "5";
                case KeyCode.Alpha6: return "6";
                case KeyCode.Alpha7: return "7";
                case KeyCode.Alpha8: return "8";
                case KeyCode.Alpha9: return "9";
                default: return key.ToString().ToUpper();
            }
        }
    }
}
