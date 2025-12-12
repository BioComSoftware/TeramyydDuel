using System;
using System.Collections.Generic;
using UnityEngine;
using Teramyyd.UI;

/// <summary>
/// Instantiates CrewMember prefabs at runtime based on the persisted crew snapshot,
/// then keeps their world positions in sync with HUD slot anchors.
/// </summary>
[AddComponentMenu("Teramyyd/Crew/Crew Runtime Spawner")]
public class CrewRuntimeSpawner : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Prefab that contains a CrewMember component (and any visuals) to clone per saved crew entry.")]
    [SerializeField] CrewMember defaultCrewPrefab;
    [Tooltip("Optional override for where spawned crew GameObjects are parented.")]
    [SerializeField] Transform crewParent;

    [Header("Visuals")]
    [Tooltip("Hide renderer components while a crew member is unassigned.")]
    [SerializeField] bool hideUnassignedVisuals = false;

    [Header("Optional HUD Bridge")]
    [Tooltip("When assigned, uses HUD drag/drop feedback to reposition crew visuals in realtime.")]
    [SerializeField] CrewHUDController hudController;

    readonly Dictionary<string, List<Transform>> _anchorLookup = new Dictionary<string, List<Transform>>(StringComparer.Ordinal);
    readonly Dictionary<string, int> _stationAnchorNextIndex = new Dictionary<string, int>(StringComparer.Ordinal);
    readonly Dictionary<string, CrewMember> _spawnedCrewById = new Dictionary<string, CrewMember>(StringComparer.Ordinal);
    readonly Dictionary<CrewMember, Renderer[]> _rendererCache = new Dictionary<CrewMember, Renderer[]>();

    void Awake()
    {
        // Anchor lookup is now populated exclusively via RegisterStationAnchors() at runtime
    }

    void OnEnable()
    {
        if (hudController != null)
        {
            hudController.OnVisualAnchorChanged += HandleVisualAnchorChanged;
        }
    }

    void OnDisable()
    {
        if (hudController != null)
        {
            hudController.OnVisualAnchorChanged -= HandleVisualAnchorChanged;
        }
    }

    void Start()
    {
        SpawnPersistedCrew();
    }

    /// <summary>
    /// Allows runtime systems (e.g., CrewStationAnchorRuntimeBuilder) to register anchors for a station.
    /// Passing null or an empty list removes the registration.
    /// </summary>
    public void RegisterStationAnchors(string stationId, IList<Transform> anchors)
    {
        if (string.IsNullOrWhiteSpace(stationId))
            return;

        if (anchors == null || anchors.Count == 0)
        {
            if (_anchorLookup.ContainsKey(stationId))
            {
                _anchorLookup.Remove(stationId);
            }
            _stationAnchorNextIndex.Remove(stationId);
            return;
        }

        if (!_anchorLookup.TryGetValue(stationId, out var list) || list == null)
        {
            list = new List<Transform>(anchors.Count);
            _anchorLookup[stationId] = list;
        }
        else
        {
            list.Clear();
        }

        for (int i = 0; i < anchors.Count; i++)
        {
            var anchor = anchors[i];
            if (anchor == null)
                continue;

            if (!list.Contains(anchor))
            {
                list.Add(anchor);
            }
        }

        _stationAnchorNextIndex[stationId] = 0;
    }

    void SpawnPersistedCrew()
    {
        if (defaultCrewPrefab == null)
        {
            Debug.LogWarning("CrewRuntimeSpawner is missing a defaultCrewPrefab reference.", this);
            return;
        }

        var persistence = CrewPersistenceManager.Instance;
        if (persistence == null)
        {
            Debug.LogWarning("CrewRuntimeSpawner requires a CrewPersistenceManager in the scene.", this);
            return;
        }

        var states = persistence.CrewStates;
        if (states == null || states.Count == 0)
            return;

        Transform parent = crewParent != null ? crewParent : transform;
        foreach (var state in states)
        {
            if (state == null || string.IsNullOrEmpty(state.crewId))
                continue;

            if (_spawnedCrewById.ContainsKey(state.crewId))
                continue;

            CrewMember crew = SpawnCrewFromState(state, parent);
            if (crew == null)
                continue;

            _spawnedCrewById[state.crewId] = crew;
            // Note: PositionCrewAtAnchor (called in SpawnCrewFromState) already handles visibility
            // based on whether valid anchors exist. No need to hide here.
        }
    }

    CrewMember SpawnCrewFromState(CrewMemberState state, Transform parent)
    {
        CrewMember.PushBootstrapState(state);
        CrewMember crew = null;
        try
        {
            crew = Instantiate(defaultCrewPrefab, parent);
        }
        catch
        {
            CrewMember.DiscardBootstrapState();
            throw;
        }

        if (crew == null)
            return null;

        crew.name = string.IsNullOrEmpty(state.displayName)
            ? $"Crew_{state.crewId}"
            : $"Crew_{state.displayName}";

        PositionCrewAtAnchor(crew, state.assignedStationId);
        return crew;
    }

    void PositionCrewAtAnchor(CrewMember crew, string stationId)
    {
        if (crew == null)
            return;

        if (!string.IsNullOrEmpty(stationId) && TryGetNextAnchor(stationId, out var anchor) && anchor != null)
        {
            crew.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            SetCrewVisualActive(crew, true);
            return;
        }

        // If no station assigned, try unassigned crew anchors
        if (string.IsNullOrEmpty(stationId))
        {
            if (TryGetNextAnchor("unassigned_crew", out var unassignedAnchor) && unassignedAnchor != null)
            {
                crew.transform.SetPositionAndRotation(unassignedAnchor.position, unassignedAnchor.rotation);
                // Show visuals if we have a valid unassigned anchor
                SetCrewVisualActive(crew, true);
                return;
            }
            
            // No unassigned anchor available - respect hideUnassignedVisuals setting
            if (hideUnassignedVisuals)
            {
                SetCrewVisualActive(crew, false);
            }
        }

        // Fallback to parent origin
        if (crewParent != null)
        {
            crew.transform.localPosition = Vector3.zero;
            crew.transform.localRotation = Quaternion.identity;
        }
    }

    bool TryGetAnchor(string stationId, out Transform anchor)
    {
        return TryGetAnchor(stationId, 0, out anchor);
    }

    bool TryGetAnchor(string stationId, int slotIndex, out Transform anchor)
    {
        anchor = null;
        if (!_anchorLookup.TryGetValue(stationId, out var list) || list == null || list.Count == 0)
            return false;

        int index = Mathf.Clamp(slotIndex, 0, list.Count - 1);
        if (slotIndex > 0 && list.Count > 1)
        {
            index = slotIndex % list.Count;
        }

        anchor = list[index];
        return anchor != null;
    }

    bool TryGetNextAnchor(string stationId, out Transform anchor)
    {
        int slotIndex = 0;
        if (!string.IsNullOrEmpty(stationId))
        {
            if (!_stationAnchorNextIndex.TryGetValue(stationId, out slotIndex))
            {
                _stationAnchorNextIndex[stationId] = 1;
                slotIndex = 0;
            }
            else
            {
                _stationAnchorNextIndex[stationId] = slotIndex + 1;
            }
        }

        return TryGetAnchor(stationId, slotIndex, out anchor);
    }

    void HandleVisualAnchorChanged(CrewMember crew, CrewStation station, Transform worldAnchor)
    {
        if (crew == null)
            return;

        if (!_spawnedCrewById.TryGetValue(crew.crewId, out var trackedCrew) || trackedCrew == null)
            return;

        if (worldAnchor != null)
        {
            trackedCrew.transform.SetPositionAndRotation(worldAnchor.position, worldAnchor.rotation);
            SetCrewVisualActive(trackedCrew, true);
            return;
        }

        if (station != null)
        {
            Transform chosenAnchor = null;
            int slotIndex = GetCrewSlotIndex(crew, station);
            if (slotIndex >= 0 && TryGetAnchor(station.stationId, slotIndex, out var indexedAnchor))
            {
                chosenAnchor = indexedAnchor;
            }
            else if (TryGetAnchor(station.stationId, out var fallbackAnchor))
            {
                chosenAnchor = fallbackAnchor;
            }

            if (chosenAnchor != null)
            {
                trackedCrew.transform.SetPositionAndRotation(chosenAnchor.position, chosenAnchor.rotation);
                SetCrewVisualActive(trackedCrew, true);
                return;
            }
        }

        // Crew is unassigned - try to position at an unassigned anchor
        if (station == null)
        {
            if (TryGetNextAnchor("unassigned_crew", out var unassignedAnchor) && unassignedAnchor != null)
            {
                trackedCrew.transform.SetPositionAndRotation(unassignedAnchor.position, unassignedAnchor.rotation);
                SetCrewVisualActive(trackedCrew, true);
                return;
            }
            
            if (hideUnassignedVisuals)
            {
                SetCrewVisualActive(trackedCrew, false);
                return;
            }
        }

        if (hideUnassignedVisuals)
        {
            SetCrewVisualActive(trackedCrew, false);
        }
    }

    int GetCrewSlotIndex(CrewMember crew, CrewStation station)
    {
        if (crew == null || station == null)
            return -1;

        var assignedCrew = station.AssignedCrew;
        if (assignedCrew == null)
            return -1;

        for (int i = 0; i < assignedCrew.Count; i++)
        {
            if (assignedCrew[i] == crew)
                return i;
        }

        return -1;
    }

    void SetCrewVisualActive(CrewMember crew, bool isActive)
    {
        if (!hideUnassignedVisuals || crew == null)
            return;

        if (!_rendererCache.TryGetValue(crew, out var renderers) || renderers == null)
        {
            renderers = crew.GetComponentsInChildren<Renderer>(includeInactive: true);
            _rendererCache[crew] = renderers;
        }

        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = isActive;
            }
        }
    }
}
