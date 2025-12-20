using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Teramyyd.UI.Editor
{
    /// <summary>
    /// Sets up the ControlsSettingsPanel with ScrollView and wires all references
    /// </summary>
    public static class ControlsSettingsPanelSetupTool
    {
        [MenuItem("Tools/UI Setup/Setup Controls Settings Panel", priority = 101)]
        public static void SetupControlsSettingsPanel()
        {
            // Find ControlsSettingsPanel in scene
            GameObject controlsPanel = GameObject.Find("ControlsSettingsPanel");
            
            if (controlsPanel == null)
            {
                // Try to find it under GlobalUIManager
                var globalUIManager = GameObject.Find("GlobalUIManager");
                if (globalUIManager != null)
                {
                    Transform controlsPanelTransform = globalUIManager.transform.Find("ControlsSettingsPanel");
                    if (controlsPanelTransform != null)
                        controlsPanel = controlsPanelTransform.gameObject;
                }
            }

            if (controlsPanel == null)
            {
                Debug.LogError("[ControlsSettingsPanelSetupTool] ControlsSettingsPanel not found in scene. Please create it first under GlobalUICanvas → GlobalUIManager.");
                return;
            }

            Debug.Log("[ControlsSettingsPanelSetupTool] Setting up ControlsSettingsPanel...");

            // Step 1: Ensure it stretches properly
            RectTransform panelRect = controlsPanel.GetComponent<RectTransform>();
            if (panelRect == null)
                panelRect = controlsPanel.AddComponent<RectTransform>();
            
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;

            // Step 2: Create or find ScrollView
            Transform scrollViewTransform = controlsPanel.transform.Find("Scroll View");
            GameObject scrollView;

            if (scrollViewTransform != null)
            {
                scrollView = scrollViewTransform.gameObject;
                Debug.Log("[ControlsSettingsPanelSetupTool] Found existing Scroll View");
            }
            else
            {
                scrollView = CreateScrollView(controlsPanel);
                Debug.Log("[ControlsSettingsPanelSetupTool] Created new Scroll View");
            }

            // Step 3: Get Content container
            Transform viewportTransform = scrollView.transform.Find("Viewport");
            if (viewportTransform == null)
            {
                Debug.LogError("[ControlsSettingsPanelSetupTool] Viewport not found in Scroll View!");
                return;
            }

            Transform contentTransform = viewportTransform.Find("Content");
            if (contentTransform == null)
            {
                Debug.LogError("[ControlsSettingsPanelSetupTool] Content not found in Viewport!");
                return;
            }

            GameObject content = contentTransform.gameObject;

            // Step 4: Configure Content
            ConfigureContent(content);

            // Step 5: Add/Get ControlsSettingsPanel script
            ControlsSettingsPanel panelScript = controlsPanel.GetComponent<ControlsSettingsPanel>();
            if (panelScript == null)
            {
                panelScript = controlsPanel.AddComponent<ControlsSettingsPanel>();
                Debug.Log("[ControlsSettingsPanelSetupTool] Added ControlsSettingsPanel component");
            }

            // Step 6: Load KeybindingRow prefab
            GameObject rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/KeybindingRow.prefab");
            if (rowPrefab == null)
            {
                Debug.LogWarning("[ControlsSettingsPanelSetupTool] KeybindingRow prefab not found at Assets/Prefabs/UI/KeybindingRow.prefab. Please create it first using 'Tools → UI Setup → Create Controls Panel Setup'");
            }

            // Step 7: Find Back button
            Button backButton = null;
            Transform backButtonTransform = controlsPanel.transform.Find("BackButton");
            if (backButtonTransform != null)
            {
                backButton = backButtonTransform.GetComponent<Button>();
            }

            // Step 8: Wire up references using SerializedObject
            SerializedObject serializedPanel = new SerializedObject(panelScript);
            serializedPanel.FindProperty("contentContainer").objectReferenceValue = content.transform;
            serializedPanel.FindProperty("rowPrefab").objectReferenceValue = rowPrefab;
            serializedPanel.FindProperty("resetButton").objectReferenceValue = null; // User can wire this manually if needed
            serializedPanel.FindProperty("backButton").objectReferenceValue = backButton;
            serializedPanel.ApplyModifiedProperties();

            Debug.Log("[ControlsSettingsPanelSetupTool] Setup complete!");
            Debug.Log($"  - Content Container: {(content != null ? "✓" : "✗")}");
            Debug.Log($"  - Row Prefab: {(rowPrefab != null ? "✓" : "✗ (run Create Controls Panel Setup first)")}");
            Debug.Log($"  - Back Button: {(backButton != null ? "✓" : "✗ (optional)")}");

            // Select the panel
            Selection.activeGameObject = controlsPanel;
            EditorGUIUtility.PingObject(controlsPanel);
        }

        private static GameObject CreateScrollView(GameObject parent)
        {
            // Create Scroll View
            GameObject scrollView = new GameObject("Scroll View");
            scrollView.transform.SetParent(parent.transform, false);

            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            // Add margins: Top=200 (below top third), Bottom=50, Left=50, Right=50
            scrollRect.offsetMin = new Vector2(50, 50); // Left, Bottom
            scrollRect.offsetMax = new Vector2(-50, -200); // Right (negative), Top (negative)
            scrollRect.anchoredPosition = Vector2.zero;

            Image scrollImage = scrollView.AddComponent<Image>();
            scrollImage.color = new Color(0.1f, 0.1f, 0.1f, 0.3f);

            ScrollRect scrollComponent = scrollView.AddComponent<ScrollRect>();
            scrollComponent.horizontal = false;
            scrollComponent.vertical = true;
            scrollComponent.movementType = ScrollRect.MovementType.Clamped;
            scrollComponent.scrollSensitivity = 20;

            // Create Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);

            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;

            Image viewportMask = viewport.AddComponent<Image>();
            viewportMask.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
            viewportMask.type = Image.Type.Sliced;

            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Create Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            contentRect.anchoredPosition = Vector2.zero;

            // Create Scrollbar
            GameObject scrollbar = new GameObject("Scrollbar Vertical");
            scrollbar.transform.SetParent(scrollView.transform, false);

            RectTransform scrollbarRect = scrollbar.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 1);
            scrollbarRect.sizeDelta = new Vector2(20, 0);
            scrollbarRect.anchoredPosition = Vector2.zero;

            Image scrollbarImage = scrollbar.AddComponent<Image>();
            scrollbarImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            scrollbarImage.type = Image.Type.Sliced;
            scrollbarImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            Scrollbar scrollbarComponent = scrollbar.AddComponent<Scrollbar>();
            scrollbarComponent.direction = Scrollbar.Direction.BottomToTop;

            // Create Sliding Area
            GameObject slidingArea = new GameObject("Sliding Area");
            slidingArea.transform.SetParent(scrollbar.transform, false);

            RectTransform slidingAreaRect = slidingArea.AddComponent<RectTransform>();
            slidingAreaRect.anchorMin = Vector2.zero;
            slidingAreaRect.anchorMax = Vector2.one;
            slidingAreaRect.sizeDelta = new Vector2(-20, -20);
            slidingAreaRect.anchoredPosition = Vector2.zero;

            // Create Handle
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(slidingArea.transform, false);

            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.sizeDelta = new Vector2(20, 20);
            handleRect.anchoredPosition = Vector2.zero;

            Image handleImage = handle.AddComponent<Image>();
            handleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            handleImage.type = Image.Type.Sliced;
            handleImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);

            // Wire up ScrollRect
            scrollComponent.viewport = viewportRect;
            scrollComponent.content = contentRect;
            scrollComponent.verticalScrollbar = scrollbarComponent;
            scrollbarComponent.handleRect = handleRect;
            scrollbarComponent.targetGraphic = handleImage;

            return scrollView;
        }

        private static void ConfigureContent(GameObject content)
        {
            // Add Vertical Layout Group
            VerticalLayoutGroup layoutGroup = content.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = content.AddComponent<VerticalLayoutGroup>();
            }

            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 5;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);

            // Add Content Size Fitter
            ContentSizeFitter sizeFitter = content.GetComponent<ContentSizeFitter>();
            if (sizeFitter == null)
            {
                sizeFitter = content.AddComponent<ContentSizeFitter>();
            }

            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Set RectTransform
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;

            Debug.Log("[ControlsSettingsPanelSetupTool] Configured Content with VerticalLayoutGroup and ContentSizeFitter");
        }
    }
}
