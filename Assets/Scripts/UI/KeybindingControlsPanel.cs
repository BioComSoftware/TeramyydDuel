using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;

namespace Teramyyd.UI
{
    /// <summary>
    /// Manages the controls settings panel, populating it with keybinding rows
    /// and handling rebinding interactions via KeyBindingConfig.
    /// </summary>
    public class KeybindingControlsPanel : MonoBehaviour
    {
        private static KeybindingControlsPanel _instance;
        public static KeybindingControlsPanel Instance => _instance;

        [Header("References")]
        [SerializeField] KeyBindingConfig keyBindingConfig;

        [Header("Prefab & Container")]
        [SerializeField] GameObject keybindingRowPrefab;
        [SerializeField] Transform rowsContainer;

        [Header("Section Separation (Optional)")]
        [SerializeField] GameObject sectionHeaderPrefab; // Optional: prefab with TextMeshProUGUI for section titles

        [Header("Footer Buttons")]
        [SerializeField] Button resetToDefaultsButton;
        [SerializeField] Button backButton;

        private Dictionary<string, KeybindingRow> _rows = new Dictionary<string, KeybindingRow>();
        private KeybindingRow _currentListeningRow;
        
        // Define action display info
        private struct ActionInfo
        {
            public string actionId;
            public string displayName;
            public string category;
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[KeybindingControlsPanel] Multiple instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (resetToDefaultsButton != null)
            {
                resetToDefaultsButton.onClick.AddListener(OnResetToDefaults);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }

        void OnEnable()
        {
            PopulateKeybindings();
        }

        void Update()
        {
            if (_currentListeningRow != null && _currentListeningRow.IsListening)
            {
                CheckForKeyInput();
            }
        }

        /// <summary>
        /// Get all available actions with their display info
        /// </summary>
        List<ActionInfo> GetAllActions()
        {
            return new List<ActionInfo>
            {
                // View Controls
                new ActionInfo { actionId = "bridgeView", displayName = "Bridge View", category = "View Controls" },
                new ActionInfo { actionId = "followView", displayName = "Follow View", category = "View Controls" },
                new ActionInfo { actionId = "overheadView", displayName = "Overhead View", category = "View Controls" },
                new ActionInfo { actionId = "bridgeSnap", displayName = "Snap to Bridge", category = "View Controls" },
                new ActionInfo { actionId = "followSnap", displayName = "Snap to Follow", category = "View Controls" },
                new ActionInfo { actionId = "overheadSnap", displayName = "Snap to Overhead", category = "View Controls" },
                
                // Camera Controls
                new ActionInfo { actionId = "lookLeft", displayName = "Look Left", category = "Camera Controls" },
                new ActionInfo { actionId = "lookRight", displayName = "Look Right", category = "Camera Controls" },
                new ActionInfo { actionId = "lookUp", displayName = "Look Up", category = "Camera Controls" },
                new ActionInfo { actionId = "lookDown", displayName = "Look Down", category = "Camera Controls" },
                new ActionInfo { actionId = "zoomIn", displayName = "Zoom In", category = "Camera Controls" },
                new ActionInfo { actionId = "zoomOut", displayName = "Zoom Out", category = "Camera Controls" },
                
                // Weapon Controls
                new ActionInfo { actionId = "fireAllWeapons", displayName = "Fire All Weapons", category = "Combat" },
                
                // UI Controls
                new ActionInfo { actionId = "instrumentZoom", displayName = "Instrument Zoom", category = "UI" },
                
                // Ship Controls
                new ActionInfo { actionId = "engineForward", displayName = "Engine Forward", category = "Ship Controls" },
                new ActionInfo { actionId = "engineReverse", displayName = "Engine Reverse", category = "Ship Controls" },
                new ActionInfo { actionId = "wheelLeft", displayName = "Wheel Left", category = "Ship Controls" },
                new ActionInfo { actionId = "wheelRight", displayName = "Wheel Right", category = "Ship Controls" },
                new ActionInfo { actionId = "liftUp", displayName = "Lift Up", category = "Ship Controls" },
                new ActionInfo { actionId = "liftDown", displayName = "Lift Down", category = "Ship Controls" },
            };
        }
        
        /// <summary>
        /// Get current KeyCode for an action
        /// </summary>
        KeyCode GetKeyForAction(string actionId)
        {
            if (keyBindingConfig == null) return KeyCode.None;
            
            switch (actionId)
            {
                case "bridgeView": return keyBindingConfig.bridgeView;
                case "followView": return keyBindingConfig.followView;
                case "overheadView": return keyBindingConfig.overheadView;
                case "bridgeSnap": return keyBindingConfig.bridgeSnap;
                case "followSnap": return keyBindingConfig.followSnap;
                case "overheadSnap": return keyBindingConfig.overheadSnap;
                case "lookLeft": return keyBindingConfig.lookLeft;
                case "lookRight": return keyBindingConfig.lookRight;
                case "lookUp": return keyBindingConfig.lookUp;
                case "lookDown": return keyBindingConfig.lookDown;
                case "zoomIn": return keyBindingConfig.zoomIn;
                case "zoomOut": return keyBindingConfig.zoomOut;
                case "fireAllWeapons": return keyBindingConfig.fireAllWeapons;
                case "instrumentZoom": return keyBindingConfig.instrumentZoom;
                case "engineForward": return keyBindingConfig.engineForward;
                case "engineReverse": return keyBindingConfig.engineReverse;
                case "wheelLeft": return keyBindingConfig.wheelLeft;
                case "wheelRight": return keyBindingConfig.wheelRight;
                case "liftUp": return keyBindingConfig.liftUp;
                case "liftDown": return keyBindingConfig.liftDown;
                default: return KeyCode.None;
            }
        }

        /// <summary>
        /// Populate the panel with all keybinding rows from KeyBindingConfig
        /// </summary>
        void PopulateKeybindings()
        {
            if (keybindingRowPrefab == null)
            {
                Debug.LogError("[KeybindingControlsPanel] Missing keybindingRowPrefab!");
                return;
            }
            
            if (rowsContainer == null)
            {
                Debug.LogError("[KeybindingControlsPanel] Missing rowsContainer!");
                return;
            }
            
            if (keyBindingConfig == null)
            {
                Debug.LogError("[KeybindingControlsPanel] KeyBindingConfig reference not assigned!");
                return;
            }

            // Clear existing rows
            foreach (Transform child in rowsContainer)
            {
                if (child != null)
                    Destroy(child.gameObject);
            }
            _rows.Clear();

            // Get all actions
            var actions = GetAllActions();
            
            // Organize by category
            var categories = actions.GroupBy(a => a.category).OrderBy(g => g.Key);

            // Create rows by category
            foreach (var categoryGroup in categories)
            {
                // Optional: Create section header
                if (sectionHeaderPrefab != null)
                {
                    GameObject headerObj = Instantiate(sectionHeaderPrefab, rowsContainer);
                    TextMeshProUGUI headerText = headerObj.GetComponentInChildren<TextMeshProUGUI>();
                    if (headerText != null)
                    {
                        headerText.text = categoryGroup.Key;
                    }
                }

                // Create rows for this category
                foreach (var action in categoryGroup)
                {
                    GameObject rowObj = Instantiate(keybindingRowPrefab, rowsContainer);
                    KeybindingRow row = rowObj.GetComponent<KeybindingRow>();

                    if (row != null)
                    {
                        KeyCode currentKey = GetKeyForAction(action.actionId);
                        var binding = new KeyBindingData { key = currentKey, ctrl = false, shift = false, alt = false };
                        // Note: This panel doesn't support modifiers, just passes simple key
                        row.Initialize(action.actionId, action.displayName, binding, null);
                        _rows[action.actionId] = row;
                    }
                    else
                    {
                        Debug.LogError($"[KeybindingControlsPanel] KeybindingRow component not found on prefab for action: {action.actionId}");
                    }
                }
            }

            Debug.Log($"[KeybindingControlsPanel] Populated {_rows.Count} keybinding rows");
        }

        /// <summary>
        /// Start rebinding process for a specific action
        /// </summary>
        public void StartRebinding(string actionId, KeybindingRow row)
        {
            // Stop any existing rebinding
            if (_currentListeningRow != null)
            {
                _currentListeningRow.StopListening();
            }

            _currentListeningRow = row;
            _currentListeningRow.StartListening();

            Debug.Log($"[KeybindingControlsPanel] Started rebinding for action: {actionId}");
        }

        /// <summary>
        /// Check for key input during rebinding
        /// </summary>
        void CheckForKeyInput()
        {
            // Check for any key press
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    // Ignore mouse buttons if you want
                    if (key == KeyCode.Mouse0 || key == KeyCode.Mouse1 || key == KeyCode.Mouse2)
                    {
                        continue;
                    }

                    // Allow Escape to cancel rebinding
                    if (key == KeyCode.Escape)
                    {
                        CancelRebinding();
                        return;
                    }

                    // Apply the new keybinding
                    ApplyNewKeybinding(key);
                    return;
                }
            }
        }

        /// <summary>
        /// Apply the new keybinding to the action
        /// </summary>
        void ApplyNewKeybinding(KeyCode newKey)
        {
            if (_currentListeningRow == null || keyBindingConfig == null) return;

            string actionId = _currentListeningRow.ActionId;

            // Check for conflicts (simple check - see if any other action uses this key)
            string conflictingAction = null;
            foreach (var kvp in _rows)
            {
                if (kvp.Key != actionId)
                {
                    KeyCode existingKey = GetKeyForAction(kvp.Key);
                    if (existingKey == newKey)
                    {
                        conflictingAction = kvp.Key;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(conflictingAction))
            {
                Debug.LogWarning($"[KeybindingControlsPanel] Key {newKey} already bound to {conflictingAction}. This will cause conflicts!");
                
                // Show conflict warning on the conflicting row
                if (_rows.TryGetValue(conflictingAction, out KeybindingRow conflictingRow))
                {
                    conflictingRow.ShowConflict(actionId);
                }
            }

            // Set the new key via KeyBindingConfig
            bool success = keyBindingConfig.SetRuntimeKey(actionId, newKey);

            if (success)
            {
                // Save to JSON
                keyBindingConfig.SaveToJSON();
                
                // Update this row's display
                var binding = new KeyBindingData { key = newKey, ctrl = false, shift = false, alt = false };
                _currentListeningRow.UpdateDisplay(binding);

                Debug.Log($"[KeybindingControlsPanel] Successfully rebound {actionId} to {newKey}");
            }
            else
            {
                Debug.LogError($"[KeybindingControlsPanel] Failed to rebind {actionId} to {newKey}");
            }

            _currentListeningRow.StopListening();
            _currentListeningRow = null;
        }

        /// <summary>
        /// Cancel the current rebinding operation
        /// </summary>
        void CancelRebinding()
        {
            if (_currentListeningRow != null)
            {
                // Restore the original key display
                KeyCode originalKey = GetKeyForAction(_currentListeningRow.ActionId);
                var binding = new KeyBindingData { key = originalKey, ctrl = false, shift = false, alt = false };
                _currentListeningRow.UpdateDisplay(binding);

                _currentListeningRow.StopListening();
                _currentListeningRow = null;

                Debug.Log("[KeybindingControlsPanel] Rebinding cancelled");
            }
        }

        /// <summary>
        /// Reset all keybindings to defaults
        /// </summary>
        void OnResetToDefaults()
        {
            if (keyBindingConfig != null)
            {
                keyBindingConfig.ResetToDefaults();
                keyBindingConfig.SaveToJSON();
                PopulateKeybindings(); // Refresh display
                Debug.Log("[KeybindingControlsPanel] Reset all keybindings to defaults");
            }
        }

        /// <summary>
        /// Handle back button click
        /// </summary>
        void OnBackButtonClicked()
        {
            if (GlobalUIManager.Instance != null)
            {
                GlobalUIManager.Instance.GoBack();
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            if (resetToDefaultsButton != null)
            {
                resetToDefaultsButton.onClick.RemoveListener(OnResetToDefaults);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackButtonClicked);
            }
        }
    }
}
