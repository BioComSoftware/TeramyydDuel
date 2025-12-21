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

        [Header("Refresh")] public float refreshInterval = 0.4f;

        [Header("Debug")]
        [Tooltip("Enables debug logging to console and Logs/game_debug.log.")]
        public bool debugLog = false;

        public event Action<CrewMember, CrewStation, Transform> OnVisualAnchorChanged;

        readonly Dictionary<string, CrewHUDCrewIcon> _iconsByCrewId = new Dictionary<string, CrewHUDCrewIcon>();
        readonly HashSet<string> _scratchIds = new HashSet<string>();
        readonly HashSet<CrewHUDCrewIcon> _processedIcons = new HashSet<CrewHUDCrewIcon>();
        readonly Dictionary<CrewHUDCrewIcon, RectTransform> _crewIconToAnchor = new Dictionary<CrewHUDCrewIcon, RectTransform>();
        readonly Dictionary<RectTransform, CrewHUDCrewIcon> _anchorToCrewIcon = new Dictionary<RectTransform, CrewHUDCrewIcon>();
        float _nextRefreshTime;
        float _suppressRefreshUntil;

        void Awake()
        {
            if (debugLog)
            {
                Debug.Log("[CrewHUD] Awake called");
                FileLogger.Log("Awake called", "CrewHUD");
            }
            
            if (dragCanvas == null)
            {
                dragCanvas = GetComponentInParent<Canvas>();
            }
        }

        void OnValidate()
        {
        }

        void OnEnable()
        {
            if (debugLog)
            {
                Debug.Log("[CrewHUD] OnEnable called, forcing refresh");
                FileLogger.Log("OnEnable called, forcing refresh", "CrewHUD");
            }
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
            if (debugLog)
            {
                string crewName = icon?.Crew?.displayName ?? "Unknown";
                string slotId = slot?.StationId ?? "null";
                Debug.Log($"[CrewHUD] HandleStationDrop: {crewName} -> {slotId}");
                FileLogger.Log($"HandleStationDrop: {crewName} -> {slotId}", "CrewHUD");
            }
            
            if (slot == null || icon == null)
            {
                if (debugLog)
                {
                    Debug.LogWarning("[CrewHUD] HandleStationDrop: slot or icon is null, returning false");
                    FileLogger.Log("HandleStationDrop: slot or icon is null, returning false", "CrewHUD");
                }
                return false;
            }

            string stationId = slot.StationId;
            if (string.IsNullOrEmpty(stationId))
            {
                if (debugLog)
                {
                    Debug.LogWarning("[CrewHUD] HandleStationDrop: stationId is empty, returning false");
                    FileLogger.Log("HandleStationDrop: stationId is empty, returning false", "CrewHUD");
                }
                return false;
            }

            var manager = CrewManager.HasInstance ? CrewManager.Instance : null;
            if (manager == null)
            {
                if (debugLog)
                {
                    Debug.LogWarning("[CrewHUD] HandleStationDrop: CrewManager not available, returning false");
                    FileLogger.Log("HandleStationDrop: CrewManager not available, returning false", "CrewHUD");
                }
                return false;
            }

            if (debugLog)
            {
                string crewName = icon?.Crew?.displayName ?? "Unknown";
                string beforeAssignment = icon.Crew?.AssignedStation?.stationId ?? "null";
                Debug.Log($"[CrewHUD] HandleStationDrop: Before assignment, {crewName}.AssignedStation = {beforeAssignment}");
                FileLogger.Log($"HandleStationDrop: Before assignment, {crewName}.AssignedStation = {beforeAssignment}", "CrewHUD");
            }

            // Pre-emptively attach icon to prevent race with refresh
            AttachIconToSlot(icon, slot);

            bool assignSuccess = manager.TryAssignCrewToStationId(icon.Crew, stationId);
            
            if (debugLog)
            {
                string crewName = icon?.Crew?.displayName ?? "Unknown";
                string afterAssignment = icon.Crew?.AssignedStation?.stationId ?? "null";
                Debug.Log($"[CrewHUD] HandleStationDrop: Assignment {(assignSuccess ? "SUCCESS" : "FAILED")}, {crewName}.AssignedStation now = {afterAssignment}");
                FileLogger.Log($"HandleStationDrop: Assignment {(assignSuccess ? "SUCCESS" : "FAILED")}, {crewName}.AssignedStation now = {afterAssignment}", "CrewHUD");
            }
            
            if (!assignSuccess)
            {
                // Assignment failed, return to pool
                if (debugLog)
                {
                    string crewName = icon?.Crew?.displayName ?? "Unknown";
                    Debug.LogWarning($"[CrewHUD] HandleStationDrop: Assignment failed for {crewName}, returning to pool");
                    FileLogger.Log($"HandleStationDrop: Assignment failed for {crewName}, returning to pool", "CrewHUD");
                }
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
            
            // Don't suppress refresh - let the normal flow handle it with the processed icons set
            // _suppressRefreshUntil = Time.unscaledTime + 0.5f;
            
            if (debugLog)
            {
                string crewName = icon?.Crew?.displayName ?? "Unknown";
                Debug.Log($"[CrewHUD] HandleStationDrop: Complete for {crewName} at {stationId}");
                FileLogger.Log($"HandleStationDrop: Complete for {crewName} at {stationId}", "CrewHUD");
            }
            
            return true;
        }

        public bool HandleReturnToPool(CrewHUDCrewIcon icon)
        {
            if (icon == null)
                return false;

            if (debugLog)
            {
                string crewName = icon.Crew != null ? icon.Crew.displayName : "Unknown";
                string beforeState = icon.Crew?.AssignedStation?.stationId ?? "null";
                Debug.Log($"[CrewHUD] HandleReturnToPool: {crewName}, AssignedStation before unassign: {beforeState}");
                FileLogger.Log($"HandleReturnToPool: {crewName}, AssignedStation before unassign: {beforeState}", "CrewHUD");
            }

            // Immediately move icon to pool visually to prevent race with refresh
            AttachIconToPool(icon);

            var manager = CrewManager.HasInstance ? CrewManager.Instance : null;
            if (manager != null)
            {
                manager.UnassignCrew(icon.Crew);
            }

            if (debugLog)
            {
                string crewName = icon.Crew != null ? icon.Crew.displayName : "Unknown";
                string afterState = icon.Crew?.AssignedStation?.stationId ?? "null";
                Debug.Log($"[CrewHUD] HandleReturnToPool: {crewName}, AssignedStation after unassign: {afterState}");
                FileLogger.Log($"HandleReturnToPool: {crewName}, AssignedStation after unassign: {afterState}", "CrewHUD");
            }

            // Don't suppress refresh - let the normal flow handle it
            // _suppressRefreshUntil = Time.unscaledTime + 0.5f;

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
            if (tooltip == null)
            {
                if (debugLog)
                {
                    string crewName = icon?.Crew?.displayName ?? "Unknown";
                    Debug.LogWarning($"[CrewHUD] ShowTooltip: tooltip is null for {crewName}");
                    FileLogger.Log($"ShowTooltip: tooltip is null for {crewName}", "CrewHUD");
                }
                return;
            }
            
            if (icon == null)
            {
                if (debugLog)
                {
                    Debug.LogWarning("[CrewHUD] ShowTooltip: icon is null");
                    FileLogger.Log("ShowTooltip: icon is null", "CrewHUD");
                }
                return;
            }

            CrewStation station = icon.CurrentSlot != null ? icon.CurrentSlot.Station : null;
            Sprite portrait = icon.portraitImage != null ? icon.portraitImage.sprite : null;
            RectTransform anchor = icon.tooltipAnchor != null ? icon.tooltipAnchor : sharedTooltipAnchor;
            
            if (debugLog)
            {
                string crewName = icon?.Crew?.displayName ?? "Unknown";
                Debug.Log($"[CrewHUD] ShowTooltip: {crewName}, anchor={anchor != null}, station={station != null}, portrait={portrait != null}");
                FileLogger.Log($"ShowTooltip: {crewName}, anchor={anchor != null}, station={station != null}, portrait={portrait != null}", "CrewHUD");
            }
            
            if (anchor == null)
            {
                if (debugLog)
                {
                    Debug.LogWarning($"[CrewHUD] ShowTooltip: Missing tooltip anchor for {icon.Crew?.crewId ?? "unknown crew"}. icon.tooltipAnchor={icon.tooltipAnchor != null}, sharedTooltipAnchor={sharedTooltipAnchor != null}", icon);
                    FileLogger.Log($"ShowTooltip: Missing tooltip anchor for {icon.Crew?.crewId ?? "unknown crew"}. icon.tooltipAnchor={icon.tooltipAnchor != null}, sharedTooltipAnchor={sharedTooltipAnchor != null}", "CrewHUD");
                }
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

            // Skip refresh if suppressed (during manual drag/drop operations)
            if (Time.unscaledTime < _suppressRefreshUntil)
            {
                return;
            }

            var manager = CrewManager.HasInstance ? CrewManager.Instance : null;
            if (manager == null)
            {
                return;
            }
            
            if (iconPrefab == null)
            {
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
            _processedIcons.Clear(); // *** FIX: Clear the set at the start of the refresh

            // First pass: Handle all assigned crew and mark them as processed
            foreach (var crew in manager.RegisteredCrew)
            {
                if (crew == null || string.IsNullOrEmpty(crew.crewId)) continue;

                _scratchIds.Add(crew.crewId);
                CrewHUDCrewIcon icon = GetOrCreateIcon(crew);

                if (icon.IsDragging)
                {
                    _processedIcons.Add(icon); // Mark dragged icons as processed so they aren't touched
                    continue;
                }

                CrewStation assignedStation = crew.AssignedStation;
                if (assignedStation != null)
                {
                    CrewHUDStationSlot shipSlot = FindSlotForStation(assignedStation.stationId);
                    if (shipSlot != null)
                    {
                        if (icon.CurrentSlot != shipSlot)
                        {
                            AttachIconToSlot(icon, shipSlot);
                        }
                        _processedIcons.Add(icon); // *** FIX: Mark as processed
                    }
                }
            }

            // Second pass: Handle all remaining (unassigned) crew
            foreach (var crew in manager.RegisteredCrew)
            {
                if (crew == null || string.IsNullOrEmpty(crew.crewId)) continue;

                CrewHUDCrewIcon icon = GetOrCreateIcon(crew);

                // *** FIX: If this icon was already placed in a station slot, skip it.
                if (_processedIcons.Contains(icon))
                {
                    continue;
                }

                // This icon is not assigned to a valid station, so it belongs in the pool.
                // Only move it if it's not already considered to be in the pool.
                if (icon.CurrentSlot != null)
                {
                    AttachIconToPool(icon);
                }
                else
                {
                    // Icon is already in the pool, just ensure it has a valid anchor
                    RectTransform poolAnchor = RequestCrewSlotAnchor(icon);
                    if (poolAnchor != null && icon.transform.parent != poolAnchor)
                    {
                        icon.AttachToParent(poolAnchor, crewSlotIconScale);
                    }
                    if (!icon.gameObject.activeSelf)
                    {
                        icon.gameObject.SetActive(true);
                    }
                }
            }

            RemoveMissingIcons();
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

            CrewHUDCrewIcon icon = Instantiate(iconPrefab, transform);
            icon.Initialize(this, crew);
            _iconsByCrewId[crew.crewId] = icon;
            
            return icon;
        }

        void AttachIconToSlot(CrewHUDCrewIcon icon, CrewHUDStationSlot slot)
        {
            if (icon == null || slot == null)
                return;

            // If icon is already correctly assigned to this slot, do nothing to prevent flickering
            if (icon.CurrentSlot == slot && icon.gameObject.activeSelf)
            {
                RectTransform expectedParent = slot.RequestAnchorFor(icon);
                if (expectedParent == null)
                {
                    expectedParent = slot.iconAnchor != null ? slot.iconAnchor : slot.transform as RectTransform;
                }
                // If it's already parented correctly, skip all operations
                if (icon.transform.parent == expectedParent)
                {
                    return;
                }
            }

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
            
            // Ensure CanvasGroup is in correct state for a stationed icon
            var canvasGroup = icon.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
            
            if (!icon.gameObject.activeSelf)
            {
                icon.gameObject.SetActive(true);
            }
            
            Transform slotWorldAnchor = slot.RequestWorldAnchorFor(icon);
            OnVisualAnchorChanged?.Invoke(icon.Crew, slot.Station, slotWorldAnchor);
        }

        void AttachIconToPool(CrewHUDCrewIcon icon)
        {
            if (icon == null)
                return;

            // If icon is already in pool and properly positioned, do nothing to prevent flickering
            if (icon.CurrentSlot == null && icon.gameObject.activeSelf)
            {
                RectTransform expectedParent = RequestCrewSlotAnchor(icon);
                // If it's already parented correctly to a pool anchor, skip all operations
                if (expectedParent != null && icon.transform.parent == expectedParent)
                {
                    return;
                }
            }

            if (icon.CurrentSlot != null)
            {
                icon.CurrentSlot.ReleaseIcon(icon);
            }

            icon.SetAssignedSlot(null);
            RectTransform parent = RequestCrewSlotAnchor(icon);
            if (parent == null)
            {
                string message = $"[CrewHUD] ERROR: No available unassigned crew slot anchors for {icon.Crew?.displayName}. Total anchors: {unassignedCrewSlotAnchors?.Length ?? 0}";
                Debug.LogError(message, icon);
                FileLogger.Log(message, "CrewHUD");
                // Don't throw exception, just hide the icon if there's no room
                icon.gameObject.SetActive(false); 
                return;
            }

            icon.AttachToParent(parent, crewSlotIconScale);
            
            if (!icon.gameObject.activeSelf)
            {
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

                if (_iconsByCrewId.TryGetValue(id, out var icon) && icon != null)
                {
                    icon.CurrentSlot?.ReleaseIcon(icon);
                    ReleaseCrewSlotAnchor(icon);
                    Destroy(icon.gameObject);
                }
                _iconsByCrewId.Remove(id);
            }

            _scratchIds.Clear();
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

        public Sprite GetPortraitForCrew(CrewMember crew)
        {
            if (crew == null || string.IsNullOrEmpty(crew.crewId))
            {
                return null;
            }

            // Get portrait from CrewManager (centralized portrait registry)
            if (CrewManager.HasInstance)
            {
                Sprite portrait = CrewManager.Instance.GetPortraitForCrew(crew);
                if (portrait != null)
                    return portrait;
            }

            // Fallback to icon prefab's default sprite
            if (iconPrefab != null && iconPrefab.portraitImage != null)
            {
                return iconPrefab.portraitImage.sprite;
            }

            return null;
        }
    }
}