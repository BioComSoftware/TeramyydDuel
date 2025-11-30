using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Teramyyd.UI
{
    /// <summary>
    /// Drop target that accepts crew icons to move them back into the unassigned pool.
    /// </summary>
    [AddComponentMenu("Teramyyd/UI/Crew HUD Unassigned Zone")]
    public class CrewHUDUnassignedZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Image highlightImage;
        public Color hoverColor = new Color(1f, 1f, 1f, 0.15f);

        CrewHUDController _controller;
        Color _defaultColor;

        public void Initialize(CrewHUDController controller)
        {
            _controller = controller;
            if (highlightImage != null)
            {
                _defaultColor = highlightImage.color;
                highlightImage.gameObject.SetActive(false);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            var icon = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<CrewHUDCrewIcon>() : null;
            if (icon == null)
                return;

            _controller?.HandleReturnToPool(icon);
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
            highlightImage.color = _defaultColor;
        }
    }
}
