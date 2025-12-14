using UnityEngine;
using UnityEngine.UI;

namespace Teramyyd.UI
{
    /// <summary>
    /// Manages global UI panels (settings, controls, audio, video menus) that persist across ship changes.
    /// Place on a GameObject in the base scene, NOT in ship prefabs.
    /// </summary>
    public class GlobalUIManager : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] GameObject settingsMenuPanel;
        [SerializeField] GameObject controlsSettingsPanel;
        [SerializeField] GameObject audioSettingsPanel;
        [SerializeField] GameObject videoSettingsPanel;

        [Header("Game State")]
        [SerializeField] bool pauseGameWhenInSettings = true;

        [Header("Debug")]
        [SerializeField] bool debugLog = false;

        // Singleton for easy access
        private static GlobalUIManager _instance;
        public static GlobalUIManager Instance => _instance;

        // Track which panel is currently open
        private GameObject _currentPanel;
        private bool _wasGamePaused;

        public enum PanelType
        {
            None,
            SettingsMenu,
            ControlsSettings,
            AudioSettings,
            VideoSettings
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[GlobalUIManager] Multiple instances detected. Destroying duplicate on {gameObject.name}");
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Ensure all panels start disabled
            HideAllPanels();
        }

        void Start()
        {
            LogDebug("GlobalUIManager initialized");
        }

        /// <summary>
        /// Show a specific panel, hiding all others
        /// </summary>
        public void ShowPanel(PanelType panelType)
        {
            GameObject targetPanel = GetPanelByType(panelType);
            
            if (targetPanel == null && panelType != PanelType.None)
            {
                Debug.LogError($"[GlobalUIManager] Panel reference not set for {panelType}. Assign in Inspector.");
                return;
            }

            LogDebug($"Showing panel: {panelType}");

            // If opening a settings panel for the first time, pause game
            if (_currentPanel == null && panelType != PanelType.None && pauseGameWhenInSettings)
            {
                PauseGame();
            }

            // Hide current panel
            if (_currentPanel != null)
            {
                _currentPanel.SetActive(false);
            }

            // Show new panel
            _currentPanel = targetPanel;
            if (_currentPanel != null)
            {
                _currentPanel.SetActive(true);
            }

            // If closing all panels, resume game
            if (_currentPanel == null && pauseGameWhenInSettings)
            {
                ResumeGame();
            }
        }

        /// <summary>
        /// Show the main settings menu
        /// </summary>
        public void ShowSettingsMenu()
        {
            ShowPanel(PanelType.SettingsMenu);
        }

        /// <summary>
        /// Show the controls settings panel
        /// </summary>
        public void ShowControlsSettings()
        {
            ShowPanel(PanelType.ControlsSettings);
        }

        /// <summary>
        /// Show the audio settings panel
        /// </summary>
        public void ShowAudioSettings()
        {
            ShowPanel(PanelType.AudioSettings);
        }

        /// <summary>
        /// Show the video settings panel
        /// </summary>
        public void ShowVideoSettings()
        {
            ShowPanel(PanelType.VideoSettings);
        }

        /// <summary>
        /// Close all panels and return to game
        /// </summary>
        public void CloseAllPanels()
        {
            ShowPanel(PanelType.None);
        }

        /// <summary>
        /// Go back to previous panel (settings menu) or close if already at main menu
        /// </summary>
        public void GoBack()
        {
            // If in a sub-panel, return to main settings menu
            if (_currentPanel == controlsSettingsPanel || 
                _currentPanel == audioSettingsPanel || 
                _currentPanel == videoSettingsPanel)
            {
                ShowSettingsMenu();
            }
            // If in main settings menu, close everything
            else if (_currentPanel == settingsMenuPanel)
            {
                CloseAllPanels();
            }
        }

        void HideAllPanels()
        {
            if (settingsMenuPanel != null) settingsMenuPanel.SetActive(false);
            if (controlsSettingsPanel != null) controlsSettingsPanel.SetActive(false);
            if (audioSettingsPanel != null) audioSettingsPanel.SetActive(false);
            if (videoSettingsPanel != null) videoSettingsPanel.SetActive(false);
            _currentPanel = null;
        }

        GameObject GetPanelByType(PanelType type)
        {
            switch (type)
            {
                case PanelType.SettingsMenu: return settingsMenuPanel;
                case PanelType.ControlsSettings: return controlsSettingsPanel;
                case PanelType.AudioSettings: return audioSettingsPanel;
                case PanelType.VideoSettings: return videoSettingsPanel;
                case PanelType.None: return null;
                default: return null;
            }
        }

        void PauseGame()
        {
            _wasGamePaused = Time.timeScale == 0f;
            if (!_wasGamePaused)
            {
                Time.timeScale = 0f;
                LogDebug("Game paused");
            }
        }

        void ResumeGame()
        {
            if (!_wasGamePaused)
            {
                Time.timeScale = 1f;
                LogDebug("Game resumed");
            }
        }

        void LogDebug(string message)
        {
            if (!debugLog) return;
            Debug.Log($"[GlobalUIManager] {message}");
            FileLogger.Log($"[GlobalUIManager] {message}", "GlobalUI");
        }

        // Public API for buttons to call
        public void OnSettingsButtonClicked() => ShowSettingsMenu();
        public void OnControlsButtonClicked() => ShowControlsSettings();
        public void OnAudioButtonClicked() => ShowAudioSettings();
        public void OnVideoButtonClicked() => ShowVideoSettings();
        public void OnBackButtonClicked() => GoBack();
        public void OnCloseButtonClicked() => CloseAllPanels();

        #if UNITY_EDITOR
        void OnValidate()
        {
            // Helper validation in editor
            if (settingsMenuPanel == null) Debug.LogWarning("[GlobalUIManager] Settings Menu Panel not assigned");
            if (controlsSettingsPanel == null) Debug.LogWarning("[GlobalUIManager] Controls Settings Panel not assigned");
            if (audioSettingsPanel == null) Debug.LogWarning("[GlobalUIManager] Audio Settings Panel not assigned");
            if (videoSettingsPanel == null) Debug.LogWarning("[GlobalUIManager] Video Settings Panel not assigned");
        }
        #endif
    }
}
