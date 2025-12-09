# Multi-Crew Anchor Setup Guide

## Overview

Weapon stations now auto-generate both their 3D world anchor points and their HUD crew icon anchors at runtime. The new `CrewStationAnchorRuntimeBuilder` component reads the crew capacity from each `CrewStation`, instantiates the correct number of anchor prefabs under the parents you provide, registers those transforms with the HUD, and pipes them into `CrewRuntimeSpawner` so 3D CrewMember prefabs follow the same data.

This guide explains how the runtime builder works and how to configure it in your scenes.

## Runtime Flow

1. `CrewStationAnchorRuntimeBuilder` runs when play mode starts.
2. It creates `Bow_weapon_mount_CrewAnchor<n>` GameObjects under your world `Crew_anchors` parent object (for example `/Ship/Bow_weapon_mount/Crew_anchors`) using the assigned world-anchor prefab (or plain transforms when no prefab is specified).
3. It creates `Bow_weapon_mount_crew_Icon_Anchor<n>` `RectTransform` children under the HUD anchor parent (for example `/Ship/HUD_Canvas/HUD_Root/ShipRepresentation/ShipOutline/Bow_weapon_mount/Bow_weapon_mount_crew_anchors`).
4. The script assigns those transforms to the matching `CrewHUDStationSlot` component on `/Ship/HUD_Canvas/HUD_Root/ShipRepresentation/ShipOutline/<mount>_weapon_mount`, then registers the world anchors with the `CrewRuntimeSpawner` component so crew visuals spawn at the right spots.
5. `CrewHUDController` and the drag/drop workflow behave exactly the same as before—icons can be dragged to the runtime-created anchors and the crew are spawned at the matching 3D positions.

## Scene Authoring Checklist

### 1. Create anchor parents

- **World hierarchy**: Under each weapon mount create an empty `Crew_anchors` object, such as `/Ship/Bow_weapon_mount/Crew_anchors`. This keeps the generated anchors positioned and oriented relative to the mount.
- **HUD hierarchy**: Under the matching HUD node create `<mount>_crew_anchors`, such as `/Ship/HUD_Canvas/HUD_Root/ShipRepresentation/ShipOutline/Bow_weapon_mount/Bow_weapon_mount_crew_anchors`. This controls where the HUD anchors spawn.

### 2. Ensure the HUD mount has a CrewHUDStationSlot component

Before wiring the runtime builder, make sure every HUD mount GameObject (for example `/Ship/HUD_Canvas/HUD_Root/ShipRepresentation/ShipOutline/Bow_weapon_mount`) has the `CrewHUDStationSlot` script attached. If the component is missing, add it to that GameObject now—this is the drag/drop target the HUD uses.

### 3. Add the runtime builder component

1. Select the GameObject that hosts the `CrewStation` (typically `/Ship/Bow_weapon_mount/Bow_weapon_mount_actual`).
2. Add the `CrewStationAnchorRuntimeBuilder` component (script).
3. Assign fields:
   - **Station (component reference)**: should auto-fill from the same GameObject (the `CrewStation` script on `<mount>_weapon_mount_actual`).
   - **HUD Slot (component reference)**: drag the GameObject that contains the `CrewHUDStationSlot` script from `/Ship/HUD_Canvas/HUD_Root/ShipRepresentation/ShipOutline/<mount>_weapon_mount`.
   - **World Anchor Parent (GameObject/Transform reference)**: the `/Ship/<mount>_weapon_mount/Crew_anchors` transform you created earlier.
   - **World Anchor Prefab (Prefab reference)**: assign the prefab that determines the orientation for `CrewMember_Default` (for example `Assets/Prefabs/CrewAnchors/CrewWorldAnchor.prefab`).
   - **HUD Anchor Parent (RectTransform reference)**: the `/Ship/HUD_Canvas/HUD_Root/ShipRepresentation/ShipOutline/<mount>_weapon_mount/<mount>_weapon_mount_crew_anchors` RectTransform.
   - **HUD Anchor Prefab (Prefab reference)**: the prefabbed RectTransform with the HUD offsets you want (for example `Assets/Prefabs/CrewAnchors/CrewHudAnchor.prefab`).
   - **Override Anchor Count (optional)**: leave at 0 to use `CrewStation.MaximumCrewAllowed`, or set explicitly if you need more/fewer anchors.
   - **Anchor Name Prefix (optional)**: only needed if you want a different naming pattern than the auto-detected mount name.

### 4. Prefab tips

- **World anchor prefab (Prefab asset reference)**: build a small prefab that has the default local position, rotation, and any helper visuals you need so crew always spawn facing the proper heading (e.g., author `Assets/Prefabs/CrewAnchors/CrewWorldAnchor.prefab`). Every instance of this prefab will be cloned per crew slot.
- **HUD anchor prefab (Prefab asset reference)**: create a prefab with a `RectTransform` and any guide sprites (e.g., `Assets/Prefabs/CrewAnchors/CrewHudAnchor.prefab`). The runtime copies its anchored position/size so every station shares a consistent look.
- If either prefab is left empty the builder will create plain transforms centered on the parent.

### 5. Linking to CrewRuntimeSpawner

No manual setup is required in `CrewRuntimeSpawner` anymore. The runtime builder calls `RegisterStationAnchors` automatically whenever it spawns world anchors. When a station is disabled the builder unregisters them so the spawner falls back to serialized defaults.

## Behavioural Notes

- Anchor counts always match the crew cap (`CrewStation.MaximumCrewAllowed`) unless you override it on the builder component.
- World anchor GameObjects are named `{Mount}_CrewAnchor1..N`; HUD anchors use `{Mount}_crew_Icon_Anchor1..N` to match the drag/drop naming convention.
- Icons still request anchors through the `CrewHUDStationSlot` script on each HUD mount, so drag/drop, tooltips, and `OnVisualAnchorChanged` continue to work without any further wiring.
- Generated anchors exist only at runtime. The scene remains clean in edit mode, so the placeholder parents stay empty until you hit Play.

## Testing

1. Enter Play mode.
2. Confirm that each `/Ship/<mount>_weapon_mount/Crew_anchors` parent now contains the expected number of `*_CrewAnchor` children and that the HUD hierarchy gained `*_crew_Icon_Anchor` children under `/Ship/HUD_Canvas/HUD_Root/ShipRepresentation/ShipOutline/<mount>_weapon_mount/<mount>_weapon_mount_crew_anchors`.
3. Drag a crew icon onto the mount's HUD slot. The icon should snap to the runtime anchor and a `CrewMember_Default` instance should appear at the matching world anchor.
4. Assign additional crew members and verify that icons/crew use the next anchors in order.
5. Unassign crew and ensure the anchors are released (icons return to unassigned slots and world crew despawn or hide).

## Troubleshooting

- **No anchors spawned**: Make sure the `CrewStationAnchorRuntimeBuilder` component is enabled, the scene contains the referenced parents (`/Ship/<mount>_weapon_mount/Crew_anchors` and the HUD anchor parent), and the GameObject is active when play starts. Check the console for `[CrewStationAnchorRuntimeBuilder]` warnings.
- **Crew spawn at origin**: Usually means the world anchor prefab reference was missing or the world parent transform was not set. The builder logs which transforms it created—inspect `/Ship/<mount>_weapon_mount/Crew_anchors` during play.
- **HUD icons overlap**: Verify the HUD anchor prefab has the offsets you expect and that the `/Ship/.../<mount>_weapon_mount/<mount>_weapon_mount_crew_anchors` RectTransform is positioned correctly on the HUD.
- **CrewRuntimeSpawner warning about missing anchors**: Ensure the builder references the correct `CrewStation` component so it can call `CrewRuntimeSpawner.RegisterStationAnchors`. The station ID must match the HUD slot/station used by `CrewManager`.

With the runtime builder in place you no longer need to hand-author arrays on `CrewHUDStationSlot` or `CrewRuntimeSpawner`. Just create the placeholder parents, assign the prefabs once, and let the system populate both the HUD and world anchor hierarchies for every weapon mount when the scene runs.
