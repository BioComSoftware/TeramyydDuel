# Crew HUD Station Slot Wiring Guide

## Overview
This guide explains how to wire `CrewHUDStationSlot` components in the Unity Editor so crew can be assigned to weapon mounts, engines, and lift devices via drag-and-drop.

**CRITICAL REQUIREMENT**: Each HUD station slot must reference the actual `CrewStation` component on your ship's subsystems (weapon mounts, engines, lift devices).

---

## Part 1: Ensure Unique Station Names

### Why This Matters
Each weapon mount, engine, and lift device auto-generates a `CrewStation` component with an ID based on its GameObject name. If multiple GameObjects have the same name, they'll have duplicate station IDs, causing all HUD slots to point to the same station.

### Step-by-Step

1. In **Hierarchy**, expand your **Ship** GameObject
2. Locate all weapon mount GameObjects (e.g., under `Ship/Model/Deck-mounts/`)
3. **Select each mount** and verify its name in the Inspector is unique:
   - ❌ BAD: Multiple mounts all named "Mount_01" or "Weapon_mount"
   - ✅ GOOD: "Bow_weapon_mount", "Port_forward_hull_weapon_mount", "Starboard_weapon_mount"
4. **Rename duplicates** by clicking the name field at top of Inspector and typing a unique name
5. Repeat for all **Engine** and **LiftDevice** GameObjects

**Generated Station IDs:**
- WeaponMount: `{GameObject.name}_crew_slot`
  - Example: "Bow_weapon_mount" → "Bow_weapon_mount_crew_slot"
- Engine: `{GameObject.name}_EngineCrew`
- LiftDevice: `{GameObject.name}_LiftCrew`

---

## Part 2: Wire HUD Station Slots

### Locate HUD Slots
1. In **Hierarchy**: `HUD_Canvas` → `HUD_Root` → `ShipRepresentation` → `ShipOutline`
2. You'll see station slot objects like:
   - `Bow_weapon_mount_crew_slot`
   - `Port_forward_hull_weapon_mount_crew_slot`
   - etc.

### For EACH HUD Slot:

#### A. Reference the Station Component
1. **Click** the HUD slot object in Hierarchy (e.g., `Bow_weapon_mount_crew_slot`)
2. In **Inspector**, find the `Crew HUD Station Slot (Script)` component
3. Under **Station Binding**:
   - Locate the **Station** field
   - Click the circle icon (⊙) next to it
   - A popup window appears showing all GameObjects with CrewStation components
   - **Double-click** the matching weapon mount from the list
     - Example: For `Bow_weapon_mount_crew_slot` → select `Bow_weapon_mount`
   
   **Alternative Method (Drag & Drop):**
   - In Hierarchy, locate the ship's weapon mount (e.g., `Ship/Bow_weapon_mount`)
   - **Click and drag** it into the **Station** field in the Inspector

4. The field should now show the GameObject name (e.g., "Bow_weapon_mount") with "(CrewStation)" below it

#### B. Assign Icon Anchor
1. Still in the same `Crew HUD Station Slot` component
2. Find the **Icon Anchor** field
3. Look for a child RectTransform under the slot (often named `IconAnchor` or similar)
4. **Drag** that child object into the **Icon Anchor** field

#### C. Assign World Anchor (Optional - for 3D crew models)
1. In the ship's 3D hierarchy, find the Transform for the crew's world position
   - Usually named like `{station}_CrewAnchor`
   - Example: `Bow_weapon_mount_CrewAnchor`
2. **Drag** this Transform into the **World Anchor** field

---

## Part 3: Verify Setup

### Before Testing
1. Press **Ctrl+S** to save the scene
2. Check **one** HUD slot to confirm all fields are filled:
   - ✅ Station: Shows a GameObject name
   - ✅ Icon Anchor: Shows a RectTransform child
   - ✅ World Anchor: (Optional) Shows a Transform

### Test in Play Mode
1. Click the **Play** button
2. In **Hierarchy**, expand the unassigned crew pool
3. You should see `CrewHUDCrewIcon` instances
4. **Drag a crew icon** onto a station slot on the ship HUD
5. **Expected**:
   - Icon snaps to the slot
   - Remains there after releasing
   - Console shows: `[CrewManager] TryAssignCrewToStation: SUCCESS`
6. **Drag from station back to unassigned area**
7. **Expected**:
   - Icon returns to pool
   - Console shows: `[CrewManager] UnassignCrew`

### Troubleshooting

| Problem | Cause | Fix |
|---------|-------|-----|
| All crew go to same station | Duplicate GameObject names | Rename weapon mounts to be unique (Part 1) |
| Station field shows "Missing" | CrewStation not created yet | Enter Play Mode once, exit, re-wire |
| Icon snaps back after drop | Station full (max capacity 1) | Check CrewStation.maximumCrewAllowed |
| "Station not found" in logs | Station field empty/wrong | Re-wire Station field (Part 2A) |
| No icons appear | CrewHUDController not configured | Check controller has icon prefab assigned |

### Check Logs
Open `Logs/game_debug.log` and search for:
- `[CrewManager] RegisterStation` - Shows which stations were found
- `[CrewManager] WARNING: Station ID` - Indicates duplicate IDs
- `[CrewManager] TryAssignCrewToStation` - Shows assignment attempts

---

## Quick Reference

### Naming Convention
| Component Type | GameObject Example | Generated Station ID |
|---------------|-------------------|---------------------|
| WeaponMount | Bow_weapon_mount | Bow_weapon_mount_crew_slot |
| WeaponMount | Port_forward_hull_weapon_mount | Port_forward_hull_weapon_mount_crew_slot |
| Engine | Engine_Main | Engine_Main_EngineCrew |
| LiftDevice | LiftDevice_Primary | LiftDevice_Primary_LiftCrew |

### Required Fields per HUD Slot
- **Station** (Required): The weapon mount/engine/lift device GameObject
- **Icon Anchor** (Required): Child RectTransform where crew icon appears
- **World Anchor** (Optional): 3D Transform for crew model placement

---

## Expected Behavior After Setup

✅ **Startup**: Crew with `assignedStationId` in `CrewPersistence.json` appear at station slots
✅ **Unassigned crew**: Icons appear in unassigned pool
✅ **Drag to station**: Icon moves to slot, crew assigns, persistence saves
✅ **Drag between stations**: Crew reassigns, persistence updates
✅ **Drag to unassigned**: Crew unassigns, icon returns to pool

