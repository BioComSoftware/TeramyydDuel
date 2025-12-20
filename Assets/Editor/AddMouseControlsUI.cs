using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.Linq;

namespace Teramyyd.UI.Editor 
{
    /// <summary>
    /// Editor utility to add mouse control UI elements to the Controls Settings Panel.
    /// Run from Unity menu: Tools > Add Mouse Controls UI
    /// </summary>
    public class AddMouseControlsUI : EditorWindow
    {
        private static TMP_FontAsset _rowFont;
        private static Material _fontMaterial;
        private static Color _actionLabelColor;

        [MenuItem("Tools/Add Mouse Controls UI")]
        public static void AddMouseControlsToPanel()
        {
            // Find the ControlsSettingsPanel in the scene (including inactive objects)
            var panel = Object.FindObjectOfType<Teramyyd.UI.ControlsSettingsPanel>(true);
            if (panel == null)
            {
                EditorUtility.DisplayDialog("Error", "ControlsSettingsPanel not found in scene. Please open the Main scene (Assets/Main.unity) that contains the Controls Settings Panel.", "OK");
                return;
            }

        GameObject panelObject = panel.gameObject;
        
        // Get font and material settings from the panel using SerializedObject
        SerializedObject so = new SerializedObject(panel);
        SerializedProperty fontProp = so.FindProperty("rowFont");
        SerializedProperty materialProp = so.FindProperty("fontMaterial");
        SerializedProperty colorProp = so.FindProperty("actionLabelColor");
        
        _rowFont = fontProp.objectReferenceValue as TMP_FontAsset;
        _fontMaterial = materialProp.objectReferenceValue as Material;
        _actionLabelColor = colorProp.colorValue;
        
        // Find the content container (where keybinding rows are added)
        Transform contentContainer = panelObject.transform.Find("PanelContainer/Scroll View/Viewport/Content");
        if (contentContainer == null)
        {
            // Try alternative paths
            contentContainer = panelObject.transform.Find("Scroll View/Viewport/Content");
            if (contentContainer == null)
            {
                contentContainer = FindChildRecursive(panelObject.transform, "Content");
            }
        }
        
        if (contentContainer == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find Content container in ControlsSettingsPanel. Please check the hierarchy.", "OK");
            return;
        }

        // Remove existing mouse control rows if they exist
        for (int i = contentContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = contentContainer.GetChild(i);
            if (child.name.StartsWith("Row_mouse") || child.name.StartsWith("---") || child.name.Contains("Mouse"))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        string logPath = System.IO.Path.Combine(Application.dataPath, "Logs", "MouseControlsUI_Log.txt");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath));
        System.Text.StringBuilder log = new System.Text.StringBuilder();
        log.AppendLine($"[{System.DateTime.Now}] Adding Mouse Controls UI");
        log.AppendLine($"Content container path: {GetFullPath(contentContainer)}");
        log.AppendLine($"Content container has {contentContainer.childCount} children before adding");
        log.AppendLine($"Content RectTransform - Anchors: ({contentContainer.GetComponent<RectTransform>().anchorMin}, {contentContainer.GetComponent<RectTransform>().anchorMax})");
        log.AppendLine($"Content RectTransform - SizeDelta: {contentContainer.GetComponent<RectTransform>().sizeDelta}");

        // Create a separator
        CreateSeparator(contentContainer, "Mouse Controls");

        // Create Mouse Sensitivity Slider
        CreateSliderRow(contentContainer, "Mouse Sensitivity", "mouseSensitivity", 1f, 10f, 5f);

        // Create Mouse Wheel Sensitivity Slider
        CreateSliderRow(contentContainer, "Mouse Wheel Sensitivity", "mouseWheelSensitivity", 1f, 10f, 5f);

        // Create Mouse Wheel Forward Dropdown
        CreateDropdownRow(contentContainer, "Mouse Wheel Forward", "mouseWheelForward", new string[] { "ZoomIn", "ZoomOut" }, "ZoomIn");

        // Create Mouse Wheel Backward Dropdown
        CreateDropdownRow(contentContainer, "Mouse Wheel Backward", "mouseWheelBackward", new string[] { "ZoomIn", "ZoomOut" }, "ZoomOut");

        // Create Invert Mouse Y Checkbox
        CreateToggleRow(contentContainer, "Invert Mouse Y", "invertMouseY", false);

        log.AppendLine($"\nCreated {contentContainer.childCount} total children");
        log.AppendLine("\nMouse control rows created:");
        for (int i = 0; i < contentContainer.childCount; i++)
        {
            Transform child = contentContainer.GetChild(i);
            if (child.name.Contains("mouse") || child.name.Contains("Mouse") || child.name.StartsWith("---"))
            {
                RectTransform rt = child.GetComponent<RectTransform>();
                log.AppendLine($"  - {child.name}:");
                log.AppendLine($"    Active: {child.gameObject.activeSelf}");
                log.AppendLine($"    Position: {rt.anchoredPosition}");
                log.AppendLine($"    SizeDelta: {rt.sizeDelta}");
                log.AppendLine($"    Anchors: Min({rt.anchorMin}) Max({rt.anchorMax})");
                log.AppendLine($"    Has Image: {child.GetComponent<UnityEngine.UI.Image>() != null}");
                log.AppendLine($"    Has LayoutElement: {child.GetComponent<LayoutElement>() != null}");
            }
        }

        System.IO.File.WriteAllText(logPath, log.ToString());

        // Ensure content container updates its size
        ContentSizeFitter csf = contentContainer.GetComponent<ContentSizeFitter>();
        if (csf != null)
        {
            csf.enabled = false;
            csf.enabled = true;
        }
        
        VerticalLayoutGroup vlg = contentContainer.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            vlg.enabled = false;
            vlg.enabled = true;
        }

        // Force layout rebuild
        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer.GetComponent<RectTransform>());
        
        // Mark scene as dirty and refresh layout
        EditorUtility.SetDirty(panelObject);
        EditorUtility.SetDirty(contentContainer.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("Success", $"Mouse control UI elements added successfully!\n\nLog file created at:\nAssets/Logs/MouseControlsUI_Log.txt\n\nPlease save the scene (Ctrl+S) and test in Play mode.", "OK");
    }

    private static string GetFullPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    private static void CreateSeparator(Transform parent, string title)
    {
        GameObject separator = new GameObject($"--- {title} ---");
        separator.transform.SetParent(parent, false);
        
        RectTransform rt = separator.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 40);
        
        TextMeshProUGUI text = separator.AddComponent<TextMeshProUGUI>();
        text.text = title.ToUpper();
        text.fontSize = 20;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.8f, 0.2f); // Yellow/gold color
        if (_rowFont != null) text.font = _rowFont;
        if (_fontMaterial != null) text.fontMaterial = _fontMaterial;
        
        LayoutElement layout = separator.AddComponent<LayoutElement>();
        layout.minHeight = 40;
        layout.preferredHeight = 40;
    }

    private static void CreateSliderRow(Transform parent, string labelText, string settingKey, float minValue, float maxValue, float defaultValue)
    {
        GameObject row = new GameObject($"Row_{settingKey}");
        row.transform.SetParent(parent, false);
        
        RectTransform rowRT = row.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0, 1);
        rowRT.anchorMax = new Vector2(1, 1);
        rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.sizeDelta = new Vector2(0, 50);
        
        // Add background
        Image rowBg = row.AddComponent<Image>();
        rowBg.color = new Color(0.1f, 0.1f, 0.1f, 0.3f);
        
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 20;
        hlg.padding = new RectOffset(20, 20, 5, 5);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.minHeight = 50;
        rowLayout.preferredHeight = 50;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(row.transform, false);
        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.sizeDelta = new Vector2(250, 40);
        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = 16;
        label.color = _actionLabelColor;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        if (_rowFont != null) label.font = _rowFont;
        if (_fontMaterial != null) label.fontMaterial = _fontMaterial;
        LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 250;
        labelLayout.minWidth = 250;
        labelLayout.flexibleWidth = 0;

        // Slider
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(row.transform, false);
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = defaultValue;
        slider.wholeNumbers = false;
        
        RectTransform sliderRT = sliderObj.GetComponent<RectTransform>();
        sliderRT.sizeDelta = new Vector2(300, 30);
        
        LayoutElement sliderLayout = sliderObj.AddComponent<LayoutElement>();
        sliderLayout.preferredWidth = 300;
        sliderLayout.minWidth = 300;
        sliderLayout.preferredHeight = 30;
        sliderLayout.flexibleWidth = 0;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderObj.transform, false);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.sizeDelta = new Vector2(-20, 0);

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.6f, 1f);
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.sizeDelta = Vector2.zero;

        // Handle Slide Area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRT = handleArea.AddComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.sizeDelta = new Vector2(-20, 0);

        // Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;
        RectTransform handleRT = handle.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(20, 0);

        slider.fillRect = fillRT;
        slider.handleRect = handleRT;

        // Value Text
        GameObject valueObj = new GameObject("Value");
        valueObj.transform.SetParent(row.transform, false);
        RectTransform valueRT = valueObj.AddComponent<RectTransform>();
        valueRT.sizeDelta = new Vector2(60, 40);
        TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
        valueText.text = defaultValue.ToString("F1");
        valueText.fontSize = 16;
        valueText.color = _actionLabelColor;
        valueText.alignment = TextAlignmentOptions.MidlineRight;
        if (_rowFont != null) valueText.font = _rowFont;
        if (_fontMaterial != null) valueText.fontMaterial = _fontMaterial;
        LayoutElement valueLayout = valueObj.AddComponent<LayoutElement>();
        valueLayout.preferredWidth = 60;
        valueLayout.minWidth = 60;
        valueLayout.flexibleWidth = 0;

        // Add listener to update value text
        slider.onValueChanged.AddListener((value) => {
            valueText.text = value.ToString("F1");
            SaveSliderValue(settingKey, value);
        });

        // Store reference for loading
        MouseControlSlider sliderComponent = row.AddComponent<MouseControlSlider>();
        sliderComponent.settingKey = settingKey;
        sliderComponent.slider = slider;
        sliderComponent.valueText = valueText;
    }

    private static void CreateDropdownRow(Transform parent, string labelText, string settingKey, string[] options, string defaultOption)
    {
        GameObject row = new GameObject($"Row_{settingKey}");
        row.transform.SetParent(parent, false);
        
        RectTransform rowRT = row.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0, 1);
        rowRT.anchorMax = new Vector2(1, 1);
        rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.sizeDelta = new Vector2(0, 50);
        
        // Add background
        Image rowBg = row.AddComponent<Image>();
        rowBg.color = new Color(0.1f, 0.1f, 0.1f, 0.3f);
        
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 20;
        hlg.padding = new RectOffset(20, 20, 5, 5);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.minHeight = 50;
        rowLayout.preferredHeight = 50;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(row.transform, false);
        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.sizeDelta = new Vector2(250, 40);
        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = 16;
        label.color = _actionLabelColor;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        if (_rowFont != null) label.font = _rowFont;
        if (_fontMaterial != null) label.fontMaterial = _fontMaterial;
        LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 250;
        labelLayout.minWidth = 250;
        labelLayout.flexibleWidth = 0;

        // Dropdown
        GameObject dropdownObj = new GameObject("Dropdown");
        dropdownObj.transform.SetParent(row.transform, false);
        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
        
        RectTransform dropdownRT = dropdownObj.GetComponent<RectTransform>();
        dropdownRT.sizeDelta = new Vector2(200, 30);
        
        LayoutElement dropdownLayout = dropdownObj.AddComponent<LayoutElement>();
        dropdownLayout.preferredWidth = 200;
        dropdownLayout.preferredHeight = 30;
        dropdownLayout.flexibleWidth = 1;

        // Add background image
        Image dropdownImage = dropdownObj.AddComponent<Image>();
        dropdownImage.color = new Color(0.2f, 0.2f, 0.2f);

        // Create Label child
        GameObject ddLabel = new GameObject("Label");
        ddLabel.transform.SetParent(dropdownObj.transform, false);
        TextMeshProUGUI ddLabelText = ddLabel.AddComponent<TextMeshProUGUI>();
        ddLabelText.text = defaultOption;
        ddLabelText.fontSize = 14;
        ddLabelText.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform ddLabelRT = ddLabel.GetComponent<RectTransform>();
        ddLabelRT.anchorMin = Vector2.zero;
        ddLabelRT.anchorMax = Vector2.one;
        ddLabelRT.offsetMin = new Vector2(10, 0);
        ddLabelRT.offsetMax = new Vector2(-25, 0);

        // Create Arrow child
        GameObject arrow = new GameObject("Arrow");
        arrow.transform.SetParent(dropdownObj.transform, false);
        TextMeshProUGUI arrowText = arrow.AddComponent<TextMeshProUGUI>();
        arrowText.text = "▼";
        arrowText.fontSize = 14;
        arrowText.alignment = TextAlignmentOptions.MidlineRight;
        RectTransform arrowRT = arrow.GetComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1, 0);
        arrowRT.anchorMax = Vector2.one;
        arrowRT.sizeDelta = new Vector2(20, 0);
        arrowRT.anchoredPosition = new Vector2(-15, 0);

        // Create Template (dropdown list container)
        GameObject template = new GameObject("Template");
        template.transform.SetParent(dropdownObj.transform, false);
        RectTransform templateRT = template.AddComponent<RectTransform>();
        templateRT.anchorMin = new Vector2(0, 0);
        templateRT.anchorMax = new Vector2(1, 0);
        templateRT.pivot = new Vector2(0.5f, 1f);
        templateRT.anchoredPosition = new Vector2(0, 2);
        templateRT.sizeDelta = new Vector2(0, 150);

        Image templateBG = template.AddComponent<Image>();
        templateBG.color = new Color(0.15f, 0.15f, 0.15f);

        ScrollRect scrollRect = template.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(template.transform, false);
        RectTransform viewportRT = viewport.AddComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.sizeDelta = Vector2.zero;
        viewportRT.pivot = new Vector2(0, 1);

        Image viewportMask = viewport.AddComponent<Image>();
        viewportMask.color = Color.white;
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = Vector2.one;
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 28);

        // Item
        GameObject item = new GameObject("Item");
        item.transform.SetParent(content.transform, false);
        RectTransform itemRT = item.AddComponent<RectTransform>();
        itemRT.anchorMin = new Vector2(0, 0.5f);
        itemRT.anchorMax = new Vector2(1, 0.5f);
        itemRT.sizeDelta = new Vector2(0, 20);

        Toggle itemToggle = item.AddComponent<Toggle>();
        
        // Item Background
        GameObject itemBG = new GameObject("Item Background");
        itemBG.transform.SetParent(item.transform, false);
        RectTransform itemBGRT = itemBG.AddComponent<RectTransform>();
        itemBGRT.anchorMin = Vector2.zero;
        itemBGRT.anchorMax = Vector2.one;
        itemBGRT.sizeDelta = Vector2.zero;
        Image itemBGImage = itemBG.AddComponent<Image>();
        itemBGImage.color = new Color(0.2f, 0.2f, 0.2f);

        // Item Checkmark
        GameObject checkmark = new GameObject("Item Checkmark");
        checkmark.transform.SetParent(item.transform, false);
        RectTransform checkRT = checkmark.AddComponent<RectTransform>();
        checkRT.anchorMin = Vector2.zero;
        checkRT.anchorMax = new Vector2(0, 1);
        checkRT.sizeDelta = new Vector2(20, 0);
        checkRT.anchoredPosition = new Vector2(10, 0);
        TextMeshProUGUI checkText = checkmark.AddComponent<TextMeshProUGUI>();
        checkText.text = "✓";
        checkText.fontSize = 14;
        checkText.alignment = TextAlignmentOptions.Center;

        // Item Label
        GameObject itemLabel = new GameObject("Item Label");
        itemLabel.transform.SetParent(item.transform, false);
        RectTransform itemLabelRT = itemLabel.AddComponent<RectTransform>();
        itemLabelRT.anchorMin = Vector2.zero;
        itemLabelRT.anchorMax = Vector2.one;
        itemLabelRT.offsetMin = new Vector2(20, 1);
        itemLabelRT.offsetMax = new Vector2(-10, -2);
        TextMeshProUGUI itemLabelText = itemLabel.AddComponent<TextMeshProUGUI>();
        itemLabelText.fontSize = 14;
        itemLabelText.alignment = TextAlignmentOptions.MidlineLeft;

        itemToggle.targetGraphic = itemBGImage;
        itemToggle.graphic = checkText;
        itemToggle.isOn = true;

        scrollRect.content = contentRT;
        scrollRect.viewport = viewportRT;

        dropdown.template = templateRT;
        dropdown.captionText = ddLabelText;
        dropdown.itemText = itemLabelText;

        template.SetActive(false);

        // Add options
        dropdown.ClearOptions();
        dropdown.AddOptions(options.ToList());
        
        int defaultIndex = System.Array.IndexOf(options, defaultOption);
        if (defaultIndex >= 0)
            dropdown.value = defaultIndex;

        // Add listener
        dropdown.onValueChanged.AddListener((index) => {
            SaveDropdownValue(settingKey, options[index]);
        });

        // Store reference for loading
        MouseControlDropdown dropdownComponent = row.AddComponent<MouseControlDropdown>();
        dropdownComponent.settingKey = settingKey;
        dropdownComponent.dropdown = dropdown;
    }

    private static void CreateToggleRow(Transform parent, string labelText, string settingKey, bool defaultValue)
    {
        GameObject row = new GameObject($"Row_{settingKey}");
        row.transform.SetParent(parent, false);
        
        RectTransform rowRT = row.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0, 1);
        rowRT.anchorMax = new Vector2(1, 1);
        rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.sizeDelta = new Vector2(0, 50);
        
        // Add background
        Image rowBg = row.AddComponent<Image>();
        rowBg.color = new Color(0.1f, 0.1f, 0.1f, 0.3f);
        
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 20;
        hlg.padding = new RectOffset(20, 20, 5, 5);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.minHeight = 50;
        rowLayout.preferredHeight = 50;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(row.transform, false);
        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.sizeDelta = new Vector2(250, 40);
        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = 16;
        label.color = _actionLabelColor;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        if (_rowFont != null) label.font = _rowFont;
        if (_fontMaterial != null) label.fontMaterial = _fontMaterial;
        LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 250;
        labelLayout.minWidth = 250;
        labelLayout.flexibleWidth = 0;

        // Toggle
        GameObject toggleObj = new GameObject("Toggle");
        toggleObj.transform.SetParent(row.transform, false);
        Toggle toggle = toggleObj.AddComponent<Toggle>();
        toggle.isOn = defaultValue;
        
        RectTransform toggleRT = toggleObj.GetComponent<RectTransform>();
        toggleRT.sizeDelta = new Vector2(40, 40);
        
        LayoutElement toggleLayout = toggleObj.AddComponent<LayoutElement>();
        toggleLayout.preferredWidth = 40;
        toggleLayout.preferredHeight = 40;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(toggleObj.transform, false);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;

        // Checkmark
        GameObject checkmark = new GameObject("Checkmark");
        checkmark.transform.SetParent(bg.transform, false);
        Image checkImage = checkmark.AddComponent<Image>();
        checkImage.color = new Color(0.2f, 0.8f, 0.2f);
        RectTransform checkRT = checkmark.GetComponent<RectTransform>();
        checkRT.anchorMin = new Vector2(0.2f, 0.2f);
        checkRT.anchorMax = new Vector2(0.8f, 0.8f);
        checkRT.sizeDelta = Vector2.zero;

        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;

        // Add listener
        toggle.onValueChanged.AddListener((value) => {
            SaveToggleValue(settingKey, value);
        });

        // Store reference for loading
        MouseControlToggle toggleComponent = row.AddComponent<MouseControlToggle>();
        toggleComponent.settingKey = settingKey;
        toggleComponent.toggle = toggle;
    }

    private static void SaveSliderValue(string key, float value)
    {
        string path = System.IO.Path.Combine(Application.dataPath, "Resources", "keybindings.json");
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            json = System.Text.RegularExpressions.Regex.Replace(json, 
                $"\"{key}\":\\s*[0-9.]+", 
                $"\"{key}\": {value.ToString("F1")}");
            System.IO.File.WriteAllText(path, json);
        }
    }

    private static void SaveDropdownValue(string key, string value)
    {
        string path = System.IO.Path.Combine(Application.dataPath, "Resources", "keybindings.json");
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            json = System.Text.RegularExpressions.Regex.Replace(json, 
                $"\"{key}\":\\s*\"[^\"]*\"", 
                $"\"{key}\": \"{value}\"");
            System.IO.File.WriteAllText(path, json);
        }
    }

    private static void SaveToggleValue(string key, bool value)
    {
        string path = System.IO.Path.Combine(Application.dataPath, "Resources", "keybindings.json");
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            json = System.Text.RegularExpressions.Regex.Replace(json, 
                $"\"{key}\":\\s*(true|false)", 
                $"\"{key}\": {value.ToString().ToLower()}");
            System.IO.File.WriteAllText(path, json);
            
            // Trigger reload
            var kb = KeyBindingConfig.Instance;
            if (kb != null)
            {
                kb.ReloadKeybindings(); 
            }
        }
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform result = FindChildRecursive(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}
}
