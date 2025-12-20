# Controls Settings Panel - Row Prefab Setup Guide

## Overview
This guide will walk you through creating the KeybindingRow prefab that dynamically populates from keybindings.json with modifier support (Ctrl, Shift, Alt).

## Row Layout Structure
Each row displays: `[Action Name] [Modifier Dropdown] [Key Button]`

Example: `Fire All Weapons [None ▼] [F]`

---

## Step 1: Create the Row Prefab

### 1.1 Create Base GameObject
1. Right-click in Hierarchy → **UI → Panel** (or Empty GameObject)
2. Rename to `KeybindingRow`
3. Add **Horizontal Layout Group** component:
   - Child Alignment: **Middle Left**
   - Child Force Expand: **Width** ✓ **Height** ✓
   - Spacing: **10**
   - Padding: Left=10, Right=10, Top=5, Bottom=5
4. Add **Layout Element** component:
   - Preferred Height: **50**
   - Flexible Width: **1**

### 1.2 Set RectTransform
- Anchors: **Stretch Horizontal** (Alt+Shift + top-middle anchor)
- Left: **0**, Right: **0**
- Height: **50**

---

## Step 2: Create Action Label (Left Side)

### 2.1 Create Text GameObject
1. Right-click `KeybindingRow` → **UI → Text - TextMeshPro**
2. Rename to `ActionLabel`

### 2.2 Configure ActionLabel
**RectTransform:**
- Leave as default (HorizontalLayoutGroup will control it)

**TextMeshPro - Text (UI):**
- Text: `Fire All Weapons`
- Font Size: **18**
- Alignment: **Middle Left** (horizontal left, vertical center)
- Color: **White**
- Wrapping: **Disabled**
- Overflow: **Ellipsis**

**Add Layout Element:**
- Min Width: **200**
- Flexible Width: **1**

---

## Step 3: Create Modifier Dropdown (Middle)

### 3.1 Create Dropdown
1. Right-click `KeybindingRow` → **UI → Dropdown - TextMeshPro**
2. Rename to `ModifierDropdown`

### 3.2 Configure ModifierDropdown
**RectTransform:**
- Leave as default

**TMP_Dropdown:**
- Template: Keep default
- Caption Text: Should reference the `Label` child
- Item Text: Should reference `Template → Viewport → Content → Item → Item Label`

**Add Layout Element:**
- Preferred Width: **150**
- Flexible Width: **0**

### 3.3 Style the Dropdown
**Dropdown Background (Image):**
- Color: Dark gray `(40, 40, 40, 255)`

**Label (TextMeshPro):**
- Font Size: **16**
- Alignment: **Middle Center**
- Color: **White**

### 3.4 Dropdown Options
Options will be set programmatically:
- None
- Ctrl
- Shift
- Alt
- Ctrl+Shift
- Ctrl+Alt
- Shift+Alt
- Ctrl+Shift+Alt

---

## Step 4: Create Key Button (Right Side)

### 4.1 Create Button
1. Right-click `KeybindingRow` → **UI → Button - TextMeshPro**
2. Rename to `KeyButton`

### 4.2 Configure KeyButton
**RectTransform:**
- Leave as default

**Button:**
- Interactable: **✓**
- Transition: **Color Tint**
- Normal Color: `(200, 200, 200, 255)`
- Highlighted Color: `(220, 220, 220, 255)`
- Pressed Color: `(150, 150, 150, 255)`
- Disabled Color: `(100, 100, 100, 128)`

**Image (Background):**
- Color: `(50, 50, 50, 255)` - Dark gray
- Sprite: **UISprite** (default Unity UI sprite)

**Add Layout Element:**
- Preferred Width: **120**
- Flexible Width: **0**

### 4.3 Configure Button Text
Select `KeyButton → Text (TMP)` child:

**Rename to:** `KeyButtonText`

**TextMeshPro - Text (UI):**
- Text: `F`
- Font Size: **16**
- Alignment: **Middle Center**
- Color: **White**
- Font Style: **Bold**

---

## Step 5: Create Listening Indicator (Optional)

### 5.1 Create Indicator GameObject
1. Right-click `KeybindingRow` → **UI → Image**
2. Rename to `ListeningIndicator`
3. Move to be a sibling of KeyButton (after it in hierarchy)

### 5.2 Configure ListeningIndicator
**RectTransform:**
- Leave as default

**Image:**
- Color: **Yellow** `(255, 255, 0, 255)`
- Sprite: Circle or indicator sprite

**Add Layout Element:**
- Preferred Width: **30**
- Preferred Height: **30**
- Flexible Width: **0**

**Initially Disable:**
- ✓ **Uncheck the GameObject** (will be enabled during rebinding)

---

## Step 6: Add KeybindingRow Component

### 6.1 Add Script Component
1. Select `KeybindingRow` root GameObject
2. **Add Component** → Search for `KeybindingRow`
3. The script should appear as `Teramyyd.UI.KeybindingRow`

### 6.2 Wire Up References
Drag and drop components into script fields:

**UI References:**
- Action Label: Drag `ActionLabel` TextMeshPro
- Modifier Dropdown: Drag `ModifierDropdown` TMP_Dropdown
- Key Button: Drag `KeyButton` Button
- Key Button Text: Drag `KeyButtonText` TextMeshPro
- Listening Indicator: Drag `ListeningIndicator` GameObject

**Visual Feedback:**
- Normal Color: **White** `(255, 255, 255, 255)`
- Listening Color: **Yellow** `(255, 255, 0, 255)`
- Conflict Color: **Red** `(255, 0, 0, 255)`

---

## Step 7: Create Prefab

### 7.1 Save as Prefab
1. Drag `KeybindingRow` from Hierarchy to **Assets/Prefabs/UI/** folder
2. Name it `KeybindingRow`
3. Delete the instance from Hierarchy (prefab is saved)

---

## Step 8: Setup ControlsSettingsPanel

### 8.1 Find ControlsSettingsPanel in Scene
Navigate to: `GlobalUICanvas → GlobalUIManager → ControlsSettingsPanel`

### 8.2 Add ControlsSettingsPanel Script
1. Select `ControlsSettingsPanel` GameObject
2. **Add Component** → `ControlsSettingsPanel` (Teramyyd.UI)

### 8.3 Create ScrollView Structure
If not already present:

1. Right-click `ControlsSettingsPanel` → **UI → Scroll View**
2. Rename to `Scroll View`
3. Configure ScrollView:
   - **Scroll Rect**: Vertical ✓, Horizontal ✗
   - **Content**: Reference the `Content` child
   - **Viewport**: Reference the `Viewport` child

### 8.4 Configure Content Container
Select `Scroll View → Viewport → Content`:

**Add Vertical Layout Group:**
- Child Alignment: **Upper Center**
- Child Force Expand: Width ✓, Height ✗
- Spacing: **5**
- Padding: **10** on all sides

**Add Content Size Fitter:**
- Horizontal Fit: **Unconstrained**
- Vertical Fit: **Preferred Size**

**RectTransform:**
- Anchors: **Stretch** (top stretch preset)
- Left: **0**, Right: **0**, Top: **0**
- Pivot: **Top** (0.5, 1)

### 8.5 Wire Up ControlsSettingsPanel Script
Select `ControlsSettingsPanel` root GameObject:

**References:**
- Content Container: Drag `Scroll View → Viewport → Content`
- Row Prefab: Drag `KeybindingRow` prefab from Assets/Prefabs/UI/
- Reset Button: Drag `ResetButton` if you have one
- Back Button: Drag `BackButton`

---

## Step 9: Testing

### 9.1 Run the Scene
1. Press Play
2. Open Settings → Controls

### 9.2 Expected Behavior
- ✅ All keybindings from `keybindings.json` appear as rows
- ✅ Action names are formatted (e.g., "fireAllWeapons" → "Fire All Weapons")
- ✅ Current keys are displayed in buttons
- ✅ Modifier dropdowns show correct modifiers (None, Ctrl, etc.)
- ✅ Clicking key button shows "Press any key..."
- ✅ Pressing a key updates the binding
- ✅ Changing modifier dropdown updates the binding
- ✅ Conflicts are detected and shown
- ✅ Escape key cancels rebinding

### 9.3 Verify keybindings.json
After changing bindings, check `Assets/Resources/keybindings.json`:
```json
{
    "fireAllWeapons": "Ctrl+D",
    "engineForward": "W",
    ...
}
```

---

## Troubleshooting

### Rows not appearing
- ✓ Check Console for errors
- ✓ Verify `keybindings.json` exists in `Assets/Resources/`
- ✓ Ensure Content Container reference is set
- ✓ Check Row Prefab is assigned

### Dropdown not showing options
- ✓ Verify TMP_Dropdown Template is expanded in Scene view
- ✓ Check Template → Viewport → Content exists
- ✓ Options are set programmatically (should show at runtime)

### Key button not responding
- ✓ Check Button component is enabled
- ✓ Verify EventSystem exists in scene
- ✓ Check ControlsSettingsPanel script is enabled

### Modifiers not saving
- ✓ Check dropdown OnValueChanged is hooked up in script
- ✓ Verify file write permissions for `Assets/Resources/keybindings.json`
- ✓ Check Console for save errors

### Text not visible
- ✓ Set TextMeshPro color to White
- ✓ Clear Material Preset or use "Default Material"
- ✓ Check font is assigned

---

## Advanced Customization

### Adding Categories
To group keybindings by category (View, Combat, Ship, etc.), modify `ControlsSettingsPanel.cs`:
- Add category headers before each group
- Use LINQ GroupBy for organization

### Custom Row Styling
- Adjust HorizontalLayoutGroup spacing
- Change button/dropdown colors for themes
- Add hover tooltips for action descriptions

### Conflict Resolution UI
- Add visual indicator when conflicts occur
- Allow user to choose which binding to keep
- Show all conflicting actions in a popup

---

## Files Created
- `/Assets/Scripts/UI/ControlsSettingsPanel.cs` - Main panel controller
- `/Assets/Scripts/UI/KeybindingRow.cs` - Individual row component (updated with modifiers)
- `/Assets/Prefabs/UI/KeybindingRow.prefab` - Row prefab (you create this)

## Next Steps
1. Create the KeybindingRow prefab following steps above
2. Wire up ControlsSettingsPanel references
3. Test rebinding functionality
4. Style to match your game's aesthetic
5. Add category organization if desired
