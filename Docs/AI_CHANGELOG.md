# AI Changelog (for future sessions)

Purpose: This file is a fast, structured brief for a new AI assistant to pick up where the last session left off. Keep it concise and actionable.

## How to Use
- Read "Current Snapshot" first to understand the present behavior and controls.
- Use "Key Files" and "Recent Changes" to locate code.
- Follow "Open Decisions/Next Steps" to continue.

---

## Template for New Entries
- Date:
- Summary (2–3 bullets):
- Files Touched:
- Behavior Changes (what the player will notice):
- Controls Mapping (diffs only):
- Config/Inspector Notes:
- Known Issues:
- Next Steps:

---

## Current Snapshot (2025-11-09)

Summary
- Overhead camera fully rebuilt: straight-down view, follows ship with persistent X/Z offset, panning via arrows, zoom via Ctrl+Arrows, snap via Ctrl+F3.
- Multi-view manager switches Bridge/Follow/Overhead with F1/F2/F3.
- Playfield bounds exist logically; overhead currently has no boundary clamping.

Behavior (Overhead)
- Always points straight down.
- Position = ship.position + offsetXZ + up * heightAboveShip.
- Arrow keys pan (modify offsetXZ):
  - Up: camera toward -Z (ship appears lower)
  - Down: camera toward +Z (ship appears higher)
  - Left: camera toward -X (ship appears to the right)
  - Right: camera toward +X (ship appears to the left)
- Zoom:
  - Ctrl+Up: zoom in (FOV mode by default; optional height-zoom)
  - Ctrl+Down: zoom out
  - Ctrl+F3: reset zoom to baseline and snap above ship (offset = 0)

Controls (Global)
- F1 Bridge, F2 Follow, F3 Overhead (via CameraViewManager)
- Overhead specific: Ctrl+Up/Down (zoom), Ctrl+F3 (reset), Arrows (pan)

Key Files
- `Assets/Scripts/CameraViewManager.cs`: switches views; wires overhead controller.
- `Assets/Scripts/OverheadViewController.cs`: new overhead behavior (ship follow, pan, zoom, snap).
- `Assets/Scripts/CameraOrbitMove.cs`: follow orbit controller (unmodified in overhead).
- `Assets/Scripts/CameraMove.cs`: bridge rotation controller (not used in overhead).
- `Assets/Scripts/GameFieldBounds.cs`: logical bounds (currently unused by overhead for clamping).

Recent Changes (2025-11-09)
- Rewrote `OverheadViewController` from scratch:
  - Removed anchors/tilt; always straight down.
  - Added ship auto-target (finds `Ship` or `OverheadCameraMount` parent).
  - Persistent X/Z offset panning; follows ship while preserving offset.
  - Zoom: FOV-based by default, optional height-zoom with ground clamp.
  - Ctrl+F3 resets offset and zoom to baseline.
  - Auto-adjust camera clip planes to avoid grey-screen.
- Updated `CameraViewManager.EnterOverhead()` to set `shipTarget`, `heightAboveShip=1000`, and call `SnapToShipCenter()`.

Config / Inspector Defaults
- OverheadViewController:
  - heightAboveShip = 1000
  - panSpeed = 200
  - useFOVZoom = true
  - minFOV = 5, maxFOV = captured at Start (baseFOV)
  - minHeightAboveGround = 50 (only used when useFOVZoom=false)
  - farClipPadding = 300
  - snapKey = F3 (use with Ctrl)

Known Issues / Notes
- No playfield clamping for overhead yet (can pan outside field).
- OverheadCameraMount isn’t required; used only for auto-detection of `shipTarget` via parent.
- If `Ship` not found and no `OverheadCameraMount`, overhead will warn and not update.

Open Decisions / Next Steps
- Add optional boundary clamping against `GameFieldBounds` (with soft friction).
- Mouse wheel zoom + middle-click recenter.
- Smoothing for pan/zoom; configurable keybinds.
- Persist overhead offset/zoom across mode switches.
