# KeybindingRow Prefab Setup Guide

## Overview
The KeybindingRow prefab represents a single control binding in the controls settings panel. Each row shows an action name, its current key, and a button to rebind it.

## Prefab Structure

### GameObject Hierarchy
```
KeybindingRow (Root)
├── ActionLabel (TextMeshProUGUI)
├── RebindButton (Button)
│   └── ButtonLabel (TextMeshProUGUI) - "Change"
├── KeyLabel (TextMeshProUGUI)
└── ListeningIndicator (TextMeshProUGUI) - "Listening..."
```

## Step-by-Step Unity Setup

### 1. Create the Root GameObject
1. In Hierarchy: Right-click → Create Empty → Name it "KeybindingRow"
2. Add Component: `KeybindingRow` script (Assets/Scripts/UI/KeybindingRow.cs)
3. Set RectTransform:
   - Width: 800 (or match your panel width)
   - Height: 50
   - Anchor: Top-stretch

### 2. Create ActionLabel (Left Side)
1. Right-click KeybindingRow → UI → Text - TextMeshPro → Name it "ActionLabel"
2. RectTransform:
   - Anchor Preset: Left (hold Alt+Shift to set position)
   - Pos X: 20, Pos Y: 0
   - Width: 300, Height: 40
3. TextMeshProUGUI settings:
   - Text: "Action Name"
   - Font Size: 18
   - Alignment: Middle Left
   - Color: White

### 3. Create KeyLabel (Right Side)
1. Right-click KeybindingRow → UI → Text - TextMeshPro → Name it "KeyLabel"
2. RectTransform:
   - Anchor Preset: Right (hold Alt+Shift)
   - Pos X: -20, Pos Y: 0
   - Width: 200, Height: 40
3. TextMeshProUGUI settings:
   - Text: "W"
   - Font Size: 18
   - Alignment: Middle Right
   - Color: White
   - Font Style: Bold (optional)

### 4. Create RebindButton (Center)
1. Right-click KeybindingRow → UI → Button - TextMeshPro → Name it "RebindButton"
2. RectTransform:
   - Anchor Preset: Center
   - Pos X: 0, Pos Y: 0
   - Width: 120, Height: 35
3. Button settings:
   - Navigation: None (script handles clicks)
   - Colors: Set Highlighted/Pressed tints as desired
4. Rename child Text to "ButtonLabel", set text to "Change"

### 5. Create ListeningIndicator
1. Right-click KeybindingRow → UI → Text - TextMeshPro → Name it "ListeningIndicator"
2. RectTransform:
   - Anchor Preset: Center
   - Pos X: 0, Pos Y: 0
   - Width: 200, Height: 40
3. TextMeshProUGUI settings:
   - Text: "Press any key..."
   - Font Size: 16
   - Alignment: Middle Center
   - Color: Yellow (RGB: 1, 1, 0)
4. **IMPORTANT:** Disable this GameObject (checkbox in Inspector)

### 6. Wire Up KeybindingRow Script
Select the KeybindingRow root GameObject and assign references in Inspector:

**UI References:**
- Action Label: Drag ActionLabel
- Rebind Button: Drag RebindButton
- Key Label: Drag KeyLabel
- Listening Indicator: Drag ListeningIndicator

**Visual Feedback:**
- Normal Color: White (1, 1, 1, 1)
- Listening Color: Yellow (1, 1, 0, 1)
- Conflict Color: Red (1, 0, 0, 1)

### 7. Save as Prefab
1. Drag the KeybindingRow GameObject from Hierarchy to Project window
2. Save location: `Assets/Prefabs/UI/KeybindingRow.prefab`
3. Delete the instance from the hierarchy

## Integration with KeybindingControlsPanel

In your ControlsSettingsPanel GameObject:
1. Add the `KeybindingControlsPanel` component
2. Assign prefab reference:
   - Keybinding Row Prefab: Your new KeybindingRow prefab
3. Create a Scroll View for the rows container:
   - ControlsSettingsPanel → Scroll View → Viewport → Content
   - Assign "Content" to Rows Container field
4. Set up Content:
   - Add Vertical Layout Group component
   - Spacing: 5
   - Child Force Expand: Width only
   - Add Content Size Fitter: Vertical Fit = Preferred Size

## Optional: Section Headers

If you want category headers (Movement, Combat, UI, etc.):

1. Create a simple GameObject with TextMeshProUGUI
2. Style it (larger font, bold, different color)
3. Save as prefab: `Assets/Prefabs/UI/SectionHeader.prefab`
4. Assign to Section Header Prefab field in KeybindingControlsPanel

## Layout Example

```
┌────────────────────────────────────────────────┐
│ [Action Name]     [Change]      [Key]          │  ← Row 1
├────────────────────────────────────────────────┤
│ [Move Forward]    [Change]      [W]            │
├────────────────────────────────────────────────┤
│ [Move Backward]   [Change]      [S]            │
└────────────────────────────────────────────────┘
```

## Testing

1. Open the scene and ensure KeybindingManager exists
2. Play mode
3. Open Settings → Controls
4. Verify rows populate automatically
5. Click "Change" button → should show "Press any key..."
6. Press a key → should update the key label
7. Click "Reset to Defaults" → should restore original keys

## Troubleshooting

**Rows don't populate:**
- Check KeybindingManager.Instance is not null
- Verify keybindings.json exists in Assets/Resources/
- Check Console for errors in KeybindingControlsPanel.OnEnable()

**Rebinding doesn't work:**
- Ensure RebindButton is assigned and has KeybindingRow component
- Check KeybindingControlsPanel.Instance exists
- Verify KeybindingManager.RebindKey() is being called

**Layout looks wrong:**
- Check Vertical Layout Group settings on Content
- Verify Content Size Fitter is set to Vertical: Preferred Size
- Ensure Row height matches spacing

**Key labels too long:**
- Increase KeyLabel width (currently 200)
- Or reduce font size
- GetKeyDisplayName() in KeybindingRow.cs handles common keys

## Visual Variants

### Compact Layout (smaller rows)
- Row Height: 35
- ActionLabel: Width 250, Font Size 16
- KeyLabel: Width 150, Font Size 16
- Button: Width 100, Height 30

### Wide Layout (more spacing)
- Row Width: 1000
- ActionLabel: Width 400
- KeyLabel: Width 250
- Spacing: 10

### Alternative Style: Key First
Swap ActionLabel and KeyLabel positions to show key on left, action on right.
