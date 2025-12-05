using UnityEngine;
using UnityEngine.EventSystems;

namespace Teramyyd.UI
{
    /// <summary>
    /// Drop target that accepts crew icons to move them back into the unassigned HUD slots.
    /// </summary>
    [AddComponentMenu("Teramyyd/UI/Crew HUD Unassigned Zone")]
    public class CrewHUDUnassignedZone : MonoBehaviour, IDropHandler
    {
        [Tooltip("Controller that manages crew HUD icons. If left null the component searches its parents on Awake.")]
        public CrewHUDController controller;

        void Awake()
        {
            if (controller == null)
            {
                controller = GetComponentInParent<CrewHUDController>();
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            var icon = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<CrewHUDCrewIcon>() : null;
            if (icon == null)
                return;

            if (controller == null)
            {
                Debug.LogWarning("CrewHUDUnassignedZone has no controller assigned and cannot process drops.", this);
                return;
            }

            bool success = controller.HandleReturnToPool(icon);
            icon.NotifyDropHandled();

            if (!success)
            {
                icon.SnapBackToLastParent();
            }
        }

    }
}
