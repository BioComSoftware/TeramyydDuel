using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Runtime components for mouse control UI elements
namespace Teramyyd.UI
{
    // Component to store slider reference and load values
    public class MouseControlSlider : MonoBehaviour
    {
        private static string _logPath;
        
        public string settingKey;
        public Slider slider;
        public TextMeshProUGUI valueText;
        
        private void Start()
        {
            Log($"[MouseControlSlider] Starting component for {settingKey}");
            
            if (slider == null)
            {
                Log($"[MouseControlSlider] ERROR: slider is null for {settingKey}");
                return;
            }
            
            if (valueText == null)
            {
                Log($"[MouseControlSlider] ERROR: valueText is null for {settingKey}");
                return;
            }
            
            LoadValue();
            slider.onValueChanged.AddListener(OnSliderValueChanged);
            Log($"[MouseControlSlider] Listener added for {settingKey}");
        }
        
        private void OnSliderValueChanged(float value)
        {
            valueText.text = value.ToString("F1");
            Log($"[MouseControlSlider] {settingKey} changed to {value}");
            SaveValue(value);
        }
        
        private void SaveValue(float value)
        {
            string path = System.IO.Path.Combine(Application.dataPath, "Resources", "keybindings.json");
            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                string oldJson = json;
                json = System.Text.RegularExpressions.Regex.Replace(json, 
                    $"\"{settingKey}\":\\s*[0-9.]+", 
                    $"\"{settingKey}\": {value.ToString("F1")}");
                
                if (json != oldJson)
                {
                    System.IO.File.WriteAllText(path, json);
                    Log($"[MouseControlSlider] Saved {settingKey} = {value} to keybindings.json");
                }
                else
                {
                    Log($"[MouseControlSlider] ERROR: Failed to update {settingKey} in JSON - regex didn't match");
                }
                
                // Reload KeyBindingConfig
                var config = Resources.Load<KeyBindingConfig>("KeyBindingConfig");
                if (config != null)
                {
                    config.LoadFromJSON();
                }
            }
        }
        
        private void LoadValue()
        {
            string path = System.IO.Path.Combine(Application.dataPath, "Resources", "keybindings.json");
            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                var match = System.Text.RegularExpressions.Regex.Match(json, $"\"{settingKey}\":\\s*([0-9.]+)");
                if (match.Success)
                {
                    float value = float.Parse(match.Groups[1].Value);
                    slider.SetValueWithoutNotify(value);
                    valueText.text = value.ToString("F1");
                }
            }
        }
        
        private static void Log(string message)
        {
            if (string.IsNullOrEmpty(_logPath))
            {
                _logPath = System.IO.Path.Combine(Application.dataPath, "Logs", "MouseControlSlider_Runtime.txt");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_logPath));
            }
            
            string logEntry = $"[{System.DateTime.Now:HH:mm:ss}] {message}\n";
            Debug.Log(message);
            System.IO.File.AppendAllText(_logPath, logEntry);
        }
    }
    
    // Component for dropdown controls
    public class MouseControlDropdown : MonoBehaviour
    {
        public string settingKey;
        public TMP_Dropdown dropdown;
        
        private void Start()
        {
            LoadValue();
            dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }
        
        private void OnDropdownValueChanged(int index)
        {
            string value = dropdown.options[index].text;
            SaveValue(value);
        }
        
        private void SaveValue(string value)
        {
            string path = System.IO.Path.Combine(Application.dataPath, "Resources", "keybindings.json");
            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                json = System.Text.RegularExpressions.Regex.Replace(json, 
                    $"\"{settingKey}\":\\s*\"[^\"]*\"", 
                    $"\"{settingKey}\": \"{value}\"");
                System.IO.File.WriteAllText(path, json);
                
                // Reload KeyBindingConfig
                var config = Resources.Load<KeyBindingConfig>("KeyBindingConfig");
                if (config != null)
                {
                    config.LoadFromJSON();
                }
            }
        }
        
        private void LoadValue()
        {
            string path = System.IO.Path.Combine(Application.dataPath, "Resources", "keybindings.json");
            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                var match = System.Text.RegularExpressions.Regex.Match(json, $"\"{settingKey}\":\\s*\"([^\"]+)\"");
                if (match.Success)
                {
                    string value = match.Groups[1].Value;
                    for (int i = 0; i < dropdown.options.Count; i++)
                    {
                        if (dropdown.options[i].text == value)
                        {
                            dropdown.SetValueWithoutNotify(i);
                            break;
                        }
                    }
                }
            }
        }
    }
    
    // Component for toggle controls
    public class MouseControlToggle : MonoBehaviour
    {
        public string settingKey;
        public Toggle toggle;
        
        private void Start()
        {
            LoadValue();
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
        
        private void OnToggleValueChanged(bool isOn)
        {
            SaveValue(isOn);
        }
        
        private void SaveValue(bool value)
        {
            string path = System.IO.Path.Combine(Application.dataPath, "Resources", "keybindings.json");
            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                json = System.Text.RegularExpressions.Regex.Replace(json, 
                    $"\"{settingKey}\":\\s*(true|false)", 
                    $"\"{settingKey}\": {value.ToString().ToLower()}");
                System.IO.File.WriteAllText(path, json);
                
                // Reload KeyBindingConfig
                var config = Resources.Load<KeyBindingConfig>("KeyBindingConfig");
                if (config != null)
                {
                    config.LoadFromJSON();
                }
            }
        }
        
        private void LoadValue()
        {
            string path = System.IO.Path.Combine(Application.dataPath, "Resources", "keybindings.json");
            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                var match = System.Text.RegularExpressions.Regex.Match(json, $"\"{settingKey}\":\\s*(true|false)");
                if (match.Success)
                {
                    bool value = match.Groups[1].Value == "true";
                    toggle.SetIsOnWithoutNotify(value);
                }
            }
        }
    }
}
