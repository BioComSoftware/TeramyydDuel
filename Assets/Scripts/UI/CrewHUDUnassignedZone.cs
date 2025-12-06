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
            string msg1 = "[CrewHUDUnassignedZone] OnDrop called";
            Debug.Log(msg1);
            FileLogger.Log(msg1, "CrewHUD");
            
            var icon = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<CrewHUDCrewIcon>() : null;
            if (icon == null)
            {
                Debug.LogWarning("[CrewHUDUnassignedZone] OnDrop: No icon found");
                return;
            }

            string msg2 = $"[CrewHUDUnassignedZone] OnDrop: Crew {icon.Crew?.displayName ?? "null"} dropped";
            Debug.Log(msg2);
            FileLogger.Log(msg2, "CrewHUD");

            if (controller == null)
            {
                Debug.LogWarning("CrewHUDUnassignedZone has no controller assigned and cannot process drops.", this);
                return;
            }

            bool success = controller.HandleReturnToPool(icon);
            icon.NotifyDropHandled();

            string msg3 = $"[CrewHUDUnassignedZone] OnDrop: HandleReturnToPool returned {success}";
            Debug.Log(msg3);
            FileLogger.Log(msg3, "CrewHUD");

            if (!success)
            {
                icon.SnapBackToLastParent();
            }
        }

    }
}
