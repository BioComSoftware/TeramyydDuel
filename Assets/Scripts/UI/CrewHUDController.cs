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
        public CrewHUDCrewIcon iconPrefab;
        public Canvas dragCanvas;
        public CrewHUDTooltip tooltip;
        [Header("Unassigned Crew Slots")]
        [Tooltip("Ordered list of anchors used to display unassigned crew across the HUD.")]
        public RectTransform[] unassignedCrewSlotAnchors;
        [Tooltip("Optional RectTransform used for all crew tooltips when an icon does not specify its own anchor.")]
        public RectTransform sharedTooltipAnchor;

        [Header("Discovery")]
        [Tooltip("When enabled, the controller scans its children every refresh for CrewHUDStationSlot components.")]
        public bool autoDiscoverStationSlots = true;
        public CrewHUDStationSlot[] shipSlots;

        [Header("Appearance")]
        public Vector2 unassignedIconScale = Vector2.one;
        public Vector2 assignedIconScale = new Vector2(0.65f, 0.65f);
        [Tooltip("Scale applied when icons occupy the top-level crew slot anchors.")]
        public Vector2 crewSlotIconScale = new Vector2(0.5f, 0.5f);
        public Color pendingColor = new Color(1f, 0.9f, 0.4f, 1f);

        [Header("Portrait Overrides")]
        [Tooltip("Optional per-crew portraits. If no entry exists, the prefab's default sprite is used.")]
        public CrewPortraitMapping[] portraitOverrides;

        [Header("Refresh")] public float refreshInterval = 0.4f;

        public event Action<CrewMember, CrewStation, Transform> OnVisualAnchorChanged;

        readonly Dictionary<string, CrewHUDCrewIcon> _iconsByCrewId = new Dictionary<string, CrewHUDCrewIcon>();
        readonly Dictionary<string, Sprite> _portraitLookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> _scratchIds = new HashSet<string>();
        readonly Dictionary<CrewHUDCrewIcon, RectTransform> _crewIconToAnchor = new Dictionary<CrewHUDCrewIcon, RectTransform>();
        readonly Dictionary<RectTransform, CrewHUDCrewIcon> _anchorToCrewIcon = new Dictionary<RectTransform, CrewHUDCrewIcon>();
        float _nextRefreshTime;
        float _suppressRefreshUntil;

        void Awake()
        {
            string msg = "[CrewHUD] Awake called";
            Debug.Log(msg);
            FileLogger.Log(msg, "CrewHUD");
            
            if (dragCanvas == null)
            {
                dragCanvas = GetComponentInParent<Canvas>();
            }

            RebuildPortraitLookup();
        }

        void OnValidate()
        {
            RebuildPortraitLookup();
        }

        void OnEnable()
        {
            string msg = "[CrewHUD] OnEnable called, forcing refresh";
            Debug.Log(msg);
            FileLogger.Log(msg, "CrewHUD");
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

        public bool HandleStationDrop(CrewHUDStationSlot slot, CrewHUDCrewIcon icon)
        {
            string crewName = icon?.Crew?.displayName ?? "Unknown";
            string slotId = slot?.StationId ?? "null";
            
            string msg1 = $"[CrewHUD] HandleStationDrop: {crewName} -> {slotId}";
            Debug.Log(msg1);
            FileLogger.Log(msg1, "CrewHUD");
            
            if (slot == null || icon == null)
            {
                string msg2 = "[CrewHUD] HandleStationDrop: slot or icon is null, returning false";
                Debug.LogWarning(msg2);
                FileLogger.Log(msg2, "CrewHUD");
                return false;
            }

            string stationId = slot.StationId;
            if (string.IsNullOrEmpty(stationId))
            {
                string msg3 = "[CrewHUD] HandleStationDrop: stationId is empty, returning false";
                Debug.LogWarning(msg3);
                FileLogger.Log(msg3, "CrewHUD");
                return false;
            }

            var manager = CrewManager.HasInstance ? CrewManager.Instance : null;
            if (manager == null)
            {
                string msg4 = "[CrewHUD] HandleStationDrop: CrewManager not available, returning false";
                Debug.LogWarning(msg4);
                FileLogger.Log(msg4, "CrewHUD");
                return false;
            }

            string beforeAssignment = icon.Crew?.AssignedStation?.stationId ?? "null";
            string msg5 = $"[CrewHUD] HandleStationDrop: Before assignment, {crewName}.AssignedStation = {beforeAssignment}";
            Debug.Log(msg5);
            FileLogger.Log(msg5, "CrewHUD");

            // Pre-emptively attach icon to prevent race with refresh
            AttachIconToSlot(icon, slot);

            bool assignSuccess = manager.TryAssignCrewToStationId(icon.Crew, stationId);
            string afterAssignment = icon.Crew?.AssignedStation?.stationId ?? "null";
            string msg6 = $"[CrewHUD] HandleStationDrop: Assignment {(assignSuccess ? "SUCCESS" : "FAILED")}, {crewName}.AssignedStation now = {afterAssignment}";
            Debug.Log(msg6);
            FileLogger.Log(msg6, "CrewHUD");
            
            if (!assignSuccess)
            {
                // Assignment failed, return to pool
                string msg7 = $"[CrewHUD] HandleStationDrop: Assignment failed for {crewName}, returning to pool";
                Debug.LogWarning(msg7);
                FileLogger.Log(msg7, "CrewHUD");
                AttachIconToPool(icon);
                return false;
            }

            CrewStation station = slot.Station;
            if (station == null)
            {
                manager.TryGetStation(stationId, out station);
            }

            // Re-attach to ensure correct anchor and fire events
            AttachIconToSlot(icon, slot);
            
            // Suppress refresh briefly to prevent double-processing
            _suppressRefreshUntil = Time.unscaledTime + 0.5f;
            
            string msg8 = $"[CrewHUD] HandleStationDrop: Complete for {crewName} at {stationId}";
            Debug.Log(msg8);
            FileLogger.Log(msg8, "CrewHUD");
            
            return true;
        }

        public bool HandleReturnToPool(CrewHUDCrewIcon icon)
        {
            if (icon == null)
                return false;

            string crewName = icon.Crew != null ? icon.Crew.displayName : "Unknown";
            string beforeState = icon.Crew?.AssignedStation?.stationId ?? "null";
            string msg = $"[CrewHUD] HandleReturnToPool: {crewName}, AssignedStation before unassign: {beforeState}";
            Debug.Log(msg);
            FileLogger.Log(msg, "CrewHUD");

            // Immediately move icon to pool visually to prevent race with refresh
            AttachIconToPool(icon);

            var manager = CrewManager.HasInstance ? CrewManager.Instance : null;
            if (manager != null)
            {
                manager.UnassignCrew(icon.Crew);
            }

            string afterState = icon.Crew?.AssignedStation?.stationId ?? "null";
            string msg2 = $"[CrewHUD] HandleReturnToPool: {crewName}, AssignedStation after unassign: {afterState}";
            Debug.Log(msg2);
            FileLogger.Log(msg2, "CrewHUD");

            // Suppress refresh for longer to ensure it skips the next refresh cycle
            _suppressRefreshUntil = Time.unscaledTime + 0.5f;

            return true;
        }

        public void RegisterStationSlot(CrewHUDStationSlot slot)
        {
            if (slot == null)
                return;

            slot.Initialize(this);
            ForceRefresh();
        }

        internal void ShowTooltip(CrewHUDCrewIcon icon)
        {
            string crewName = icon?.Crew?.displayName ?? "Unknown";
            
            if (tooltip == null)
            {
                string msg = $"[CrewHUD] ShowTooltip: tooltip is null for {crewName}";
                Debug.LogWarning(msg);
                FileLogger.Log(msg, "CrewHUD");
                return;
            }
            
            if (icon == null)
            {
                string msg = "[CrewHUD] ShowTooltip: icon is null";
                Debug.LogWarning(msg);
                FileLogger.Log(msg, "CrewHUD");
                return;
            }

            CrewStation station = icon.CurrentSlot != null ? icon.CurrentSlot.Station : null;
            Sprite portrait = icon.portraitImage != null ? icon.portraitImage.sprite : null;
            RectTransform anchor = icon.tooltipAnchor != null ? icon.tooltipAnchor : sharedTooltipAnchor;
            
            string msg1 = $"[CrewHUD] ShowTooltip: {crewName}, anchor={anchor != null}, station={station != null}, portrait={portrait != null}";
            Debug.Log(msg1);
            FileLogger.Log(msg1, "CrewHUD");
            
            if (anchor == null)
            {
                string msg2 = $"[CrewHUD] ShowTooltip: Missing tooltip anchor for {icon.Crew?.crewId ?? "unknown crew"}. icon.tooltipAnchor={icon.tooltipAnchor != null}, sharedTooltipAnchor={sharedTooltipAnchor != null}";
                Debug.LogWarning(msg2, icon);
                FileLogger.Log(msg2, "CrewHUD");
                tooltip.Hide(icon);
                return;
            }

            tooltip.Show(icon.Crew, station, anchor, portrait);
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

            string startMsg = $"[CrewHUD] RefreshAssignments called at {Time.unscaledTime:F2}";
            Debug.Log(startMsg);
            FileLogger.Log(startMsg, "CrewHUD");

            // Skip refresh if suppressed (during manual drag/drop operations)
            if (Time.unscaledTime < _suppressRefreshUntil)
            {
                string msg = $"[CrewHUD] RefreshAssignments suppressed until {_suppressRefreshUntil:F2} (current: {Time.unscaledTime:F2})";
                Debug.Log(msg);
                FileLogger.Log(msg, "CrewHUD");
                return;
            }

            var manager = CrewManager.HasInstance ? CrewManager.Instance : null;
            if (manager == null)
            {
                string msg = "[CrewHUD] RefreshAssignments: CrewManager not available";
                Debug.LogWarning(msg);
                FileLogger.Log(msg, "CrewHUD");
                return;
            }
            
            if (iconPrefab == null)
            {
                string msg = "[CrewHUD] RefreshAssignments: iconPrefab is null";
                Debug.LogWarning(msg);
                FileLogger.Log(msg, "CrewHUD");
                return;
            }

            if (autoDiscoverStationSlots)
            {
                shipSlots = GetComponentsInChildren<CrewHUDStationSlot>(includeInactive: true);
                foreach (var slot in shipSlots)
                {
                    slot.Initialize(this);
                }
            }

            _scratchIds.Clear();
            int crewCount = 0;
            foreach (var crew in manager.RegisteredCrew)
            {
                crewCount++;
                if (crew == null || string.IsNullOrEmpty(crew.crewId))
                {
                    string msg = $"[CrewHUD] Skipping null or invalid crew (index {crewCount})";
                    Debug.LogWarning(msg);
                    FileLogger.Log(msg, "CrewHUD");
                    continue;
                }

                _scratchIds.Add(crew.crewId);
                CrewHUDCrewIcon icon = GetOrCreateIcon(crew);
                
                string iconMsg = $"[CrewHUD] Processing crew: {crew.displayName} ({crew.crewId}), icon exists: {icon != null}";
                Debug.Log(iconMsg);
                FileLogger.Log(iconMsg, "CrewHUD");
                
                // Skip refresh for icons being actively dragged
                if (icon.IsDragging)
                {
                    continue;
                }

                CrewStation assignedStation = crew.AssignedStation;
                if (assignedStation != null)
                {
                    CrewHUDStationSlot slot = FindSlotForStation(assignedStation.stationId);
                    if (slot != null)
                    {
                        // Only reattach if icon isn't already at this slot
                        if (icon.CurrentSlot != slot)
                        {
                            string msg2 = $"[CrewHUD] Re-attaching {crew.displayName} to slot {assignedStation.stationId}";
                            Debug.Log(msg2);
                            FileLogger.Log(msg2, "CrewHUD");
                            icon.ClearPendingState();
                            AttachIconToSlot(icon, slot);
                        }
                        continue;
                    }
                }

                if (!string.IsNullOrEmpty(crew.PendingStationId))
                {
                    CrewHUDStationSlot pendingSlot = FindSlotForStation(crew.PendingStationId);
                    if (pendingSlot != null)
                    {
                        if (icon.CurrentSlot != pendingSlot)
                        {
                            icon.ClearPendingState();
                            AttachIconToSlot(icon, pendingSlot);
                        }
                        return;
                    }

                    icon.SetPendingState($"Waiting for {crew.PendingStationId}", pendingColor);
                    // Still attach to pool while waiting for station
                    AttachIconToPool(icon);
                }
                else
                {
                    icon.ClearPendingState();
                    // No assignment and no pending - ensure icon is in pool
                    // But only move it if it's not already there
                    if (icon.CurrentSlot != null)
                    {
                        string poolMsg = $"[CrewHUD] Moving {crew.displayName} to pool (was at {icon.CurrentSlot.StationId})";
                        Debug.Log(poolMsg);
                        FileLogger.Log(poolMsg, "CrewHUD");
                        AttachIconToPool(icon);
                    }
                    else
                    {
                        string alreadyMsg = $"[CrewHUD] {crew.displayName} already in pool (CurrentSlot is null)";
                        Debug.Log(alreadyMsg);
                        FileLogger.Log(alreadyMsg, "CrewHUD");
                        // Icon thinks it's in pool, but make sure it's actually positioned and visible
                        // This handles the case where icon was just created
                        RectTransform poolAnchor = RequestCrewSlotAnchor(icon);
                        if (poolAnchor != null)
                        {
                            icon.AttachToParent(poolAnchor, crewSlotIconScale);
                            if (!icon.gameObject.activeSelf)
                            {
                                string enableMsg = $"[CrewHUD] Enabling newly created icon for {crew.displayName}";
                                Debug.Log(enableMsg);
                                FileLogger.Log(enableMsg, "CrewHUD");
                                icon.gameObject.SetActive(true);
                            }
                        }
                    }
                }
            }

            string summaryMsg = $"[CrewHUD] RefreshAssignments complete: processed {crewCount} crew members";
            Debug.Log(summaryMsg);
            FileLogger.Log(summaryMsg, "CrewHUD");

            RemoveMissingIcons();
        }

        void ApplyCrewAssignment(CrewHUDCrewIcon icon, CrewMember crew)
        {
            if (icon == null)
                return;

            CrewStation assigned = crew.AssignedStation;
            string crewName = crew != null ? crew.displayName : "Unknown";
            string assignedId = assigned != null ? assigned.stationId : "null";
            string currentSlotId = icon.CurrentSlot != null ? icon.CurrentSlot.StationId : "pool";
            
            string msg = $"[CrewHUD] ApplyCrewAssignment: {crewName}, AssignedStation={assignedId}, CurrentSlot={currentSlotId}, IsDragging={icon.IsDragging}";
            Debug.Log(msg);
            FileLogger.Log(msg, "CrewHUD");
            
            if (assigned != null)
            {
                CrewHUDStationSlot slot = FindSlotForStation(assigned.stationId);
                if (slot != null)
                {
                    // Only reattach if icon isn't already at this slot
                    if (icon.CurrentSlot != slot)
                    {
                        string msg2 = $"[CrewHUD] Re-attaching {crewName} to slot {assigned.stationId}";
                        Debug.Log(msg2);
                        FileLogger.Log(msg2, "CrewHUD");
                        icon.ClearPendingState();
                        AttachIconToSlot(icon, slot);
                    }
                    return;
                }
            }

            if (!string.IsNullOrEmpty(crew.PendingStationId))
            {
                CrewHUDStationSlot pendingSlot = FindSlotForStation(crew.PendingStationId);
                if (pendingSlot != null)
                {
                    if (icon.CurrentSlot != pendingSlot)
                    {
                        icon.ClearPendingState();
                        AttachIconToSlot(icon, pendingSlot);
                    }
                    return;
                }

                icon.SetPendingState($"Waiting for {crew.PendingStationId}", pendingColor);
                // Still attach to pool while waiting for station
                AttachIconToPool(icon);
            }
            else
            {
                icon.ClearPendingState();
                // No assignment and no pending - ensure icon is in pool
                // But only move it if it's not already there
                if (icon.CurrentSlot != null)
                {
                    string poolMsg = $"[CrewHUD] Moving {crew.displayName} to pool (was at {icon.CurrentSlot.StationId})";
                    Debug.Log(poolMsg);
                    FileLogger.Log(poolMsg, "CrewHUD");
                    AttachIconToPool(icon);
                }
                else
                {
                    string alreadyMsg = $"[CrewHUD] {crew.displayName} already in pool (CurrentSlot is null)";
                    Debug.Log(alreadyMsg);
                    FileLogger.Log(alreadyMsg, "CrewHUD");
                    // Icon thinks it's in pool, but make sure it's actually positioned and visible
                    // This handles the case where icon was just created
                    RectTransform poolAnchor = RequestCrewSlotAnchor(icon);
                    if (poolAnchor != null)
                    {
                        icon.AttachToParent(poolAnchor, crewSlotIconScale);
                        if (!icon.gameObject.activeSelf)
                        {
                            string enableMsg = $"[CrewHUD] Enabling newly created icon for {crew.displayName}";
                            Debug.Log(enableMsg);
                            FileLogger.Log(enableMsg, "CrewHUD");
                            icon.gameObject.SetActive(true);
                        }
                    }
                }
            }
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

            string msg = $"[CrewHUD] Creating new icon for {crew.displayName}";
            Debug.Log(msg);
            FileLogger.Log(msg, "CrewHUD");

            CrewHUDCrewIcon icon = Instantiate(iconPrefab, transform);
            icon.Initialize(this, crew);
            _iconsByCrewId[crew.crewId] = icon;
            
            string msg2 = $"[CrewHUD] New icon created for {crew.displayName}, will be positioned by ApplyCrewAssignment";
            Debug.Log(msg2);
            FileLogger.Log(msg2, "CrewHUD");
            
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

            ReleaseCrewSlotAnchor(icon);
            if (icon.CurrentSlot != null && icon.CurrentSlot != slot)
            {
                icon.CurrentSlot.ReleaseIcon(icon);
            }

            RectTransform parent = slot.RequestAnchorFor(icon);
            if (parent == null)
            {
                parent = slot.iconAnchor != null ? slot.iconAnchor : slot.transform as RectTransform;
            }
            icon.SetAssignedSlot(slot);
            icon.AttachToParent(parent, assignedIconScale);
            
            string crewName = icon.Crew != null ? icon.Crew.displayName : "Unknown";
            if (!icon.gameObject.activeSelf)
            {
                string msg = $"[CrewHUD] Enabling icon for {crewName} at slot {slot.StationId}";
                Debug.Log(msg);
                FileLogger.Log(msg, "CrewHUD");
                icon.gameObject.SetActive(true);
            }
            
            Transform slotWorldAnchor = slot.RequestWorldAnchorFor(icon);
            OnVisualAnchorChanged?.Invoke(icon.Crew, slot.Station, slotWorldAnchor);
        }

        void AttachIconToPool(CrewHUDCrewIcon icon)
        {
            if (icon == null)
                return;

            string crewName = icon.Crew != null ? icon.Crew.displayName : "Unknown";
            string msg1 = $"[CrewHUD] AttachIconToPool: {crewName}, starting attachment";
            Debug.Log(msg1);
            FileLogger.Log(msg1, "CrewHUD");

            if (icon.CurrentSlot != null)
            {
                icon.CurrentSlot.ReleaseIcon(icon);
            }

            icon.SetAssignedSlot(null);
            RectTransform parent = RequestCrewSlotAnchor(icon);
            if (parent == null)
            {
                string message = $"[CrewHUD] ERROR: No available unassigned crew slot anchors for {crewName}. Total anchors: {unassignedCrewSlotAnchors?.Length ?? 0}";
                Debug.LogError(message, icon);
                FileLogger.Log(message, "CrewHUD");
                throw new InvalidOperationException(message);
            }

            Vector2 scale = crewSlotIconScale;
            string msg2 = $"[CrewHUD] AttachIconToPool: {crewName} attached to anchor {parent.name}, scale={scale}";
            Debug.Log(msg2);
            FileLogger.Log(msg2, "CrewHUD");
            
            icon.AttachToParent(parent, scale);
            
            if (!icon.gameObject.activeSelf)
            {
                string msg3 = $"[CrewHUD] Enabling icon for {crewName}";
                Debug.Log(msg3);
                FileLogger.Log(msg3, "CrewHUD");
                icon.gameObject.SetActive(true);
            }
            
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
                    _iconsByCrewId[id].CurrentSlot?.ReleaseIcon(_iconsByCrewId[id]);
                    ReleaseCrewSlotAnchor(_iconsByCrewId[id]);
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

        RectTransform RequestCrewSlotAnchor(CrewHUDCrewIcon icon)
        {
            if (icon == null)
                return null;

            if (_crewIconToAnchor.TryGetValue(icon, out var existing) && existing != null)
                return existing;

            if (unassignedCrewSlotAnchors == null)
                return null;

            for (int i = 0; i < unassignedCrewSlotAnchors.Length; i++)
            {
                var anchor = unassignedCrewSlotAnchors[i];
                if (anchor == null)
                    continue;

                if (!_anchorToCrewIcon.TryGetValue(anchor, out var occupant) || occupant == null)
                {
                    _anchorToCrewIcon[anchor] = icon;
                    _crewIconToAnchor[icon] = anchor;
                    return anchor;
                }
            }

            return null;
        }

        void ReleaseCrewSlotAnchor(CrewHUDCrewIcon icon)
        {
            if (icon == null)
                return;

            if (_crewIconToAnchor.TryGetValue(icon, out var anchor))
            {
                _crewIconToAnchor.Remove(icon);
                if (anchor != null && _anchorToCrewIcon.TryGetValue(anchor, out var occupant) && occupant == icon)
                {
                    _anchorToCrewIcon.Remove(anchor);
                }
            }
        }
    }
}
