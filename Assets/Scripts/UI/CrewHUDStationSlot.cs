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
        [Tooltip("Optional explicit stationId if the HUD cannot reference the CrewStation component directly.")]
        public string stationIdOverride;
        [Tooltip("Primary RectTransform the first crew portrait snaps to.")]
        public RectTransform iconAnchor;
        [Tooltip("Optional extra anchors for additional crew assigned to the same station.")]
        public RectTransform[] additionalIconAnchors;
        [Tooltip("World-space anchor for spawning future 3D crew representations.")]
        public Transform worldAnchor;

        [Header("Visual Feedback")]
        public Image highlightImage;
        public Color hoverColor = new Color(1f, 1f, 1f, 0.25f);

        CrewHUDController _controller;
        Color _defaultHighlight;
        readonly Dictionary<CrewHUDCrewIcon, RectTransform> _iconToAnchor = new Dictionary<CrewHUDCrewIcon, RectTransform>();
        readonly Dictionary<RectTransform, CrewHUDCrewIcon> _anchorToIcon = new Dictionary<RectTransform, CrewHUDCrewIcon>();

        public string StationId
        {
            get
            {
                if (!string.IsNullOrEmpty(stationIdOverride))
                    return stationIdOverride;

                return Station != null ? Station.stationId : string.Empty;
            }
        }

        public CrewStation Station
        {
            get
            {
                if (!string.IsNullOrEmpty(stationIdOverride))
                {
                    if (station != null && station.stationId == stationIdOverride)
                        return station;

                    if (CrewManager.HasInstance && CrewManager.Instance.TryGetStation(stationIdOverride, out var overrideStation))
                    {
                        station = overrideStation;
                        return station;
                    }

                    return null;
                }

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
            var icon = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<CrewHUDCrewIcon>() : null;
            if (icon == null || _controller == null)
                return;

            bool success = _controller.HandleStationDrop(this, icon);
            icon.NotifyDropHandled();

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
    }
}
