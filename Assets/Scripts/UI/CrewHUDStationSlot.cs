using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Teramyyd.UI
{
    /// <summary>
    /// Drop target that represents a specific CrewStation on the ship outline HUD.
    /// </summary>
    [AddComponentMenu("Teramyyd/UI/Crew HUD Station Slot")]
    public class CrewHUDStationSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Station Binding")]
        public CrewStation station;
        [Tooltip("Primary RectTransform the first crew portrait snaps to.")]
        public RectTransform iconAnchor;
        [Tooltip("Optional extra anchors for additional crew assigned to the same station.")]
        public RectTransform[] additionalIconAnchors;
        [Tooltip("World-space anchor for the first crew portrait (used to place 3D crew).")]
        public Transform worldAnchor;
        [Tooltip("Optional extra world anchors so multiple crew members at this station each have a unique position in the world.")]
        public Transform[] additionalWorldAnchors;

        [Header("Visual Feedback")]
        public Image highlightImage;
        public Color hoverColor = new Color(1f, 1f, 1f, 0.25f);

        CrewHUDController _controller;
        Color _defaultHighlight;
        readonly Dictionary<CrewHUDCrewIcon, RectTransform> _iconToAnchor = new Dictionary<CrewHUDCrewIcon, RectTransform>();
        readonly Dictionary<RectTransform, CrewHUDCrewIcon> _anchorToIcon = new Dictionary<RectTransform, CrewHUDCrewIcon>();
        readonly Dictionary<CrewHUDCrewIcon, Transform> _iconToWorldAnchor = new Dictionary<CrewHUDCrewIcon, Transform>();
        readonly Dictionary<Transform, CrewHUDCrewIcon> _worldAnchorToIcon = new Dictionary<Transform, CrewHUDCrewIcon>();

        public string StationId
        {
            get
            {
                return station != null ? station.stationId : string.Empty;
            }
        }

        public CrewStation Station
        {
            get
            {
                return station;
            }
        }

        public void Initialize(CrewHUDController controller)
        {
            _controller = controller;
            if (highlightImage != null)
            {
                _defaultHighlight = highlightImage.color;
                highlightImage.gameObject.SetActive(false);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            string msg1 = $"[CrewHUDStationSlot] OnDrop called for station {StationId}";
            Debug.Log(msg1);
            FileLogger.Log(msg1, "CrewHUD");
            
            var icon = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<CrewHUDCrewIcon>() : null;
            if (icon == null || _controller == null)
            {
                string msg2 = $"[CrewHUDStationSlot] OnDrop failed: icon={icon != null}, controller={_controller != null}";
                Debug.LogWarning(msg2);
                FileLogger.Log(msg2, "CrewHUD");
                return;
            }

            string msg3 = $"[CrewHUDStationSlot] OnDrop: Crew {icon.Crew?.displayName ?? "null"} dropped on station {StationId}";
            Debug.Log(msg3);
            FileLogger.Log(msg3, "CrewHUD");

            bool success = _controller.HandleStationDrop(this, icon);
            icon.NotifyDropHandled();

            string msg4 = $"[CrewHUDStationSlot] OnDrop: HandleStationDrop returned {success}";
            Debug.Log(msg4);
            FileLogger.Log(msg4, "CrewHUD");

            if (!success)
            {
                icon.SnapBackToLastParent();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (highlightImage == null)
                return;

            highlightImage.gameObject.SetActive(true);
            highlightImage.color = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (highlightImage == null)
                return;

            highlightImage.gameObject.SetActive(false);
            highlightImage.color = _defaultHighlight;
        }

        public RectTransform RequestAnchorFor(CrewHUDCrewIcon icon)
        {
            if (icon == null)
                return null;

            if (_iconToAnchor.TryGetValue(icon, out var existing) && existing != null)
                return existing;

            foreach (var anchor in EnumerateAnchors())
            {
                if (anchor == null)
                    continue;

                if (!_anchorToIcon.TryGetValue(anchor, out var occupant) || occupant == null)
                {
                    _anchorToIcon[anchor] = icon;
                    _iconToAnchor[icon] = anchor;
                    return anchor;
                }
            }

            return iconAnchor != null ? iconAnchor : transform as RectTransform;
        }

        public void ReleaseIcon(CrewHUDCrewIcon icon)
        {
            if (icon == null)
                return;

            if (_iconToAnchor.TryGetValue(icon, out var anchor))
            {
                _iconToAnchor.Remove(icon);
                if (anchor != null && _anchorToIcon.TryGetValue(anchor, out var occupant) && occupant == icon)
                {
                    _anchorToIcon.Remove(anchor);
                }
            }

            if (_iconToWorldAnchor.TryGetValue(icon, out var worldAnchorRef))
            {
                _iconToWorldAnchor.Remove(icon);
                if (worldAnchorRef != null && _worldAnchorToIcon.TryGetValue(worldAnchorRef, out var occupant) && occupant == icon)
                {
                    _worldAnchorToIcon.Remove(worldAnchorRef);
                }
            }
        }

        IEnumerable<RectTransform> EnumerateAnchors()
        {
            if (iconAnchor != null)
                yield return iconAnchor;

            if (additionalIconAnchors == null)
                yield break;

            for (int i = 0; i < additionalIconAnchors.Length; i++)
            {
                var anchor = additionalIconAnchors[i];
                if (anchor != null)
                    yield return anchor;
            }
        }

        public Transform RequestWorldAnchorFor(CrewHUDCrewIcon icon)
        {
            if (icon == null)
                return worldAnchor;

            if (_iconToWorldAnchor.TryGetValue(icon, out var existing) && existing != null)
                return existing;

            foreach (var candidate in EnumerateWorldAnchors())
            {
                if (candidate == null)
                    continue;

                if (!_worldAnchorToIcon.TryGetValue(candidate, out var occupant) || occupant == null)
                {
                    _worldAnchorToIcon[candidate] = icon;
                    _iconToWorldAnchor[icon] = candidate;
                    return candidate;
                }
            }

            return worldAnchor;
        }

        IEnumerable<Transform> EnumerateWorldAnchors()
        {
            if (worldAnchor != null)
                yield return worldAnchor;

            if (additionalWorldAnchors == null)
                yield break;

            for (int i = 0; i < additionalWorldAnchors.Length; i++)
            {
                var anchor = additionalWorldAnchors[i];
                if (anchor != null)
                    yield return anchor;
            }
        }
    }
}
