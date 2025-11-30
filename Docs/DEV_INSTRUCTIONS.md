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
  - Tune the launcher’s serialized spread/jitter as “untrained” baselines; the skill curve halves the penalty at skill 5, quarters it by skill 7, and removes it entirely at skill 10.

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
- **CrewStation** (`Assets/Scripts/Crew/CrewStation.cs`): Place anywhere an interaction point exists. Configure `stationId`, `displayName`, `primarySkill`, `minimumSkillLevel`, `minimumCrewRequired`, `maximumCrewAllowed`, `enforceRequirements`, and optional training parameters (`trainingSkill`, `skillGainMultiplier`). The helper `GetBestSkillLevel()` is how systems query the top-performing operator on that station.
- **CrewManager** (`Assets/Scripts/Crew/CrewManager.cs`): Singleton registry that wires stations and crew together, enforces min crew + skill thresholds, and exposes helper APIs such as `TryAssignCrewToStationId`, `RegisteredCrew`, and `GetUnassignedCrew()`. When `enforceCrewRequirements` is enabled it becomes the gatekeeper for engines, lifts, and weapon mounts.
- **CrewPersistenceManager** (`Assets/Scripts/Systems/CrewPersistenceManager.cs`): Serializes skill floats, health, and assignments to `Assets/Resources/CrewPersistence.json` in the Editor (or `Application.persistentDataPath` in builds). Exists as a `DontDestroyOnLoad` singleton so leveling progress survives scene loads.

### Authoring Workflow
1. **Prep crew prefabs**: Add `Health`, then `CrewMember`. Fill the display name and all skill sliders (Gunnery/Nav/Repair/Power/Lift); leave `crewId` blank unless you need deterministic IDs across variants.
2. **Place crew in the scene**: Drop crew prefabs anywhere under the ship. Physical hierarchy does not matter for assignments. Optionally set `initialStationId` to auto-seat them.
3. **Create crew stations**: For every subsystem needing staff, add `CrewStation` (often as a child of the subsystem). Give stable `stationId`s (`Engine_Main_Crew`, `BowGun_Port`). Configure `primarySkill`, `minimumSkillLevel`, and headcount limits.
4. **Hook stations into systems**: `WeaponMount`, `Engine`, and `LiftDevice` expose `crewStation`, `autoCreateCrewStation`, `defaultCrewSkill`, and min/max crew fields.
  - Preferred: author a dedicated `CrewStation`, set `primarySkill` + `minimumSkillLevel`, assign it to the component, and disable `autoCreateCrewStation` once the scene object is stable.
  - Temporary/testing: leave the reference empty with `autoCreateCrewStation = true`. The component will spawn a basic station that targets `defaultCrewSkill`, requires at least `defaultCrewRequired` crew, and enforces minimum skill 1. Tweak those defaults per prefab so designers know which skill improves that subsystem.
5. **Verify runtime wiring**: Enter Play Mode. The first crew/station registration spawns `CrewManager`. Watch the Console (set `CrewManager.debugLog = true`) to confirm registrations. Use `CrewManager.GetUnassignedCrew()` in a temporary `Debug.Log` to see who still needs a seat.
6. **UI/interaction**: The HUD or in-world consoles should call `CrewManager.TryAssignCrewToStationId(crew, stationId)` when the designer drags/drops crew icons. The manager queues requests if the station loads later.

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
      "crewId": "crew_default_quartermaster",
      "displayName": "Quartermaster Ryn Calder",
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
- **Systems stuck “awaiting crew”**: Check that the system’s `crewStation` reference points to the right component. Auto-created stations live on the same GameObject; inspect during Play to ensure min/max counts make sense. Watch the Console for warnings when `minimumCrewRequired > maximumCrewAllowed` (values auto-clamped).
- **Missing stations**: If a crew member’s `initialStationId` doesn’t exist yet, the manager stores it in `PendingStationId` and keeps retrying until a matching station registers. Use consistent IDs baked into prefabs to avoid typos.
- **UI hookup tips**: Build crew lists from `CrewManager.RegisteredCrew`. Idle crew = entries where `AssignedStation == null`. When dragging to a slot, call `TryAssignCrewToStationId`; the manager validates requirements and handles unassigning from previous stations.
- **Testing shortcuts**: Enable `debugLog` on `WeaponMount`, `Engine`, or `LiftDevice` to see explicit “awaiting crew” vs “requirements satisfied” logs. Expose temporary inspector buttons to call `CrewManager.UnassignCrew()` for fast iteration. Toggle the global enforcement flag mid-play to verify fallback behavior.
- **Extensibility hooks**: Skills are floats by design (defaults land between 1–10). Use `CrewSkillUtility` helpers (e.g., `EvaluateAccuracyScale`) when translating skills into modifiers so tuning stays consistent. `CrewStation.maximumCrewAllowed > minimumCrewRequired` still supports redundancy bonuses (e.g., second engineer adds a passive buff but is optional).

### HUD Roster & Drag/Drop Setup
1. **Build the crew icon prefab** (Assets/UI/Prefabs/CrewHUDCrewIcon?):
  1. UI → Image (100x100) = portrait background.
  2. Add child Text for `nameLabel` (top), another Text for `specializationLabel` (bottom-right, shows the dominant skill + value), optional overlay Image/Text for `pendingBackground`/`pendingText` (hidden by default).
  3. Add `CrewHUDCrewIcon` component, wire the references, assign optional role sprites (General/Gunnery/Navigation/Repair/Power/Lift icons).
  4. Add `CanvasGroup` (required by script) and mark Raycast Target on Image true so dragging works.
2. **Create the tooltip panel**:
  1. UI → Panel next to the ship outline, add `CrewHUDTooltip`.
  2. Inside panel, add Text objects for Name, Skill Label, Stats (`Gun xx / Nav xx` line), Current Station, and Health; add an Image with Image Type = Filled for `healthFill`.
  3. Assign these references on `CrewHUDTooltip`, drag the panel root into the `root` field, leave it disabled in the scene.
3. **Add `CrewHUDController` to the HUD canvas**:
  1. Select the Ship HUD Canvas → Add Component → `CrewHUDController`.
  2. Set `Unassigned Container` to a vertical layout group sitting beside the ship outline (these slots are the “crew pool”).
  3. Add `CrewHUDUnassignedZone` to the same RectTransform so drops there unassign crew; point its `highlightImage` to a semi-transparent frame.
  4. Assign the crew icon prefab to `Icon Prefab`, hook the tooltip, and (optionally) specify a dedicated drag canvas if your HUD uses nested canvases.
4. **Author ship-outline slots**:
  1. For every subsystem that wants visible crew, duplicate a small square (`50x50`) next to the relevant element on the ship outline.
  2. Add `CrewHUDStationSlot` to that square, drag its `iconAnchor` to the inner image (so the portrait scales inside), and point `highlightImage` to a subtle border.
  3. Enter the actual `CrewStation` reference if the scene object is accessible; otherwise type the `stationIdOverride` string (e.g., `Mount_Bow_Crew`).
  4. Optional: create an empty child under the 3D ship mesh, position it where the real crew should stand, and assign it to `worldAnchor`. This lets the game spawn live crew objects later using `CrewHUDController.OnVisualAnchorChanged`.
5. **Connect `CrewHUDController` fields**:
  - `Unassigned Container`: the left/right column hosting free crew slots.
  - `Unassigned Drop Zone`: the same RectTransform with `CrewHUDUnassignedZone` on it.
  - `Ship Slots`: leave empty to auto-discover, or drag explicit `CrewHUDStationSlot` entries if you prefer manual ordering.
  - `Tooltip`: drag the tooltip panel you made earlier.
6. **Playtest flow**:
  1. Enter Play Mode → the controller instantiates one icon per `CrewManager.RegisteredCrew`.
  2. Hover over any portrait to see stats, dominant skill label, current/pending station, and live health pulled from the `Health` component.
  3. Drag a portrait from the pool onto a ship slot → the controller calls `CrewManager.TryAssignCrewToStation` and, on success, snaps the shrunken portrait into that slot.
  4. Drag from a ship slot back to the pool → drops onto the unassigned zone, which calls `CrewManager.UnassignCrew` and frees the subsystem.
  5. Pending stations (station exists later or is outside the HUD) tint the icon with the `pendingBackground` color and display “Waiting for <stationId>`.
7. **3D crew hand-off (planning for later)**:
  - Subscribe to `CrewHUDController.OnVisualAnchorChanged` to know when a crew member gains/loses a slot. The event passes `(CrewMember crew, CrewStation station, Transform worldAnchor)`.
  - Spawn/enable a 3D crew avatar at the provided `worldAnchor` so the battlefield representation mirrors the HUD assignment. On pool/unassign, despawn or park the avatar at a neutral location.
  - `CrewManager.RegisteredStations` and `CrewManager.TryGetStation(stationId, out CrewStation)` exist so authoring tools (or later mission scripts) can query the same data without digging into dictionaries.

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
