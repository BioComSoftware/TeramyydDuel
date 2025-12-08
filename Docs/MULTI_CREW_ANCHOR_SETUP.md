# Multi-Crew Anchor Setup Guide

## Overview

The crew system now supports multiple crew members at a single station, with each crew member positioned at their own dedicated 3D world anchor. This guide explains how the system works and how to configure it in Unity.

## How It Works

### Crew Assignment Order
When multiple crew members are assigned to a station:
1. The first crew member is assigned to anchor index 0
2. The second crew member is assigned to anchor index 1
3. And so on...

The system automatically determines which crew member should use which anchor based on their position in the station's `AssignedCrew` list.

### HUD and 3D World Anchors

Each station has two types of anchors that must match:

1. **HUD Anchors** (2D UI positions):
   - `iconAnchor` - First crew icon position
   - `additionalIconAnchors[]` - Additional crew icon positions
   
2. **World Anchors** (3D game world positions):
   - `worldAnchors[0]` - First crew 3D position
   - `worldAnchors[1]` - Second crew 3D position
   - `worldAnchors[2]` - Third crew 3D position (if needed)

**Important**: The number of world anchors must match the number of HUD anchors for each station.

## Unity Setup Instructions

### 1. CrewHUDStationSlot Setup

For each station slot in your HUD:

1. Select the `CrewHUDStationSlot` GameObject
2. In the Inspector, locate the **Station Binding** section
3. Set the **World Anchors** array size to match your crew capacity:
   - For 1-crew stations: Size = 1
   - For 2-crew stations: Size = 2
   - For 3-crew stations: Size = 3

4. Drag the corresponding world anchor Transforms into each array element:
   - Element 0: `Bow_weapon_mount_CrewAnchor1`
   - Element 1: `Bow_weapon_mount_CrewAnchor2`
   - Etc.

### 2. CrewRuntimeSpawner Setup

The `CrewRuntimeSpawner` component needs to be configured with world anchors for each station:

1. Select the GameObject with the `CrewRuntimeSpawner` component
2. In the Inspector, locate the **Station Anchors** array
3. For each station, configure the `StationAnchorBinding`:
   - **Station Id**: Must match the `CrewStation.stationId` (e.g., "bow_weapon_mount")
   - **Anchors**: Array of Transform references
     - Element 0: First crew position (e.g., `Bow_weapon_mount_CrewAnchor1`)
     - Element 1: Second crew position (e.g., `Bow_weapon_mount_CrewAnchor2`)
     - Etc.

### 3. World Anchor Transform Naming Convention

For consistency and easier debugging, follow this naming pattern:

```
{StationName}_CrewAnchor{Index}
```

Examples:
- `Bow_weapon_mount_CrewAnchor1`
- `Bow_weapon_mount_CrewAnchor2`
- `Stern_weapon_mount_CrewAnchor1`
- `Stern_weapon_mount_CrewAnchor2`

**Note**: While the code doesn't require this naming convention, it helps with organization.

## Technical Details

### Key Components Modified

1. **CrewHUDStationSlot.cs**
   - Changed `worldAnchor` (single) to `worldAnchors[]` (array)
   - Added `GetWorldAnchorForIcon()` method to map crew icons to world anchors
   - Added `GetAnchorIndex()` helper to determine anchor position

2. **CrewRuntimeSpawner.cs**
   - Changed `StationAnchorBinding.anchor` to `StationAnchorBinding.anchors[]`
   - Updated `_anchorLookup` to store `Transform[]` instead of single `Transform`
   - Added `GetCrewIndexAtStation()` to determine crew's position in station
   - Added `TryGetAnchorForCrew()` to select appropriate anchor based on crew index

3. **CrewHUDController.cs**
   - Modified `AttachIconToSlot()` to use `GetWorldAnchorForIcon()` when invoking `OnVisualAnchorChanged`

### Fallback Behavior

If there are more crew assigned than anchors available:
- The system falls back to using `anchors[0]` (the first anchor)
- Multiple crew will occupy the same 3D position
- This prevents crashes but results in visual overlap

**Best Practice**: Always ensure the number of world anchors matches the station's `maximumCrewAllowed` setting.

## Example Configuration

For a 2-crew weapon mount station:

### CrewStation Component
```
Station Id: bow_weapon_mount
Maximum Crew Allowed: 2
```

### CrewHUDStationSlot Component
```
Icon Anchor: Bow_weapon_mount_slot_Icon_Anchor1
Additional Icon Anchors: [Bow_weapon_mount_slot_Icon_Anchor2]
World Anchors: [Bow_weapon_mount_CrewAnchor1, Bow_weapon_mount_CrewAnchor2]
```

### CrewRuntimeSpawner Component
```
Station Anchors:
  - Station Id: bow_weapon_mount
    Anchors: [Bow_weapon_mount_CrewAnchor1, Bow_weapon_mount_CrewAnchor2]
```

## Testing

To verify the setup:

1. Start the game
2. Assign one crew member to the station
   - Check HUD: Icon should appear at first HUD anchor
   - Check 3D world: Crew should appear at first world anchor
3. Assign a second crew member to the same station
   - Check HUD: Second icon should appear at second HUD anchor
   - Check 3D world: Second crew should appear at second world anchor
4. Remove crew members
   - Each crew should disappear from their respective positions
5. Reassign in different order
   - First assigned crew should always use index 0 anchors
   - Order matters: assignment sequence determines anchor usage

## Troubleshooting

### Crew appearing at wrong position
- Verify world anchor array order matches HUD anchor array order
- Check that anchor indices are consistent (Element 0 = first crew, Element 1 = second crew)

### Multiple crew at same 3D position
- Check `worldAnchors` array size matches number of HUD anchors
- Ensure all array elements are assigned (not null)
- Verify `CrewRuntimeSpawner` has matching anchor count in `StationAnchorBinding`

### Crew not visible in 3D world
- Check `CrewRuntimeSpawner.hideUnassignedVisuals` setting
- Verify world anchor Transforms exist in scene
- Check that station IDs match exactly (case-sensitive)

### HUD icons overlapping
- This is a separate issue from world anchors
- Check `iconAnchor` and `additionalIconAnchors` are properly positioned
- Verify `CrewHUDStationSlot.RequestAnchorFor()` is finding available anchors
