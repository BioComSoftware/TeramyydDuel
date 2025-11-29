Teramyyd — Add Weapon Mounts Instructions

## Purpose
These notes walk a non-Unity user through adding a brand-new weapon mount to the player ship, wiring the cannon prefab so it points the correct direction, and exposing that mount on the Ship Representation HUD (including the Fire-at-Will toggle and the per-mount FIRE buttons). Follow every step even if you duplicate an existing mount—the goal is to ensure transforms stay clean and the UI stays in sync.

---

## Autopop Workflow Cheatsheet
- **Ship-side hierarchy:** Every mount lives under `Ship/<X>_weapon_mount/Weapon_mount`. `<X>` identifies the location (Bow, Aft, Port, etc.). The blank parent `<X>_weapon_mount` is only a locator; `Weapon_mount` carries the `WeaponMount` component.
- **HUD-side hierarchy:** HUD icons live under `HUD/ShipRepresentation/ShipOutline/<X>_weapon_mount`. This GameObject name must match the ship-side blank `<X>_weapon_mount` exactly (case-sensitive). Place all HUD-only children (ReadyIndicators, Healthbar, FIRE button, etc.) under this node.
- **Duplication-friendly:** You can safely duplicate either hierarchy (ship or HUD) as long as you immediately rename the duplicate to the new `<X>_weapon_mount`. The scripts rescan and rebind whenever the GameObject activates, so no manual drag-dropping is required after duplication.
- **Autopop references:** `ShipHUDMountDisplay` now auto-fills weapon mounts, icon images, ready indicators, target indicators, health bars, and FIRE buttons whenever their canonical child names are present. Even with autopop, the following sections describe each reference so you can customize or override them when needed.
- **When does it refresh?** The component refreshes during Reset, OnValidate, Awake, and OnEnable (play mode). Entering Play Mode or duplicating a node instantly re-syncs every reference.

Keep those rules in mind while following the step-by-step guides below.

---

## Part 1 · Build the Physical Weapon Mount

### 1. Create (or duplicate) the `<X>_weapon_mount` locator
1. In the Hierarchy, expand `Ship` (the playable airship prefab in the scene).
2. **Preferred:** duplicate an existing `<X>_weapon_mount` GameObject (e.g., duplicate `Bow_weapon_mount` and rename the duplicate to `Aft_weapon_mount`). Duplicating preserves all children, including `Weapon_mount`, sensors, and helper pivots.
3. **From scratch:** Right-click `Ship` → **Create Empty**, name it `<X>_weapon_mount` (for example `Port_weapon_mount`). Reset its Transform so Position/Rotation = 0 and Scale = 1.
4. Move/rotate the blank `<X>_weapon_mount` object to the exact deck location/orientation for the new weapon. Only this locator should receive placement edits.

### 2. Ensure the `Weapon_mount` child exists
- If you duplicated an existing mount, you should already have `Ship/<X>_weapon_mount/Weapon_mount` with the correct children—skip to the next section.
- Building from scratch:
   1. Create an empty child under the blank `<X>_weapon_mount` and name it `Weapon_mount` (lowercase, underscore).
   2. Create two grandchildren: `YawBase` and, under it, `PitchBarrel`. Reset both transforms.
   3. Add the `WeaponMount` component to `Weapon_mount`.
   4. Drag `YawBase` → `Yaw Base` field and `PitchBarrel` → `Pitch Barrel` field in the inspector.

### 3. Configure WeaponMount
1. Give it a unique `mountId` (example: `Bow_weapon_mount`). This id is what HUD bindings reference.
2. Set `mountType` to the logical weapon type (`cannon`, `missile`, etc.) so UI sprites map correctly.
3. Adjust `Yaw Limit`, `Pitch Up`, and `Pitch Down` to match the firing arc you want (the defaults work for most cases).
4. Leave `YawBase` and `PitchBarrel` rotations at zero. If you need the cannon to point aft, rotate the **socket** 180° around Y instead. This keeps the pivots orthogonal so the auto-alignment math works.
5. Under **Launcher Axis Mapping**, keep `Launcher Axis = Up` and `Invert Launcher Axis = false` for the stock cannon (its spawn point’s +Y is the barrel direction). Only change this if you author a prefab with a different muzzle axis.
6. Optional but recommended:
   - Check `autoPopulateOnStart` and assign the `Cannon` prefab to `autoPopulatePrefab` when you want the mount to spawn a cannon automatically in Play Mode.
   - Enable `autoAssignTargetAcquisitionCollider` so the mount auto-finds the barrel sensor collider on the prefab.
   - If you move the sensor, update `targetAcquisitionColliderNameHint` to the sensor’s child name (e.g., `Cylinder`).

### 4. Verify the cannon prefab
1. Enter Play Mode. Each mount with `autoPopulateOnStart` should instantiate `Cannon(Clone)` under `PitchBarrel`.
2. Select the cannon in the Hierarchy and confirm:
   - Local Position = (0,0,0) and Local Scale = (1,1,1).
   - The spawn point (`Cannon/SpawnPoint`) has its blue Z axis pointing sideways and its green Y axis pointing down the barrel (this is normal; the mount maps +Y to world forward automatically).
3. Aim at a target and press **F**. The cannon should track the target using the mount pivots. If it turns backwards, reset the pivot rotations and rotate only the socket GameObject.

#### WeaponMount reference glossary
- **Yaw Base / Pitch Barrel:** Required pivot references. They define the axes the mount rotates around; keep them orthogonal and unscaled.
- **mountId / mountType:** Text identifiers consumed by logging and HUD sprite mapping. `mountId` is typically the same as `<X>_weapon_mount`.
- **targetingController:** Optional manual override. Leave empty to let the mount auto-discover the global `TargetingController` at runtime.
- **targetAcquisitionCollider / autoAssignTargetAcquisitionCollider / targetAcquisitionColliderNameHint:** Sensor collider that determines when a target is “inside” the firing cone. Leave the reference empty and keep `autoAssignTargetAcquisitionCollider` enabled so the mount searches the mounted weapon for a trigger named by the hint.
- **autoPopulatePrefab / autoPopulateOnStart:** When set, the mount spawns a weapon automatically on Start for testing. Clear these fields in production if weapons are equipped via gameplay logic.
- **launcher axis + inversion toggles:** Advanced overrides when authoring non-standard launchers. Defaults assume the prefab fires along +Y.
- **debug settings:** `enableDebugLogging` prints to both the Console and `Logs/game_debug.log`, invaluable when troubleshooting targeting/firing flow.

### 5. Clean parenting recap
```
Ship
└── <X>_weapon_mount  (position/rotation adjusted for placement, scale 1)
   └── Weapon_mount  (holds WeaponMount component; leave at default transform)
      └── YawBase
         └── PitchBarrel
            └── Cannon prefab (spawned at runtime)
```
Everything below `<X>_weapon_mount` must stay at Scale (1,1,1) with zeroed rotations. Rotate/move only the blank `<X>_weapon_mount` locator when positioning a new cannon bay.

---

## Part 2 · Register the Mount with the HUD

### 1. Prepare the HUD artwork
1. Open the HUD Canvas (usually `HUD/ShipRepresentation`). Expand `ShipRepresentation/ShipOutline`—this is where every HUD mount icon lives.
2. Duplicate an existing mount icon folder (for example `ShipOutline/Bow_weapon_mount`) and rename the duplicate so it matches the new ship-side locator exactly (e.g., `Aft_weapon_mount`). Name parity is mandatory; the autopop system compares `GameObject.name` when binding.
3. Inside the icon folder you will find the Image, ReadyIndicators, TargetNotAquired (typo intentional, matches prefab), Healthbar, and FIRE button sub-objects. Feel free to tweak visuals, but keep the canonical child names if you want auto-assignment to work without manual wiring.

### 2. Configure ShipHUDDisplay + per-icon components
1. Each icon GameObject (`ShipRepresentation/ShipOutline/<X>_weapon_mount`) must have a `ShipHUDMountDisplay` component. Prefabs already contain one; duplicates inherit it.
2. You rarely need to drag references anymore. The component auto-fills everything on Reset, OnValidate, Awake, and OnEnable by matching child names:
    - `weaponMount` ← `Ship/<X>_weapon_mount/Weapon_mount`
    - `iconImage` ← the Image component on the same GameObject
    - `emptySprite` ← default icon at `Assets/UI/Icons/Mount.png`
    - `readyIndicatorImage`, `readySprite`, `notReadySprite` ← children `ReadyIndicators/GreenStoplight` + `ReadyIndicators/RedStoplight`
    - `targetNotAcquiredImage` ← child `TargetNotAquired`
    - `healthBarContainer` ← child `Healthbar`
    - `fireButton` ← child `FIRE`
3. Even though these fields auto-populate, you can override any of them manually. The component will keep your overrides as long as the reference still points to a child belonging to the same mount. If you intentionally clear a field, duplicated icons will refill it the next time they’re enabled.
4. Repeat for every mount icon (bow, aft, starboard, etc.). Duplicating an icon plus renaming it is the fastest way to add a new HUD entry.
5. Select the object hosting `ShipHUDDisplay`. Keep `Auto Populate Mount Displays` enabled unless you have a niche reason to curate the list yourself; the script compares the cached array to the live hierarchy each frame and refreshes it when the counts differ, so newly duplicated icons appear automatically.

#### ShipHUDMountDisplay reference glossary
- **weaponMount:** Runtime link to `Ship/<X>_weapon_mount/Weapon_mount`. The script verifies the parent name matches the HUD icon before reusing the reference.
- **iconImage:** The Image that renders the mount icon sprite.
- **emptySprite:** Sprite used when no weapon is installed or the mapping fails. Defaults to `Assets/UI/Icons/Mount.png`.
- **Ready Indicator block:**
   - `manageReadyIndicator` (default on)
   - `readyIndicatorImage` (auto-uses `ReadyIndicators/GreenStoplight`)
   - `readySprite` (green) and `notReadySprite` (red)
   - `hideReadyIndicatorWhenNoWeapon` toggles visibility when a mount is empty.
- **Target Acquisition block:**
   - `manageTargetNotAcquiredIndicator` (forced on until an image exists)
   - `targetNotAcquiredImage` (auto from `TargetNotAquired` child)
   - The script toggles this image based on sensor overlap + firing solution.
- **Health Bar block:**
   - `manageHealthBar` (default on when no container assigned)
   - `healthBarContainer` (auto from `Healthbar` child)
   - Color fields define healthy/damaged/disabled gradients; you may tweak them per mount.
- **Fire Button block:**
   - `fireButton` (auto from `FIRE` child). The script wires up `onClick` to call `WeaponMount.TryFire()` and cleans up listeners automatically.

If you delete any canonical child, the corresponding reference simply stays null and the optional UI feature disables itself. Restore the child (or assign a new object) to re-enable that portion of the HUD.

### 3. Hook the Fire-at-Will Toggle
1. Locate the UI button that should toggle Fire-at-Will (for example `HUD/ShipRepresentation/Buttons/FireAtWillButton`). Make sure it has both a `Button` component and an `Image`.
2. Select the ship root (or a controller GameObject) and add the `FireAtWillController` component if it is not already present.
3. Fill the inspector:
   - **Fire At Will Button:** drag the toggle button.
   - **Fire At Will Image:** optional; leave empty to reuse the button’s target graphic.
   - **Inactive/Active Sprites:** assign the “Off” and “On” sprites.
   - **Start Active:** leave unchecked unless you want auto-fire on at spawn.
   - **Weapon Mounts:** leave empty when using auto-populate.
   - **Auto Populate Mounts From Children:** keep enabled.
   - **Auto Populate Root Override:** drag the top-level `Ship` transform. This guarantees both bow and aft mounts are discovered even if the controller lives elsewhere.
   - **Enable Debug Logging:** check this when troubleshooting; log entries go to `Logs/game_debug.log`.
4. Enter Play Mode, press the Fire-at-Will button, and watch the log for `[FireAtWill] Auto-populated N mount(s)...` lines. When active, the controller calls `TryFire()` on every registered mount each frame.

### 4. Double-check keyboard and HUD firing
1. Press **F** to confirm global firing still works (this runs through each `WeaponMount.TryFire()` via `ProjectileLauncher`).
2. Click each HUD “FIRE” button; they should light up only when the cannon is ready and has a target lock.
3. Toggle Fire-at-Will; both cannons should fire continuously whenever they gain a solution. If nothing happens, enable debug logs on both `FireAtWillController` and the affected `WeaponMount` components, reproduce the issue, and inspect `Logs/game_debug.log` for guidance.

### 5. Reference recap + autopop status
| Component | Field | Default Source | Autopop Notes |
|-----------|-------|---------------|---------------|
| WeaponMount | `Yaw Base` | Child `YawBase` | Manual; required once when creating a mount from scratch. |
| WeaponMount | `Pitch Barrel` | Child `PitchBarrel` | Manual; required once. |
| WeaponMount | `autoPopulatePrefab` | `Assets/Prefabs/Cannon` | Optional; keeps runtime testing convenient. |
| ShipHUDMountDisplay | `weaponMount` | `Ship/<X>_weapon_mount/Weapon_mount` | Auto every Reset/OnEnable using the icon’s name. |
| ShipHUDMountDisplay | `iconImage` | Image on same GO | Auto via `GetComponent<Image>()`. |
| ShipHUDMountDisplay | `emptySprite` | `Assets/UI/Icons/Mount.png` | Auto via editor-time AssetDatabase lookup; override to customize per mount. |
| ShipHUDMountDisplay | Ready indicator fields | `ReadyIndicators/GreenStoplight`, `/RedStoplight` | Auto; ensures green is default sprite and hides redundant red object. |
| ShipHUDMountDisplay | `targetNotAcquiredImage` | Child `TargetNotAquired` | Auto; component forces management on until an image exists. |
| ShipHUDMountDisplay | `healthBarContainer` | Child `Healthbar` | Auto; also enables `manageHealthBar` when missing. |
| ShipHUDMountDisplay | `fireButton` | Child `FIRE` | Auto; listeners wired/unwired automatically. |
| FireAtWillController | `fireAtWillButton` | HUD toggle button | Manual (drag once). |
| FireAtWillController | `fireAtWillImage` | Same button Image | Optional (leave empty to reuse button target graphic). |
| FireAtWillController | `autoPopulateRootOverride` | `Ship` transform | Manual but recommended so controller finds every mount. |

Follow this chart meticulously—most HUD references now self-heal, but understanding where each value originates lets you customize confidently.

---

## Appendix · Troubleshooting Tips
- **Cannon looks stretched or sideways:** Something above `PitchBarrel` has a non-uniform scale. Reset Scale to (1,1,1) for every parent and rotate only the socket.
- **HUD button never enables:** The mount needs a valid target (`TargetingController` must have selected one) and the sensor collider must overlap the target (`HasTargetInsideAcquisitionCollider = true`). Enable debug logging on `ShipHUDDisplay` and `WeaponMount` to see the exact condition.
- **Fire-at-Will logs “Update skipped: no weapon mounts registered”:** Either the controller is on a different GameObject and lacks an `autoPopulateRootOverride`, or every entry in `weaponMounts` is null. Set the override to the Ship root and click the toggle again.
- **Target indicator never hides:** Assign the `targetNotAcquiredImage` and ensure the mount’s sensor collider is enabled and positioned in front of the barrel.

With these steps completed, every new mount you add to the ship will be fully represented in the HUD and respond to keyboard, HUD buttons, and Fire-at-Will input without extra coding.
