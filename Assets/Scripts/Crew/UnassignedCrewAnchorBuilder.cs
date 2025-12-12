using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates 3D world anchors for unassigned crew members in a grid layout.
/// The grid distributes evenly within the bounds of the unassigned HUD RectTransform.
/// </summary>
[AddComponentMenu("Teramyyd/Crew/Unassigned Crew Anchor Builder")]
public class UnassignedCrewAnchorBuilder : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Parent transform under which 3D world anchors will be created.")]
    [SerializeField] Transform worldAnchorParent;
    
    [Tooltip("Prefab to instantiate for each world anchor. Leave empty to create simple empty GameObjects.")]
    [SerializeField] Transform worldAnchorPrefab;

    [Header("Grid Layout")]
    [Tooltip("Number of crew anchors to place in each row.")]
    [SerializeField, Min(1)] int anchorsPerRow = 10;
    
    [Tooltip("Approximate size (in meters) each crew member occupies in the 3D world.")]
    [SerializeField, Min(0.1f)] float crewFootprintSize = 1f;
    
    [Tooltip("Spacing multiplier between crew positions (1.0 = touching, >1.0 = more space).")]
    [SerializeField, Min(1f)] float spacingMultiplier = 1.2f;

    [Header("Options")]
    [Tooltip("Prefix for generated anchor names.")]
    [SerializeField] string anchorNamePrefix = "UnassignedCrewAnchor";
    
    [Tooltip("Rebuild anchors when component enables in play mode.")]
    [SerializeField] bool rebuildOnEnable = true;
    
    [Tooltip("Log verbose details during anchor creation.")]
    [SerializeField] bool logVerbose = false;

    readonly List<Transform> _worldAnchors = new List<Transform>();
    CrewRuntimeSpawner _spawner;

    const string UNASSIGNED_STATION_ID = "unassigned_crew";

    void Reset()
    {
        AutoAssignReferences();
    }

    void Awake()
    {
        AutoAssignReferences();
        
        if (Application.isPlaying && rebuildOnEnable)
        {
            RebuildAnchors();
        }
    }

    void OnDisable()
    {
        if (Application.isPlaying)
        {
            CleanupAnchors();
            UnregisterAnchors();
        }
    }

    void AutoAssignReferences()
    {
        if (worldAnchorParent == null)
        {
            worldAnchorParent = transform;
        }
    }

    /// <summary>
    /// Rebuilds all unassigned crew world anchors based on current crew count.
    /// </summary>
    public void RebuildAnchors()
    {
        if (!Application.isPlaying)
            return;

        AutoAssignReferences();
        CleanupAnchors();

        int crewCount = GetUnassignedCrewCount();
        if (crewCount <= 0)
        {
            LogVerbose("No unassigned crew to create anchors for.");
            return;
        }

        BuildWorldAnchors(crewCount);
        RegisterAnchors();
    }

    int GetUnassignedCrewCount()
    {
        // Get crew count from persistence data directly (before CrewManager registration)
        var persistence = CrewPersistenceManager.Instance;
        if (persistence == null)
        {
            Debug.LogWarning("[UnassignedCrewAnchorBuilder] CrewPersistenceManager not found.");
            return 0;
        }

        var states = persistence.CrewStates;
        if (states == null || states.Count == 0)
            return 0;

        // Count crew that don't have an assigned station
        int count = 0;
        foreach (var state in states)
        {
            if (state != null && string.IsNullOrEmpty(state.assignedStationId))
                count++;
        }

        return count;
    }

    void BuildWorldAnchors(int crewCount)
    {
        Transform parent = worldAnchorParent != null ? worldAnchorParent : transform;
        
        // Calculate grid layout based on anchorsPerRow setting
        int columns = Mathf.Min(crewCount, anchorsPerRow);
        int rows = Mathf.CeilToInt((float)crewCount / columns);
        
        // Calculate spacing
        float spacing = crewFootprintSize * spacingMultiplier;
        
        // Calculate grid center offset to center the grid at parent origin
        float totalWidth = (columns - 1) * spacing;
        float totalDepth = (rows - 1) * spacing;
        Vector3 gridOrigin = new Vector3(-totalWidth * 0.5f, 0f, -totalDepth * 0.5f);

        int anchorIndex = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (anchorIndex >= crewCount)
                    break;

                Vector3 localPos = gridOrigin + new Vector3(col * spacing, 0f, row * spacing);
                Transform anchor = CreateWorldAnchor(parent, anchorIndex, localPos);
                _worldAnchors.Add(anchor);
                
                LogVerbose($"Created unassigned crew anchor '{anchor.name}' at local position {localPos}");
                anchorIndex++;
            }
        }

        LogMessage($"Created {_worldAnchors.Count} unassigned crew world anchors in {rows}x{columns} grid.");
    }

    Transform CreateWorldAnchor(Transform parent, int index, Vector3 localPosition)
    {
        Transform anchor;
        
        if (worldAnchorPrefab != null)
        {
            anchor = Instantiate(worldAnchorPrefab, parent, false);
        }
        else
        {
            GameObject go = new GameObject($"{anchorNamePrefix}_{index + 1}");
            anchor = go.transform;
            anchor.SetParent(parent, false);
        }

        anchor.name = $"{anchorNamePrefix}_{index + 1}";
        anchor.localPosition = localPosition;
        anchor.localRotation = Quaternion.identity;

        return anchor;
    }

    void RegisterAnchors()
    {
        if (_spawner == null)
        {
            _spawner = FindFirstObjectByType<CrewRuntimeSpawner>();
        }

        if (_spawner == null)
        {
            Debug.LogWarning("[UnassignedCrewAnchorBuilder] CrewRuntimeSpawner not found in scene.");
            return;
        }

        _spawner.RegisterStationAnchors(UNASSIGNED_STATION_ID, _worldAnchors);
        LogMessage($"Registered {_worldAnchors.Count} unassigned crew anchors with CrewRuntimeSpawner.");
    }

    void UnregisterAnchors()
    {
        if (_spawner != null)
        {
            _spawner.RegisterStationAnchors(UNASSIGNED_STATION_ID, null);
        }
    }

    void CleanupAnchors()
    {
        foreach (var anchor in _worldAnchors)
        {
            if (anchor != null)
            {
                Destroy(anchor.gameObject);
            }
        }
        _worldAnchors.Clear();
    }

    void LogMessage(string message)
    {
        Debug.Log($"[UnassignedCrewAnchorBuilder] {message}");
    }

    void LogVerbose(string message)
    {
        if (logVerbose)
        {
            Debug.Log($"[UnassignedCrewAnchorBuilder] {message}");
        }
    }
}
