# Teramyyd Game Development Journal

## AI Snapshot (2025-11-14 — Mount orientation, debug controls, HUD markers)

Purpose: lock in weapon mount orientation rules, runtime dev controls for pivots, launcher variance knobs, and HUD ship representation markers by occupancy/type.

Mounting/Orientation
- Standardized how launchers align when auto-mounted:
  - The selected launcher axis (usually spawnPoint.up) is mapped to the mount’s forward contract (currently mount −Z by design to satisfy the cannon prefab without editing it).
  - Added `launcherAxis` (Up/Forward/Right) and `invertLauncherAxis` to handle prefabs whose natural axis differs or points the opposite way.
  - Mount exposes pivots: `yawBase` (rotate around local Y) and `pitchBarrel` (rotate around local X). Weapon prefab is parented under `pitchBarrel`.
  - Clamps: `yawLimitDeg` (± half-range), `pitchUpDeg`, `pitchDownDeg` enforce movement limits.
- Avoid double-mounting: use either ship-level auto-populate or per-mount `autoPopulateOnStart`, not both.

Runtime Dev Controls (temporary)
- Added letter keys for quick testing during Play (when enabled on the mount):
  - `j` = yaw left, `l` = yaw right; `i` = pitch up, `k` = pitch down.
  - Optional direction flips: `invertYawDirection`, `invertPitchDirection`.
  - Obeys clamps and only affects the assigned `yawBase`/`pitchBarrel`.

Launcher Variance (tunable)
- `ProjectileLauncher` exposes runtime-tunable accuracy fields:
  - `angleSpreadDegrees = 5f` (max cone spread in degrees).
  - `speedJitterPercent = 5f` (± percent of launch speed).
- Gameplay code can tighten/loosen these at runtime (e.g., crew skill).

HUD — Ship Representation Markers
- Added ship-driven HUD representation that can display mount markers by `mountId`:
  - Each marker has a normalized position within the ship icon.
  - Code selects which sprite to show based on occupancy/type:
    - Uses a `defaultEmptySprite` for unoccupied mounts.
    - Uses a type→sprite map (e.g., "cannon") for occupied mounts.
- No “unknown populated” default; populated sprites must be mapped by type.

Notes
- We deliberately “lie” the mount forward to use mount −Z, so we don’t have to edit the cannon prefab’s internal axes. If/when we standardize to +Z, keep the mapping toggles to migrate smoothly.
- Keep transforms non-mirrored (positive scale) to avoid axis confusion.

## AI Snapshot (2025-11-12 — Ship HUD, collider, data stubs)

Purpose: capture new ship-driven HUD wiring, triangle collider tooling, and persistence/data stubs (no square HUD overlay).

Added Systems/Data
- PlayerProfile + SaveSystem:
  - PlayerProfile: captainId, gold, reputation, ships (OwnedShip), activeShipId, crew, Inventory (ItemStacks).
  - SaveSystem: JSON save/load to persistentDataPath (profile_<id>.json).
- ScriptableObjects:
  - ItemDefinition: id/name/value/stack rules.
  - WeaponDefinition: id, weaponPrefab, weaponType, cost, baseline stats.
  - ShipDefinition: id, base stats, List<ShipMountConfig> (mountId + yaw/pitch limits/acceptedType).

Ship HUD Representation (from Ship → HUD Canvas)
- Ship component: ShipHUDRepresentation (on Ship root) exposes hudSprite, opacity, anchor (9‑point), anchoredOffset (px), size (px);
  auto-registers/unregisters with the HUD.
- HUD view: ShipHUDPanel (on HUD Canvas) builds/owns a RectTransform + Image and applies the ShipHUDRepresentation each frame.
- Default anchor = CenterRight; design can tune from the Ship side per ship prefab.

Triangle Collider Tooling
- EquilateralTriangleCollider3D (procedural MeshCollider):
  - Geometry: sideLength (legacy equilateral) or width+length (isosceles), thickness (Z depth).
  - Placement/orientation: centerOffset (local), rotationEuler (local X/Y/Z). Convex MeshCollider, rebuilds on Validate.

Notes
- ProjectileLauncher variance (angle spread + speed jitter) is the firing contract; gameplay (crew skill) should tune these live.
- Ship prefab authoring contract (Model/Deck-mounts/camera mounts) remains the basis for automatic wiring.


## AI Snapshot (2025-11-12)

Purpose: late-session delta; capture runtime tuning and content wiring for quick resume.

Changes
- ProjectileLauncher variance (runtime):
  - Fields: angleSpreadDegrees (deg), speedJitterPercent (±%).
  - Applied at fire: builds orthonormal basis around spawnPoint.up, rotates by random tilt ≤ spread and random azimuth; speed scaled by Random[1−jitter, 1+jitter].
  - Velocity set via Rigidbody.linearVelocity if available, else Rigidbody.velocity.
- Cannon/Cannonball pipeline maintained:
  - Cannon inherits variance; audio child node follows muzzle; health lives on 3D visual child; self-damage via CannonSelfDamage (fractional accumulate).
  - Projectile parent-friendly: Rigidbody on root, Colliders on children; still applies Health damage and spawns hit VFX.
- CannonBall (Projectile subclass): explosion VFX + optional shrapnel (RB+Collider; optional Projectile for shrapnel damage).
- VFX content: sparks/smoke tuned (drag/dampen/lifetime), URP particles materials, soft particles; flipbook/gif guidance.
- Audio: cannon audio hardened (child AudioSource at muzzle). Explosion audio recommended on Explosion VFX root (AudioSource, Play On Awake, 3D).
- UI: Spacebar Bridge-switch cause = UI Submit on focused button; code path unchanged.
- Recorder/URP/material how-to captured for future reference.

Runtime Tuning (code)
- Access any launcher (Cannon derives from ProjectileLauncher) and set fields at runtime:
  - `var launcher = GetComponent<ProjectileLauncher>();`
  - `launcher.angleSpreadDegrees = 5f;`  // max angular deviation (degrees)
  - `launcher.speedJitterPercent = 5f;`  // ±percentage of launchSpeed
- Crew skill mapping example (0..1 skill → tighter spread/jitter):
  - `float s = crew.Skill01;`
  - `launcher.angleSpreadDegrees = Mathf.Lerp(8f, 1f, s);`
  - `launcher.speedJitterPercent = Mathf.Lerp(8f, 1f, s);`
- Spawn axis contract: `spawnPoint.up` is the muzzle axis; keep its local Y aligned with barrel direction.
- Logging: Fire prints pos/dir/speed/spread per shot for tuning (can be silenced later).

Gotchas / Setup Contracts
- Single Rigidbody per projectile hierarchy (on root). Child colliders OK. Non-trigger for collisions.
- Explosion VFX lifetime: either PS Stop Action = Destroy (and set CannonBall.explosionEffectLifetime = 0) or ensure lifetime ≥ audio length.
- Sparks boxes: use URP/Particles/Unlit Additive with soft alpha edges, clamp wrap; Smoke uses Alpha blending, longer lifetime and slower alpha fade.

Next
- Optional Projectile hooks (virtual Pre/OnImpact/Post) to reduce duplication in CannonBall.
- Pool shrapnel and VFX; recoil/camera shake hooks; input unification to KeyBindingConfig.

## Ship Prefab Structure (Authoring Contract)

Goal: all designer-built ships follow a consistent hierarchy so code can find mounts, cameras, and centers without scene-specific hacks.

Reference layout (simplified):
- Ship (root)
  - Model (visuals only)
    - Bridge
    - Hull_Forward_Port / Starboard / Central_* / Aft / Bow etc.
    - Deck
    - Deck-mounts            ← parents that contain WeaponMount components as children
    - Internal               ← anything non-visible / structural
  - BridgeCameraMount        ← Transform for bridge view parenting
  - FollowCameraMount        ← Transform for initial follow view placement
  - FollowCameraFocalPoint   ← Transform for orbit focal point (used by CameraViewManager)
  - OverheadCameraMount      ← optional; overhead auto-finds ship if absent

Code expectations:
- WeaponMount components live under `Model/Deck-mounts/...` (any depth ok). Each mount exposes:
  - `mountType` (e.g., "cannon"), yaw/pitch limits are defined on mount (or in ShipDefinition/ShipMountConfig).
  - A stable logical `mountId` (GameObject name or explicit field) so `ShipDefinition.mounts` can bind.
- ViewCenterAnchor may be placed under the ship root and pointed at `Model` to compute geometric center.
- CameraViewManager:
  - bridgeMount → BridgeCameraMount
  - followMount → FollowCameraMount
  - followTarget → FollowCameraFocalPoint (or auto-find by name)

Data mapping:
- `ShipDefinition.mounts[*].mountId` must match a WeaponMount logical id/name under `Model/Deck-mounts`.
- Mount limits: prefer reading from mount component at runtime; `ShipMountConfig` serves as authoring defaults/validation.

Reasoning:
- Keeping visuals under `Model` lets numeric bounds and view centering ignore helper transforms.
- Dedicated camera anchors avoid hardcoding offsets per ship.

## AI Snapshot (2025-11-11 — late)

Purpose: fast internal delta log since last snapshot.

Changes Since Prior Entry
- Projectile core
  - `Assets/Scripts/Projectile.cs`: made parent-friendly.
    - Removed same-object Collider requirement; keep single `Rigidbody` requirement + `DisallowMultipleComponent`.
    - Keeps damage-on-hit via `Health`, optional hit VFX, timed self-destroy.
    - Works when RB is on root and Collider on child.
- Cannon weapon
  - `Assets/Scripts/Cannon.cs`: audio path hardened.
    - Added child `CannonAudio` node with `AudioSource` that follows `spawnPoint`; uses `PlayOneShot`.
    - New fields: `force2DForDebug`, `audioMinDistance`, `audioMaxDistance`, `pitchRange`, `fireVolume`.
    - Removed Health from Cannon root (no `RequireComponent<Health>`); health expected on visual child.
- Cannon self-wear
  - `Assets/Scripts/CannonSelfDamage.cs`: new helper.
    - Applies fractional self-damage per shot; accumulates remainder until whole points.
    - Auto-finds `Health` on children (preferred) or on self; mirrors Cannon `fireKey`.
- Cannonball projectile
  - `Assets/Scripts/CannonBall.cs`: subclass of `Projectile` with extras intact.
    - Explosion VFX: `explosionEffectPrefab` (+ lifetime) spawned at contact.
    - Shrapnel spawn: `shrapnelPrefab`, count/speed/lifetime/damage with outward normal bias.
    - Still applies direct-hit `Health` damage before VFX/shrapnel.
- UI safeguard (optional)
  - `Assets/Scripts/UI/DisableSpacebarUI.cs`: prevents Space from triggering UI Submit when attached to EventSystem (not auto-used).

Operational Notes
- Projectile setup: one Rigidbody on root, non-trigger Collider(s) on child(ren). Launcher uses `spawnPoint.up`.
- Audio audibility: overhead camera requires large `audioMaxDistance` or temporary `force2DForDebug = true`.
- Spacebar switching views root-cause: UI Submit on selected button; not a code binding.

Next TODOs
- Consider `Projectile` virtual hooks (pre/post impact) to avoid re-implementing in `CannonBall`.
- Optional: add recoil/camera-shake hooks in `Cannon`.
- Pool shrapnel and explosion VFX later for perf.

## AI Snapshot (2025-11-11)

Purpose: concise internal log so I can resume instantly next session.

Changes Today
- CannonBall impact pipeline
  - OnCollisionEnter now: direct-hit damage (Health), spawn `explosionEffectPrefab`, emit physics-driven shrapnel, destroy self.
  - Shrapnel: prefab with Rigidbody + Collider (+ optional Projectile). Script sets `Projectile.damage` and `lifeTime`, assigns initial velocity, spawns at contact point + normal offset, supports outward normal bias.
- Cannon SFX
  - `Cannon` overrides `FireProjectile()` to play `fireClip` via 3D `AudioSource` (spatialBlend 1, log rolloff, min/max distances). Optional pitch variance.
- Base extensibility
  - `ProjectileLauncher.FireProjectile()` is now `protected virtual` so weapon subclasses can prepend/append behavior.

UX/Authoring Notes
- Explosion VFX prefab: built quick recipe (flash/sparks/smoke), suggested URP particle materials (Additive for flashes/sparks, Alpha for smoke). CannonBall uses `explosionEffectPrefab` and optionally `hitEffectPrefab` fallback.
- Soft smoke sprite: explained DIY and import settings (Wrap Clamp, Alpha is Transparency, black RGB in transparent border, mipmaps on for smoke). Avoids red halos.
- Sparks red outline: causes + fixes (texture edge bleed, mipmaps, additive + low alpha, trail material). Use white/yellow early, RGB → black as alpha → 0.

URP Migration / Materials
- Use URP Pipeline Asset in `Project Settings > Graphics (Default Render Pipeline)` and in `Project Settings > Quality` for all levels.
- Converter path (newer Unity): `Window > Rendering > Render Pipeline Converter` (Built-in → URP). If anything remains pink, swap manually:
  - Create real Material assets (can’t edit built-in Default-Material). Shader: `URP/Lit` for meshes; `URP/Particles/Unlit` for particles.
  - Assign Base Map, Normal, Metallic/Smoothness as appropriate. For trails, consider Alpha blend material.

Recorder
- Auto-start options: Recorder’s “Start Recording on Play” or a tiny Editor script to hook play state; alternative is Windows Game Bar (Win+Alt+R).

Open TODOs
- Shrapnel prefab: provide a minimal example (small sphere mesh + Rigidbody + Collider + optional Projectile, own material). Consider layer masks to avoid self/caster hits; tune counts/lifetimes for perf; pooling later.
- ProjectileLauncher hooks: consider `protected virtual` pre/post methods (PreFire/Spawn/AfterFire) for finer overrides.
- Add recoil and camera shake hooks in `Cannon` (configurable amplitude/duration).
- Add optional PS retrigger robustness (`StopEmittingAndClear` then `Play`) and null-warning for `MuzzleBlast`.
- Overhead camera: clamp to `GameFieldBounds`, add smoothing and mouse wheel zoom; persist offset/zoom across view switches.
- Unify camera input to `KeyBindingConfig` mappings (replace raw arrow reads where feasible).

Quick Resume Pointers
- CannonBall expects: `explosionEffectPrefab` (optional) + `shrapnelPrefab` (must have RB+Collider). Tune count/speed/life in Inspector. Physics handles occlusion.
- If VFX or Recorder UI seems missing, first clear compile errors; domain reload required for new menus.

## AI Snapshot (2025-11-10)

Purpose: fast internal log so I can resume instantly next session.

Changes Today
- Weapons
  - ProjectileLauncher: added `MuzzleBlast` ParticleSystem (plays on fire) alongside existing `muzzleSmoke`.
    - Preserved existing scene/prefab refs via `[FormerlySerializedAs("Muxxleblast")]`.
    - Note: for reliable retrigger visibility, consider `Stop(StopEmittingAndClear)` before `Play()`.
  - Cannon: new component deriving from `ProjectileLauncher` (AddComponentMenu: Teramyyd/Weapons/Cannon).
    - `Reset()` sets sane defaults (launchSpeed 50, spawnOffset 1, fireKey F).
    - Use for cannon-specific behavior without touching the base.
  - CannonBall: new projectile deriving from `Projectile` (AddComponentMenu: Teramyyd/Weapons/CannonBall).
    - `Reset()` defaults: damage 25, lifeTime 5.

Notes / Ops
- Particle authoring: provided quick red muzzle flash recipe (short lifetime, additive, small cone, burst 20–40).
- If blast isn’t visible in play: verify assignment on the firing instance, slight forward offset to avoid occlusion, additive material, culling mask, near clip, start size/alpha.

Editor/Recording
- Unity Recorder: installed path is Window > General > Recorder > Recorder Window (may require domain reload and clean compile to appear).
- Auto-start approaches: use Recorder’s “Start Recording on Play” option or a tiny Editor script hook on play state. Alternative: Windows Game Bar (Win+Alt+R) or OBS.

Open TODOs (carryover + new)
- Make `ProjectileLauncher` more extensible: split `FireProjectile()` with protected virtual pre/post hooks for subclasses (e.g., Cannon) to override.
- Optional: add robust retrigger for PS (`StopEmittingAndClear` then `Play`) and a warning log if `MuzzleBlast` is unassigned.
- Overhead camera: boundary clamping with `GameFieldBounds`, smoothing, mouse wheel zoom, persist offset/zoom across view switches.
- Unify camera input to use `KeyBindingConfig` mappings instead of raw arrow keys everywhere.

Key Files Touched
- `Assets/Scripts/ProjectileLauncher.cs` (new MuzzleBlast field + play)
- `Assets/Scripts/Cannon.cs` (new, subclass of ProjectileLauncher)
- `Assets/Scripts/CannonBall.cs` (new, subclass of Projectile)

Resumption Tip
- If particles or Recorder UI “don’t work,” first check Console for compile errors; new editor menus and component behaviors won’t initialize with compiler errors present.

## AI Snapshot (2025-11-09)

Purpose: Fast, structured brief so a new AI can resume work immediately.

Current Focus
- Overhead camera rewritten: straight-down, follows ship, persistent X/Z pan offset, zoom with Ctrl+Arrows, snap with Ctrl+F3.
- View switching via `CameraViewManager` (F1 Bridge, F2 Follow, F3 Overhead).

Player-Facing Behavior (Overhead)
- Always points straight down.
- Camera position = ship.position + offsetXZ + up * heightAboveShip (default heightAboveShip = 1000).
- Panning (relative offset):
  - Up Arrow: move camera toward -Z (ship appears lower)
  - Down Arrow: move camera toward +Z (ship appears higher)
  - Left Arrow: move camera toward -X (ship appears to the right)
  - Right Arrow: move camera toward +X (ship appears to the left)
- Zoom:
  - Ctrl+Up: zoom in (FOV by default; optionally height-based)
  - Ctrl+Down: zoom out
  - Ctrl+F3: snap above ship and reset zoom to baseline

Key Files
- `Assets/Scripts/OverheadViewController.cs` — Overhead camera logic (ship follow, pan offset, zoom, snap, clip planes, auto ship-target).
- `Assets/Scripts/CameraViewManager.cs` — Mode switching and overhead wiring (assigns shipTarget, sets heightAboveShip, calls SnapToShipCenter).
- `Assets/Scripts/GameFieldBounds.cs` — Logical bounds; not yet used for overhead clamping.
- `Assets/Scripts/CameraOrbitMove.cs` — Follow orbit (unused in overhead).
- `Assets/Scripts/CameraMove.cs` — Bridge controller (unused in overhead).

Config/Inspector Defaults (Overhead)
- heightAboveShip = 1000
- panSpeed = 200
- Zoom modes:
  - useFOVZoom = true (default)
  - minFOV = 5, maxFOV = captured at Start as baseFOV
  - If useFOVZoom = false → height zoom with minHeightAboveGround = 50 and baseHeight captured from heightAboveShip
- farClipPadding = 300 (ensures rendering at height)
- snapKey = F3 (use with Ctrl)

Known Issues / Notes
- Overhead has no boundary clamping yet; camera can pan outside playfield.
- `OverheadCameraMount` is not required; auto-detection uses it only to find the Ship parent if needed.
- If neither `Ship` nor `OverheadCameraMount` is found, overhead logs a warning and does not update.

Open Decisions / Next Steps
- Add optional soft boundary clamping to `GameFieldBounds`.
- Mouse wheel zoom + middle-click recenter.
- Smoothing for pan and zoom; configurable inputs.
- Persist overhead offset/zoom across mode switches.


## Current State (as of Nov 5, 2025)

### Project Structure
```
Teramyyd game/
├── Assets/
│   ├── Scripts/
│   │   ├── PlayerAircraft.cs    (basic flight & combat)
│   │   ├── EnemyAircraft.cs     (pursuit AI & combat)
│   │   ├── Projectile.cs        (movement & damage)
│   │   ├── GameManager.cs       (spawn & score systems)
│   │   └── InputManager.cs      (input wrapper)
│   └── Prefabs/                 (pending setup)
└── Docs/
    ├── Design.md               (core systems & roadmap)
    └── Prompt.txt             (original requirements)
```

### Implementation Status
1. ✅ Basic project scaffold created
2. ⏳ Core scripts added (needs Unity testing)
3. 🔄 Editor setup complete (VS Code integration)
4. ⏳ Unity scene setup pending
5. ⚠️ Original prompt needs to be added to Docs/Prompt.txt

### Next Steps
1. Create Unity scene with:
   - Player GameObject + PlayerAircraft script
   - Camera setup
   - Test enemy spawn points
2. Create essential prefabs:
   - Player aircraft
   - Enemy aircraft
   - Projectile

### Core Features To Implement
- [ ] Player flight mechanics refinement
- [ ] Weapon systems
- [ ] Enemy AI behaviors
- [ ] Wave spawning system
- [ ] Scoring & progression
- [ ] Basic UI/HUD

### Development Notes
- Player controls use Unity's Input system (configurable in InputManager.cs)
- Enemy AI uses simple pursuit with configurable detection/fire ranges
- GameManager handles spawning and scoring as singleton

### How to Resume Development
1. Open Unity Editor, load project
2. Check DEV_JOURNAL.md (this file) for current state
3. Open scripts in VS Code (Unity will use existing VS Code window)
4. Start with next unchecked item in Core Features
5. Update this journal as you progress

### Key Decisions & Parameters
- Player aircraft default speed: 80f
- Enemy detection radius: 200f
- Fire range: 150f
- Basic health system (100 HP for player)

### Known Issues/TODOs
- Scene needs to be created and configured
- Prefabs need to be created
- Need to implement proper health system UI
- Need to add audio system hooks

## Session Notes

### Session 1 (Nov 5, 2025)
- Created initial project structure
 
## Session 2 (Nov 6, 2025 - Early)
- Ran the Editor utility script `Teramyyd/Create HUD Canvas` to automatically create and wire the HUD Canvas, EventSystem, HealthText, ScoreText, and HUDController in the scene.
- Set up VS Code integration with Unity
- Created development tracking system (this journal)

## Session 3 (Nov 6-7, 2025 - Ship Component System)
**Ship Hierarchy Created (UPDATED - Simplified Structure):**
- Created modular Ship structure in Unity Hierarchy with simplified organization:
  ```
  Ship (main parent GameObject)
  └── Model (contains all ship parts with visual + functional components)
      ├── Bridge
      │   └── Cube (has Health, ShipComponent, Box Collider, Mesh Renderer)
      ├── Hull_Forward_Starboard
      │   └── Cube (has Health, ShipComponent, Box Collider, Mesh Renderer)
      ├── Hull_Forward_Port
      │   └── Cube
      ├── Hull_Central_Starboard
      │   └── Cube
      ├── Hull_Central_Port
      │   └── Cube
      ├── Hull_Rear_Starboard
      │   └── Cube
      ├── Hull_Rear_Port
      │   └── Cube
      ├── Hull_Aft
      │   └── Cube
      ├── Hull_Bow
      │   └── Cube
      ├── Deck
      │   ├── Starboard_mount_1, 2, 3 (weapon mount points with WeaponMount script)
      │   ├── Port_mount_1, 2, 3
      │   ├── Aft_mount
      │   ├── Bow_mount
      │   ├── Propulsion (has Cube child with Health, ShipComponent, Collider)
      │   └── Lift (has Cube child with Health, ShipComponent, Collider)
      └── Deck_Mast_Forward, Central, Rear
  
  REMOVED: Ship/Components folder (no longer needed with simplified structure)
  REMOVED: Ship/Internal folder (consolidated into Model)
  ```

**ARCHITECTURE CHANGE (Nov 7, 2025):**
- **Simplified from separated Model/Components to unified structure**
- All functional components (Health, ShipComponent, Box Collider) now live on the visual object (Cube)
- Parent objects (Bridge, Hull_Forward_Starboard, etc.) serve as organizational containers
- This eliminates the need to manually wire "Visual Model" references

**New Scripts Created This Session:**

1. **`Assets/Scripts/WeaponMount.cs`** — Weapon mount point system
   - Manages attaching/detaching weapons to mount points on the ship
   - Properties: `mountType` (what weapons this mount accepts), `isOccupied` status
   - Methods: `MountWeapon(prefab)`, `UnmountWeapon()`, `CanMountWeaponType(type)`
   - Tracks mounted weapon's Health component if it has one
   - Usage: Add to each weapon mount GameObject (e.g., Starboard_mount_1)

2. **`Assets/Scripts/Weapon.cs`** — Base weapon class
   - Base class for all weapons (cannons, harpoons, etc.)
   - Properties: `weaponType`, `damage`, `range`, `fireRate`
   - Virtual method `Fire()` for subclasses to override
   - Tracks reference to the mount it's attached to
   - Usage: Extend this class for specific weapon types (e.g., Cannon, Harpoon)

3. **`Assets/Scripts/ShipComponent.cs`** — Links Health to visual damage feedback (UPDATED for simplified structure)
   - Auto-finds Health component on same GameObject (no manual wiring needed)
   - Auto-finds all Renderer components in children using `GetComponentsInChildren<Renderer>()`
   - Subscribes to Health events (`onHealthChanged`, `onDeath`)
   - Updates visual model color based on damage (white → red gradient as health decreases)
   - On component destruction, changes visual to black
   - **UPDATED (Nov 7)**: Removed `visualModel` field - now automatically finds renderers
   - **FIXED**: Removed invalid null-conditional operator usage (Unity C# compatibility issue)
   - Usage: Add to visual GameObject (Cube) alongside Health component

**Setup Steps for Ship Parts (UPDATED - Simplified):**

1. **For each ship part (Bridge, Hull sections, etc.):**
   1. Parent GameObject (e.g., `Ship/Model/Bridge`) is just an organizational container
   2. Child Cube GameObject has all the functional components

2. **On the Cube child, add these components:**
   1. Health script:
      - Add Component → Health
      - Set `Max Health` (100 for hull, 80 for bridge, 150 for propulsion, etc.)
   2. ShipComponent script:
      - Add Component → ShipComponent
      - Drag the Health component into "Health System" field (or leave empty to auto-find)
   3. Box Collider:
      - Add Component → Box Collider
      - Check "Is Trigger" ✓
      - Set Center to (0,0,0)
      - Set Size to match cube dimensions

3. **The Cube will already have:**
   - Mesh Renderer (for visuals)
   - Transform component

**Final structure per part:**
```
Bridge (empty parent - organizational only)
└── Cube
    ├── Health script
    ├── ShipComponent script
    ├── Box Collider (Is Trigger = true)
    ├── Mesh Renderer
    └── Mesh Filter
```

**Current Implementation Status:**
- ✅ Ship hierarchy structure created in Unity scene
- ✅ Ship structure SIMPLIFIED (Nov 7) - consolidated Model and Components into one unified structure
- ✅ Health component added to ship part cubes
- ✅ ShipComponent script updated for auto-detection of renderers
- ✅ Box Colliders added to ship part cubes with Is Trigger enabled
- ✅ WeaponMount system created for modular weapon attachment
- ✅ Base Weapon class created for weapon prefabs
- ⏳ Need to complete setup for all remaining ship parts
- ⏳ Need to delete old Ship/Components folder (after migrating all parts)
- ⏳ Need to create weapon prefabs (Cannon, Harpoon, etc.)
- ⏳ Need to create projectile damage system for testing

**Architecture Notes (UPDATED Nov 7):**
- **Simplified Structure**: Visuals, logic, and collision detection all on the same GameObject (the Cube)
- **Parent as Container**: Parent objects (Bridge, Hull_Forward_Starboard) are organizational containers with no components
- **Why This Is Simpler**: 
  - No need to manually wire "Visual Model" references
  - Collider is on same GameObject as Health - direct hit detection
  - Everything for one ship part is in one place
  - Easier to understand and maintain
- **Health + ShipComponent Pattern**: 
  - Health = damage points and logic (how much damage, when destroyed)
  - ShipComponent = visual reactions (color changes based on damage, destruction effects)
  - Both exist on the same GameObject (the Cube)
- **Mount System**: Weapon mounts don't have health themselves. Weapons attached to mounts can have health. Mounts serve as attachment points and can accept/reject weapon types.

**Key Parameters Set:**
- Hull sections: 100 HP
- Bridge: 80 HP
- Propulsion/Lift: 150-200 HP (critical systems)
- Weapon mounts: 50 HP (if weapons have health)

**Known Issues Resolved:**
- ✅ ShipComponent compile error: Invalid use of null-conditional operator `?.` on left side of assignment
  - Fixed by replacing with explicit null-checked renderer assignments
- ✅ ShipComponent not appearing in Unity Add Component menu
  - Was due to compile errors preventing script compilation
- ✅ Complexity of separated Model/Components structure
  - RESOLVED (Nov 7): Simplified to unified structure with everything on visual GameObject
  - Removed need for manual "Visual Model" reference wiring
- ✅ Collider positioning issues
  - RESOLVED (Nov 7): Collider now on same GameObject as Health and visual, ensuring proper alignment
 
### Small utilities added (Nov 5, 2025)
- `Assets/Scripts/CameraFollow.cs` — smooth camera follow script. Attach to Main Camera and assign Player as Target.
- `Assets/Scripts/Health.cs` — reusable Health component with UnityEvent hooks for onHealthChanged and onDeath.
- `Assets/Scripts/HUDController.cs` — simple HUD wiring for health and score (requires Canvas + Text elements).

Wiring notes:
- Add `CameraFollow` to your Main Camera and set `target` to the Player GameObject. Adjust `offset` and `followSpeed` in the Inspector.
- Add `Health` to the Player and to Enemy prefabs. Configure `maxHealth` in the Inspector. For enemies, consider subscribing to `onDeath` to add explosion VFX and AddScore via GameManager.
- Create a Canvas (Screen Space - Overlay), add two UI -> Text elements (or TextMeshPro if preferred). Assign them to `HUDController.healthText` and `HUDController.scoreText`, and link `playerHealth`.

**Next steps**
1. Create Scene `Assets/Scenes/Main.unity` and set up Player, Camera, Projectile and Enemy prefabs.
2. Wire `Health` into `PlayerAircraft` and `EnemyAircraft` (subscribe to damage events or call `TakeDamage`).
3. Create simple UI Canvas and attach `HUDController`.

**Next Session Starting Point:**
1. Complete setup for all remaining ship parts (apply Health, ShipComponent, Box Collider to all Cubes)
2. Delete the old Ship/Components folder once all parts are migrated
3. Create projectile system that detects collider hits and calls Health.TakeDamage()
4. Test damage visualization (cube color changes as health decreases)
5. Create weapon prefabs:
   - Create Cannon prefab (extend Weapon class)
   - Create Harpoon prefab (extend Weapon class)
   - Add visual models and configure damage/range/fireRate
6. Test mounting weapons to WeaponMount points
7. Create ship control script for player input (movement, firing weapons)

**Immediate Todo (Before Next Major Features):**
- [X] Apply Health + ShipComponent + Box Collider to all ship part Cubes
- [X] Delete Ship/Components folder
- [X] Create projectile prefab that detects hits and damages ship components
- [X] Test end-to-end: fire projectile → hit hull section → health decreases → visual changes color
- [X] Create simple test script to damage components and verify visual feedback

**Key Setup Reminder:**
Each ship part Cube needs:
1. Health script (set Max Health appropriately)
2. ShipComponent script (wire Health System field or leave empty to auto-find)
3. Box Collider (Is Trigger = **UNCHECKED** for collision-based projectile damage)
4. Mesh Renderer (already present on Cube)

**Projectile Prefab Requirements:**
1. Rigidbody component (gravity enabled/disabled as needed)
2. Collider component (Is Trigger = **UNCHECKED**)
3. Projectile script
4. Optional: Visual mesh (Sphere, etc.)

Developer has successfully migrated Bridge to the new simplified structure (Nov 7, 2025).

## Session 4 (Nov 7, 2025 - Projectile System & Camera Controls)

**Projectile System Implementation:**
- Created `ProjectileLauncher.cs` script for testing projectile firing from cannon
  - Spawns projectile prefabs on keypress (default: Spacebar)
  - Configurable spawn point (uses Cylinder child transform for accurate firing direction)
  - Uses cylinder's local Y-axis (up direction) for firing direction
  - Sets projectile velocity directly using Rigidbody.velocity
  - **Updated (Nov 7)**: Simplified to use direct velocity setting instead of AddForce
  - **Updated (Nov 7)**: Added Physics.IgnoreCollision to prevent projectile hitting cannon
  - Configurable launch speed (default: 50 units/s) and spawn offset (default: 1 unit)
  
- Updated `Projectile.cs` for standard collision-based damage:
  - **BREAKING CHANGE (Nov 7)**: Switched from trigger-based to collision-based detection
  - Uses `OnCollisionEnter` instead of `OnTriggerEnter`
  - Requires `[RequireComponent(typeof(Rigidbody))]` and `[RequireComponent(typeof(Collider))]`
  - Simplified implementation: removed speed/movement logic (velocity set by spawner)
  - Removed `launchDirection` field (no longer needed)
  - Collision detection finds Health component on hit objects
  - Spawns optional hit effect at collision point
  - Auto-destroys on impact or after lifetime expires
  
**Cannon Setup (Final Working Configuration):**
```
Cannon (parent GameObject)
├── ProjectileLauncher script (fires projectiles)
├── Rotation: Any orientation to aim at target
└── Cylinder (child GameObject, scale 0.5x0.5x0.5)
    ├── Rotation: (0, 0, 0) - kept at zero for consistent Y-axis orientation
    ├── Opening faces along Y-axis by default
  └── Assigned to ProjectileLauncher's "Spawn Point" field
```

**Key Architecture Decisions:**
- Cannon parent controls aim direction via rotation
- Cylinder child stays at (0,0,0) rotation to maintain Y-axis alignment
- ProjectileLauncher uses cylinder's `transform.up` (world-space Y-axis after parent rotation)
- Projectile uses **standard colliders** (not triggers) with OnCollisionEnter
- **IMPORTANT**: Ship hull colliders have "Is Trigger" turned OFF for collision-based damage
- Physics.IgnoreCollision prevents projectile from hitting the cannon that fired it
- Velocity set directly via Rigidbody.velocity for predictable, physics-based movement

**Camera Control System:**
- Created `CameraMove.cs` (renamed from CameraRotate.cs) with comprehensive controls:
  - **Arrow Keys**: Rotate camera (look around)
    - Left/Right: Horizontal rotation (yaw)
    - Up/Down: Vertical rotation (pitch, clamped to prevent flipping)
  - **Shift + Arrow Keys**: Pan camera position
    - Shift + Left/Right: Drift left/right
    - Shift + Up/Down: Drift up/down
  - **Ctrl + Arrow Keys**: Zoom in/out
    - Ctrl + Up: Move forward (zoom in)
    - Ctrl + Down: Move backward (zoom out)
  - Configurable speeds: `rotationSpeed` (50°/s default), `moveSpeed` (10 units/s default)
  - Optional orbit mode: Can orbit around a target object while maintaining distance

**Scripts Created/Modified This Session:**
1. `ProjectileLauncher.cs` - New script for cannon firing mechanics
2. `Projectile.cs` - Updated to use explicit launch direction
3. `CameraMove.cs` - New comprehensive camera control script

**Testing Status:**
- ✅ Projectile spawning working
- ✅ Projectile direction matches cannon orientation
- ✅ Cannon can be rotated to any angle
- ✅ Camera controls functional (rotation, pan, zoom)
- ⏳ Need to test projectile hitting ship components
- ⏳ Need to verify damage system integration

**Known Issues Resolved:**
- ✅ Projectile firing in wrong direction (global Z-axis)
  - Fixed by using cylinder's transform.up and setting explicit launch direction
- ✅ Projectile not spawning when cylinder at (0,0,0)
  - Fixed by adding spawn distance offset in firing direction
- ✅ Projectile ignoring cannon rotation
  - Fixed by passing launch direction to Projectile script before Start() runs

**Next Steps:**
1. Test complete damage chain: cannon fires → projectile hits ship part → health decreases → color changes
2. Fine-tune projectile speed, lifetime, and spawn distance
3. Consider adding muzzle flash or firing effects
4. Implement weapon mounting system for cannons
5. Create additional weapon types (harpoons, etc.)

## Session 5 (Nov 9, 2025 - Overhead Camera Revamp & View System)

Overview
- Rebuilt the overhead camera system to meet top-down strategy view requirements: always look straight down, follow the ship, allow persistent pan offset, and support zoom with a snap-to-center reset.
- Integrated with the existing CameraViewManager so F3 switches to overhead cleanly.

Key Changes
1) Overhead camera rewritten
   - File: `Assets/Scripts/OverheadViewController.cs`
   - Behavior:
     - Camera rides above the ship at a configurable world-space height (`heightAboveShip`, default 1000).
     - Maintains a persistent X/Z offset relative to the ship; arrow keys change this offset:
       - Up = move camera toward -Z (ship appears to move down)
       - Down = move camera toward +Z (ship appears to move up)
       - Left = move camera toward -X (ship appears to move right)
       - Right = move camera toward +X (ship appears to move left)
     - Always points straight down (no tilt or roll).
     - Ctrl+F3 snaps back directly above the ship and resets zoom to the baseline.
   - Robustness:
     - Auto-finds `Ship` or `OverheadCameraMount` (uses its parent if present) if `shipTarget` isn’t assigned.
     - Ensures camera `farClipPlane` is extended enough for y≈1000 views (prevents “grey screen” background).

2) Zoom support (two modes)
   - FOV Zoom (default): Ctrl+Up zooms in (narrows FOV), Ctrl+Down zooms out. `minFOV` lowered to allow very close-in views. Ctrl+F3 resets to `baseFOV`.
   - Height Zoom (optional): set `useFOVZoom = false` in `OverheadViewController` to zoom by changing `heightAboveShip`. Clamped by `minHeightAboveGround` so the camera never goes below ground. Ctrl+F3 resets to `baseHeight`.

3) View manager integration
   - File: `Assets/Scripts/CameraViewManager.cs`
   - `EnterOverhead()` now:
     - Disables other camera controllers.
     - Ensures `OverheadViewController` is on the Main Camera.
     - Assigns `shipTarget = followTarget` and sets `heightAboveShip = 1000`.
     - Calls `SnapToShipCenter()` to start directly above the ship.

Controls (Overhead)
- Arrow Keys: Pan (modify persistent offset relative to ship)
  - Up: camera toward -Z, Down: camera toward +Z
  - Left: camera toward -X, Right: camera toward +X
- Ctrl+Up / Ctrl+Down: Zoom in/out (FOV or height depending on configuration)
- Ctrl+F3: Snap above ship and reset zoom to baseline

Notes & Decisions
- Overhead view is intentionally tilt-free to keep a pure top-down perspective.
- `OverheadCameraMount` is not required by the controller; however, auto-discovery uses it if present to resolve `shipTarget` via its parent.
- Panning is currently unclamped. We can add soft boundaries using `GameFieldBounds` later if desired.

Known Fixes
- Grey-screen in overhead mode fixed by:
  - Auto-assigning `shipTarget` when missing (prevents early bail-out in LateUpdate).
  - Raising `farClipPlane` to exceed camera height (ensures ship/ground render at y≈1000).

Next Steps
- Optional: add soft boundary clamping and friction near edges of the playfield.
- Optional: mouse wheel zoom + middle-click recenter.
- Optional: smoothing for pan/zoom and configurable keybinds.

## Session 6 (Nov 10, 2025 - HUD, Keybindings JSON, View Fixes, Cannon FX)

Summary
- Added JSON-based keybinding config and auto-loading at runtime; integrated with view switching (F1/F2/F3) and overhead snap/zoom modifiers.
- Implemented a direct HUD creation script with a persistent top-right settings button (sprite supported), independent of active camera view.
- Fixed view switching bug: Overhead controller no longer remains active after switching back to Bridge/Follow; each mode now resets to default layout on entry.
- Enhanced projectile system: fire key changed to F; optional muzzle smoke ParticleSystem plays on firing.

Player-Facing Changes
- View buttons or keybindings instantly reset the selected view to its baseline (Bridge centered, Follow re-orbits, Overhead snaps above ship).
- Overhead still pans with arrows and zooms with Ctrl+Up/Down; Ctrl+F3 resets.
- Settings gear always anchored to screen (Screen Space - Overlay) regardless of mode.
- Cannon firing now uses F (instead of Space) and can display smoke.

Technical Additions
- `KeyBindingConfig`: Added `KeyBindingData` (string-key JSON). Methods `LoadFromJSON()` / `SaveToJSON()`. Auto-load in `Instance`.
- `keybindings.json`: User-editable keys (e.g., "F1", "LeftArrow", "Alpha1"). Invalid names fall back with warnings.
- `CameraViewManager`: Added disabling of other controllers on mode change; resets each view; added debug logs.
- `CreateHUD_Direct`: Reliable HUD canvas + settings button creation (removed Health/Score from HUD per design shift).
- `ViewSwitchButton`: Simple component to map a UI Button to a `ViewMode`.
- `ProjectileLauncher`: Added `muzzleSmoke` ParticleSystem field; plays effect on fire; default `fireKey = KeyCode.F`.

How to Use New Systems
- Keybindings: Edit `Assets/Resources/keybindings.json`, save, and play—runtime loads automatically.
- HUD: Run menu `Teramyyd/Create HUD Canvas (Direct)`; assign sprite (import PNG as Sprite (2D and UI), Mode = Single) to settings button Image.
- Muzzle Smoke: Create ParticleSystem at barrel exit, disable Play On Awake & Looping, assign to `muzzleSmoke` field.

Recommended Particle Settings (Starter)
- Main: Lifetime 0.6–1.0, Speed 5–8, Size 0.6–1.2, Color light gray (alpha 255).
- Emission: Burst 25–40 at time 0.
- Shape: Cone, Angle 20°, Radius 0.15.
- Color over Lifetime: fade to transparent.
- Renderer: Billboard, Material = Particles/Standard Unlit.

Outstanding / Next Steps
- Wire `CameraMove` & `CameraOrbitMove` to `KeyBindingConfig` (currently hard-coded arrows/ctrl).
- Add Bridge / Follow snap shortcuts (Ctrl+F1 / Ctrl+F2) using config.
- Add optional settings panel UI toggled by the gear button.
- Smoke material helper script to auto-assign visible particle material.
- Overhead enhancements: boundary clamping, mouse wheel zoom, smoothing transitions.
- Persist overhead offset/zoom when leaving and re-entering overhead view.

Merged AI Changelog Snapshot (2025-11-09)
- Overhead camera rebuilt: straight-down, ship follow with persistent X/Z offset; panning arrows; zoom Ctrl+Arrows; snap Ctrl+F3.
- Multi-view manager (Bridge/Follow/Overhead) keys F1/F2/F3; overhead initialization and clip-plane fix.
- Known issues (still applicable): no boundary clamping; offset/zoom persistence not implemented; no smoothing.


Here’s a clean, scalable way to handle runtime weapon placement with aiming limits and destroy/spawn during gameplay, using systems you already have.

Core Concepts

WeaponMount component (on fixed mount points)
Knows what it can accept (type/class), whether it’s occupied, and its aiming limits.
Exposes two pivot transforms: yawBase (left/right) and pitchBarrel (up/down) so clamping is trivial.
API: CanMount(type), MountWeapon(prefab), UnmountWeapon(), GetMounted().
Weapon component (base) + specific weapons (Cannon etc.)
Weapon uses ProjectileLauncher for firing logic, reads its mount to aim/orient/constraints.
Health lives on the visual child (as you’re already doing). On death, unmount cleans up.
ScriptableObject weapon definitions
WeaponDefinition: id/type, prefab, default stats (damage, fire rate), cost, crew requirements.
Lets you build inventory/shops without touching prefabs.
Mount/Aim Hierarchy

Mount GameObject (WeaponMount)
yawBase (empty transform rotates around local Y for left/right)
pitchBarrel (empty transform rotates around local X for up/down)
WeaponRoot (instantiated prefab parented here)
With this structure:
Yaw = clamp(yawCurrent + deltaYaw, -yawHalfRange, +yawHalfRange)
Pitch = clamp(pitchCurrent + deltaPitch, -pitchDown, +pitchUp)
Constraints live on the mount, not the weapon (so any weapon can be dropped in and work).
Runtime Placement Flow

Player selects a weapon in UI (weaponDefinition).
Raycast/select a mount point (WeaponMount) on the ship.
Validate: mount accepts type and is not occupied; resource/crew checks pass.
Instantiate weapon prefab:
Parent under mount.pitchBarrel (so aiming moves only pivots).
Zero local position/rotation/scale.
Set Weapon.SetMount(mount) and mount.MountWeapon(weaponPrefab).
For removal: mount.UnmountWeapon() → Destroy weapon GO (or pool), refund/penalize as rules dictate.
Aiming + Firing

Aiming input goes to the mount, not the weapon:
Convert desired aim (from cursor or target pos) into local yaw/pitch deltas for yawBase and pitchBarrel.
Clamp per limits: yaw ∈ [−yawLimit, +yawLimit], pitch ∈ [−down, +up].
Optionally smooth with RotateTowards.
Firing lives on the weapon (ProjectileLauncher/Cannon). It just uses current barrel forward/up direction at spawnPoint.
Integrate your variance:
ProjectileLauncher already supports angle spread/speed jitter (runtime-adjustable). Crew quality can shrink these values while playing.
Health/Damage

Put Health on the weapon’s visual child as you have done.
On death: UnityEvent onDeath → Weapon/WeaponMount handle clean unmount; destroy the weapon object (mount remains).
Optional: Mount has its own Health to disable the mount if destroyed.
Inventory + Game Systems

Player inventory: a list of WeaponDefinition stacks + resource budget.
Mount manager on the ship keeps a registry of all WeaponMounts (for UI and quick lookups).
UI
Hover highlight mount points; show type acceptance (green/red).
Ghost preview: temporary transparent weapon at the mount while deciding.
Confirm to place; invalid selections show reason (type mismatch, occupied, no budget).
Save/Load: serialize a list of mount slots with weapon IDs and runtime state (ammo, damage state) to rebuild on load.
Pooling (later optimization)

Pool weapon prefabs and projectiles to avoid GC spikes during rapid build/destroy.
Mounts request from pool and return on unmount/destroy.
Networking (future-proof)

Server/host validates placement (mount type, occupancy, costs).
Authoritative firing from server; variance seeded by shot index/weapon id if determinism is needed.
Edge Cases + Contracts

Exactly one Rigidbody per projectile hierarchy; child colliders OK.
For aiming checks, compute signed angles in mount local space (no world-euler surprises).
When replacing a weapon in combat: unmount first or block replace during reload/cooldown.
Crew effects: expose mount.AccuracyMultiplier and mount.TraverseSpeed to modulate rotation speed and call into ProjectileLauncher’s angleSpreadDegrees/speedJitterPercent at runtime.
Minimal APIs (pseudocode)

WeaponMount
fields: acceptedType, yawLimitDeg, pitchUpDeg, pitchDownDeg, yawBase, pitchBarrel
bool CanMount(string type)
GameObject MountWeapon(GameObject prefab)
GameObject UnmountWeapon()
void AimAt(Vector3 worldTarget) → computes local yaw/pitch, clamps, applies to pivots
Weapon (base)
void SetMount(WeaponMount mount)
Health is on visual child
ProjectileLauncher (already in place)
float angleSpreadDegrees, float speedJitterPercent (runtime adjustable)
uses spawnPoint.up as muzzle axis; keep spawnPoint oriented correctly
Why this design

Keeps constraints and placement logic on the mount (single place of truth).
Makes weapons plug-and-play prefabs (build/destroy at will).
Clear separation of concerns: UI/selection → ship mount manager → WeaponMount → Weapon firing.
If you want, I can sketch the exact fields and minimal methods for WeaponMount with clamp math and an AimAt(target) routine you can drop in.

New Features

Persistent Captain Profile

Player logs in as a captain; long-term progression persists across combat sessions.
Starts with a basic ship; can upgrade ships, weapons, crew; buy/sell equipment.
Tracks wealth (gold) and potentially reputation/other stats.
Inventory persists and grows via looting; some wealth is off-ship and safe.
Post-combat outcomes: on win, loot/salvage; on loss, lose some gold/components; repair and salaries consume wealth.
Multi-Scene Structure

Live multiplayer duel scene (primary).
Solo practice/AI scene (later).
Game initiation/login scene (later).
Settings scene (keybinds, audio, mouse/touchpad; writes JSON).
Between-combat management scene (upgrade ship, crew management, payments, inventory).
Recommended Systems And Code Additions

Persistence & Profile

Add PlayerProfile (captain ID, wealth, reputation, owned ships, inventory, crew roster).
Add SaveSystem service: JSON serialize to Application.persistentDataPath + cloud later.
Add SessionManager to track active captain/session across scenes and bootstrap services.
Economy & Inventory

ItemDefinition (SO): id, name, type, value, stack rules.
Inventory component/service: list of ItemStacks; add/remove, serialize.
EconomyService: add/spend gold, repair costs, crew salaries, shop buy/sell.
LootTable (SO) for ships/enemies; LootService rolls drops on win.
Ships, Weapons, Crew

ShipDefinition (SO): hull stats, mounts layout, mass/turn/thrust caps, price.
WeaponDefinition (SO): type, prefab, cost, damage, crew requirements.
CrewMember (class/SO): name, skill (0..1), salary, traits; affects accuracy, reload, repairs.
Keep runtime placement via WeaponMount + Weapon you already have. Mount accepts WeaponDefinition and does MountWeapon(prefab)/UnmountWeapon().
Wire crew skill to firing accuracy:
Use ProjectileLauncher.angleSpreadDegrees and speedJitterPercent (runtime adjustable) with:
angleSpreadDegrees = Mathf.Lerp(maxSpread, minSpread, crewSkill)
speedJitterPercent = Mathf.Lerp(maxJitter, minJitter, crewSkill)
Between-Combat Management

Separate scene UI to:
Equip weapons to available WeaponMounts (drag-drop or list + mount selector).
Upgrade ship (swap ShipDefinition), hire/fire/promote crew, manage repairs.
Inventory management (loot, sell, craft/salvage).
Apply changes back to PlayerProfile and persist via SaveSystem.
Scenes & Flow

Add a small Bootstrap scene (first in build) that:
Loads/creates PlayerProfile
Initializes services (SaveSystem, EconomyService, Inventory)
Routes to Login/Init or Between-Combat depending on session state
Settings scene:
Bind to existing KeyBindingConfig + keybindings.json
Add audio/mouse/touchpad UI; save to JSON/PlayerPrefs.
Multiplayer (later)

Keep API seams in services to swap local with networked backends.
Record RNG seeds for projectile variance if determinism is needed.
Data Contracts (Suggested)

PlayerProfile
string captainId, int gold, float reputation
List<OwnedShip> ships (each with ShipDefinition id, installed weapons, health state)
OwnedShip activeShip
List<CrewMemberState> crew
Inventory inventory
OwnedShip
string shipDefId, List<MountedWeaponState> mounts, float hullHealth, etc.
MountedWeaponState
string mountId, string weaponDefId, runtime durability/ammo if applicable.
CrewMemberState
string crewDefId, float skill, status (active/injured), salary due.
Immediate Glue With Existing Code (No Changes Today)

Use WeaponMount.MountWeapon(prefab) at runtime for placement.
Use ProjectileLauncher variance fields to reflect crew skill in moment-to-moment accuracy.
Keep Health on the 3D child; CannonSelfDamage continues to accumulate fractional wear.
Explosion VFX/audio via explosionEffectPrefab and an AudioSource on that prefab.
Step-By-Step Next (When You Want To Implement)

Add ScriptableObjects: ShipDefinition, WeaponDefinition, ItemDefinition, LootTable.
Add services: SaveSystem, EconomyService, Inventory, SessionManager.
Add PlayerProfile JSON load/save and a Bootstrap scene.
Add Between-Combat scene UI to mount weapons (using your WeaponMount) and manage inventory/economy.
Wire crew effects to launchers: update angleSpreadDegrees and speedJitterPercent at runtime from crew skill.
Add basic loot on win and loss penalties; serialize changes.

Created minimal, compile-ready stubs for persistence and data definitions. No existing code was changed.

What I added

Player profile data
Assets/Scripts/Systems/PlayerProfile.cs:1
PlayerProfile: captainId, gold, reputation, ships, activeShipId, inventory, crew
OwnedShip, MountedWeaponState, CrewMemberState
Inventory with simple Add/Remove/GetCount and ItemStack
Save/load helper
Assets/Scripts/Systems/SaveSystem.cs:1
GetProfilePath(captainId), SaveProfile(PlayerProfile), LoadProfile(captainId)
Uses JsonUtility and Application.persistentDataPath
ScriptableObjects
Assets/Scripts/Data/ItemDefinition.cs:1
id, displayName, description, baseValue, stackable, maxStack
Assets/Scripts/Data/WeaponDefinition.cs:1
id, displayName, description, cost, weaponPrefab, weaponType, damage, fireRate, crewSkillRequired
Assets/Scripts/Data/ShipDefinition.cs:1
id, displayName, description, cost, maxHull, baseSpeed, turnRate
mounts: List<ShipMountConfig> with mountId, acceptedType, yaw/pitch limits
How to use (example)

Create data assets
Right-click in Project:
Create > Teramyyd/Items/Item Definition
Create > Teramyyd/Items/Weapon Definition
Create > Teramyyd/Ships/Ship Definition
Save/load profile
var profile = SaveSystem.LoadProfile("captain123");
profile.gold += 100;
SaveSystem.SaveProfile(profile);
Tie-in with runtime systems

Weapon mounts: use ShipDefinition.mounts as authoritative constraints; your WeaponMount reads acceptedType and limits.
Weapon placement: use WeaponDefinition.weaponPrefab when mounting; weaponType should match WeaponMount.mountType.
Crew effects: read PlayerProfile.crew to drive ProjectileLauncher.angleSpreadDegrees and speedJitterPercent at runtime.
Want me to also stub CrewDefinition (SO) and a tiny Bootstrap/SessionManager to load the profile on startup?

5 files changed
+236
-0

Undo

View all changes
ItemDefinition.cs
+16
-0

ShipDefinition.cs
+34
-0

WeaponDefinition.cs
+23
-0

PlayerProfile.cs
+108
-0

SaveSystem.cs
+55
-0

## AI Snapshot (2025-11-12 � Mounts + Orientation + Dev Controls)

Purpose: weapon mounts updated, orientation mapping stabilized, temporary dev controls added.

Changes
- Mount scripts:
  - WeaponMount.cs (general) and ProjectileLauncherMount.cs (ProjectileLauncher-specific).
  - Pivots: yawBase (local Y) and pitchBarrel (local X) with clamped limits: yawLimitDeg (�half), pitchUpDeg, pitchDownDeg.
  - Orientation at mount time: select launcher firing axis via launcherAxis (Up/Forward/Right) + invertLauncherAxis and map to mount direction (currently mount -Z).
  - Runtime API: Mount/Unmount + SetYawPitch/ApplyYawDelta/ApplyPitchDelta.
  - Auto-populate test path: autoPopulatePrefab + autoPopulateOnStart.
- Dev input (temporary):
  - Enable debugKeypadControl on a mount to move it during Play.
  - Keys: j/l = yaw left/right, i/k = pitch up/down; speeds configurable; optional invertYawDirection/invertPitchDirection.
- Fixes:
  - Removed duplicate Update() in ProjectileLauncherMount (compile error).
  - Ensured only one Update() contains the dev-input logic.

Contracts
- Keep transform scales positive (no mirror) for Ship/Model/Mount/pivots.
- Baseline pose: yawBase +Z is straight ahead at yaw=0/pitch=0; pitchBarrel rotates only on X.
- Launcher firing axis for current launchers = spawnPoint.up (+Y). If prefab differs, set launcherAxis/invert on the mount rather than editing the prefab.
- Current mapping targets mount -Z per request; flip later if design changes.

Testing notes
- Use AutoPopulateLauncherMounts on the Ship root OR per-mount autoPopulateOnStart � not both � to avoid duplicate cannons.
- After mounting, fire once and confirm projectile velocity aligns with mount direction (accounting for spread/jitter).
