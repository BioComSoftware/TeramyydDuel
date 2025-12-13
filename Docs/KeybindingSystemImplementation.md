# Keybinding System Implementation Summary

## Overview
Implemented a two-layer keybinding system with centralized developer defaults.

## Key Components

### 1. DefaultKeybindings.cs (NEW)
**Purpose:** Centralized location for developer to set default keybinding values in Unity Inspector.

**Location:** Attach to GameObject named "DefaultKeybindings" in scene

**Contains:**
- All keyboard binding defaults (F1-F3, arrows, F, Z, etc.)
- Boolean modifiers (snapRequiresCtrl, zoomRequiresCtrl)
- GetDefaults() method to fetch all defaults including autoReturnSpeedDegPerSec from ShipWheelController

**DOES NOT contain:**
- autoReturnSpeedDegPerSec (remains in ShipWheelController)

### 2. KeyBindingConfig.cs (MODIFIED)
**Purpose:** Runtime keybinding management - reads from keybindings.json

**Changes:**
- Removed all "default" Inspector fields (moved to DefaultKeybindings)
- Now reads defaults from DefaultKeybindings.Instance at runtime
- Still manages runtime values (_bridgeView, _fireAllWeapons, etc.)
- Still loads/saves keybindings.json
- ResetToDefaults() now calls InitializeFromDefaults() which reads from DefaultKeybindings

### 3. ShipWheelController.cs (UNCHANGED)
**Purpose:** Ship wheel UI control

**Contains:**
- autoReturnSpeedDegPerSec field (developer default value)
- useConfigurableSpeed flag (when true, reads from KeyBindingConfig/JSON)
- HandleAutoReturn() reads from KeyBindingConfig.Instance.autoReturnSpeedDegPerSec when useConfigurableSpeed=true

### 4. keybindings.json (UNCHANGED)
**Purpose:** Authoritative source for player keybindings during gameplay

**Contains all settings including:**
- All keyboard bindings
- autoReturnSpeedDegPerSec
- Boolean modifiers

## Workflow

### Developer Sets Defaults:
1. **Keyboard bindings:** Edit DefaultKeybindings GameObject in Unity Inspector
2. **Ship wheel speed:** Edit ShipWheelController component on ship wheel UI

### Runtime Loading:
1. KeyBindingConfig.LoadFromJSON() reads keybindings.json
2. If JSON missing/corrupt, calls InitializeFromDefaults()
3. InitializeFromDefaults() gets values from DefaultKeybindings.Instance
4. For autoReturnSpeedDegPerSec, uses hardcoded 90f fallback (ShipWheelController handles its own default)

### Player Changes Setting:
1. Game code calls KeyBindingConfig.Instance.SetRuntimeKey("fireAllWeapons", KeyCode.G)
2. Game code calls KeyBindingConfig.Instance.SaveToJSONFile()
3. keybindings.json is updated
4. Developer defaults in DefaultKeybindings remain unchanged

### Player Resets to Defaults:
1. Game code calls KeyBindingConfig.Instance.ResetToDefaults()
2. ResetToDefaults() calls InitializeFromDefaults()
3. Copies all values from DefaultKeybindings.Instance
4. Gets autoReturnSpeedDegPerSec from ShipWheelController (via DefaultKeybindings.GetDefaults())
5. Game code calls SaveToJSONFile() to persist reset

## Migration Notes

### Scripts That Already Use KeyBindingConfig:
- ProjectileLauncher.cs - reads fireAllWeapons (no changes needed)
- InstrumentPanelZoom.cs - reads instrumentZoom (no changes needed)
- CameraViewManager.cs - reads view switching keys (no changes needed)
- OverheadViewController.cs - reads overhead controls (no changes needed)

### Required Setup:
1. Create GameObject named "DefaultKeybindings" in scene
2. Attach DefaultKeybindings component
3. Set all default values in Inspector
4. ShipWheelController already has autoReturnSpeedDegPerSec field configured

## File Locations
- `/Assets/Scripts/Input/DefaultKeybindings.cs` (NEW)
- `/Assets/Scripts/Input/KeyBindingConfig.cs` (MODIFIED)
- `/Assets/Scripts/ShipWheelController.cs` (UNCHANGED)
- `/Assets/Resources/keybindings.json` (UNCHANGED)
