using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;

namespace Teramyyd.UI.Editor
{
    /// <summary>
    /// Creates the KeybindingRow prefab and ControlsSettingsPanel structure automatically
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

            Debug.Log("[ControlsPanelSetupTool] Starting Controls Panel setup...");

            // Create KeybindingRow prefab
            GameObject rowPrefab = CreateKeybindingRowPrefab(canvas);
            
            if (rowPrefab != null)
            {
                Debug.Log($"[ControlsPanelSetupTool] KeybindingRow prefab created successfully at: Assets/Prefabs/UI/KeybindingRow.prefab");
                
                // Select the prefab in the Project window
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/KeybindingRow.prefab");
                EditorGUIUtility.PingObject(Selection.activeObject);
            }

            Debug.Log("[ControlsPanelSetupTool] Setup complete!");
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

            // ========== Step 3: Create Modifier Dropdown ==========
            GameObject dropdown = CreateTMPDropdown("ModifierDropdown", row.transform);
            
            LayoutElement dropdownLayout = dropdown.AddComponent<LayoutElement>();
            dropdownLayout.preferredWidth = 150;
            dropdownLayout.flexibleWidth = 0;

            TMP_Dropdown tmpDropdown = dropdown.GetComponent<TMP_Dropdown>();
            tmpDropdown.options.Clear();
            tmpDropdown.options.Add(new TMP_Dropdown.OptionData("None"));

            // Style dropdown
            Image dropdownBg = dropdown.GetComponent<Image>();
            dropdownBg.color = new Color(0.16f, 0.16f, 0.16f, 1f);

            // ========== Step 4: Create Key Button ==========
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
            buttonLayout.preferredWidth = 120;
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

            // ========== Step 5: Create Listening Indicator ==========
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

            // ========== Step 6: Add KeybindingRow Component ==========
            var keybindingRow = row.AddComponent<Teramyyd.UI.KeybindingRow>();

            // Use SerializedObject to set private fields
            SerializedObject serializedRow = new SerializedObject(keybindingRow);
            serializedRow.FindProperty("actionLabel").objectReferenceValue = labelText;
            serializedRow.FindProperty("modifierDropdown").objectReferenceValue = tmpDropdown;
            serializedRow.FindProperty("keyButton").objectReferenceValue = button;
            serializedRow.FindProperty("keyButtonText").objectReferenceValue = buttonTMP;
            serializedRow.FindProperty("listeningIndicator").objectReferenceValue = indicator;
            serializedRow.FindProperty("normalColor").colorValue = Color.white;
            serializedRow.FindProperty("listeningColor").colorValue = Color.yellow;
            serializedRow.FindProperty("conflictColor").colorValue = Color.red;
            serializedRow.ApplyModifiedProperties();

            // ========== Step 7: Save as Prefab ==========
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

        private static GameObject CreateTMPDropdown(string name, Transform parent)
        {
            // Use Unity's menu command to create a proper TMP Dropdown
            GameObject dropdown = new GameObject(name);
            dropdown.transform.SetParent(parent, false);

            RectTransform dropdownRect = dropdown.AddComponent<RectTransform>();
            
            Image dropdownImage = dropdown.AddComponent<Image>();
            dropdownImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            dropdownImage.type = Image.Type.Sliced;
            dropdownImage.color = new Color(0.16f, 0.16f, 0.16f, 1f);

            TMP_Dropdown tmpDropdown = dropdown.AddComponent<TMP_Dropdown>();

            // Create Label
            GameObject label = new GameObject("Label");
            label.transform.SetParent(dropdown.transform, false);
            
            RectTransform labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 6);
            labelRect.offsetMax = new Vector2(-25, -7);

            TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
            labelText.text = "None";
            labelText.fontSize = 16;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.white;

            // Create Arrow
            GameObject arrow = new GameObject("Arrow");
            arrow.transform.SetParent(dropdown.transform, false);
            
            RectTransform arrowRect = arrow.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(20, 20);
            arrowRect.anchoredPosition = new Vector2(-15, 0);

            Image arrowImage = arrow.AddComponent<Image>();
            arrowImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");

            // Create Template
            GameObject template = new GameObject("Template");
            template.transform.SetParent(dropdown.transform, false);
            
            RectTransform templateRect = template.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = new Vector2(0, 2);
            templateRect.sizeDelta = new Vector2(0, 150);

            Image templateImage = template.AddComponent<Image>();
            templateImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            templateImage.type = Image.Type.Sliced;

            ScrollRect scrollRect = template.AddComponent<ScrollRect>();

            // Create Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(template.transform, false);
            
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = new Vector2(-18, 0);
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
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 28);

            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();

            // Create Item
            GameObject item = new GameObject("Item");
            item.transform.SetParent(content.transform, false);
            
            RectTransform itemRect = item.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 20);

            Toggle itemToggle = item.AddComponent<Toggle>();

            Image itemBackground = item.AddComponent<Image>();
            itemBackground.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            // Item Checkmark
            GameObject itemCheckmark = new GameObject("Item Checkmark");
            itemCheckmark.transform.SetParent(item.transform, false);
            
            RectTransform checkmarkRect = itemCheckmark.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = new Vector2(0, 1);
            checkmarkRect.sizeDelta = new Vector2(20, 0);
            checkmarkRect.anchoredPosition = new Vector2(10, 0);

            Image checkmarkImage = itemCheckmark.AddComponent<Image>();
            checkmarkImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");

            itemToggle.graphic = checkmarkImage;

            // Item Label
            GameObject itemLabel = new GameObject("Item Label");
            itemLabel.transform.SetParent(item.transform, false);
            
            RectTransform itemLabelRect = itemLabel.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(20, 1);
            itemLabelRect.offsetMax = new Vector2(-10, -2);

            TextMeshProUGUI itemLabelText = itemLabel.AddComponent<TextMeshProUGUI>();
            itemLabelText.text = "Option A";
            itemLabelText.fontSize = 14;
            itemLabelText.alignment = TextAlignmentOptions.Left;
            itemLabelText.color = Color.white;

            // Wire up Dropdown
            tmpDropdown.targetGraphic = dropdownImage;
            tmpDropdown.template = templateRect;
            tmpDropdown.captionText = labelText;
            tmpDropdown.itemText = itemLabelText;

            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;

            template.SetActive(false);

            return dropdown;
        }
    }
}
