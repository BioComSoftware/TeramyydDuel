# Developer Reference Map

Use this file to keep a concise record of which GameObjects host each crew-related script plus the references those scripts expect. Update this list whenever we wire new crew stations, HUD slots, or runtime anchor builders.

## Crew Station: Bow Weapon Mount

- `/Ship/Bow_weapon_mount/Bow_weapon_mount_actual`
  - `CrewStation.stationId`: blank in edit mode; runtime overrides to `Bow_weapon_mount_actual_crew_slot`.
  - `CrewStationRequirementProfile` (from mounted cannon prefab `Assets/Prefabs/Cannon.prefab`): sets minimum crew = 1, maximum crew = 2.
  - `CrewStationAnchorRuntimeBuilder.Station`: (self) `CrewStation` on `/Ship/Bow_weapon_mount/Bow_weapon_mount_actual`.
  - `CrewStationAnchorRuntimeBuilder.HUD Slot`: `/Ship/HUD_Canvas/HUD_Root/ShipRepresentation/ShipOutline/Bow_weapon_mount` (`CrewHUDStationSlot`).
  - `CrewStationAnchorRuntimeBuilder.World Anchor Parent`: `/Ship/Bow_weapon_mount/Crew_anchors` (Transform).
  - `CrewStationAnchorRuntimeBuilder.World Anchor Prefab`: `Assets/Prefabs/CrewAnchors/CrewWorldAnchor.prefab`.
  - `CrewStationAnchorRuntimeBuilder.HUD Anchor Parent`: `/Ship/HUD_Canvas/HUD_Root/ShipRepresentation/ShipOutline/Bow_weapon_mount/Bow_weapon_mount_crew_anchors` (RectTransform).
  - `CrewStationAnchorRuntimeBuilder.HUD Anchor Prefab`: `Assets/Prefabs/CrewAnchors/CrewHudAnchor.prefab`.
  - `CrewStationAnchorRuntimeBuilder.Override Anchor Count`: `0` (uses `CrewStation.MaximumCrewAllowed`).
  - `CrewStationAnchorRuntimeBuilder.HUD Layout`: `autoDistributeHudAnchors = true`, `hudAnchorSpacingPadding = 2`, `hudAnchorMinSpacing = 24` (default spacing yields ~13px offsets for two anchors).

- `/Ship/HUD_Canvas/HUD_Root/ShipRepresentation/ShipOutline/Bow_weapon_mount`
  - `CrewHUDStationSlot.Station`: `/Ship/Bow_weapon_mount/Bow_weapon_mount_actual` (`CrewStation`).
  - `CrewHUDStationSlot.iconAnchor`: blank in edit mode (populated at runtime by `CrewStationAnchorRuntimeBuilder`).
  - `CrewHUDStationSlot.additionalIconAnchors`: blank (runtime populated if multiple crew slots exist).
  - `CrewHUDStationSlot.worldAnchor`: blank in edit mode (runtime populated).
  - `CrewHUDStationSlot.additionalWorldAnchors`: blank in edit mode (runtime populated).

- `/Ship/Bow_weapon_mount/Crew_anchors`
  - Transform-only parent for world anchor instances spawned by `CrewStationAnchorRuntimeBuilder`.

- `/Ship/HUD_Canvas/HUD_Root/ShipRepresentation/ShipOutline/Bow_weapon_mount/Bow_weapon_mount_crew_anchors`
  - RectTransform parent for HUD anchor instances spawned by `CrewStationAnchorRuntimeBuilder`.
