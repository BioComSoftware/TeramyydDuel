# Global UI Setup Guide

## Overview
This guide explains how to set up the GlobalUIManager system for settings menus that persist independently of ship prefabs.

## Unity Hierarchy Setup

### Step 1: Create Global UI Canvas

1. In your main scene (not in ship prefab), create a new Canvas:
   - Right-click in Hierarchy → UI → Canvas
   - Rename to `GlobalUICanvas`

2. Configure Canvas component:
   - Render Mode: Screen Space - Overlay
   - **Sort Order: 100** (renders on top of ship HUD)
   - Pixel Perfect: ✓ (optional, for crisp UI)

3. Add Canvas Scaler component (if not present):
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080
   - Match: 0.5 (balance width/height)

4. Ensure Canvas has GraphicRaycaster component (for button clicks)

### Step 2: Create GlobalUIManager GameObject

1. Create empty GameObject as child of GlobalUICanvas:
   - Right-click GlobalUICanvas → Create Empty
   - Rename to `GlobalUIManager`

2. Add GlobalUIManager script:
   - Select GlobalUIManager GameObject
   - Add Component → Scripts → Teramyyd.UI → Global UI Manager

### Step 3: Create Settings Menu Panel

1. Create main settings panel:
   ```
   GlobalUICanvas
   └── SettingsMenuPanel
       ├── Background (Image - opaque, fills screen)
       ├── PanelContainer (Image - panel background)
       │   ├── Title (TextMeshPro - "Settings")
       │   ├── ControlsButton (Button)
       │   ├── AudioButton (Button)
       │   ├── VideoButton (Button)
       │   └── BackButton (Button)
   ```

   **IMPORTANT: Background should be OPAQUE (alpha = 255) to block ship HUD visibility**

2. **SettingsMenuPanel setup**:
   - Add RectTransform: Anchors = Stretch (all corners)
   - Offsets = 0 on all sides (fills screen)
   - **IMPORTANT**: Disable GameObject in Inspector (unchecked)

3. **Background setup**:
   - Add Image component
   - Color: Black with Alpha = 255 (fully opaque, blocks ship HUD)
   - RectTransform: Stretch to fill parent

4. **PanelContainer setup**:
   - Add Image component (panel sprite or solid color)
   - RectTransform: Center, Width = 800, Height = 600
   - Add Vertical Layout Group:
     - Padding: 20 on all sides
     - Spacing: 20
     - Child Alignment: Upper Center
     - Child Force Expand: Width ✓, Height ✗

5. **Buttons setup** (for each button):
   - Add Button component
   - Width: Preferred = 700, Height: Preferred = 80
   - Add TextMeshPro child for button label
   - **Wire up button OnClick**:
     - Controls Button: GlobalUIManager → OnControlsButtonClicked()
     - Audio Button: GlobalUIManager → OnAudioButtonClicked()
     - Video Button: GlobalUIManager → OnVideoButtonClicked()
     - Back Button: GlobalUIManager → OnBackButtonClicked()

### Step 4: Create Controls Settings Panel

1. Create controls panel:
   ```
   GlobalUICanvas
   └── ControlsSettingsPanel
       ├── Background (Image - dark semi-transparent)
       ├── PanelContainer (Image - lighter panel background)
       │   ├── Title (TextMeshPro - "Keyboard Controls")
       │   ├── ScrollView (Scroll Rect)
       │   │   └── Content
       │   │       ├── KeybindingRow1 (will be populated by code)
       │   │       ├── KeybindingRow2
       │   │       └── ... (more rows)
       │   └── BackButton (Button)
   ```

2. **ControlsSettingsPanel setup**:
   - Same as SettingsMenuPanel (stretch, disabled by default)
   - Background: Opaque (alpha = 255)
   - PanelContainer: centered, larger (width=1200, height=800)

3. **ScrollView setup**:
   - Right-click PanelContainer → UI → Scroll View
   - Remove Horizontal Scrollbar (only need vertical)
   - Content: Add Vertical Layout Group
     - Spacing: 10
     - Child Force Expand: Width ✓, Height ✗

4. **BackButton**:
   - OnClick: GlobalUIManager → OnBackButtonClicked()

### Step 5: Create Placeholder Panels

Create similar panels for Audio and Video (we'll implement these later):

1. **AudioSettingsPanel**:
   - Copy SettingsMenuPanel
   - Rename to AudioSettingsPanel
   - Change title to "Audio Settings"
   - Add placeholder content (volume sliders, etc.)
   - Disable GameObject

2. **VideoSettingsPanel**:
   - Copy SettingsMenuPanel
   - Rename to VideoSettingsPanel
   - Change title to "Video Settings"
   - Add placeholder content (resolution, quality, etc.)
   - Disable GameObject

### Step 6: Wire Up GlobalUIManager

1. Select GlobalUIManager GameObject
2. In Inspector, assign panel references:
   - Settings Menu Panel: → SettingsMenuPanel
   - Controls Settings Panel: → ControlsSettingsPanel
   - Audio Settings Panel: → AudioSettingsPanel
   - Video Settings Panel: → VideoSettingsPanel
3. Set Pause Game When In Settings: ✓
4. (Optional) Assign Ship HUD Canvas reference to dim during settings

### Step 7: Create Settings Button

**Option A: On Ship HUD** (button moves with ship prefab)
1. Find your ship's HUD canvas
2. Add Button (e.g., top-right corner)
3. Label: "Settings" or gear icon
4. OnClick: GlobalUIManager → OnSettingsButtonClicked()

**Option B: On Global UI** (recommended - button stays with scene)
1. Create button as child of GlobalUICanvas (not in any panel)
2. Position in corner (e.g., top-right)
3. Always visible, always accessible
4. OnClick: GlobalUIManager → OnSettingsButtonClicked()

## Canvas Sort Order Summary

Make sure your canvases render in correct order:

- **ShipHUD_Canvas** (on ship prefab): Sort Order = **0**
- **GlobalUICanvas** (in scene): Sort Order = **100**

This ensures settings panels render on top of ship UI.

## Testing

1. **Play Mode**
2. Click Settings button
   - Settings menu should appear
   - Game should pause (Time.timeScale = 0)
   - Ship HUD should dim/hide (if configured)
3. Click "Controls"
   - Controls panel should appear
   - Settings menu should hide
4. Click "Back"
   - Should return to Settings menu
5. Click "Back" again
   - Should close all panels
   - Game should resume
   - Ship HUD should reappear

## Troubleshooting

**Settings button doesn't work:**
- Verify GlobalUIManager GameObject exists in scene
- Check GlobalUIManager has panel references assigned
- Ensure button OnClick is wired to GlobalUIManager.OnSettingsButtonClicked()

**Panel doesn't appear:**
- Check panel is assigned in GlobalUIManager Inspector
- Verify panel has Canvas and GraphicRaycaster components (inherited from parent)
- Check panel isn't being hidden by other UI

**Buttons don't respond:**
- Ensure GlobalUICanvas has EventSystem in scene
- Verify GraphicRaycaster on canvas
- Check button raycast target is enabled

**Game doesn't pause:**
- Enable "Pause Game When In Settings" in GlobalUIManager
- Note: Physics-based systems use Time.deltaTime (will pause)
- UI animations use Time.unscaledDeltaTime (won't pause)

## Next Steps

After basic setup is working:
1. **Create KeybindingRow prefab** - See KEYBINDING_ROW_SETUP.md for detailed guide
2. Add KeybindingControlsPanel component to ControlsSettingsPanel
3. Integrate with existing KeybindingManager
4. Add audio sliders for AudioSettingsPanel
5. Add graphics options for VideoSettingsPanel
6. Add visual transitions (fade in/out, slide animations)

## Example Button Code (if needed)

If you want to call from code instead of Inspector:
```csharp
using Teramyyd.UI;

public class MyScript : MonoBehaviour
{
    void OpenSettings()
    {
        if (GlobalUIManager.Instance != null)
        {
            GlobalUIManager.Instance.ShowSettingsMenu();
        }
    }
}
```

## Files Created
- `Assets/Scripts/UI/GlobalUIManager.cs` - Main manager script
- `Assets/Scripts/UI/KeybindingRow.cs` - Individual keybinding row component
- `Assets/Scripts/UI/KeybindingControlsPanel.cs` - Controls panel manager
- `Docs/GLOBAL_UI_SETUP.md` - This setup guide
- `Docs/KEYBINDING_ROW_SETUP.md` - Detailed KeybindingRow prefab creation guide

## Related Systems
- KeybindingManager (for controls settings integration)
- AudioManager (for audio settings integration)
- QualitySettings (for video settings integration)
