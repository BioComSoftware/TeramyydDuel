using System;
using System.Collections.Generic;
using UnityEngine;
using Teramyyd.UI;

[AddComponentMenu("Teramyyd/Crew/Crew Station Anchor Runtime Builder")]
public sealed class CrewStationAnchorRuntimeBuilder : MonoBehaviour
{
    [Header("Station Connections")]
    [SerializeField] CrewStation station;
    [SerializeField] CrewHUDStationSlot hudSlot;

    [Header("World Anchor Prefab")]    
    [Tooltip("Parent under which runtime world anchors will be created (e.g., Bow_weapon_mount/Crew_anchors). Defaults to this transform when empty.")]
    [SerializeField] Transform worldAnchorParent;
    [Tooltip("Prefab or template transform cloned for each world anchor. Leave empty to spawn simple empty transforms.")]
    [SerializeField] Transform worldAnchorPrefab;

    [Header("World Layout")]
    [Tooltip("Side-to-side spacing (local X) between crew anchors when more than one is required.")]
    [SerializeField, Min(0.05f)] float worldAnchorLateralSpacing = 0.5f;
    [Tooltip("Front-to-back spacing (local Z) between rows when three or more anchors are required.")]
    [SerializeField, Min(0.05f)] float worldAnchorRowSpacing = 0.5f;

    [Header("HUD Anchor Prefab")]
    [Tooltip("Parent under which runtime HUD anchors will be created (e.g., ShipOutline/Bow_weapon_mount/Bow_weapon_mount_crew_anchors)." )]
    [SerializeField] RectTransform hudAnchorParent;
    [Tooltip("Prefab cloned for each HUD anchor. Leave empty to generate a bare RectTransform.")]
    [SerializeField] RectTransform hudAnchorPrefab;

    [Header("HUD Layout")]
    [SerializeField] bool autoDistributeHudAnchors = true;
    [Min(0f)] [SerializeField] float hudAnchorSpacingPadding = 2f;
    [Min(1f)] [SerializeField] float hudAnchorMinSpacing = 24f;

    [Header("Options")]
    [Tooltip("If true, the 'Override Anchor Count' value will be used instead of the Station's Maximum Crew.")]
    [SerializeField] bool useOverrideAnchorCount = false;

    [Tooltip("Override for number of anchors to create. Only used if 'Use Override Anchor Count' is true.")]
    [SerializeField, Min(0)] int overrideAnchorCount = 0;

    [Tooltip("Optional prefix override for generated anchor names (defaults to station or parent name).")]
    [SerializeField] string anchorNamePrefix = string.Empty;
    [Tooltip("Automatically rebuild anchors when this component enables in play mode.")]
    [SerializeField] bool rebuildOnEnable = true;
    [Tooltip("Writes verbose logs while constructing anchors.")]
    [SerializeField] bool logVerbose = false;

    readonly List<Transform> _worldAnchors = new List<Transform>();
    readonly List<RectTransform> _hudAnchors = new List<RectTransform>();
    CrewRuntimeSpawner _spawner;

    void Reset()
    {
        AutoAssignReferences();
    }

    void Awake()
    {
        AutoAssignReferences();
    }

    void OnEnable()
    {
        if (Application.isPlaying && rebuildOnEnable)
        {
            RebuildAnchors();
        }
    }

    void OnDisable()
    {
        if (Application.isPlaying)
        {
            CleanupSpawnedAnchors();
            RegisterWorldAnchors(null);
        }
    }

    public void RebuildAnchors()
    {
        if (!Application.isPlaying)
            return;

        AutoAssignReferences();
        CleanupSpawnedAnchors();

        int anchorCount = DetermineAnchorCount();
        if (anchorCount <= 0)
        {
            LogWarning("No anchors generated because anchorCount resolved to 0.");
            return;
        }

        BuildWorldAnchors(anchorCount);
        BuildHudAnchors(anchorCount);
        ApplyAnchorsToHudSlot();
        RegisterWorldAnchors(_worldAnchors);
    }

    void AutoAssignReferences()
    {
        if (station == null)
        {
            station = GetComponent<CrewStation>();
        }

        if (hudSlot == null)
        {
            hudSlot = GetComponent<CrewHUDStationSlot>();
        }

        if (hudAnchorParent == null && hudSlot != null)
        {
            hudAnchorParent = hudSlot.transform as RectTransform;
        }
    }

    int DetermineAnchorCount()
    {
        if (useOverrideAnchorCount && overrideAnchorCount > 0)
            return overrideAnchorCount;

        if (station != null)
        {
            station.EnsureStationId();
            // Allow 0 anchors if MaximumCrewAllowed is 0 (e.g. automated station)
            return Mathf.Max(0, station.MaximumCrewAllowed);
        }

        return 1;
    }

    void BuildWorldAnchors(int anchorCount)
    {
        Transform parent = worldAnchorParent != null ? worldAnchorParent : transform;
        string prefix = ResolvePrefix(GetSuggestedWorldPrefix(parent));

        var offsets = GenerateWorldAnchorOffsets(anchorCount);

        for (int i = 0; i < anchorCount; i++)
        {
            Transform instance = CreateWorldAnchorInstance(parent, i);
            instance.name = $"{prefix}_CrewAnchor{i + 1}";
            if (i < offsets.Count)
            {
                instance.localPosition = offsets[i];
            }
            _worldAnchors.Add(instance);
            LogVerbose($"Created world anchor '{instance.name}'.");
        }
    }

    Transform CreateWorldAnchorInstance(Transform parent, int index)
    {
        Transform instance;
        if (worldAnchorPrefab != null)
        {
            instance = Instantiate(worldAnchorPrefab, parent, false);
        }
        else
        {
            var go = new GameObject("CrewWorldAnchor" + (index + 1));
            instance = go.transform;
            instance.SetParent(parent, false);
        }

        return instance;
    }

    List<Vector3> GenerateWorldAnchorOffsets(int totalCount)
    {
        var offsets = new List<Vector3>(totalCount);
        if (totalCount <= 0)
            return offsets;

        if (totalCount == 1)
        {
            offsets.Add(Vector3.zero);
            return offsets;
        }

        if (totalCount == 2)
        {
            float half = worldAnchorLateralSpacing * 0.5f;
            offsets.Add(new Vector3(-half, 0f, 0f));
            offsets.Add(new Vector3(half, 0f, 0f));
            return offsets;
        }

        if (totalCount == 3)
        {
            float half = worldAnchorLateralSpacing * 0.5f;
            offsets.Add(new Vector3(-half, 0f, 0f));
            offsets.Add(new Vector3(half, 0f, 0f));
            offsets.Add(new Vector3(0f, 0f, worldAnchorRowSpacing));
            return offsets;
        }

        var rows = CalculateWorldAnchorRows(totalCount);
        float zOffset = 0f;
        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            int rowCount = rows[rowIndex];
            float centerOffset = (rowCount - 1) * 0.5f;
            for (int i = 0; i < rowCount; i++)
            {
                float x = (i - centerOffset) * worldAnchorLateralSpacing;
                offsets.Add(new Vector3(x, 0f, zOffset));
            }
            zOffset += worldAnchorRowSpacing;
        }

        return offsets;
    }

    List<int> CalculateWorldAnchorRows(int totalCount)
    {
        var rows = new List<int>();
        int remaining = totalCount;

        while (remaining > 0)
        {
            if (remaining == 4)
            {
                rows.Add(2);
                rows.Add(2);
                break;
            }

            if (remaining == 5)
            {
                rows.Add(3);
                rows.Add(2);
                break;
            }

            if (remaining == 1 && rows.Count > 0)
            {
                int lastIndex = rows.Count - 1;
                if (rows[lastIndex] > 2)
                {
                    rows[lastIndex] -= 1;
                    rows.Add(2);
                }
                else
                {
                    rows.Add(1);
                }
                break;
            }

            int row = Mathf.Min(3, remaining);
            rows.Add(row);
            remaining -= row;
        }

        return rows;
    }

    void BuildHudAnchors(int anchorCount)
    {
        if (hudSlot == null && hudAnchorParent == null)
        {
            LogWarning("HUD slot or anchor parent missing; skipping HUD anchor creation.");
            return;
        }

        RectTransform parent = hudAnchorParent != null ? hudAnchorParent : hudSlot.transform as RectTransform;
        string prefix = ResolvePrefix(GetSuggestedHudPrefix(parent));

        for (int i = 0; i < anchorCount; i++)
        {
            RectTransform instance = CreateHudAnchorInstance(parent, i);
            instance.name = $"{prefix}_crew_Icon_Anchor{i + 1}";
            ApplyHudAnchorOffset(instance, i, anchorCount);
            _hudAnchors.Add(instance);
            LogVerbose($"Created HUD anchor '{instance.name}'.");
        }
    }

    RectTransform CreateHudAnchorInstance(RectTransform parent, int index)
    {
        RectTransform instance;
        if (hudAnchorPrefab != null)
        {
            instance = Instantiate(hudAnchorPrefab, parent, false);
        }
        else
        {
            var go = new GameObject("CrewHudAnchor" + (index + 1), typeof(RectTransform));
            instance = go.GetComponent<RectTransform>();
            instance.SetParent(parent, false);
            instance.anchorMin = instance.anchorMax = instance.pivot = new Vector2(0.5f, 0.5f);
            instance.sizeDelta = Vector2.zero;
        }

        return instance;
    }

    void ApplyHudAnchorOffset(RectTransform instance, int index, int totalCount)
    {
        if (!autoDistributeHudAnchors || instance == null || totalCount <= 1)
            return;

        float width = instance.rect.width;
        if (width <= 0f && hudAnchorPrefab != null)
        {
            width = hudAnchorPrefab.rect.width;
        }
        if (width <= 0f)
        {
            width = hudAnchorMinSpacing;
        }

        float spacing = Mathf.Max(hudAnchorMinSpacing, width + hudAnchorSpacingPadding);
        float firstOffset = -spacing * 0.5f * (totalCount - 1);
        float offset = firstOffset + spacing * index;

        var anchored = instance.anchoredPosition;
        anchored.x += offset;
        instance.anchoredPosition = anchored;
    }

    void ApplyAnchorsToHudSlot()
    {
        if (hudSlot == null)
            return;

        if (_worldAnchors.Count > 0)
        {
            hudSlot.worldAnchor = _worldAnchors[0];
            hudSlot.additionalWorldAnchors = CreateTransformExtras(_worldAnchors);
        }
        else
        {
            hudSlot.worldAnchor = null;
            hudSlot.additionalWorldAnchors = Array.Empty<Transform>();
        }

        if (_hudAnchors.Count > 0)
        {
            hudSlot.iconAnchor = _hudAnchors[0];
            hudSlot.additionalIconAnchors = CreateRectTransformExtras(_hudAnchors);
        }
        else
        {
            hudSlot.iconAnchor = null;
            hudSlot.additionalIconAnchors = Array.Empty<RectTransform>();
        }
    }

    Transform[] CreateTransformExtras(List<Transform> anchors)
    {
        if (anchors.Count <= 1)
            return Array.Empty<Transform>();

        var extras = new Transform[anchors.Count - 1];
        for (int i = 1; i < anchors.Count; i++)
        {
            extras[i - 1] = anchors[i];
        }
        return extras;
    }

    RectTransform[] CreateRectTransformExtras(List<RectTransform> anchors)
    {
        if (anchors.Count <= 1)
            return Array.Empty<RectTransform>();

        var extras = new RectTransform[anchors.Count - 1];
        for (int i = 1; i < anchors.Count; i++)
        {
            extras[i - 1] = anchors[i];
        }
        return extras;
    }

    void RegisterWorldAnchors(IList<Transform> anchors)
    {
        if (station == null)
            return;

        station.EnsureStationId();

        if (_spawner == null)
        {
            _spawner = FindFirstObjectByType<CrewRuntimeSpawner>();
        }

        if (_spawner != null)
        {
            _spawner.RegisterStationAnchors(station.stationId, anchors);
        }
    }

    void CleanupSpawnedAnchors()
    {
        foreach (var anchor in _worldAnchors)
        {
            if (anchor != null)
            {
                Destroy(anchor.gameObject);
            }
        }
        _worldAnchors.Clear();

        foreach (var anchor in _hudAnchors)
        {
            if (anchor != null)
            {
                Destroy(anchor.gameObject);
            }
        }
        _hudAnchors.Clear();
    }

    string ResolvePrefix(string fallback)
    {
        if (!string.IsNullOrEmpty(anchorNamePrefix))
            return anchorNamePrefix;

        if (!string.IsNullOrEmpty(fallback))
            return fallback;

        return station != null ? station.gameObject.name : gameObject.name;
    }

    void LogVerbose(string message)
    {
        if (logVerbose)
        {
        }
    }

    void LogWarning(string message)
    {
    }

    string GetSuggestedWorldPrefix(Transform anchorParent)
    {
        if (anchorParent != null && anchorParent.parent != null)
            return anchorParent.parent.name;

        if (anchorParent != null)
            return anchorParent.name;

        if (station != null)
            return station.gameObject.name;

        return gameObject.name;
    }

    string GetSuggestedHudPrefix(Transform anchorParent)
    {
        if (anchorParent != null && anchorParent.parent != null)
            return anchorParent.parent.name;

        if (anchorParent != null)
            return anchorParent.name;

        if (hudSlot != null && hudSlot.transform.parent != null)
            return hudSlot.transform.parent.name;

        return GetSuggestedWorldPrefix(worldAnchorParent);
    }
}
