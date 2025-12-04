using UnityEngine;
using UnityEngine.EventSystems;

namespace Teramyyd.UI
{
    /// <summary>
    /// Drop target that accepts crew icons to move them back into the unassigned pool.
    /// </summary>
    [AddComponentMenu("Teramyyd/UI/Crew HUD Unassigned Zone")]
    public class CrewHUDUnassignedZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        CrewHUDController _controller;

        public void Initialize(CrewHUDController controller)
        {
            _controller = controller;
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
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }
    }
}
