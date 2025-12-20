using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Teramyyd.UI
{
    /// <summary>
    /// Dynamically generates controls settings from keybindings.json with modifier support
    /// </summary>
    public class ControlsSettingsPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform contentContainer;
        [SerializeField] GameObject rowPrefab;
        [SerializeField] Button resetButton;
        [SerializeField] Button backButton;

        [Header("Visual Settings")]
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color listeningColor = Color.yellow;
        [SerializeField] Color conflictColor = Color.red;

        [Header("Row Styling")]
        [SerializeField] TMP_FontAsset rowFont;
        [SerializeField] Material fontMaterial;
        [SerializeField] Color actionLabelColor = Color.white;
        [SerializeField] float actionLabelFontSize = 18f;
        [SerializeField] Color keyButtonTextColor = Color.white;
        [SerializeField] float keyButtonFontSize = 16f;
        [SerializeField] Color rowBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        [SerializeField] Color keyButtonBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        private Dictionary<string, KeyBindingData> _keybindings = new Dictionary<string, KeyBindingData>();
        private List<KeybindingRow> _rows = new List<KeybindingRow>();
        private KeybindingRow _currentListeningRow;
        private string _keybindingsPath;

        private void Awake()
        {
            _keybindingsPath = Path.Combine(Application.dataPath, "Resources", "keybindings.json");
        }

        private void OnEnable()
        {
            LoadKeybindings();
            GenerateControlsUI();

            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetToDefaults);
            if (backButton != null)
                backButton.onClick.AddListener(OnBack);
        }

        private void OnDisable()
        {
            if (resetButton != null)
                resetButton.onClick.RemoveListener(OnResetToDefaults);
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBack);

            if (_currentListeningRow != null)
            {
                _currentListeningRow.StopListening();
                _currentListeningRow = null;
            }
        }

        private void Update()
        {
            if (_currentListeningRow != null)
            {
                CheckForKeyInput();
            }
        }

        private void LoadKeybindings()
        {
            _keybindings.Clear();

            if (!File.Exists(_keybindingsPath))
            {
                Debug.LogError($"[ControlsSettingsPanel] Keybindings file not found: {_keybindingsPath}");
                return;
            }

            try
            {
                string json = File.ReadAllText(_keybindingsPath);
                var data = JsonUtility.FromJson<KeybindingsWrapper>("{\"items\":" + json + "}");

                // Parse each keybinding entry
                string[] lines = json.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim().TrimEnd(',');
                    if (trimmed.StartsWith("\"") && trimmed.Contains(":"))
                    {
                        int colonIndex = trimmed.IndexOf(':');
                        string key = trimmed.Substring(1, trimmed.IndexOf("\"", 1) - 1);
                        string valueStr = trimmed.Substring(colonIndex + 1).Trim().Trim('"', ',');

                        // Skip non-key entries (floats, bools)
                        if (valueStr.Contains(".") || valueStr == "true" || valueStr == "false")
                            continue;

                        _keybindings[key] = ParseKeyBinding(valueStr);
                    }
                }

                Debug.Log($"[ControlsSettingsPanel] Loaded {_keybindings.Count} keybindings");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlsSettingsPanel] Failed to load keybindings: {ex.Message}");
            }
        }

        private KeyBindingData ParseKeyBinding(string value)
        {
            var data = new KeyBindingData();

            // Check for modifiers
            if (value.Contains("+"))
            {
                string[] parts = value.Split('+');
                foreach (string part in parts)
                {
                    string trimmed = part.Trim().ToLower();
                    if (trimmed == "ctrl" || trimmed == "control")
                        data.ctrl = true;
                    else if (trimmed == "shift")
                        data.shift = true;
                    else if (trimmed == "alt")
                        data.alt = true;
                    else
                        data.key = ParseKeyCode(part.Trim());
                }
            }
            else
            {
                data.key = ParseKeyCode(value);
            }

            return data;
        }

        private KeyCode ParseKeyCode(string keyString)
        {
            if (Enum.TryParse(keyString, true, out KeyCode keyCode))
                return keyCode;
            return KeyCode.None;
        }

        private void GenerateControlsUI()
        {
            // Clear existing rows
            foreach (var row in _rows)
            {
                if (row != null && row.gameObject != null)
                    Destroy(row.gameObject);
            }
            _rows.Clear();

            if (contentContainer == null)
            {
                Debug.LogError("[ControlsSettingsPanel] Content container not assigned!");
                return;
            }

            if (rowPrefab == null)
            {
                Debug.LogError("[ControlsSettingsPanel] Row prefab not assigned!");
                return;
            }

            // Generate rows for each keybinding
            foreach (var kvp in _keybindings.OrderBy(x => x.Key))
            {
                GameObject rowObj = Instantiate(rowPrefab, contentContainer);
                KeybindingRow row = rowObj.GetComponent<KeybindingRow>();

                if (row != null)
                {
                    string displayName = FormatDisplayName(kvp.Key);
                    row.Initialize(kvp.Key, displayName, kvp.Value, this);
                    ApplyRowStyling(rowObj, row);
                    _rows.Add(row);
                }
                else
                {
                    Debug.LogWarning($"[ControlsSettingsPanel] Row prefab missing KeybindingRow component");
                }
            }
        }

        private void ApplyRowStyling(GameObject rowObj, KeybindingRow row)
        {
            // Apply row background color
            Image rowImage = rowObj.GetComponent<Image>();
            if (rowImage != null)
            {
                rowImage.color = rowBackgroundColor;
            }

            // Get references to child components
            TextMeshProUGUI actionLabel = rowObj.transform.Find("ActionLabel")?.GetComponent<TextMeshProUGUI>();
            Transform keyButton = rowObj.transform.Find("KeyButton");
            TextMeshProUGUI keyButtonText = keyButton?.Find("KeyButtonText")?.GetComponent<TextMeshProUGUI>();

            // Style Action Label
            if (actionLabel != null)
            {
                if (rowFont != null) actionLabel.font = rowFont;
                actionLabel.color = actionLabelColor;
                actionLabel.fontSize = actionLabelFontSize;
                // Apply font material if specified, otherwise clear to use default
                if (fontMaterial != null)
                    actionLabel.fontSharedMaterial = fontMaterial;
                else
                    actionLabel.fontSharedMaterial = null;
            }

            // Style Key Button
            if (keyButton != null)
            {
                Image buttonBg = keyButton.GetComponent<Image>();
                if (buttonBg != null)
                {
                    buttonBg.color = keyButtonBackgroundColor;
                }
            }

            if (keyButtonText != null)
            {
                if (rowFont != null) keyButtonText.font = rowFont;
                keyButtonText.color = keyButtonTextColor;
                keyButtonText.fontSize = keyButtonFontSize;
                // Apply font material if specified, otherwise clear to use default
                if (fontMaterial != null)
                    keyButtonText.fontSharedMaterial = fontMaterial;
                else
                    keyButtonText.fontSharedMaterial = null;
            }
        }

        private string FormatDisplayName(string key)
        {
            // Convert camelCase to Display Name
            string result = "";
            for (int i = 0; i < key.Length; i++)
            {
                if (i > 0 && char.IsUpper(key[i]))
                    result += " ";
                result += i == 0 ? char.ToUpper(key[i]) : key[i];
            }
            return result;
        }

        public void StartListening(KeybindingRow row)
        {
            if (_currentListeningRow != null)
            {
                _currentListeningRow.StopListening();
            }

            _currentListeningRow = row;
            row.StartListening();
        }

        private void CheckForKeyInput()
        {
            // Escape cancels listening
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _currentListeningRow.StopListening();
                _currentListeningRow = null;
                return;
            }

            // Check for any key press
            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    // Ignore modifier keys themselves and mouse buttons
                    if (keyCode == KeyCode.LeftControl || keyCode == KeyCode.RightControl ||
                        keyCode == KeyCode.LeftShift || keyCode == KeyCode.RightShift ||
                        keyCode == KeyCode.LeftAlt || keyCode == KeyCode.RightAlt)
                        continue;
                    
                    if (keyCode >= KeyCode.Mouse0 && keyCode <= KeyCode.Mouse6)
                        continue;
                    if (keyCode == KeyCode.None)
                        continue;

                    ApplyNewKeybinding(keyCode);
                    return;
                }
            }
        }

        private void ApplyNewKeybinding(KeyCode newKey)
        {
            if (_currentListeningRow == null)
                return;

            string actionId = _currentListeningRow.ActionId;

            // Detect modifier keys that are currently being held
            var data = new KeyBindingData
            {
                key = newKey,
                ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
                shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
                alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)
            };

            // Check for conflicts
            foreach (var kvp in _keybindings)
            {
                if (kvp.Key == actionId)
                    continue;

                if (kvp.Value.Equals(data))
                {
                    Debug.LogWarning($"[ControlsSettingsPanel] Key conflict: {data.ToString()} already assigned to {kvp.Key}");
                    _currentListeningRow.ShowConflict(FormatDisplayName(kvp.Key));
                    return;
                }
            }

            // Update keybinding
            _keybindings[actionId] = data;
            _currentListeningRow.UpdateDisplay(data);
            _currentListeningRow.StopListening();
            _currentListeningRow = null;

            SaveKeybindings();
        }

        private void SaveKeybindings()
        {
            try
            {
                // Read original file to preserve formatting and non-key values
                string originalJson = File.ReadAllText(_keybindingsPath);
                string[] lines = originalJson.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                List<string> newLines = new List<string>();

                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("\"") && trimmed.Contains(":"))
                    {
                        int colonIndex = trimmed.IndexOf(':');
                        string key = trimmed.Substring(1, trimmed.IndexOf("\"", 1) - 1);

                        if (_keybindings.ContainsKey(key))
                        {
                            // Replace with updated keybinding
                            string indent = line.Substring(0, line.IndexOf('"'));
                            string keyString = KeyBindingDataToString(_keybindings[key]);
                            bool hasComma = trimmed.EndsWith(",");
                            newLines.Add($"{indent}\"{key}\": \"{keyString}\"{(hasComma ? "," : "")}");
                        }
                        else
                        {
                            // Keep original line (for floats, bools, etc.)
                            newLines.Add(line);
                        }
                    }
                    else
                    {
                        newLines.Add(line);
                    }
                }

                string newJson = string.Join("\n", newLines);
                File.WriteAllText(_keybindingsPath, newJson);

                Debug.Log("[ControlsSettingsPanel] Keybindings saved successfully");

                // Immediately reload keybindings in the active game systems
                KeyBindingConfig keyBindingConfig = KeyBindingConfig.Instance;
                if (keyBindingConfig != null)
                {
                    keyBindingConfig.ReloadKeybindings();
                    Debug.Log("[ControlsSettingsPanel] Keybindings reloaded - changes active immediately");
                }
                else
                {
                    Debug.LogWarning("[ControlsSettingsPanel] KeyBindingConfig not found - changes will apply on next game restart");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlsSettingsPanel] Failed to save keybindings: {ex.Message}");
            }
        }

        private string KeyBindingDataToString(KeyBindingData data)
        {
            string result = "";

            if (data.ctrl)
                result += "Ctrl+";
            if (data.shift)
                result += "Shift+";
            if (data.alt)
                result += "Alt+";

            result += data.key.ToString();

            return result;
        }

        private void OnResetToDefaults()
        {
            // TODO: Load default keybindings from a separate file or resource
            Debug.Log("[ControlsSettingsPanel] Reset to defaults requested");
            LoadKeybindings();
            GenerateControlsUI();
        }

        private void OnBack()
        {
            GlobalUIManager.Instance?.GoBack();
        }

        [Serializable]
        private class KeybindingsWrapper
        {
            public Dictionary<string, string> items;
        }
    }
}
