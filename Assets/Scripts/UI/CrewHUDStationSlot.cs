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
        public RectTransform iconAnchor;
        [Tooltip("World-space anchor for spawning future 3D crew representations.")]
        public Transform worldAnchor;

        [Header("Visual Feedback")]
        public Image highlightImage;
        public Color hoverColor = new Color(1f, 1f, 1f, 0.25f);

        CrewHUDController _controller;
        Color _defaultHighlight;

        public string StationId => Station != null ? Station.stationId : stationIdOverride;

        public CrewStation Station
        {
            get
            {
                if (station != null)
                    return station;

                if (!string.IsNullOrEmpty(stationIdOverride) && CrewManager.HasInstance)
                {
                    if (CrewManager.Instance.TryGetStation(stationIdOverride, out var found))
                    {
                        station = found;
                    }
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
            if (icon == null)
                return;

            _controller?.HandleStationDrop(this, icon);
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
    }
}
