Teramyyd — Add Weapon Mounts Instructions

## Purpose
These notes walk a non-Unity user through adding a brand-new weapon mount to the player ship, wiring the cannon prefab so it points the correct direction, and exposing that mount on the Ship Representation HUD (including the Fire-at-Will toggle and the per-mount FIRE buttons). Follow every step even if you duplicate an existing mount—the goal is to ensure transforms stay clean and the UI stays in sync.

---

## Part 1 · Build the Physical Weapon Mount

### 1. Create a clean socket under the ship
1. In the Hierarchy, expand `Ship` (the playable airship prefab in the scene).
2. Right-click `Ship` → **Create Empty**. Name it after the area you are working on (for example `Bow_mount_socket` or `Aft_mount_socket`).
3. With the new socket selected, press **Reset** on the Transform component so Position = (0,0,0), Rotation = (0,0,0), Scale = (1,1,1).
4. Move the socket (with the Move tool) to the deck location where you want the weapon base to sit. Rotate the socket only if you want the entire mount to face a different compass direction. Keep Scale at (1,1,1). This socket keeps the mount out of any skewed/scaled geometry while letting you position it anywhere.

### 2. Add or duplicate the WeaponMount object
- **Duplicate route:** Select an existing mount such as `Ship/Bow_weapon_mount/Weapon_mount`, press **Ctrl+D**, drag the duplicate under the new socket, then rename it (e.g., `Aft_weapon_mount`).
- **From scratch:**
  1. Inside the socket, create an empty GameObject named `Weapon_mount`.
  2. Create two children: `YawBase` and under it `PitchBarrel`. Reset both to default transforms.
  3. Select `Weapon_mount` and add the `WeaponMount` component.
  4. Drag `YawBase` into the `Yaw Base` field and `PitchBarrel` into the `Pitch Barrel` field of the component.

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

### 5. Clean parenting recap
```
Ship
└── Bow_mount_socket (positioned/rotated as needed)
    └── Weapon_mount (contains WeaponMount component)
        └── YawBase
            └── PitchBarrel
                └── Cannon prefab (spawned at runtime)
```
Keep every Transform in that chain at Scale (1,1,1). Only the socket’s Position/Rotation should be edited for placement.

---

## Part 2 · Register the Mount with the HUD

### 1. Prepare the HUD artwork
1. Open the HUD Canvas (usually `HUD/ShipRepresentation`). Inside you should see the ship outline sprite.
2. Add or duplicate an icon Image where you want the mount marker. Name it the same as the mount (e.g., `Bow_weapon_mount_icon`).
3. (Optional) Add child Images for status lights, target-not-acquired indicators, health bar placeholders, and a Button labeled “FIRE”. These are purely UI elements.

### 2. Configure ShipHUDDisplay
1. Select the GameObject that hosts `ShipHUDDisplay` (commonly `HUD/ShipRepresentation/Ship HUD Display`).
2. In the inspector, expand the `Mount Icons` array and add a new element.
3. For the new `MountIconBinding` fill the fields:
   - **Weapon Mount:** drag the `Weapon_mount` instance from the Ship hierarchy.
   - **Icon Image:** drag the HUD Image that represents this mount on the outline.
   - **Empty Sprite:** assign the “blank” or grey icon to show when nothing is mounted.
4. Optional sub-features (only hook what you are using):
   - **Ready Indicator:** check `Manage Ready Indicator`, assign the little light Image plus `Ready` (green) and `Not Ready` (red) sprites. Leave `Hide Ready Indicator When No Weapon` checked so the light disappears for empty mounts.
   - **Target Acquisition Indicator:** check `Manage Target Not Acquired Indicator` and drag the warning sprite Image. ShipHUDDisplay will toggle it whenever the mount lacks a firing solution.
   - **Health Bar:** check `Manage Health Bar`, assign the `RectTransform` placeholder. Colors can stay default or be customized.
   - **Fire Button:** drag the HUD “FIRE” button. Ensure the button has a `Button` component and the canvas has an `EventSystem`. No manual OnClick wiring is required; ShipHUDDisplay adds/removes listeners automatically.
5. Repeat for every mount on the ship.

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

### 5. Recap of references to fill in
| Component | Field | Drag This |
|-----------|-------|-----------|
| WeaponMount | `Yaw Base` | The child pivot that should yaw around local Y |
| WeaponMount | `Pitch Barrel` | The child pivot that should pitch around local X |
| WeaponMount | `autoPopulatePrefab` | `Assets/Prefabs/Cannon` (optional but recommended) |
| ShipHUDDisplay | `Mount Icons[x].weaponMount` | The in-scene `Weapon_mount` object |
| ShipHUDDisplay | `Mount Icons[x].iconImage` | The HUD Image for that mount |
| ShipHUDDisplay | `Mount Icons[x].fireButton` | (Optional) HUD Button labeled FIRE |
| ShipHUDDisplay | `Mount Icons[x].targetNotAcquiredImage` | (Optional) warning sprite |
| ShipHUDDisplay | `Mount Icons[x].healthBarContainer` | (Optional) RectTransform placeholder |
| FireAtWillController | `fireAtWillButton` | HUD toggle button |
| FireAtWillController | `fireAtWillImage` | (Optional) same button Image |
| FireAtWillController | `autoPopulateRootOverride` | Ship root transform |

Follow these wiring tables meticulously—missing references are the #1 reason cannons fail to appear on the HUD or refuse to auto-fire.

---

## Appendix · Troubleshooting Tips
- **Cannon looks stretched or sideways:** Something above `PitchBarrel` has a non-uniform scale. Reset Scale to (1,1,1) for every parent and rotate only the socket.
- **HUD button never enables:** The mount needs a valid target (`TargetingController` must have selected one) and the sensor collider must overlap the target (`HasTargetInsideAcquisitionCollider = true`). Enable debug logging on `ShipHUDDisplay` and `WeaponMount` to see the exact condition.
- **Fire-at-Will logs “Update skipped: no weapon mounts registered”:** Either the controller is on a different GameObject and lacks an `autoPopulateRootOverride`, or every entry in `weaponMounts` is null. Set the override to the Ship root and click the toggle again.
- **Target indicator never hides:** Assign the `targetNotAcquiredImage` and ensure the mount’s sensor collider is enabled and positioned in front of the barrel.

With these steps completed, every new mount you add to the ship will be fully represented in the HUD and respond to keyboard, HUD buttons, and Fire-at-Will input without extra coding.
