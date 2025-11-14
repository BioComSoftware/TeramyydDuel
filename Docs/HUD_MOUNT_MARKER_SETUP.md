# Ship HUD Mount Marker System - Setup Guide

## Overview
This simplified system allows designers to visually place mount icons on the ship HUD sprite. Icons automatically switch between "empty mount" and "weapon mounted" sprites based on runtime weapon mounting.

## Architecture

### Components
1. **MountHUDMarker** - Attached to each mount GameObject (e.g., Bow_weapon_mount)
2. **ShipHUDDisplay** - Attached to HUD Canvas, renders all markers
3. **WeaponTypeDetector** - Helper utility for weapon type detection

### How It Works
- Each mount GameObject has a `MountHUDMarker` component
- Designer sets where the icon appears on the ship sprite (normalized 0-1 coordinates)
- Designer assigns default "empty mount" sprite (Mount.png)
- At runtime, `ShipHUDDisplay` finds all markers and creates UI Images for them
- Every frame, checks if mount is occupied and switches sprite accordingly

## Setup Instructions

### 1. Prepare Sprite Assets

**Import Sprites:**
1. Place `Mount.png` in `Assets/UI/Icons/`
2. Place `ShipsHUD-cannon.png` in `Assets/UI/Icons/`
3. Select each sprite in Unity
4. In Inspector, set:
   - Texture Type: **Sprite (2D and UI)**
   - Sprite Mode: **Single**
   - Click **Apply**

### 2. Add Marker to Mount GameObject

**For each mount (e.g., Bow_weapon_mount):**

1. Select the mount GameObject in Hierarchy
2. Click **Add Component**
3. Search for and add: **Mount HUD Marker**
4. Configure the marker:

   **Default Sprite:**
   - Drag `Mount.png` into this field
   
   **Icon Size:**
   - Set to (20, 20) or desired pixel size
   
   **Position On HUD Sprite:**
   - This is where the icon appears on the ship sprite
   - Values are normalized (0 to 1)
   - Examples:
     - Bow (front): (0.5, 0.9) - center horizontally, near top
     - Stern (back): (0.5, 0.1) - center horizontally, near bottom
     - Port side: (0.2, 0.5) - left side, centered vertically
     - Starboard side: (0.8, 0.5) - right side, centered vertically
   
   **Custom Occupied Sprite (Optional):**
   - Leave empty to use weapon type mapping
   - Or assign a specific sprite to always show when this mount is occupied

### 3. Configure HUD Canvas

**One-time setup on your HUD Canvas:**

1. Select your **HUD Canvas** GameObject
2. Ensure it has a **Canvas** component set to **Screen Space - Overlay**
3. Find or create an **Image** component showing your ship silhouette sprite
4. Click **Add Component** → **Ship HUD Display**
5. Configure:

   **Ship Sprite Image:**
   - Drag the Image component (from step 3) here
   
   **Ship Game Object:**
   - Drag your **Ship** root GameObject from Hierarchy here
   
   **Weapon Sprite Mappings:**
   - Click **+** to add a mapping
   - Element 0:
     - Weapon Type: `cannon`
     - Sprite: Drag `ShipsHUD-cannon.png` here
   
   **Debug Log:**
   - Check this to see detailed console logs (helpful for setup)
   - Uncheck in production

### 4. Test

**Play the game:**

1. **Empty Mount Test:**
   - Look at HUD - you should see Mount.png icon at the position you specified
   
2. **Mounted Weapon Test:**
   - If your mount has `autoPopulateOnStart` enabled with a Cannon prefab:
     - Icon should switch to ShipsHUD-cannon.png
   
3. **Runtime Mount/Unmount:**
   - Mount a weapon via code: icon updates to weapon sprite
   - Unmount weapon: icon reverts to Mount.png

## Position Guide

### Understanding Normalized Coordinates
- (0, 0) = bottom-left of ship sprite
- (1, 1) = top-right of ship sprite
- (0.5, 0.5) = exact center

### Common Mount Positions (Top-Down Ship View)

```
        (0.5, 0.9)  Bow
             ▲
             |
(0.2, 0.5) ◄─┼─► (0.8, 0.5)  Port/Starboard
  Port       |       Starboard
             |
             ▼
        (0.5, 0.1)  Stern
```

### Tips
- Start with approximate positions
- Run game in Play mode
- Adjust position values in Inspector while game is running
- Stop game and copy final values

## Adding New Weapon Types

**To support a new weapon type (e.g., "harpoon"):**

1. In `ShipHUDDisplay` component on HUD Canvas:
   - Click **+** on Weapon Sprite Mappings
   - New Element:
     - Weapon Type: `harpoon`
     - Sprite: Drag your harpoon HUD sprite here

2. Ensure your weapon prefab:
   - Has a `ProjectileLauncher` component (or subclass like `Cannon`)
   - GameObject name contains "harpoon" OR
   - You create a `Harpoon` class extending `ProjectileLauncher`

3. Update `WeaponTypeDetector.cs` if needed (for custom types)

## Troubleshooting

### Icon doesn't appear
- Check `ShipHUDDisplay.debugLog` is enabled
- Check Console for error messages
- Verify `shipSpriteImage` and `shipGameObject` are assigned
- Verify marker has a sprite assigned to `defaultSprite`

### Icon appears at wrong position
- Remember coordinates are normalized (0-1, not pixels)
- Check ship sprite's size - position is relative to sprite bounds
- Try (0.5, 0.5) to see center position first
- Enable debug logging to see calculated pixel positions

### Icon doesn't change when weapon mounts
- Check `WeaponMount.isOccupied` is true (enable WeaponMount debugLog)
- Verify weapon type string matches mapping exactly ("cannon" = "cannon")
- Check weapon sprite mapping is configured in `ShipHUDDisplay`
- Enable debug logging to see weapon type detection

### Multiple icons for one mount
- Each mount should have exactly ONE `MountHUDMarker`
- Check Hierarchy - remove duplicate markers

## Architecture Benefits

✅ **Simple** - No complex coordinate math or type mapping lookup
✅ **Designer-Friendly** - Visual setup in Inspector, immediate feedback
✅ **Automatic** - Icons update when weapons mount/unmount
✅ **Maintainable** - Each mount owns its HUD appearance
✅ **Extensible** - Easy to add new weapon types
✅ **Debuggable** - Components visible in Hierarchy and Inspector

## File Locations

**New System:**
- `Assets/Scripts/UI/MountHUDMarker.cs`
- `Assets/Scripts/UI/ShipHUDDisplay.cs`
- `Assets/Scripts/Helpers/WeaponTypeDetector.cs`

**Archived (Old System):**
- `Assets/Scripts/ARCHIVED/ShipHUDRepresentation.cs`
- `Assets/Scripts/ARCHIVED/ShipHUDPanel.cs`
