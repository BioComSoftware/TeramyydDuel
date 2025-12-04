using System;
using System.Collections.Generic;
using UnityEngine;

namespace Teramyyd.UI
{
    /// <summary>
    /// Binds crew data from CrewManager to HUD widgets, spawns icons for each crew member,
    /// and coordinates drag/drop plus tooltip forwarding.
    /// </summary>
    [AddComponentMenu("Teramyyd/UI/Crew HUD Controller")]
    [Serializable]
    public struct CrewPortraitMapping
    {
        public string crewId;
        public Sprite portrait;
    }

    public class CrewHUDController : MonoBehaviour
    {
        [Header("References")]
        public RectTransform unassignedContainer;
        public CrewHUDUnassignedZone unassignedDropZone;
        public CrewHUDCrewIcon iconPrefab;
        public Canvas dragCanvas;
        public CrewHUDTooltip tooltip;

        [Header("Discovery")]
        [Tooltip("When enabled, the controller scans its children every refresh for CrewHUDStationSlot components.")]
        public bool autoDiscoverStationSlots = true;
        public CrewHUDStationSlot[] shipSlots;

        [Header("Appearance")]
        public Vector2 unassignedIconScale = Vector2.one;
        public Vector2 assignedIconScale = new Vector2(0.65f, 0.65f);
        public Color pendingColor = new Color(1f, 0.9f, 0.4f, 1f);

        [Header("Portrait Overrides")]
        [Tooltip("Optional per-crew portraits. If no entry exists, the prefab's default sprite is used.")]
        public CrewPortraitMapping[] portraitOverrides;

        [Header("Refresh")] public float refreshInterval = 0.4f;

        public event Action<CrewMember, CrewStation, Transform> OnVisualAnchorChanged;

        readonly Dictionary<string, CrewHUDCrewIcon> _iconsByCrewId = new Dictionary<string, CrewHUDCrewIcon>();
        readonly Dictionary<string, Sprite> _portraitLookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> _scratchIds = new HashSet<string>();
        float _nextRefreshTime;

        void Awake()
        {
            if (dragCanvas == null)
            {
                dragCanvas = GetComponentInParent<Canvas>();
            }

            if (unassignedDropZone != null)
            {
                unassignedDropZone.Initialize(this);
            }

            RebuildPortraitLookup();
        }

        void OnValidate()
        {
            RebuildPortraitLookup();
        }

        void OnEnable()
        {
            ForceRefresh();
        }

        void Update()
        {
            if (Time.unscaledTime >= _nextRefreshTime)
            {
                RefreshAssignments();
            }
        }

        public Canvas GetDragCanvas() => dragCanvas;

        public void ForceRefresh()
        {
            _nextRefreshTime = 0f;
            RefreshAssignments();
        }

        public void HandleStationDrop(CrewHUDStationSlot slot, CrewHUDCrewIcon icon)
        {
            if (slot == null || icon == null)
                return;

            CrewStation station = slot.Station;
            if (station == null)
            {
                icon.SnapBackToLastParent();
                return;
            }

            var manager = CrewManager.HasInstance ? CrewManager.Instance : null;
            if (manager == null)
            {
                icon.SnapBackToLastParent();
                return;
            }

            if (!manager.TryAssignCrewToStation(icon.Crew, station))
            {
                icon.SnapBackToLastParent();
                return;
            }

            AttachIconToSlot(icon, slot);
            icon.MarkDropAccepted();
        }

        public void HandleReturnToPool(CrewHUDCrewIcon icon)
        {
            if (icon == null)
                return;

            var manager = CrewManager.HasInstance ? CrewManager.Instance : null;
            if (manager != null)
            {
                manager.UnassignCrew(icon.Crew);
            }

            AttachIconToPool(icon);
            icon.MarkDropAccepted();
        }

        public void RegisterStationSlot(CrewHUDStationSlot slot)
        {
            if (slot == null)
                return;

            slot.Initialize(this);
            ForceRefresh();
        }

        internal void ShowTooltip(CrewHUDCrewIcon icon, Vector2 screenPos)
        {
            if (tooltip == null || icon == null)
                return;

            CrewStation station = icon.CurrentSlot != null ? icon.CurrentSlot.Station : null;
            tooltip.Show(icon.Crew, station, screenPos);
        }

        internal void HideTooltip(CrewHUDCrewIcon icon)
        {
            if (tooltip == null)
                return;

            tooltip.Hide(icon);
        }

        void RefreshAssignments()
        {
            _nextRefreshTime = Time.unscaledTime + Mathf.Max(0.1f, refreshInterval);

            var manager = CrewManager.HasInstance ? CrewManager.Instance : null;
            if (manager == null || iconPrefab == null || unassignedContainer == null)
                return;

            if (autoDiscoverStationSlots)
            {
                shipSlots = GetComponentsInChildren<CrewHUDStationSlot>(includeInactive: true);
                foreach (var slot in shipSlots)
                {
                    slot.Initialize(this);
                }
            }

            _scratchIds.Clear();
            foreach (var crew in manager.RegisteredCrew)
            {
                if (crew == null || string.IsNullOrEmpty(crew.crewId))
                    continue;

                _scratchIds.Add(crew.crewId);
                CrewHUDCrewIcon icon = GetOrCreateIcon(crew);
                ApplyPortrait(icon, crew);
                ApplyCrewAssignment(icon, crew);
            }

            RemoveMissingIcons();
        }

        void ApplyCrewAssignment(CrewHUDCrewIcon icon, CrewMember crew)
        {
            if (icon == null)
                return;

            CrewStation assigned = crew.AssignedStation;
            if (assigned != null)
            {
                CrewHUDStationSlot slot = FindSlotForStation(assigned.stationId);
                if (slot != null)
                {
                    icon.ClearPendingState();
                    AttachIconToSlot(icon, slot);
                    return;
                }
            }

            if (!string.IsNullOrEmpty(crew.PendingStationId))
            {
                CrewHUDStationSlot pendingSlot = FindSlotForStation(crew.PendingStationId);
                if (pendingSlot != null)
                {
                    icon.ClearPendingState();
                    AttachIconToSlot(icon, pendingSlot);
                    return;
                }

                icon.SetPendingState($"Waiting for {crew.PendingStationId}", pendingColor);
            }
            else
            {
                icon.ClearPendingState();
            }

            AttachIconToPool(icon);
        }

        CrewHUDStationSlot FindSlotForStation(string stationId)
        {
            if (string.IsNullOrEmpty(stationId) || shipSlots == null)
                return null;

            foreach (var slot in shipSlots)
            {
                if (slot == null)
                    continue;

                string id = slot.StationId;
                if (!string.IsNullOrEmpty(id) && id == stationId)
                    return slot;
            }

            return null;
        }

        CrewHUDCrewIcon GetOrCreateIcon(CrewMember crew)
        {
            if (_iconsByCrewId.TryGetValue(crew.crewId, out var existing) && existing != null)
                return existing;

            CrewHUDCrewIcon icon = Instantiate(iconPrefab, unassignedContainer);
            icon.Initialize(this, crew, unassignedIconScale);
            _iconsByCrewId[crew.crewId] = icon;
            return icon;
        }

        void ApplyPortrait(CrewHUDCrewIcon icon, CrewMember crew)
        {
            if (icon == null || crew == null)
                return;

            if (TryResolvePortrait(crew.crewId, out var sprite))
            {
                icon.SetPortraitSprite(sprite);
            }
        }

        void AttachIconToSlot(CrewHUDCrewIcon icon, CrewHUDStationSlot slot)
        {
            if (icon == null || slot == null)
                return;

            RectTransform parent = slot.iconAnchor != null ? slot.iconAnchor : slot.transform as RectTransform;
            icon.SetAssignedSlot(slot);
            icon.AttachToParent(parent, assignedIconScale);
            OnVisualAnchorChanged?.Invoke(icon.Crew, slot.Station, slot.worldAnchor);
        }

        void AttachIconToPool(CrewHUDCrewIcon icon)
        {
            if (icon == null)
                return;

            icon.SetAssignedSlot(null);
            icon.AttachToParent(unassignedContainer, unassignedIconScale);
            OnVisualAnchorChanged?.Invoke(icon.Crew, null, null);
        }

        void RemoveMissingIcons()
        {
            var keys = new List<string>(_iconsByCrewId.Keys);
            foreach (var id in keys)
            {
                if (_scratchIds.Contains(id))
                    continue;

                if (_iconsByCrewId[id] != null)
                {
                    Destroy(_iconsByCrewId[id].gameObject);
                }
                _iconsByCrewId.Remove(id);
            }

            _scratchIds.Clear();
        }

        void RebuildPortraitLookup()
        {
            _portraitLookup.Clear();
            if (portraitOverrides == null)
                return;

            foreach (var entry in portraitOverrides)
            {
                if (string.IsNullOrWhiteSpace(entry.crewId) || entry.portrait == null)
                    continue;

                _portraitLookup[entry.crewId] = entry.portrait;
            }
        }

        bool TryResolvePortrait(string crewId, out Sprite sprite)
        {
            if (string.IsNullOrEmpty(crewId))
            {
                sprite = null;
                return false;
            }

            return _portraitLookup.TryGetValue(crewId, out sprite);
        }
    }
}
