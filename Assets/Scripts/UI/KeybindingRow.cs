using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Teramyyd.UI
{
    /// <summary>
    /// Represents a single row in the controls settings panel showing an action and its keybinding.
    /// Handles rebinding with modifier key detection (Ctrl, Shift, Alt pressed during rebind).
    /// </summary>
    public class KeybindingRow : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] TextMeshProUGUI actionLabel;
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

        public void UpdateDisplay(KeyBindingData binding)
        {
            _currentBinding = binding;

            if (keyButtonText != null)
            {
                keyButtonText.text = binding.ToString(); // Shows "Ctrl+Shift+F1" format
                keyButtonText.color = normalColor;
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
                keyButtonText.text = "Press key combo...";
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
                keyButtonText.text = _currentBinding.ToString();
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
                keyButtonText.text = _currentBinding.ToString();
                keyButtonText.color = normalColor;
            }
        }
    }
}
