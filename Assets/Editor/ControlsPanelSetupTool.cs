using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;

namespace Teramyyd.UI.Editor
{
    /// <summary>
    /// Creates complete ControlsSettingsPanel hierarchy and KeybindingRow prefab
    /// </summary>
    public static class ControlsPanelSetupTool
    {
        [MenuItem("Tools/UI Setup/Create Controls Panel Setup", priority = 100)]
        public static void CreateControlsPanelSetup()
        {
            // Check if Canvas exists
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[ControlsPanelSetupTool] No Canvas found in scene. Please create a Canvas first.");
                return;
            }

            Debug.Log("[ControlsPanelSetupTool] Starting Complete Controls Panel setup...");

            // Step 1: Create KeybindingRow prefab
            GameObject rowPrefab = CreateKeybindingRowPrefab(canvas);
            
            if (rowPrefab != null)
            {
                Debug.Log($"[ControlsPanelSetupTool] ✓ KeybindingRow prefab created at: Assets/Prefabs/UI/KeybindingRow.prefab");
            }

            // Step 2: Create ControlsSettingsPanel hierarchy
            GameObject controlsPanel = CreateControlsSettingsPanelHierarchy(canvas, rowPrefab);

            if (controlsPanel != null)
            {
                Debug.Log($"[ControlsPanelSetupTool] ✓ ControlsSettingsPanel created in scene");
            }

            Debug.Log("[ControlsPanelSetupTool] ========== SETUP COMPLETE ==========");
            Debug.Log("[ControlsPanelSetupTool] Created:");
            Debug.Log("[ControlsPanelSetupTool]   - KeybindingRow prefab (Assets/Prefabs/UI/)");
            Debug.Log("[ControlsPanelSetupTool]   - ControlsSettingsPanel (In scene hierarchy)");
            Debug.Log("[ControlsPanelSetupTool]   - ScrollView with Viewport, Content, Scrollbar");
            Debug.Log("[ControlsPanelSetupTool]   - BackButton");
            Debug.Log("[ControlsPanelSetupTool] All references wired automatically!");
        }

        private static GameObject CreateKeybindingRowPrefab(Canvas canvas)
        {
            // Create temporary parent for building the prefab
            GameObject tempParent = new GameObject("TempParent");
            tempParent.transform.SetParent(canvas.transform, false);

            // ========== Step 1: Create Base KeybindingRow ==========
            GameObject row = new GameObject("KeybindingRow");
            row.transform.SetParent(tempParent.transform, false);

            // Add RectTransform
            RectTransform rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(0, 50);
            rowRect.anchoredPosition = Vector2.zero;

            // Add Image for background
            Image rowImage = row.AddComponent<Image>();
            rowImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

            // Add Horizontal Layout Group
            HorizontalLayoutGroup layoutGroup = row.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = true;
            layoutGroup.spacing = 10;
            layoutGroup.padding = new RectOffset(10, 10, 5, 5);

            // Add Layout Element
            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 50;
            rowLayout.flexibleWidth = 1;

            // ========== Step 2: Create Action Label ==========
            GameObject actionLabel = new GameObject("ActionLabel");
            actionLabel.transform.SetParent(row.transform, false);
            
            RectTransform labelRect = actionLabel.AddComponent<RectTransform>();
            
            TextMeshProUGUI labelText = actionLabel.AddComponent<TextMeshProUGUI>();
            labelText.text = "Fire All Weapons";
            labelText.fontSize = 18;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.color = Color.white;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            labelText.enableWordWrapping = false;

            LayoutElement labelLayout = actionLabel.AddComponent<LayoutElement>();
            labelLayout.minWidth = 200;
            labelLayout.flexibleWidth = 1;

            // ========== Step 3: Create Key Button ==========
            GameObject keyButton = new GameObject("KeyButton");
            keyButton.transform.SetParent(row.transform, false);

            RectTransform buttonRect = keyButton.AddComponent<RectTransform>();

            Image buttonImage = keyButton.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            buttonImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            buttonImage.type = Image.Type.Sliced;

            Button button = keyButton.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.highlightedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.pressedColor = new Color(0.59f, 0.59f, 0.59f, 1f);
            colors.disabledColor = new Color(0.39f, 0.39f, 0.39f, 0.5f);
            button.colors = colors;

            LayoutElement buttonLayout = keyButton.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 200;
            buttonLayout.flexibleWidth = 0;

            // Create button text
            GameObject buttonText = new GameObject("KeyButtonText");
            buttonText.transform.SetParent(keyButton.transform, false);

            RectTransform buttonTextRect = buttonText.AddComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.sizeDelta = Vector2.zero;
            buttonTextRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI buttonTMP = buttonText.AddComponent<TextMeshProUGUI>();
            buttonTMP.text = "F";
            buttonTMP.fontSize = 16;
            buttonTMP.alignment = TextAlignmentOptions.Center;
            buttonTMP.color = Color.white;
            buttonTMP.fontStyle = FontStyles.Bold;

            // ========== Step 4: Create Listening Indicator ==========
            GameObject indicator = new GameObject("ListeningIndicator");
            indicator.transform.SetParent(row.transform, false);

            RectTransform indicatorRect = indicator.AddComponent<RectTransform>();
            
            Image indicatorImage = indicator.AddComponent<Image>();
            indicatorImage.color = Color.yellow;
            indicatorImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            LayoutElement indicatorLayout = indicator.AddComponent<LayoutElement>();
            indicatorLayout.preferredWidth = 30;
            indicatorLayout.preferredHeight = 30;
            indicatorLayout.flexibleWidth = 0;

            indicator.SetActive(false);

            // ========== Step 5: Add KeybindingRow Component ==========
            var keybindingRow = row.AddComponent<Teramyyd.UI.KeybindingRow>();

            // Use SerializedObject to set private fields
            SerializedObject serializedRow = new SerializedObject(keybindingRow);
            serializedRow.FindProperty("actionLabel").objectReferenceValue = labelText;
            serializedRow.FindProperty("keyButton").objectReferenceValue = button;
            serializedRow.FindProperty("keyButtonText").objectReferenceValue = buttonTMP;
            serializedRow.FindProperty("listeningIndicator").objectReferenceValue = indicator;
            serializedRow.FindProperty("normalColor").colorValue = Color.white;
            serializedRow.FindProperty("listeningColor").colorValue = Color.yellow;
            serializedRow.FindProperty("conflictColor").colorValue = Color.red;
            serializedRow.ApplyModifiedProperties();

            // ========== Step 6: Save as Prefab ==========
            string prefabFolder = "Assets/Prefabs/UI";
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(prefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }

            string prefabPath = $"{prefabFolder}/KeybindingRow.prefab";
            
            // Check if prefab already exists
            if (File.Exists(prefabPath))
            {
                if (!EditorUtility.DisplayDialog("Prefab Exists", 
                    "KeybindingRow.prefab already exists. Overwrite?", 
                    "Overwrite", "Cancel"))
                {
                    Object.DestroyImmediate(tempParent);
                    return null;
                }
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(row, prefabPath);

            // Cleanup
            Object.DestroyImmediate(tempParent);

            return prefab;
        }

        private static GameObject CreateControlsSettingsPanelHierarchy(Canvas canvas, GameObject rowPrefab)
        {
            // Find or create parent (GlobalUIManager)
            GameObject globalUIManager = GameObject.Find("GlobalUIManager");
            Transform parentTransform;

            if (globalUIManager == null)
            {
                // Try to find it as child of canvas
                Transform canvasUIManager = canvas.transform.Find("GlobalUIManager");
                if (canvasUIManager != null)
                {
                    globalUIManager = canvasUIManager.gameObject;
                    parentTransform = globalUIManager.transform;
                }
                else
                {
                    // Create it directly under canvas
                    Debug.LogWarning("[ControlsPanelSetupTool] GlobalUIManager not found. Creating ControlsSettingsPanel directly under Canvas.");
                    parentTransform = canvas.transform;
                }
            }
            else
            {
                parentTransform = globalUIManager.transform;
            }

            // Check if ControlsSettingsPanel already exists
            Transform existingPanel = parentTransform.Find("ControlsSettingsPanel");
            if (existingPanel != null)
            {
                if (!EditorUtility.DisplayDialog("Panel Exists",
                    "ControlsSettingsPanel already exists. Delete and recreate?",
                    "Recreate", "Cancel"))
                {
                    return null;
                }
                Object.DestroyImmediate(existingPanel.gameObject);
            }

            // Create ControlsSettingsPanel
            GameObject controlsPanel = new GameObject("ControlsSettingsPanel");
            controlsPanel.transform.SetParent(parentTransform, false);

            RectTransform panelRect = controlsPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = controlsPanel.AddComponent<Image>();
            panelImage.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);

            // Create Scroll View
            GameObject scrollView = CreateScrollView(controlsPanel);

            // Get Content reference
            Transform contentTransform = scrollView.transform.Find("Viewport/Content");
            
            // Create Back Button
            GameObject backButton = CreateBackButton(controlsPanel);

            // Add ControlsSettingsPanel script
            ControlsSettingsPanel panelScript = controlsPanel.AddComponent<ControlsSettingsPanel>();

            // Wire up references
            SerializedObject serializedPanel = new SerializedObject(panelScript);
            serializedPanel.FindProperty("contentContainer").objectReferenceValue = contentTransform;
            serializedPanel.FindProperty("rowPrefab").objectReferenceValue = rowPrefab;
            serializedPanel.FindProperty("resetButton").objectReferenceValue = null;
            serializedPanel.FindProperty("backButton").objectReferenceValue = backButton.GetComponent<Button>();
            serializedPanel.ApplyModifiedProperties();

            // Select the panel
            Selection.activeGameObject = controlsPanel;
            EditorGUIUtility.PingObject(controlsPanel);

            return controlsPanel;
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

            // Configure Content Layout
            VerticalLayoutGroup layoutGroup = content.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 5;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);

            ContentSizeFitter sizeFitter = content.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

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

        private static GameObject CreateBackButton(GameObject parent)
        {
            GameObject backButton = new GameObject("BackButton");
            backButton.transform.SetParent(parent.transform, false);

            RectTransform buttonRect = backButton.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0, 1);
            buttonRect.anchorMax = new Vector2(0, 1);
            buttonRect.pivot = new Vector2(0, 1);
            buttonRect.sizeDelta = new Vector2(120, 50);
            buttonRect.anchoredPosition = new Vector2(20, -20);

            Image buttonImage = backButton.AddComponent<Image>();
            buttonImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            buttonImage.type = Image.Type.Sliced;
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            Button button = backButton.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.highlightedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.pressedColor = new Color(0.59f, 0.59f, 0.59f, 1f);
            button.colors = colors;

            // Create button text
            GameObject buttonText = new GameObject("Text");
            buttonText.transform.SetParent(backButton.transform, false);

            RectTransform textRect = buttonText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI buttonTMP = buttonText.AddComponent<TextMeshProUGUI>();
            buttonTMP.text = "Back";
            buttonTMP.fontSize = 18;
            buttonTMP.alignment = TextAlignmentOptions.Center;
            buttonTMP.color = Color.white;
            buttonTMP.fontStyle = FontStyles.Bold;

            return backButton;
        }
    }
}
