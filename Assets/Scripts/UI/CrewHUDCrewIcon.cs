using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Teramyyd.UI
{
    /// <summary>
    /// Visual + interaction container for a crew member portrait. Handles tooltip hooks and drag/drop gestures.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class CrewHUDCrewIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        public Image portraitImage;
        public TMP_Text nameLabel;
        public TMP_Text specializationLabel;
        public TMP_Text statsLabel;
        public Image pendingBackground;
        public TMP_Text pendingText;


        public CrewMember Crew { get; private set; }
        public CrewHUDStationSlot CurrentSlot { get; private set; }

        RectTransform _rectTransform;
        CanvasGroup _canvasGroup;
        CrewHUDController _controller;
        RectTransform _lastParent;
        int _lastSiblingIndex;
        Vector2 _lastScale = Vector2.one;
        bool _dropAccepted;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Initialize(CrewHUDController controller, CrewMember crew, Vector2 initialScale)
        {
            _controller = controller;
            Crew = crew;
            UpdateVisuals();
            ClearPendingState();
            AttachToParent(controller != null ? controller.unassignedContainer : null, initialScale);
        }

        public void AttachToParent(RectTransform parent, Vector2 scale)
        {
            if (parent == null)
                return;

            _rectTransform.SetParent(parent, worldPositionStays: false);
            _rectTransform.localScale = new Vector3(scale.x, scale.y, 1f);
            _rectTransform.anchoredPosition = Vector2.zero;
            _rectTransform.SetAsLastSibling();
            _lastScale = scale;
        }

        public void SetAssignedSlot(CrewHUDStationSlot slot)
        {
            CurrentSlot = slot;
        }

        public void SetPendingState(string message, Color tint)
        {
            if (pendingBackground != null)
            {
                pendingBackground.gameObject.SetActive(true);
                pendingBackground.color = tint;
            }

            if (pendingText != null)
            {
                pendingText.gameObject.SetActive(true);
                pendingText.text = message;
            }
        }

        public void ClearPendingState()
        {
            if (pendingBackground != null)
            {
                pendingBackground.gameObject.SetActive(false);
            }

            if (pendingText != null)
            {
                pendingText.gameObject.SetActive(false);
                pendingText.text = string.Empty;
            }
        }

        public void MarkDropAccepted()
        {
            _dropAccepted = true;
        }

        public void SnapBackToLastParent()
        {
            if (_lastParent == null)
                return;

            _rectTransform.SetParent(_lastParent, worldPositionStays: false);
            _rectTransform.SetSiblingIndex(_lastSiblingIndex);
            _rectTransform.localScale = new Vector3(_lastScale.x, _lastScale.y, 1f);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Crew == null)
                return;

            _dropAccepted = false;
            _controller?.HideTooltip(this);
            _lastParent = _rectTransform.parent as RectTransform;
            _lastSiblingIndex = _rectTransform.GetSiblingIndex();

            Canvas canvas = _controller != null ? _controller.GetDragCanvas() : GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _rectTransform.SetParent(canvas.transform, worldPositionStays: true);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.alpha = 0.85f;
            }

            UpdateDragPosition(eventData, canvas);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Canvas canvas = _controller != null ? _controller.GetDragCanvas() : GetComponentInParent<Canvas>();
            UpdateDragPosition(eventData, canvas);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.alpha = 1f;
            }

            if (!_dropAccepted)
            {
                SnapBackToLastParent();
            }

            _dropAccepted = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _controller?.ShowTooltip(this, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _controller?.HideTooltip(this);
        }

        void UpdateDragPosition(PointerEventData eventData, Canvas canvas)
        {
            if (canvas == null)
                return;

            RectTransform canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            Vector2 localPoint;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, camera, out localPoint))
            {
                _rectTransform.anchoredPosition = localPoint;
            }
        }

        void UpdateVisuals()
        {
            if (Crew == null)
                return;

            CrewSkill topSkill = CrewSkillUtility.GetDominantSkill(Crew);

            if (specializationLabel != null)
            {
                string label = CrewSkillUtility.GetShortLabel(topSkill);
                specializationLabel.text = $"{label} {Crew.GetSkillLevel(topSkill):0.0}";
            }

            if (statsLabel != null)
            {
                statsLabel.text = CrewSkillUtility.BuildStatSummary(Crew);
            }

            if (nameLabel != null)
            {
                nameLabel.text = Crew.displayName;
            }
        }

    }
}
