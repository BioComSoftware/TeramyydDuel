# Developer Instructions

**Last Updated:** 2025-12-14

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
4. [Crew Systems](#crew-systems)
  - [Crew Components](#crew-components)
  - [Authoring Workflow](#authoring-workflow)
  - [Persistence & Save Data](#persistence--save-data)
  - [Runtime Usage & Troubleshooting](#runtime-usage--troubleshooting)
5. [HUD Systems](#hud-systems)

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
- Crew skill integration:
  - On every Update the mount checks `crewStation.GetBestSkillLevel()` and feeds the result into `CrewSkillUtility.EvaluateAccuracyScale`.
  - The computed multiplier is pushed into the mounted `ProjectileLauncher` through `SetCrewAccuracyScale`, scaling both `angleSpreadDegrees` and `speedJitterPercent` (1.0 = worst accuracy, 0.0 = perfect aim).
  - Mounts also query `crewStation.GetStaffingRatio()` so understaffed stations blend back toward the baseline spread/reload penalties. Ratio `0` behaves like an unmanned weapon, `1` unlocks the full skill benefit.
  - When optional seats are filled, `crewStation.GetCrewRatio()` reports the >1 multiplier that divides the post-skill reload time. Two crew halve the reload delay, three cut it to one-third, etc.
  - Reload times now flow through `ProjectileLauncher.SetCrewReloadScale` using `CrewSkillUtility.EvaluateReloadScale`. Skill 1 ≈ 1.25× slower, Skill 10 ≈ 0.55× of the serialized `reloadTime`, and partial staffing pushes the value back toward a 1.5× penalty before the multi-crew bonus is applied.
  - Tune the launcher's serialized spread/jitter/reload as “untrained” baselines; the skill curve halves the penalty at skill 5, quarters it by skill 7, and removes it entirely at skill 10 provided the station is fully staffed. Any crew above the minimum then stack linearly for reload speed.

## ProjectileLauncherMount (specialized mount)
- Same pivot/limit pattern as WeaponMount but intended specifically for ProjectileLauncher weapons.
- Supports the same launcherAxis / invertLauncherAxis mapping and optional i/j/k/l debug controls.
- Choose either WeaponMount or ProjectileLauncherMount per your pipeline — not both on the same object.

## AutoPopulateLauncherMounts (optional Ship helper)
- Attach to the Ship root to quickly mount a given prefab into all empty mounts under the ship at Start.
- Fields: launcherPrefab, 
unOnStart.
- Note: Use either this helper OR the per‑mount utoPopulateOnStart — not both — to avoid double mounting.

---

## Crew Systems

### Crew Components
- **CrewSkill (enum)**: Discipline identifiers used throughout the sim (`Gunnery`, `Navigation`, `Repair`, `PowerEngineering`, `LiftEngineering`, plus `None` for “no focus”). Skills are floats ranging from 1–10, but stations can demand any minimum value.
- **CrewMember** (`Assets/Scripts/Crew/CrewMember.cs`): Attach to each crew prefab alongside `Health`. Key fields: `crewId` (auto `crew_<guid>` if blank), `displayName`, skill sliders for every discipline (no specialization flag anymore), `initialStationId`, and optional per-skill gain rates. At runtime the component tracks `AssignedStation`, `PendingStationId`, and handles training progress + persistence callbacks.
- **CrewStation** (`Assets/Scripts/Crew/CrewStation.cs`): Place anywhere an interaction point exists. Configure `stationId`, `displayName`, `primarySkill`, `minimumSkillLevel`, `enforceRequirements`, and optional training parameters (`trainingSkill`, `skillGainMultiplier`). Crew counts are now supplied by the owning subsystem through `CrewStation.SetCrewLimits`, so designers adjust headcount on the subsystem inspector (`defaultCrewRequired/defaultCrewMax`, etc.). Runtime helpers such as `GetBestSkillLevel()`, `GetStaffingRatio()`, `GetCrewRatio()`, `AssignedCrewCount`, and `IsUnderstaffed` let subsystems translate staffing into both baseline coverage and multi-crew bonuses.
- **CrewManager** (`Assets/Scripts/Crew/CrewManager.cs`): Singleton registry that wires stations and crew together, enforces min crew + skill thresholds, and exposes helper APIs such as `TryAssignCrewToStationId`, `RegisteredCrew`, and `GetUnassignedCrew()`. When `enforceCrewRequirements` is enabled it becomes the gatekeeper for engines, lifts, and weapon mounts.
- **CrewPersistenceManager** (`Assets/Scripts/Systems/CrewPersistenceManager.cs`): Serializes skill floats, health, and assignments to `Assets/Resources/CrewPersistence.json` in the Editor (or `Application.persistentDataPath` in builds). Exists as a `DontDestroyOnLoad` singleton so leveling progress survives scene loads.

### Complete Setup Workflow (Step-by-Step)

#### Phase 1: Scene Prerequisites (Do This First)
These singleton managers must exist before any crew systems work:

1. **Verify ShipCharacteristics**:
   - Select `Ship` root in Hierarchy
   - Ensure it has `ShipCharacteristics` component
   - If missing: **Add Component → Ship Characteristics**

2. **Add CrewPersistenceManager**:
   - Create empty GameObject in scene root: **GameObject → Create Empty**
   - Rename to `CrewPersistenceManager`
   - **Add Component → Crew Persistence Manager**
   - Configure:
     - Auto Save Enabled: ✓
     - Save Interval Seconds: 30.0

3. **Add CrewRuntimeSpawner**:
   - Create empty GameObject in scene root: **GameObject → Create Empty**
   - Rename to `CrewRuntimeSpawner`
   - **Add Component → Crew Runtime Spawner**
   - Crew Prefab: Drag `Assets/Prefabs/CrewMember_Default.prefab`

4. **Add UnassignedCrewAnchorBuilder**:
   - Under `Ship`, create child: **Right-click Ship → Create Empty**
   - Rename to `UnassignedCrewAnchors`
   - **Add Component → Unassigned Crew Anchor Builder**
   - Configure:
     - Anchors Per Row: 10
     - Crew Footprint Size: 1.0
     - Spacing Multiplier: 1.2
     - World Anchor Parent: Drag `Ship/UnassignedCrewAnchors` (itself)
   - Position where unassigned crew should stand

#### Phase 2: CrewStationRequirementProfile (Configure Prefabs FIRST)
**CRITICAL**: Do this BEFORE adding HUD slots or anchor builders.

For each weapon/engine/lift prefab:

1. **Open prefab** in Prefab Mode (double-click in Project)
2. Select prefab root
3. **Add Component → Crew Station Requirement Profile**
4. Configure:
   - **Primary Skill**: `Gunnery` (weapons), `PowerEngineering` (engines), `LiftEngineering` (lifts)
   - **Minimum Skill Level**: 1.0-10.0 (e.g., 3.0 for training guns)
   - **Minimum Crew Required**: 1 (how many must be assigned)
   - **Maximum Crew Allowed**: 4 (total seats)
   - **Training Skill**: Same as Primary Skill (or different if cross-training)
   - **Accrual Method**: `Time` (per-second) or `Event` (per-action)
   - **Skill Gain Per Second**: 0.01 (if Time)
   - **Accrual Event**: `PerFiring` (if Event)
   - **Skill Gain Per Event**: 0.05 (if Event)
5. **Save prefab**: Ctrl+S

**Example (Cannon)**:
- Primary Skill: Gunnery
- Minimum Skill Level: 3.0
- Minimum Crew Required: 1
- Maximum Crew Allowed: 4
- Training Skill: Gunnery
- Accrual Method: Event
- Accrual Event: PerFiring
- Skill Gain Per Event: 0.05

#### Phase 3: HUD Station Slot (Do This BEFORE Anchor Builder)
**WHY**: Anchor builder needs to reference the slot component.

For each station needing HUD icons:

1. Navigate to `HUD_Canvas/HUD_Root/ShipRepresentation/ShipOutline`
2. Find existing icon (e.g., `Bow_weapon_mount`)
3. Right-click → **Duplicate**
4. Rename to `Bow_weapon_mount_CrewSlot`
5. Position near original icon
6. Right-click slot → **UI → Image**
7. Rename child to `IconAnchor`
8. Resize to ~40x40 pixels
9. Select slot root
10. **Add Component → Crew HUD Station Slot**
11. Configure:
    - **Station**: Leave EMPTY (assign in Phase 4)
    - **Station Id Override**: Type exact ID (e.g., `BowWeaponMount_Crew`)
    - **Icon Anchor**: Drag `IconAnchor` child
    - **Highlight Image**: Drag slot's border Image
12. **Save scene**: Ctrl+S

#### Phase 4: Subsystem Integration (Hook Up Stations)
**IMPORTANT**: Profile applies settings automatically.

For each weapon/engine/lift in scene:

1. Locate subsystem (e.g., `Ship/Model/Bow_weapon_mount/Weapon_mount`)
2. Select GameObject with `WeaponMount`/`Engine`/`LiftDevice`
3. **Add Component → Crew Station**
4. Configure:
   - **Station Id**: Must match Phase 3's `stationIdOverride` (e.g., `BowWeaponMount_Crew`)
   - **Display Name**: Human-readable (e.g., `Bow Weapon Mount`)
   - **Enforce Requirements**: ✓
5. On subsystem component (`WeaponMount`/`Engine`/`LiftDevice`):
   - **Crew Station**: Drag the `CrewStation` component
   - **Auto Create Crew Station**: ✗ (uncheck)
6. Go back to HUD slot (`HUD_Canvas/.../Bow_weapon_mount_CrewSlot`)
7. On `CrewHUDStationSlot`:
   - **Station**: Drag `CrewStation` from `Ship/Model/Bow_weapon_mount/Weapon_mount`

#### Phase 5: CrewStationAnchorRuntimeBuilder (Add AFTER HUD Slot)
**WHY**: Builder references Phase 3's HUD slot.

For each multi-crew station:

1. Select subsystem (e.g., `Ship/Model/Bow_weapon_mount/Weapon_mount`)
2. **Add Component → Crew Station Anchor Runtime Builder**
3. Configure:
   - **Crew Station**: Drag `CrewStation` (same GameObject)
   - **Station HUD Slot**: Drag Phase 3 slot (e.g., `HUD_Canvas/.../Bow_weapon_mount_CrewSlot`)
   - **World Anchor Parent**: Create child:
     1. Right-click subsystem → **Create Empty**
     2. Rename to `CrewAnchor_World`
     3. Position where crew should stand
     4. Drag into field
   - **HUD Anchor Parent**: Drag HUD slot's `IconAnchor` child
   - **Crew Footprint Size**: 1.0
   - **Icon Spacing**: 40.0
4. **Save scene**: Ctrl+S

#### Phase 6: Crew Prefabs and Persistence

1. **Create crew prefab** (if not exists):
   - **GameObject → 3D Object → Capsule**
   - Rename to `CrewMember_Default`
   - **Add Component → Health**
     - Max Health: 100.0
     - Current Health: 100.0
   - **Add Component → Crew Member**
     - Crew Id: Leave blank
     - Display Name: "Crew Member"
     - Skills: Set to 5.0 each
     - Initial Station Id: Leave blank
   - Drag to `Assets/Prefabs/`
   - Delete from scene

2. **Seed crew data**:
   - Create `Assets/Resources/CrewPersistence.json`:

```json
{
  "version": "1.0.0",
  "lastSavedUtc": "2025-01-01T00:00:00.0000000Z",
  "crewMembers": [
    {
      "crewId": "crew_001",
      "displayName": "Ryn Calder",
      "gunnery": 4.5,
      "navigation": 6.2,
      "repair": 5.0,
      "powerEngineering": 3.8,
      "liftEngineering": 4.1,
      "maxHealth": 100.0,
      "currentHealth": 100.0,
      "assignedStationId": ""
    },
    {
      "crewId": "crew_002",
      "displayName": "Mira Voss",
      "gunnery": 7.0,
      "navigation": 4.0,
      "repair": 5.5,
      "powerEngineering": 6.0,
      "liftEngineering": 3.5,
      "maxHealth": 100.0,
      "currentHealth": 100.0,
      "assignedStationId": ""
    }
  ]
}
```

3. Verify `CrewRuntimeSpawner` has prefab assigned

#### Phase 7: Testing and Verification

1. **Enter Play Mode**
2. **Check Console**:
   - `[CrewManager] Initialized`
   - `[CrewPersistenceManager] Loaded X crew members`
3. **Verify crew spawning**:
   - Expand `CrewRuntimeSpawner` in Hierarchy
   - Should see `CrewMember_Default(Clone)` instances
4. **Verify unassigned anchors**:
   - Expand `Ship/UnassignedCrewAnchors`
   - Should see `UnassignedAnchor_0`, `UnassignedAnchor_1`, etc.
   - Crew prefabs should be positioned there
5. **Verify station anchors**:
   - Expand weapon/engine with `CrewStationAnchorRuntimeBuilder`
   - Should see `CrewAnchor_HUD_0`, `CrewAnchor_HUD_1` (up to `maximumCrewAllowed`)
   - Should see world anchors under `CrewAnchor_World`
6. **Test assignment** (via Inspector):
   - Select `CrewMember_Default(Clone)`
   - Set `Pending Station Id` to `BowWeaponMount_Crew`
   - Crew should move to world anchor
   - HUD icon should appear
7. **Test skill effects**:
   - Assign crew with different skills
   - Fire weapon, observe spread/reload differences
8. **Test persistence**:
   - Assign crew, wait 30s
   - Stop Play Mode
   - Check `CrewPersistence.json` updated
   - Re-enter Play Mode
   - Crew should auto-assign to saved stations

### Authoring Workflow (Quick Reference)
1. **Prep crew prefabs**: Add `Health`, then `CrewMember`. Fill skills; leave `crewId` blank.
2. **Place crew**: Drop anywhere under ship. Set `initialStationId` for auto-seating.
3. **Create stations**: Add `CrewStation` to subsystems. Set stable `stationId`s. Configure `primarySkill` + `minimumSkillLevel`.
4. **Hook stations**: Link `CrewStation` to `WeaponMount`/`Engine`/`LiftDevice`. Disable `autoCreateCrewStation` if manually assigned.
5. **Verify wiring**: Enter Play Mode. Enable `CrewManager.debugLog` to watch registrations.
6. **UI interaction**: HUD should call `CrewManager.TryAssignCrewToStationId()` on drag/drop.

#### Skill Wiring Checklist
1. **Author station requirements**
  - Decide which skill drives the subsystem bonus (`Gunnery`, `Navigation`, `Repair`, `PowerEngineering`, `LiftEngineering`).
  - On each `CrewStation`, set `primarySkill` accordingly and enter the minimum acceptable skill (e.g., 3.5 for training cannons, 6+ for capital mounts). If the station should train a different discipline (e.g., Navigation station trains Repair for triage), set `trainingSkill`; otherwise leave it at `None` to default to the primary.
2. **Link stations to gameplay scripts**
  - Assign the station reference directly in the inspector for `WeaponMount`, `Engine`, `JetEngine`, `LiftDevice`, `AntiGravityDevice`, etc. If the system lives on a spawned prefab, enable `autoCreateCrewStation` and configure `defaultCrewSkill`, `defaultCrewRequired`, and `defaultCrewMax` so the runtime-generated station mirrors your intent.
  - For weapon prefabs, no additional wiring is required: `WeaponMount` automatically queries `crewStation.GetBestSkillLevel()` and pushes the resulting accuracy multiplier into the attached `ProjectileLauncher`.
3. **Verify bonuses in Play Mode**
  - Select the mount and watch the inspector-only `currentLauncher` field. When crew sit down, `WeaponMount` logs the applied accuracy scale if `enableDebugLogging` is true.
  - With the HUD running, drag crew of different skill levels onto the mount and observe how `angleSpreadDegrees`/`speedJitterPercent` shrink (value stream is visible in the `ProjectileLauncher` inspector while in play).
  - Repeat the process for engines/lift devices once their subsystems consume skill curves (power allocation, repair speed, etc.).

### Persistence & Save Data
- Snapshot format (`CrewPersistenceSnapshot`) contains version, `lastSavedUtc`, and an array of `CrewMemberState` records (id, skill floats, health, current + pending station IDs).
- Autosave every `saveIntervalSeconds` (default 30). Toggle `autoSaveEnabled` while profiling to reduce IO noise.
- Editor data path: `Assets/Resources/CrewPersistence.json`. Delete to reset roster; a new file is generated at next launch.
- Build data path: `Application.persistentDataPath/CrewPersistence.json`. Surface a debug button to nuke saves when needed.
- Fractional health from `Health` is persisted, so injuries carry forward. Death currently sets `currentHealth = 0` but leaves the roster entry intact for future revival systems.

#### JSON Layout
Each entry under `crewMembers` mirrors `CrewMemberState` and must include:

| Field | Description |
| --- | --- |
| `crewId` | Unique identifier (auto-filled if left blank in the scene, but required in the JSON seed). |
| `displayName` | UI-friendly name. |
| `gunnery`, `navigation`, `repair`, `powerEngineering`, `liftEngineering` | Float skills (1–10 recommended) for every discipline. |
| `maxHealth`, `currentHealth` | Health values copied to the `Health` component at load time. |
| `assignedStationId` | Station to re-seat on load. Use empty string to start unassigned. |

**Example (`Assets/Resources/CrewPersistence.json`)**

```json
{
  "version": "1.0.0",
  "lastSavedUtc": "2025-11-30T00:00:00.0000000Z",
  "crewMembers": [
    {
      "crewId": "crew_ryn_calder",
      "displayName": "Ryn Calder",
      "gunnery": 4.5,
      "navigation": 6.2,
      "repair": 5.0,
      "powerEngineering": 3.8,
      "liftEngineering": 4.1,
      "maxHealth": 100.0,
      "currentHealth": 100.0,
      "assignedStationId": ""
    }
  ]
}
```

Drop-in replacements should follow this structure; the persistence manager will auto-append new crew as they appear in scenes and update the file whenever skills/health change.

### Runtime Usage & Troubleshooting
- **Requirement enforcement**: Global on `CrewManager.enforceCrewRequirements`. When true, engines, lift devices, and weapon mounts call `CrewManager.MeetsRequirement()` each update; when false, requirements short-circuit for rapid iteration. You can also toggle each `CrewStation.enforceRequirements` for optional redundancy seats.
- **Systems stuck “awaiting crew”**: Check that the system’s `crewStation` reference points to the right component. Auto-created stations live on the same GameObject; inspect during Play to ensure the subsystem’s `defaultCrewRequired/defaultCrewMax` values make sense. The manager clamps invalid pairs automatically, but mismatched IDs still prevent assignments.
- **Missing stations**: If a crew member’s `initialStationId` doesn’t exist yet, the manager stores it in `PendingStationId` and keeps retrying until a matching station registers. Use consistent IDs baked into prefabs to avoid typos.
- **UI hookup tips**: Build crew lists from `CrewManager.RegisteredCrew`. Idle crew = entries where `AssignedStation == null`. When dragging to a slot, call `TryAssignCrewToStationId`; the manager validates requirements and handles unassigning from previous stations.
- **Testing shortcuts**: Enable `debugLog` on `WeaponMount`, `Engine`, or `LiftDevice` to see explicit “awaiting crew” vs “requirements satisfied” logs. Expose temporary inspector buttons to call `CrewManager.UnassignCrew()` for fast iteration. Toggle the global enforcement flag mid-play to verify fallback behavior.
- **Extensibility hooks**: Skills are floats by design (defaults land between 1–10). Use `CrewSkillUtility` helpers (e.g., `EvaluateAccuracyScale`) when translating skills into modifiers so tuning stays consistent. Set redundancy bonuses by raising a subsystem’s `defaultCrewMax` above `defaultCrewRequired`; the station inherits those limits automatically via `SetCrewLimits`.

### HUD Roster & Drag/Drop Setup
1. **Build the crew icon prefab** (stores in `Assets/UI/Prefabs/CrewHUDCrewIcon.prefab`):
  1. In the **Hierarchy**, locate your HUD canvas (e.g., `ShipHUD`) and right-click it → **UI → Image**. This creates a child `Image` object in the scene hierarchy (name it `CrewHUDCrewIcon_Working`). Set its `RectTransform` size to `100 x 100`; this is the portrait background that you can style in the Inspector.
  2. With `CrewHUDCrewIcon_Working` selected, right-click it in the **Hierarchy** → **UI → TextMeshPro → Text**. Rename the new child to `NameLabel`, anchor it to the top center, and assign this object to the `nameLabel` field later. (TMP components are required because the HUD scripts read `TMP_Text` fields.)
  3. Repeat the previous step to add another TextMeshPro child named `SpecializationLabel`. Anchor it to the bottom-right corner and format it to show the dominant skill/value. Add optional children for `pendingBackground` (UI → Image) and `pendingText` (UI → TextMeshPro → Text) if you want the “waiting for station” overlay.
  4. Still in the **Hierarchy**, select the root `CrewHUDCrewIcon_Working` object. In the Inspector click **Add Component** → search for `CrewHUDCrewIcon` and add it. Then click **Add Component** again and add a `CanvasGroup`. In the Image component, ensure **Raycast Target** remains checked so the icon can be dragged.
  5. Drag `CrewHUDCrewIcon_Working` from the **Hierarchy** into `Assets/UI/Prefabs/` within the **Project** window to create the prefab asset. Rename the saved prefab to `CrewHUDCrewIcon`. You can now delete the temporary instance from the scene; the prefab asset is what you will spawn at runtime.
2. **Create the tooltip panel**:
  > **TMP required**: Every text element on the tooltip must be created via **UI → TextMeshPro → Text**. The `CrewHUDTooltip` script now exposes `TMP_Text` fields, so legacy Text components will not wire up.
  1. **Hierarchy**: Expand `HUD_Canvas/HUD_Root`, right-click `ShipRepresentation` → **UI → Panel**. Rename the new child to `CrewHUDTooltip_Working` and position it beside `ShipOutline` using the RectTool so it overlays empty space (this will be the floating tooltip).
  2. **Inspector (CrewHUDTooltip_Working)**: In the Panel’s Image component, set `Color` alpha to ~160 so it is semi-transparent, then click **Add Component** → search `CrewHUDTooltip` and add it.
  3. **Hierarchy**: Right-click `CrewHUDTooltip_Working` → **UI → TextMeshPro → Text**. Rename this child `NameLabel`. In the Inspector set `Alignment = Upper Center`, anchor preset = top stretch (Alt+Shift+click top-center), and resize the RectTransform height to ~24. This text drives the crew name.
  4. **Hierarchy**: Repeat the previous step four more times to create `SkillLabel`, `StatsLabel`, `CurrentStationLabel`, and `HealthLabel`. Place them in vertical order down the panel using RectTransform Y offsets of -24, -48, -72, and -96 respectively so they stack evenly.
  5. **Hierarchy**: Create one more child by right-clicking `CrewHUDTooltip_Working` → **UI → Image** (make sure you pick the plain **Image**, not **Raw Image**). Rename it `HealthFill`. With the child selected, look for the **Image (Script)** component in the Inspector: first assign any sprite (a 1×1 white sprite works) so the control unlocks, then change **Type** from **Simple** to **Filled**, set **Fill Method = Horizontal**, **Fill Origin = Left**, and stretch it to width ~160 × height 10. This becomes the health bar graphic.
  6. **Inspector (CrewHUDTooltip component)**: Drag each child into the matching serialized field: `nameLabel` ← `NameLabel`, `skillLabel` ← `SkillLabel`, `statsLabel` ← `StatsLabel`, `currentStationLabel` ← `CurrentStationLabel`, `healthLabel` ← `HealthLabel`, and `healthFillImage` ← `HealthFill`. Drag the root `CrewHUDTooltip_Working` object into the `root` field.
  7. **Hierarchy**: Leave `CrewHUDTooltip_Working` disabled for runtime usage by unchecking its checkbox in the Inspector. Once everything is wired, drag `CrewHUDTooltip_Working` from the Hierarchy into `Assets/UI/Prefabs/` if you want it as a prefab, then delete the working instance from the scene and drag the prefab back under `ShipRepresentation` so the HUD references a clean asset.
3. **Add `CrewHUDController` to the HUD canvas**:
  1. **Hierarchy**: Click the root `HUD_Canvas` object (Screen Space overlay). In the **Inspector**, scroll to the bottom and click **Add Component** → search for `CrewHUDController` → press Enter to add it.
  2. **Hierarchy**: Under `HUD_Canvas/HUD_Root`, right-click in empty space → **UI → Empty Object**. Rename it `CrewPool`. With `CrewPool` selected, add a **Vertical Layout Group** (Add Component → search). Adjust `Child Alignment = Upper Left`, `Spacing = 10`, and set its RectTransform width to ~120 so it becomes the column that will host unassigned crew icons. This object becomes the “Unassigned Container”.
  3. **Hierarchy (make the highlight Image)**: Right-click `CrewPool` → **UI → Image** to create a child named `HighlightFrame`. In its RectTransform, zero out the Pos X/Y, set width to match the pool (e.g., 120) and height ≈ the portrait size (110–120). In the Image (Script) component, assign any 1×1 sprite (the default `UISprite` works), set `Color` to a bright hue, then drop the alpha channel to ~80–120 so it becomes translucent. Leave **Raycast Target** enabled so pointer events still register through the CanvasGroup on the crew icons.
  4. **Hierarchy (optional styling)**: To create an outline instead of a solid block, add a second Image child under `HighlightFrame`, shrink it by a few pixels, and set its alpha lower. Duplicate borders for top/bottom if you prefer a frame look. These children stay disabled by default; the zone script toggles them during hover.
  5. **Inspector**: Re-select the root `CrewPool`, click **Add Component** → search `CrewHUDUnassignedZone` and add it. Drag `HighlightFrame` (or whichever Image you styled) into the `highlightImage` field so the drop zone flashes when hovered.
  6. **Inspector (CrewHUDController on HUD_Canvas)**: Drag `HUD_Root/CrewPool` from the Hierarchy into the `Unassigned Container` field. Drag the same object into `Unassigned Drop Zone` (the script reads the `CrewHUDUnassignedZone` attached to it).
  7. Still on the controller, drag your saved `CrewHUDCrewIcon` prefab from `Assets/UI/Prefabs/` into `Icon Prefab`. Drag the tooltip prefab/instance (`CrewHUDTooltip` from step 2) into `Tooltip`. If the HUD uses a dedicated drag canvas (e.g., `HUD_Canvas/DragLayer`), drag that object into `Drag Canvas`; otherwise leave blank.
4. **Author ship-outline slots**:
  1. **Hierarchy**: Expand `HUD_Canvas/HUD_Root/ShipRepresentation/ShipOutline`. Right-click an existing square (e.g., `Bow_weapon_mount`) → **Duplicate**. Immediately rename the copy to match the subsystem (e.g., `Bow_weapon_mount_slot`). Before moving it, change the duplicated slot’s Image (Script) → **Source Image** to your blank-slot sprite (a neutral square that represents “empty crew”). The Bow mount icon shows a cannon silhouette; swapping the sprite now prevents empty stations from inheriting weapon art. After the sprite swap, use the RectTool to drag the slot beside the correct silhouette location; keep size around `50x50` for consistency.
  2. If the duplicated square does not contain an inner image, right-click it → **UI → Image** to add a child named `IconAnchor`. Resize this child slightly smaller (e.g., `40x40`) and give it a faint background color; this child keeps the portrait ratio stable.
  3. **Inspector (slot root)**: Click the duplicated slot object, then **Add Component** → `CrewHUDStationSlot`. Drag the inner image (either the existing child or the new `IconAnchor`) into the `iconAnchor` field. Drag the border/highlight Image (often the slot root) into `highlightImage`.
    4. In the same component, set up identification: 
      - If the relevant `CrewStation` is already placed in the scene (for example the runtime `Ship/Bow_weapon_mount`), drag the object that actually has the `CrewStation` component into the `station` field. The canonical setup is to add `CrewStation` directly to the subsystem’s serialized `Weapon_mount` child (`Ship/Bow_weapon_mount/Weapon_mount`). That component becomes the authoritative seat the HUD will reference. If the subsystem uses a different structure, add `CrewStation` to whichever child represents the interactive mount so a valid reference exists.
      - If the station will be spawned later (prefab or auto-created), leave `station` empty and type the exact ID (e.g., `Mount_Bow_Crew`) into `stationIdOverride`.
  5. **Optional world anchor**: In the 3D ship hierarchy (`Ship/Model/...`), right-click the subsystem and choose **Create Empty** to make a Transform positioned where the crew avatar should appear. Name it `Bow_weapon_mount_CrewAnchor`. Back on the slot’s Inspector, drag this Transform into `worldAnchor` so future 3D visuals know where to spawn.
5. **Connect `CrewHUDController` fields**:
  1. **Inspector (HUD_Canvas with CrewHUDController)**: Scroll through the serialized fields.
  2. Drag `HUD_Root/CrewPool` from the Hierarchy onto `Unassigned Container`. The same object should already contain `CrewHUDUnassignedZone`, so drag it again onto `Unassigned Drop Zone`.
  3. For `Ship Slots`, choose one of two paths:
     - Leave the list empty and click the **⋯** foldout to confirm it reads “Size 0” (the controller will auto-discover every `CrewHUDStationSlot` under `ShipRepresentation`).
     - Or click the **+** icon to add elements, then drag each slot (e.g., `HUD_Root/ShipRepresentation/ShipOutline/Bow_weapon_mount`) from the Hierarchy into the list in the order you want them displayed.
  4. Drag the tooltip asset/instance (e.g., `HUD_Root/ShipRepresentation/CrewHUDTooltip`) into `Tooltip`. If you saved it as a prefab earlier, drag the in-scene instance to keep references stable.
  5. Confirm `Icon Prefab` still references `CrewHUDCrewIcon` and optional fields such as `Drag Canvas` or `debugLog` are set as desired, then press **Ctrl+S** to save the scene.
6. **Playtest flow**:
  1. **Scene**: Make sure `Ship`, crew prefabs, and HUD are active. Click **File → Save** so any prefab overrides are stored.
  2. Hit the **Play** button. Watch the **Hierarchy** as `CrewHUDController` spawns one `CrewHUDCrewIcon` under `CrewPool` per entry in `CrewManager.RegisteredCrew` (check the Console for registration logs if nothing appears).
  3. Move your mouse over a portrait in the pool. In the **Inspector**, you can confirm `CrewHUDTooltip/root` became active; on-screen you should see Name/Skill/Health populate from the hovered crew member.
  4. Click and hold an icon, drag it onto a ship slot (e.g., `ShipOutline/Bow_weapon_mount`), and release when the slot highlight turns on. Watch the Console for `CrewManager.TryAssignCrewToStation` success messages and confirm the icon snaps into the slot.
  5. To unassign, drag the icon back from the slot until it hovers over `CrewPool` (the `CrewHUDUnassignedZone` highlight turns on) and release. The icon returns to full size in the pool and the slot becomes empty.
  6. For any crew assigned to stations that do not yet exist, look for icons tinted by `pendingBackground` with “Waiting for <stationId>” text. When you later add/register that station and re-enter Play Mode, the tint clears automatically.
7. **3D crew hand-off (planning for later)**:
  1. **Project**: In `Assets/Scripts/UI/` (or another systems folder), create a new C# script named `CrewVisualBridge`. Open it and add a serialized reference to `CrewHUDController` plus a prefab for your 3D crew avatar.
  2. **Script**: In `OnEnable`, subscribe to `controller.OnVisualAnchorChanged += HandleAnchorChanged;`. In `OnDisable`, unsubscribe. Implement `HandleAnchorChanged(CrewMember crew, CrewStation station, Transform worldAnchor)`.
  3. **Runtime logic**:
     - When `worldAnchor` is not null (crew seated at a slot with an anchor), either instantiate an avatar prefab at `worldAnchor.position/rotation` or move an existing pooled avatar there. Parent the avatar under `worldAnchor` so it follows ship motion.
     - When `worldAnchor` is null (crew returned to pool), disable or park the avatar at a staging Transform (e.g., `Ship/CrewHoldingArea`).
  4. **Inspector wiring**: Back in Unity, add `CrewVisualBridge` to a manager object (e.g., `Ship/CrewSystems`). Drag the `HUD_Canvas` → `CrewHUDController` component into the controller field, assign your avatar prefab, and populate any optional pools.
  5. **Station lookup helpers**: If you need additional context (like finding the actual `CrewStation` GameObject), call `CrewManager.Instance.TryGetStation(station.stationId, out var resolvedStation)` inside your handler. Use `resolvedStation.transform` when the HUD slot did not specify a `worldAnchor`.

> **Quick sanity checklist**: before playtesting, make sure the HUD canvas has an EventSystem, the crew icon prefab has a `CanvasGroup`, the unassigned container allows `Raycast Target`, and every `CrewHUDStationSlot` references the correct `stationId`. If a station’s icon never lights up, toggle `CrewManager.debugLog` to watch assignment attempts in the Console.

---

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

---

## HUD Systems

### Instrument Panel Overview
**Purpose**: Aircraft-style analog gauges displaying ship telemetry in real-time. All instruments read from ShipCharacteristics automatically.

**Complete Setup Guide**: See INSTRUMENT_PANEL_SETUP_GUIDE.md for full 7-phase step-by-step instructions.

### AirspeedIndicator
**Purpose**: Displays ship speed in knots using a rotating clock-hand.

**Location**: Attach to empty GameObject positioned over airspeed gauge face.

**Key Fields**:
- 
eedleTransform: RectTransform of the rotating needle (pivot at bottom-center)
- shipCharacteristics: Auto-discovered if not set
- maxAirspeedKnots: Maximum speed shown (default 12 knots = full rotation)
- dampingFactor: Smoothing speed (5 = smooth, 0 = instant)

**Setup**:
1. Create UI Image for needle sprite (thin pointer)
2. Set RectTransform pivot to (0.5, 0) - bottom center
3. Attach AirspeedIndicator script to parent container
4. Assign needle RectTransform reference

**Behavior**: Needle rotates clockwise from 12 o'clock (0 knots) to full circle (maxAirspeedKnots).

### AltimeterIndicator
**Purpose**: Three-hand altitude display (like real aircraft altimeter).

**Location**: Attach to empty GameObject positioned over altimeter gauge face.

**Key Fields**:
- 	ensHandTransform: Fastest hand (0-100m per rotation)
- hundredsHandTransform: Medium hand (0-1000m per rotation)
- 	housandsHandTransform: Slowest hand (0-10000m per rotation)
- shipCharacteristics: Auto-discovered if not set

**Setup**:
1. Create three UI Image needles (different colors for visibility)
2. All needles: pivot (0.5, 0), stacked at same position
3. Layer order: thousands (bottom)  hundreds  tens (top)
4. Attach AltimeterIndicator script
5. Assign all three hand RectTransforms

**Behavior**: 
- At 2,456 meters: thousands hand at 2, hundreds hand at 4.56, tens hand at 5.6
- All hands rotate independently and continuously

### VerticalSpeedIndicator
**Purpose**: Shows climb/descent rate in meters per second.

**Location**: Attach to empty GameObject positioned over VSI gauge face.

**Key Fields**:
- 
eedleTransform: Single rotating needle
- maxClimbRateMPS: Maximum rate shown (default 20 m/s)
- maxClimbRotationDegrees: Rotation for max climb (default 180 right)
- maxDescentRotationDegrees: Rotation for max descent (default -180 left)
- dampingFactor: Lag factor (3 = realistic VSI lag)

**Setup**:
1. Create UI Image needle
2. Set pivot to (0.5, 0)
3. Attach VerticalSpeedIndicator script
4. Assign needle RectTransform

**Behavior**:
- 12 o'clock = 0 m/s (level flight)
- Right (3 o'clock) = climbing
- Left (9 o'clock) = descending
- Intentional lag mimics real VSI behavior

### AttitudeIndicator
**Purpose**: Displays pitch, roll, and yaw using airplane silhouette and yaw triangle.

**Location**: Attach to empty GameObject positioned over attitude gauge face.

**Key Fields**:
- irplaneTransform: RectTransform of airplane sprite (rotates for roll, moves Y for pitch)
- yawTriangleTransform: RectTransform of triangle sprite (moves X for yaw)
- maxPitchDegrees: Pitch angle range (default 45)
- maxPitchMovementPixels: Vertical movement range for max pitch (default 40px)
- maxYawDegrees: Yaw angle range (default 45)
- maxYawMovementPixels: Horizontal movement range for max yaw (default 50px)

**Setup**:
1. Create airplane silhouette sprite (top-down view, wings visible)
2. Create triangle sprite for yaw indicator
3. Both: pivot (0.5, 0.5) - center
4. Position yaw triangle below airplane
5. Attach AttitudeIndicator script
6. Assign both RectTransforms

**Behavior**:
- **Roll**: Airplane sprite rotates (wings tilt)
- **Pitch**: Airplane sprite moves vertically (nose up/down)
- **Yaw**: Triangle moves horizontally (heading deviation)

### InstrumentPanelManager
**Purpose**: Coordinates all instruments; provides unified control and auto-wiring.

**Location**: Attach to container GameObject under instrument panel background.

**Key Fields**:
- shipCharacteristics: Auto-discovered if not set
- irspeedIndicator, ltimeterIndicator, erticalSpeedIndicator, ttitudeIndicator: Auto-discovered from children
- instrumentsEnabled: Master enable/disable
- panelCanvasGroup: Optional for fading entire panel

**Setup**:
1. Create manager GameObject as child of panel background
2. Attach InstrumentPanelManager script
3. Create all four instruments as siblings
4. Manager auto-links everything on Start()

**API Methods**:
- SetInstrumentsEnabled(bool enabled): Enable/disable all instruments
- SetPanelAlpha(float alpha): Fade panel (requires CanvasGroup)
- ShowPanel(): Make panel visible and enabled
- HidePanel(): Make panel invisible and disabled

### Quick Setup Workflow

1. **Import Sprites**:
   - Instrument panel background (brass panel image)
   - Needle sprites (thin pointers, pivot at bottom)
   - Airplane silhouette (top-down view)
   - Yaw triangle

2. **Create Canvas**:
   - UI  Canvas (Screen Space Overlay)
   - Add Canvas Scaler (Scale With Screen Size)
   - Set reference resolution (1920x1080)

3. **Add Background**:
   - UI  Image (child of Canvas)
   - Assign panel background sprite
   - Position at bottom of screen

4. **Build Instruments** (for each):
   - Create empty GameObject over gauge position
   - Add indicator script
   - Create needle Image children (correct pivots!)
   - Assign references

5. **Add Manager**:
   - Create empty GameObject
   - Attach InstrumentPanelManager
   - Enable debugLog for first test

6. **Test**:
   - Enter Play Mode
   - Check Console for setup confirmation
   - Move ship and verify instruments respond

### Troubleshooting

**Needles don't move**:
- Check ShipCharacteristics exists in scene
- Verify needle RectTransforms assigned
- Check pivot points (should be 0.5, 0 for needles)

**Wrong rotation direction**:
- Toggle 
otateClockwise setting
- Adjust zeroRotationDegrees

**Needles point wrong way**:
- Check pivot point (bottom-center for clock hands)
- Verify sprite points UP in source image

**Movement too fast/slow**:
- Adjust dampingFactor (higher = slower)

### Advanced Configuration

**Custom Ranges**:
- Airspeed: Change maxAirspeedKnots (if ship goes faster than 12 knots)
- VSI: Change maxClimbRateMPS (for higher climb rates)
- Pitch: Change maxPitchDegrees (for more extreme maneuvers)

**Visual Customization**:
- Needle colors: Select Image component, change Color
- Needle length: Adjust RectTransform Height
- Needle thickness: Adjust RectTransform Width

**Performance**:
- All instruments use Update() loop
- Damping reduces visual jitter
- Consider lower update rates if needed (modify scripts)

---

## Weapon Targeting & Ballistics (Updated 2025-12-14)

### LPLS Multi-Angle Targeting System

**Purpose**: WeaponMount calculates optimal firing solutions by testing multiple pitch angles and selecting the one requiring the lowest launch speed.

**How It Works**:
1. Tests 8 equal segments between max pitch up and max pitch down (9 total test points)
2. For each angle, calculates required launch speed using ballistic formula
3. Selects angle with lowest speed that stays within launcher's speed range
4. If no valid solution found, displays "Target Not Acquired"

**Example Pitch Tests** (±15° cannon):
```
-15.00° (max up)
-11.25°
 -7.50°
 -3.75°
  0.00° (level)
 +3.75°
 +7.50°
+11.25°
+15.00° (max down)
```

**Pitch Coordinate System (CRITICAL)**:
```csharp
// Unity GameObject rotation
float pitch = -15f;  // Aims barrel UP (negative = up)
float pitch = +15f;  // Aims barrel DOWN (positive = down)

// Physics calculations require NEGATION
float physicsAngle = -pitch;  // Convert for ballistic formulas
// Because physics formulas expect positive angle = up
```

**Cannon Movement Behavior**:
- **Yaw (horizontal)**: Continuously tracks target
- **Pitch (vertical)**: Stays at last firing angle between shots
- **Just before firing**: Pitch snaps to calculated ballistic angle
- **After firing**: Pitch remains at firing angle (no reset)

**Configuration** (WeaponMount Inspector):
- `pitchUpDeg`: Maximum upward pitch angle (e.g., 15°)
- `pitchDownDeg`: Maximum downward pitch angle (e.g., 15°)
- `yawLimitDeg`: Horizontal arc (e.g., 60° for ±30°)
- `autoAimYawSpeedDegPerSec`: How fast yaw tracks (e.g., 45°/s)
- `autoAimPitchSpeedDegPerSec`: Not used during tracking, only for manual control

**Debug Logging** (enable on WeaponMount):
```
CHECK 1 PASSED: Target within yaw arc. TargetYaw=12.3° Limit=±30.0°
CHECK 2 PASSED: Optimal LPLS solution. Yaw=12.3° Pitch=-8.5° Speed=45.2 | Target: H=120.5m V=15.3m
```

### Crew Skill Impact on Accuracy

**Accuracy Scale Formula** (CrewSkillUtility.cs):
- Skill 10 → 0.0 scale → Perfect accuracy (no spread/jitter)
- Skill 7 → 0.25 scale → 25% of base spread/jitter
- Skill 5 → 0.5 scale → 50% of base spread/jitter
- Skill 1 → 1.0 scale → Full spread/jitter

**ProjectileLauncher Configuration**:
- `angleSpreadDegrees`: Base cone spread (e.g., 5°)
- `speedJitterPercent`: Launch speed variance (e.g., 10%)
- `disableAccuracyError`: Override to disable all spread/jitter

**Effective Calculation**:
```csharp
float effectiveSpread = angleSpreadDegrees * crewAccuracyScale;
float effectiveJitter = speedJitterPercent * crewAccuracyScale * 0.01f;
```

**Debug Logging** (enable on ProjectileLauncher):
```
[ProjectileLauncher] Fire accuracy: angleSpread=5.00°, crewScale=1.000, finalSpread=5.00°, disableError=False
```

---

## Crew Persistence & Save System (Updated 2025-12-14)

### CrewPersistence.json Structure

**Location**: `Assets/Resources/CrewPersistence.json`

**JSON Format**:
```json
{
    "version": "1.0.0",
    "lastSavedUtc": "2025-12-14T12:34:56Z",
    "crewMembers": [
        {
            "crewId": "crew_serena_brasswolf",
            "displayName": "Serena Brasswolf",
            "gunnery": 1.0,
            "navigation": 1.0,
            "repair": 1.0,
            "powerEngineering": 5.5,
            "liftEngineering": 3.2,
            "maxHealth": 100.0,
            "currentHealth": 85.0,
            "assignedStationId": "Bow_weapon_mount_01_crew_slot"
        }
    ]
}
```

**Authority Rules**:
1. **At Game Start**: JSON is AUTHORITATIVE - all skill values loaded from JSON override prefab defaults
2. **During Gameplay**: CrewMember runtime values are authoritative - saved back to JSON on quit/autosave
3. **Prefab Values**: Only used for NEW crew members not in JSON

**Save Timing**:
- **Auto-save**: Every 30 seconds (if changes made)
- **On Quit**: Forced save via `OnApplicationQuit()`
- **Manual**: Call `CrewPersistenceManager.Instance.SaveSnapshot()`

### Common Issues & Solutions

**Issue: Crew showing wrong skill values**
- **Symptom**: JSON has `gunnery: 1.0` but game shows `gunnery: 10.0`
- **Cause**: JSON file wasn't saved properly after editing
- **Solution**: Edit JSON in external editor, save, verify file timestamp updated, reload Unity

**Issue: Duplicate crew entries in JSON**
- **Symptom**: Same crew appears multiple times in JSON
- **Cause**: Previous bug (now fixed) that allowed duplicates
- **Solution**: System auto-deduplicates on load, logs warnings, saves clean version

**Issue: Crew not spawning at assigned stations**
- **Symptom**: Crew assigned in JSON but spawns in unassigned zone
- **Cause**: Anchors not registered before crew spawning
- **Solution**: System now waits one frame for anchor registration (auto-fixed)

### Debug Logging

**CrewPersistenceManager** (enable debugLog):
```
[CrewPersistence] LoadSnapshot: Successfully loaded 10 crew members from JSON
[CrewPersistence] Loaded crew: crew_serena_brasswolf (Serena Brasswolf) -> Station: Bow_weapon_mount_01_crew_slot
[CrewPersistence] ApplySkillState to crew_serena_brasswolf: Before=(G:10.0 N:1.0), JSON=(G:1.0 N:1.0)
[CrewPersistence] ApplySkillState to crew_serena_brasswolf: After=(G:1.0 N:1.0)
[CrewPersistence] WARNING: Duplicate crew ID 'crew_morgan_lee' found in JSON and removed
```

### Station ID Best Practices

**Naming Convention**:
- WeaponMount: `{GameObject.name}_crew_slot` (e.g., "Bow_weapon_mount_01_crew_slot")
- Engine: `{GameObject.name}_EngineCrew` (e.g., "Port_Engine_EngineCrew")
- LiftDevice: `{GameObject.name}_LiftCrew` (e.g., "Main_AntiGravity_LiftCrew")

**Critical Rule**: Use GameObject.name for uniqueness, NOT mountId (which may be duplicated across multiple mounts)

**Setup Verification**:
1. Select weapon mount/engine/lift device in Hierarchy
2. Check GameObject name at top of Inspector (must be unique)
3. Play game and check Console for "CrewStation registered: {stationId}"
4. Verify no "WARNING: Station {stationId} already registered" messages

---
