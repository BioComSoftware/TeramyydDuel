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
    [Serializable]
    public struct StationAnchorBinding
    {
        [Tooltip("CrewStation.stationId this anchor represents (case-sensitive).")]
        public string stationId;
        public Transform anchor;
    }

    [Header("Setup")]
    [Tooltip("Prefab that contains a CrewMember component (and any visuals) to clone per saved crew entry.")]
    [SerializeField] CrewMember defaultCrewPrefab;
    [Tooltip("Optional override for where spawned crew GameObjects are parented.")]
    [SerializeField] Transform crewParent;
    [Tooltip("World anchors that correspond to station identifiers so spawned crew can snap into place.")]
    [SerializeField] StationAnchorBinding[] stationAnchors;

    [Header("Visuals")]
    [Tooltip("Hide renderer components while a crew member is unassigned.")]
    [SerializeField] bool hideUnassignedVisuals = false;

    [Header("Optional HUD Bridge")]
    [Tooltip("When assigned, uses HUD drag/drop feedback to reposition crew visuals in realtime.")]
    [SerializeField] CrewHUDController hudController;

    readonly Dictionary<string, Transform> _anchorLookup = new Dictionary<string, Transform>(StringComparer.Ordinal);
    readonly Dictionary<string, CrewMember> _spawnedCrewById = new Dictionary<string, CrewMember>(StringComparer.Ordinal);
    readonly Dictionary<CrewMember, Renderer[]> _rendererCache = new Dictionary<CrewMember, Renderer[]>();

    void Awake()
    {
        RebuildAnchorLookup();
    }

    void OnValidate()
    {
        RebuildAnchorLookup();
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

    void RebuildAnchorLookup()
    {
        _anchorLookup.Clear();
        if (stationAnchors == null)
            return;

        foreach (var binding in stationAnchors)
        {
            if (binding.anchor == null || string.IsNullOrWhiteSpace(binding.stationId))
                continue;

            _anchorLookup[binding.stationId] = binding.anchor;
        }
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
            if (hideUnassignedVisuals && string.IsNullOrEmpty(state.assignedStationId))
            {
                SetCrewVisualActive(crew, false);
            }
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

        if (!string.IsNullOrEmpty(stationId) && TryGetAnchor(stationId, out var anchor) && anchor != null)
        {
            crew.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            SetCrewVisualActive(crew, true);
            return;
        }

        if (crewParent != null)
        {
            crew.transform.localPosition = Vector3.zero;
            crew.transform.localRotation = Quaternion.identity;
        }
    }

    bool TryGetAnchor(string stationId, out Transform anchor)
    {
        return _anchorLookup.TryGetValue(stationId, out anchor);
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

        if (station != null && TryGetAnchor(station.stationId, out var anchor) && anchor != null)
        {
            trackedCrew.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            SetCrewVisualActive(trackedCrew, true);
            return;
        }

        if (hideUnassignedVisuals)
        {
            SetCrewVisualActive(trackedCrew, false);
        }
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
