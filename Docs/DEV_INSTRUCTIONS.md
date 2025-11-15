# Developer Instructions

This document explains how to place, wire, and test the main scripts in this project. It is written for designers/devs authoring ships, mounts, and prefabs — not for players. The goal is a predictable authoring contract with minimal surprises.

## Table of Contents
1. [Ship Systems](#ship-systems)
   - [ShipCharacteristics](#shipcharacteristics)
   - [Engine & JetEngine](#engine--jetengine)
   - [LiftDevice & AntiGravityDevice](#liftdevice--antigravitydevice)
2. [Weapon Systems](#weapon-systems)
   - [ProjectileLauncher](#projectilelauncher)
   - [Cannon](#cannon)
   - [CannonBall](#cannonball)
   - [Projectile & Shrapnel](#projectile--shrapnel)
3. [Mount Systems](#mount-systems)
4. [HUD Systems](#hud-systems)

---

## Ship Systems

### ShipCharacteristics
**Purpose**: Central ship physics coordinator; manages mass, aggregates thrust from engines, calculates movement.

**Location**: Attach to Ship root GameObject.

**Key Fields**:
- `shipWeightTons`: Total ship mass in metric tons (converted to kg internally: tons * 1000)
- `dragCoefficient`: Air/space resistance (0 = no drag, higher = more resistance)
- Read-only status: `currentSpeedKnots`, `currentSpeedMetersPerSecond`, `totalThrustAvailable`

**Automatic Setup**:
- Creates/configures Rigidbody automatically:
  - Mass = shipWeightTons * 1000 (kilograms)
  - useGravity = true (lift devices will counteract)
  - Constraints = FreezeRotation (prevents tumbling)
  - linearDamping = 0.1, angularDamping = 1.0
- Finds all Engine children and aggregates thrust
- Applies F=ma physics: acceleration = totalThrust / mass

**Usage Example**:
```csharp
// On Ship root GameObject:
ShipCharacteristics shipStats = gameObject.AddComponent<ShipCharacteristics>();
shipStats.shipWeightTons = 30f; // 30-ton ship
shipStats.dragCoefficient = 0.5f;
```

### Engine & JetEngine
**Purpose**: Provides thrust for ship movement; power-based with burn rate control and heat management (JetEngine).

**Location**: Attach to engine GameObject (child of Ship). Ship must have ShipCharacteristics.

**Base Engine Fields**:
- **Power Control**:
  - `allocatedPowerPerSecond`: Power input (0-100 units/s)
  - `powerToThrustRatio`: Newtons of thrust per unit of power (e.g., 1000)
  - `burnRateMultiplier`: Burn intensity (100-300%, affects power draw and damage)
- **Damage**:
  - `usageDamagePerSecond`: Base damage rate
  - `burnDamageMultiplier`: Extra damage from high burn rates
  - Requires Health component on same GameObject

**JetEngine Additional Fields** (extends Engine):
- **Heat Management**:
  - `maxSafeTemperature`: Maximum safe operating heat (e.g., 100)
  - `heatGenerationRate`: Heat per second when running
  - `heatDissipationRate`: Cooling rate when idle
  - `overheatDamageRate`: Damage per second when overheated
- **Performance**:
  - `heatEfficiencyPenalty`: Thrust loss per degree over safe temp
  - Read-only: `currentTemperature`, `isOverheating`

**Automatic Behavior**:
- Auto-finds ShipCharacteristics parent
- Calculates thrust based on power and burn rate
- Applies usage damage continuously
- JetEngine: Manages heat, applies overheat damage

**Usage Example**:
```csharp
// On engine GameObject (child of Ship):
JetEngine engine = gameObject.AddComponent<JetEngine>();
engine.allocatedPowerPerSecond = 50f;
engine.powerToThrustRatio = 1000f; // 50,000 N thrust at 100% burn
engine.burnRateMultiplier = 150f; // 50% extra burn
engine.maxSafeTemperature = 100f;

// Requires Health component
Health health = gameObject.AddComponent<Health>();
health.maxHealth = 500;
```

**Runtime Control**:
```csharp
// Adjust power allocation
engine.allocatedPowerPerSecond = 75f;

// Increase burn for emergency thrust
engine.burnRateMultiplier = 250f; // Caution: high damage!

// Emergency heat dump (JetEngine only)
jetEngine.EmergencyHeatDump();
```

### LiftDevice & AntiGravityDevice
**Purpose**: Provides vertical lift for ships using direct altitude control (not physics forces).

**Location**: Attach to lift device GameObject (child of Ship). Ship must have ShipCharacteristics.

**Core Mechanics**:
- **Power = 0**: Gravity enabled, Unity physics handles fall at 9.82 m/s²
- **Power > 0**: Gravity disabled, direct altitude control active
- **Auto-allocates** minimum power at start for immediate hover

**LiftDevice Base Fields**:
- **Power Settings**:
  - `minimumPowerPerSecond`: Power needed to hover (recommend = shipWeightTons)
  - `powerPerTonPerMeterPerSecond`: Climb rate multiplier (1 = standard)
  - `allocatedPowerPerSecond`: Current power input (auto-sets to minimum if 0)
- **Damage**:
  - `usageDamagePerSecond`: Wear-and-tear damage rate
  - Requires Health component

**AntiGravityDevice Additional Fields** (extends LiftDevice):
- **Field Properties**:
  - `fieldEfficiency`: Power multiplier (1.0 standard, >1.0 more efficient)
  - `fieldStability`: Lift consistency (1.0 perfect, <1.0 fluctuating)
  - `maxSafeFieldStrength`: Overload threshold (% of ship weight)
  - `altitudeCalibration`: Offset for altitude reading
- **Read-only Status**:
  - `currentAltitude`: Current altitude with calibration applied
  - `fieldStrengthPercent`: Field strength as % of ship weight
  - `isFieldOverloaded`: Overload warning

**Power-to-Velocity Formula**:
```
Hover (power = minimum):
  verticalVelocity = 0 m/s

Climb (power > minimum):
  excessPower = allocatedPower - minimumPower
  verticalVelocity = excessPower / (shipWeightTons * powerPerTonPerMeterPerSecond)
  
Descend (power < minimum):
  powerRatio = allocatedPower / minimumPower
  descentRate = 9.82 * (1 - powerRatio) m/s

Examples (30-ton ship, PPTPMPS=1, minimum=30):
  Power = 0   → Falls at 9.82 m/s² (Unity gravity)
  Power = 7.5 → Descends at 7.365 m/s (constant)
  Power = 15  → Descends at 4.91 m/s (constant)
  Power = 30  → Hovers at 0 m/s
  Power = 45  → Climbs at 0.5 m/s
  Power = 60  → Climbs at 1.0 m/s
  Power = 120 → Climbs at 3.0 m/s
```

**Usage Example**:
```csharp
// On lift device GameObject (child of Ship):
AntiGravityDevice lift = gameObject.AddComponent<AntiGravityDevice>();
lift.minimumPowerPerSecond = 30f; // Match ship weight (30 tons)
lift.powerPerTonPerMeterPerSecond = 1f;
lift.allocatedPowerPerSecond = 30f; // Start at hover (or leave 0 to auto-set)
lift.fieldEfficiency = 1.2f; // 20% more efficient
lift.altitudeCalibration = -100f; // Set ground level as altitude 0

// Requires Health component
Health health = gameObject.AddComponent<Health>();
health.maxHealth = 300;
```

**Runtime Control**:
```csharp
// Hover
lift.allocatedPowerPerSecond = lift.minimumPowerPerSecond;

// Climb at 1 m/s (30-ton ship, PPTPMPS=1)
lift.allocatedPowerPerSecond = 60f; // minimum(30) + 30 = 1 m/s climb

// Descend at ~5 m/s
lift.allocatedPowerPerSecond = 15f; // 50% power = 50% gravity descent

// Emergency boost (AntiGravityDevice only)
antiGravLift.EmergencyFieldBoost(); // Increases efficiency, reduces stability

// Calculate power needed for specific climb rate
float powerNeeded = antiGravLift.CalculatePowerForVelocity(2.0f); // 2 m/s climb
```

**Important Notes**:
- Ship maintains exact attitude (pitch/roll/yaw) during all lift operations
- No tumbling or rotation from ground contact (rotation is frozen)
- Altitude control works at any ship orientation (nose-down, banking, etc.)
- Power allocation can be changed at runtime for dynamic control
- Health depletion causes lift failure → ship falls under gravity

---

## Weapon Systems

### ProjectileLauncher (on a weapon prefab, e.g., Cannon)
- Purpose: fires projectiles from a spawn point; applies variance (spread/jitter).
- Key fields:
  - projectilePrefab: the projectile to spawn (must have Rigidbody + Collider; Projectile.cs is recommended).
  - spawnPoint: a child Transform that marks the muzzle. The current launcher fires along spawnPoint.up (local +Y). Make the child's green arrow (+Y) point out of the barrel.
  - launchSpeed, spawnOffset: initial speed and how far ahead of the muzzle to spawn.
  - angleSpreadDegrees (default 5), speedJitterPercent (default 5): runtime‑tunable knobs. Set these from gameplay (e.g., crew skill).
  - reloadTime: time in seconds before weapon can fire again after firing (default 2.0).
  - startReady: whether weapon starts ready to fire or needs to reload first (default true).
- Authoring steps:
  1) Assign Cylinder to spawnPoint.
  2) Parent muzzle effects (smoke/blast) under the Cannon root; set their Simulation Space = Local. Place both at the opening of the cannon (based on the cylinder visual object.)
  3) Add Health component to weapon prefab if you want health tracking on the HUD.
- Runtime API:
  - IsReadyToFire(): returns true if weapon can fire (not reloading).
  - GetRemainingReloadTime(): returns seconds remaining until ready (0 if ready).

### Cannon
**Purpose**: Specialized cannon weapon with audio effects (extends ProjectileLauncher).

**Additional Features**:
- Audio support via child AudioSource at muzzle
- Spatial 3D audio with configurable min/max distance
- Optional pitch variance for audio variety
- All ProjectileLauncher features (spread, jitter, reload, etc.)

**Setup**:
1. Create Cannon prefab from ProjectileLauncher base
2. Add Cannon component (replaces or extends ProjectileLauncher)
3. Child AudioSource will be auto-created at muzzle location
4. Configure audio clip for firing sound

**Fields** (in addition to ProjectileLauncher):
- `fireClip`: AudioClip to play when firing
- `audioMinDistance`: Minimum hearing distance (default: 10)
- `audioMaxDistance`: Maximum hearing distance (default: 500)
- `pitchRange`: Random pitch variance (default: 0.1)
- `fireVolume`: Sound volume (default: 0.8)

### CannonBall
**Purpose**: Cannon projectile with explosion and shrapnel (extends Projectile).

**Features**:
- Direct-hit damage to Health components
- Explosion VFX at impact point
- Shrapnel spawn with physics-driven spread
- Shrapnel ignores collision with each other
- Rotation spin for visual effect

**Fields**:
- `explosionEffectPrefab`: VFX prefab spawned at impact
- `explosionEffectLifetime`: How long explosion VFX lives (0 = use VFX Stop Action)
- `shrapnelPrefab`: Shrapnel prefab (requires Rigidbody + Collider)
- `shrapnelCount`: Number of shrapnel pieces (default: 16)
- `shrapnelSpeed`: Initial shrapnel velocity (default: 10)
- `shrapnelLifetime`: How long shrapnel exists (default: 2s)
- `shrapnelDamage`: Damage per shrapnel piece (default: 5)
- `shrapnelSpinSpeed`: Angular rotation speed (default: 360°/s)

**Shrapnel Setup**:
- Prefab must have:
  - Rigidbody (useGravity = true recommended)
  - Collider (non-trigger for collision detection)
  - Optional: Projectile component for damage-on-hit
  - Optional: Visual mesh (small sphere, etc.)

**Impact Behavior**:
1. Apply direct-hit damage from cannonball
2. Spawn explosion VFX at impact point
3. Spawn shrapnel in random outward directions
4. Apply Physics.IgnoreCollision between all shrapnel pieces
5. Destroy cannonball

### Projectile & Shrapnel
**Purpose**: Base projectile class with damage-on-collision.

**Features**:
- Collision-based damage (OnCollisionEnter)
- Automatic lifetime destruction
- Optional hit VFX at impact
- Rigidbody-based physics movement

**Requirements**:
- Rigidbody component (required)
- Collider component (required, non-trigger)
- Velocity set by spawner (ProjectileLauncher)

**Fields**:
- `damage`: Damage dealt on collision
- `lifeTime`: Auto-destroy after this many seconds
- `hitEffectPrefab`: Optional VFX on impact

**SimpleShrapnel**:
- Lightweight shrapnel without Projectile component
- Just physics object that exists for lifetime then destroys
- No damage-on-hit (pure visual/physics)

---

## Mount Systems
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
  - Health Bar Indicator:
    - Show Health Bar: Check to display health bar when weapon is mounted (requires Health component on weapon prefab)
    - Health Bar Offset: Position offset in pixels relative to weapon icon (default: 15, 0 = right side)
    - Health Bar Size: Width and height in pixels (default: 6, 20 = thin vertical bar)
    - Health Full Color: Color at 100% health (default: green)
    - Health Empty Color: Color at 0% health (default: red)
  - Ready Status Indicator:
    - Show Ready Indicator: Check to display ready status circle (weapon must have ProjectileLauncher with reload time)
    - Ready Indicator Offset: Position offset in pixels relative to weapon icon (default: -15, 0 = left side)
    - Ready Indicator Size: Diameter of circle in pixels (default: 8)
    - Ready Color: Color when weapon is ready to fire (default: green)
    - Not Ready Color: Color when weapon is reloading (default: red)

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
- Mounted Weapon Test: If your mount has autoPopulateOnStart enabled with a Cannon prefab:
  - Icon should switch to ShipsHUD-cannon.png
  - Health bar should appear next to the cannon icon (if weapon has Health component)
  - Ready indicator circle should appear and turn red after firing, green when reloaded
- Runtime Mount/Unmount: Mount a weapon via code - icon updates to weapon sprite; unmount weapon - icon reverts to Mount.png
- Health Testing: Use Health.TakeDamage() to reduce weapon health and watch the bar change from green to red
- Ready Testing: Fire the weapon and watch the ready indicator turn red during reload, then green when ready

### Health Bar and Ready Indicator Details

The health bar and ready indicator are optional visual elements that appear next to mounted weapons:

**Health Bar:**
- Vertical bar that shows weapon's current health (requires Health component on weapon prefab)
- Fills from bottom (0% health) to top (100% health)
- Color gradient: Full green at 100% health → Full red at 0% health
- Position and size customizable per mount via Health Bar Offset and Health Bar Size
- Automatically hidden when mount is empty, shown when weapon mounted
- Updates every frame based on Health.currentHealth and Health.maxHealth

**Ready Indicator:**
- Circular indicator that shows if weapon is ready to fire (requires ProjectileLauncher with reloadTime > 0)
- Green when weapon.IsReadyToFire() returns true
- Red when weapon is reloading (after firing)
- Position and size customizable per mount via Ready Indicator Offset and Ready Indicator Size
- Automatically hidden when mount is empty, shown when weapon mounted
- Updates every frame based on ProjectileLauncher.IsReadyToFire()

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
- Health bar doesn't appear:
  - Verify Show Health Bar is checked on the MountHUDMarker
  - Ensure weapon prefab has a Health component attached
  - Health bar only shows when weapon is mounted (hidden when mount is empty)
- Health bar wrong color or fill:
  - Check Health Full Color and Health Empty Color settings
  - Verify Health component's currentHealth and maxHealth values
  - Bar fills from bottom (0%) to top (100%)
  - Color interpolates: green at full health → red as health decreases
- Ready indicator doesn't appear:
  - Verify Show Ready Indicator is checked on the MountHUDMarker
  - Ensure weapon prefab has ProjectileLauncher component
  - Indicator only shows when weapon is mounted
- Ready indicator always red (or always green):
  - Check ProjectileLauncher.reloadTime is set (default: 2.0 seconds)
  - Verify startReady setting (if false, weapon starts reloading)
  - Test by firing weapon - should turn red during reload, green when ready
  - Check Ready Color and Not Ready Color settings on marker

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
  - Check the mount's baseline: local +Z should be straight ahead at yaw=0/pitch=0.
  - In the mount, set launcherAxis to the spawn point's firing axis (often Up) and toggle invertLauncherAxis.
  - Ensure no negative scale on Ship/Model/Mount/pivots.
- Two projectiles/smoke plumes:
  - Ensure you aren't mounting twice (Ship helper + per‑mount auto). Use only one path.
- Cannon doesn't move with keys:
  - Enable debugKeypadControl on the correct mount; make sure the Game view has focus; check that pivots are assigned and limits are non‑zero.
- Ship falls instead of hovering:
  - Check LiftDevice.allocatedPowerPerSecond is set (should auto-set to minimum at start)
  - Verify minimumPowerPerSecond matches or exceeds ship weight in tons
  - Enable debugLog on LiftDevice to see power allocation
- Ship climbs too fast or falls too slowly:
  - Check powerPerTonPerMeterPerSecond value (1 = standard)
  - Verify ship weight in ShipCharacteristics matches actual design
  - Check allocatedPowerPerSecond value relative to minimumPowerPerSecond
- Ship tumbles after touching ground:
  - Should be fixed by FreezeRotation constraint (auto-applied by ShipCharacteristics)
  - Verify Rigidbody.constraints = FreezeRotation
- Lift device not working:
  - Ensure ship has ShipCharacteristics component
  - Verify LiftDevice has Health component
  - Check that gravity toggles correctly (watch Rigidbody.useGravity in Inspector during play)
- Engine not providing thrust:
  - Ensure ship has ShipCharacteristics component
  - Verify Engine.allocatedPowerPerSecond > 0
  - Check Engine has Health component
  - Enable debugLog to see thrust calculations

## Complete Ship Setup Workflow

### Step 1: Ship Root GameObject
```
Ship (root)
├── Add ShipCharacteristics component
│   ├── shipWeightTons = 30
│   └── dragCoefficient = 0.5
└── Rigidbody auto-created with:
    ├── mass = 30000 kg
    ├── useGravity = true
    ├── constraints = FreezeRotation
    └── linearDamping = 0.1
```

### Step 2: Add Engines (children of Ship)
```
Ship
└── Engine_Main (child GameObject)
    ├── Add JetEngine component
    │   ├── allocatedPowerPerSecond = 50
    │   ├── powerToThrustRatio = 1000
    │   ├── burnRateMultiplier = 150
    │   └── maxSafeTemperature = 100
    └── Add Health component
        └── maxHealth = 500
```

### Step 3: Add Lift Device (child of Ship)
```
Ship
└── AntiGrav_Main (child GameObject)
    ├── Add AntiGravityDevice component
    │   ├── minimumPowerPerSecond = 30 (match ship weight)
    │   ├── powerPerTonPerMeterPerSecond = 1
    │   ├── allocatedPowerPerSecond = 0 (auto-sets to 30)
    │   ├── fieldEfficiency = 1.0
    │   ├── fieldStability = 1.0
    │   └── altitudeCalibration = -100 (ground = 0)
    └── Add Health component
        └── maxHealth = 300
```

### Step 4: Add Weapon Mounts (under Ship/Model/Deck-mounts)
```
Ship/Model/Deck-mounts
└── Bow_mount_1
    ├── Add WeaponMount component
    │   ├── mountId = "bow_01"
    │   ├── mountType = "cannon"
    │   ├── yawLimitDeg = 90
    │   ├── pitchUpDeg = 30
    │   └── pitchDownDeg = 10
    ├── Create YawBase child → assign to yawBase field
    └── Create PitchBarrel child of YawBase → assign to pitchBarrel field
```

### Step 5: Create Weapon Prefabs
```
Cannon Prefab
├── Add Cannon component (extends ProjectileLauncher)
│   ├── projectilePrefab = CannonBall prefab
│   ├── fireKey = F
│   ├── launchSpeed = 50
│   ├── reloadTime = 2.0
│   ├── angleSpreadDegrees = 5
│   └── speedJitterPercent = 5
├── Add Health component
│   └── maxHealth = 100
├── Create Cylinder child (visual + spawnPoint)
│   └── Assign to spawnPoint field
├── Create MuzzleSmoke ParticleSystem child
│   └── Assign to muzzleSmoke field
└── Create MuzzleBlast ParticleSystem child
    └── Assign to MuzzleBlast field
```

### Step 6: Create Projectile Prefabs
```
CannonBall Prefab
├── Add CannonBall component (extends Projectile)
│   ├── damage = 25
│   ├── lifeTime = 5
│   ├── explosionEffectPrefab = Explosion VFX prefab
│   ├── shrapnelPrefab = Shrapnel prefab
│   ├── shrapnelCount = 16
│   ├── shrapnelSpeed = 10
│   └── shrapnelDamage = 5
├── Add Rigidbody component
│   └── useGravity = true
└── Add Collider component (Sphere/Capsule)
    └── isTrigger = false
```

### Step 7: Test Power Allocation
**Hover Test** (30-ton ship):
1. Set LiftDevice.allocatedPowerPerSecond = 30
2. Enter Play mode
3. Ship should hover perfectly (altitude unchanging)

**Climb Test**:
1. Set LiftDevice.allocatedPowerPerSecond = 60
2. Ship should climb at 1 m/s
3. Check AntiGravityDevice.currentAltitude increases steadily

**Descent Test**:
1. Set LiftDevice.allocatedPowerPerSecond = 15
2. Ship should descend at ~4.91 m/s (constant, no acceleration)

**Fall Test**:
1. Set LiftDevice.allocatedPowerPerSecond = 0
2. Ship should fall at 9.82 m/s² (Unity gravity)

**Thrust Test**:
1. Engine.allocatedPowerPerSecond = 50, burnRate = 150%, powerToThrustRatio = 1000
2. Expected thrust = 50 * 1.5 * 1000 = 75,000 N
3. Ship mass = 30,000 kg
4. Expected acceleration = 75,000 / 30,000 = 2.5 m/s²

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
