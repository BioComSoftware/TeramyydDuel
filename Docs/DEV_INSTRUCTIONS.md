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
  1) In the weapon prefab, create a child “Muzzle” at the barrel tip; rotate it so +Y points out of the barrel.
  2) Assign Muzzle to spawnPoint.
  3) Parent muzzle effects (smoke/blast) under Muzzle; set their Simulation Space = Local.

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

## ShipHUDRepresentation (on Ship root) + ShipHUDPanel (on HUD Canvas)
- Representation (model):
  - ShipHUDRepresentation lives on the Ship; defines the ship icon sprite, opacity, anchor/offset/size.
  - You can extend it with a list of mount markers keyed by mountId to render per‑weapon statuses.
- Panel (view):
  - ShipHUDPanel lives on the HUD Canvas; auto‑creates a container + Image.
  - Reads the active representation and renders the ship icon in the chosen screen corner.
  - To show mount markers: add marker records (normalized positions 0..1). The panel converts to anchored positions.

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
