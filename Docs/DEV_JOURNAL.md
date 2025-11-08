# Teramyyd Game Development Journal

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

