# Developer Instructions

This document explains how to place, wire, and test the main scripts in this project. It is written for designers/devs authoring ships, mounts, and prefabs — not for players. The goal is a predictable authoring contract with minimal surprises.

## ProjectileLauncher (on a weapon prefab, e.g., Cannon)
- Purpose: fires projectiles from a spawn point; applies variance (spread/jitter).
- Key fields:
  - projectilePrefab: the projectile to spawn (must have Rigidbody + Collider; Projectile.cs is recommended).
  - spawnPoint: a child Transform that marks the muzzle. The current launcher fires along spawnPoint.up (local +Y). Make the child’s green arrow (+Y) point out of the barrel.
  - launchSpeed, spawnOffset: initial speed and how far ahead of the muzzle to spawn.
  - ngleSpreadDegrees (default 5), speedJitterPercent (default 5): runtime‑tunable knobs. Set these from gameplay (e.g., crew skill).
- Authoring steps:
  1) Assign Cylinder to spawnPoint.
  3) Parent muzzle effects (smoke/blast) under the Cannon root; set their Simulation Space = Local. Place both at teh opening of the cannon (based on the cylinder visual object.)

## WeaponMount (general mount; attach to Ship at each mount location)
- Purpose: a generic mount with yaw/pitch pivots and runtime Mount/Unmount.
- Fields:
  - Identity: mountId (unique), mountType (e.g., "cannon").
  - Pivots:
    - yawBase: rotates around local Y (left/right).
    - pitchBarrel: rotates around local X (up/down). The weapon prefab is parented here.
    - Tip: If your mount object is empty, create children: YawBase and PitchBarrel (child of YawBase). Assign accordingly.
  - Limits: yawLimitDeg (total arc), pitchUpDeg, pitchDownDeg.
  - Launcher axis mapping:
    - launcherAxis: choose which local axis of the launcher’s spawnPoint represents its firing axis (Up/Forward/Right).
    - invertLauncherAxis: flip the chosen axis if the prefab fires along the negative axis.
    - The mount maps the chosen axis to its target direction (currently mount −Z).
  - Testing: utoPopulatePrefab + utoPopulateOnStart mounts a weapon automatically at Play.
  - Debug (temporary):
    - debugKeypadControl (enable to move pivots with keys during Play).
    - Keys: j/l = yaw left/right; i/k = pitch up/down. Invert with invertYawDirection / invertPitchDirection.
- Runtime API (for gameplay):
  - MountWeapon(prefab), UnmountWeapon().
  - SetYawPitch(yawDeg, pitchDeg), ApplyYawDelta(delta), ApplyPitchDelta(delta).
- Authoring contract:
  - Keep Ship/Model/Mount and pivots at positive scale (1,1,1). Do not mirror axes.
  - Baseline pose: with yaw=0 and pitch=0, the mount’s +Z should be straight ahead; leave pivots at zero rotation in the prefab/scene.

## ProjectileLauncherMount (specialized mount)
- Same pivot/limit pattern as WeaponMount but intended specifically for ProjectileLauncher weapons.
- Supports the same launcherAxis / invertLauncherAxis mapping and optional i/j/k/l debug controls.
- Choose either WeaponMount or ProjectileLauncherMount per your pipeline — not both on the same object.

## AutoPopulateLauncherMounts (optional Ship helper)
- Attach to the Ship root to quickly mount a given prefab into all empty mounts under the ship at Start.
- Fields: launcherPrefab, 
unOnStart.
- Note: Use either this helper OR the per‑mount utoPopulateOnStart — not both — to avoid double mounting.

## Ship HUD Mount Marker System
- Purpose: Simplified system for designers to visually place mount icons on the ship HUD sprite. Icons automatically switch between "empty mount" and "weapon mounted" sprites.
- Components:
  - MountHUDMarker: attached to each mount GameObject (e.g., Bow_weapon_mount).
  - ShipHUDDisplay: attached to HUD Canvas, renders all markers.
  - WeaponTypeDetector: helper utility for weapon type detection.
- How It Works:
  - Each mount GameObject has a MountHUDMarker component.
  - Designer sets where the icon appears on the ship sprite (normalized 0-1 coordinates).
  - Designer assigns default "empty mount" sprite (Mount.png).
  - At runtime, ShipHUDDisplay finds all markers and creates UI Images for them.
  - Every frame, checks if mount is occupied and switches sprite accordingly.

### Setup Instructions

#### Step 1: Prepare Sprite Assets
- Place Mount.png in Assets/UI/Icons/
- Place ShipsHUD-cannon.png in Assets/UI/Icons/
- Select each sprite in Unity
- In Inspector, set:
  - Texture Type: Sprite (2D and UI)
  - Sprite Mode: Single
  - Click Apply

#### Step 2: Add Marker to Mount GameObject
For each mount (e.g., Bow_weapon_mount):
- Select the mount GameObject in Hierarchy
- Click Add Component
- Search for and add: Mount HUD Marker
- Configure the marker:
  - Default Sprite: Drag Mount.png into this field
  - Icon Size: Set to (20, 20) or desired pixel size
  - Position On HUD Sprite: Where the icon appears on the ship sprite (normalized 0 to 1)
    - Examples:
      - Bow (front): (0.5, 0.9) - center horizontally, near top
      - Stern (back): (0.5, 0.1) - center horizontally, near bottom
      - Port side: (0.2, 0.5) - left side, centered vertically
      - Starboard side: (0.8, 0.5) - right side, centered vertically
  - Custom Occupied Sprite (Optional): Leave empty to use weapon type mapping, or assign a specific sprite

#### Step 3: Configure HUD Canvas
Create Ship Silhouette Image:
- In Hierarchy, find your HUD Canvas and expand to HUD_Root
- Right-click on HUD_Root → UI → Image
- Rename the new Image to ShipSilhouette
- Select this Image GameObject
- In Inspector, in the Image (Script) component:
  - Source Image: Drag your ship sprite (e.g., ShipOutline)
  - Raycast Target: Uncheck (optional optimization)
  - Preserve Aspect: Will be controlled by Ship HUD Display

Add Ship HUD Display Component:
- Select your HUD Canvas GameObject (the root Canvas, not HUD_Root)
- Click Add Component → Search for Ship HUD Display
- Configure:
  - Ship Sprite:
    - Ship Sprite Image: Drag the ShipSilhouette Image GameObject
    - Ship Game Object: Drag the Ship root GameObject from scene Hierarchy
  - Ship Sprite Layout:
    - Ship Sprite Size: Set pixel size (e.g., 300, 700)
    - Screen Anchor: Where to anchor on screen
      - (1, 0.5) = right side, centered vertically (default)
      - (1, 1) = top-right corner
      - (0.5, 0.5) = center screen
    - Anchor Offset: Pixel offset from anchor (e.g., -30, 75)
      - Negative X = move left from anchor
      - Positive Y = move up from anchor
    - Preserve Aspect: Check to prevent distortion (recommended)
  - Weapon Sprite Mappings:
    - Click + to add a mapping
    - Element 0:
      - Weapon Type: cannon
      - Sprite: Drag ShipsHUD-cannon.png here
  - Debug Log: Check for detailed console logging during setup (uncheck in production)

Common Ship Positions:
- Right side, centered (default): Screen Anchor: (1, 0.5), Anchor Offset: (-30, 75)
- Top-right corner: Screen Anchor: (1, 1), Anchor Offset: (-30, -30)
- Bottom-right corner: Screen Anchor: (1, 0), Anchor Offset: (-30, 30)
- Center screen: Screen Anchor: (0.5, 0.5), Anchor Offset: (0, 0)

#### Step 4: Test
Play the game:
- Empty Mount Test: Look at HUD - you should see Mount.png icon at the position you specified
- Mounted Weapon Test: If your mount has autoPopulateOnStart enabled with a Cannon prefab, icon should switch to ShipsHUD-cannon.png
- Runtime Mount/Unmount: Mount a weapon via code - icon updates to weapon sprite; unmount weapon - icon reverts to Mount.png

### Position Guide
Understanding Normalized Coordinates:
- (0, 0) = bottom-left of ship sprite
- (1, 1) = top-right of ship sprite
- (0.5, 0.5) = exact center

Common Mount Positions (Top-Down Ship View):
- Bow: (0.5, 0.9)
- Stern: (0.5, 0.1)
- Port: (0.2, 0.5)
- Starboard: (0.8, 0.5)

Tips:
- Start with approximate positions
- Run game in Play mode
- Adjust position values in Inspector while game is running
- Stop game and copy final values

### Adding New Weapon Types
To support a new weapon type (e.g., "harpoon"):
- In ShipHUDDisplay component on HUD Canvas:
  - Click + on Weapon Sprite Mappings
  - New Element:
    - Weapon Type: harpoon
    - Sprite: Drag your harpoon HUD sprite here
- Ensure your weapon prefab:
  - Has a ProjectileLauncher component (or subclass like Cannon)
  - GameObject name contains "harpoon" OR
  - You create a Harpoon class extending ProjectileLauncher
- Update WeaponTypeDetector.cs if needed (for custom types)

### Ship HUD Mount Marker Troubleshooting
- Icon doesn't appear:
  - Check ShipHUDDisplay.debugLog is enabled
  - Check Console for error messages
  - Verify shipSpriteImage and shipGameObject are assigned
  - Verify marker has a sprite assigned to defaultSprite
  - Check that MountHUDMarker is on the mount GameObject, not the Ship root
- Icon appears at wrong position:
  - Remember coordinates are normalized (0-1, not pixels)
  - Check ship sprite's size - position is relative to sprite bounds
  - Try (0.5, 0.5) to see center position first
  - Enable debug logging to see calculated pixel positions
- Ship sprite is distorted or wrong size:
  - Adjust Ship Sprite Size in ShipHUDDisplay component
  - Enable Preserve Aspect to prevent distortion
  - Check that your sprite import settings are correct (Sprite 2D and UI)
- Ship sprite is in wrong location on screen:
  - Adjust Screen Anchor to change which corner/edge it anchors to
  - Adjust Anchor Offset to fine-tune position in pixels
- Icon doesn't change when weapon mounts:
  - Check WeaponMount.isOccupied is true (enable WeaponMount debugLog)
  - Verify weapon type string matches mapping exactly ("cannon" = "cannon")
  - Check weapon sprite mapping is configured in ShipHUDDisplay
  - Enable debug logging to see weapon type detection
- Multiple icons for one mount:
  - Each mount should have exactly ONE MountHUDMarker
  - Check Hierarchy - remove duplicate markers

### File Locations
Current System:
- Assets/Scripts/UI/MountHUDMarker.cs
- Assets/Scripts/UI/ShipHUDDisplay.cs
- Assets/Scripts/Helpers/WeaponTypeDetector.cs

Archived (Old System):
- Assets/Scripts/ARCHIVED/ShipHUDRepresentation.cs
- Assets/Scripts/ARCHIVED/ShipHUDPanel.cs

## EquilateralTriangleCollider3D (procedural collider)
- Add to any GameObject to create a convex triangular prism collider.
- Fields:
  - Geometry: sideLength (legacy equilateral) or width + length (isosceles), 	hickness (Z depth).
  - Placement/orientation: centerOffset (local), 
otationEuler (local X/Y/Z).
- MeshCollider is assigned automatically and set Convex=true for RB usage.

## Common Troubleshooting
- Weapon fires the wrong way:
  - Check the mount’s baseline: local +Z should be straight ahead at yaw=0/pitch=0.
  - In the mount, set launcherAxis to the spawn point’s firing axis (often Up) and toggle invertLauncherAxis.
  - Ensure no negative scale on Ship/Model/Mount/pivots.
- Two projectiles/smoke plumes:
  - Ensure you aren’t mounting twice (Ship helper + per‑mount auto). Use only one path.
- Cannon doesn’t move with keys:
  - Enable debugKeypadControl on the correct mount; make sure the Game view has focus; check that pivots are assigned and limits are non‑zero.

## Minimal Code Snippets
- Mount a weapon by id:
`
var mounts = shipRoot.GetComponentsInChildren<WeaponMount>(true);
var m = System.Array.Find(mounts, x => x.mountId == "Bow_01");
if (m != null && m.CanMountWeaponType("cannon")) m.MountWeapon(cannonPrefab);
`
- Adjust accuracy live:
`
var launcher = m.currentLauncher; // or GetComponentInChildren<ProjectileLauncher>()
if (launcher != null)
{
    launcher.angleSpreadDegrees = Mathf.Lerp(8f, 0f, crew.Skill01);
    launcher.speedJitterPercent = Mathf.Lerp(8f, 0f, crew.Skill01);
}
`

---
Keep this document close as you author ships and mounts. If a script slot isn’t clear, search for the component file referenced above to see fields and defaults.
