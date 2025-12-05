using System.Collections;
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
        public void SetPortraitSprite(Sprite sprite)
        {
            if (portraitImage == null)
                return;

            if (sprite != null)
            {
                portraitImage.sprite = sprite;
                portraitImage.enabled = true;
            }
            else
            {
                portraitImage.enabled = false;
            }
        }

        [Header("UI References")]
        public Image portraitImage;
        public Image pendingBackground;
        public TMP_Text pendingText;

        [Header("Tooltip")]
        [Tooltip("Optional anchor used to position the tooltip at a fixed location.")]
        public RectTransform tooltipAnchor;

        [Header("Debug")]
        [Tooltip("When enabled, pointer events log to both the Console and Logs/game_debug.log.")]
        public bool debugLog = false;


        public CrewMember Crew { get; private set; }
        public CrewHUDStationSlot CurrentSlot { get; private set; }

        RectTransform _rectTransform;
        CanvasGroup _canvasGroup;
        CrewHUDController _controller;
        RectTransform _lastParent;
        int _lastSiblingIndex;
        Vector2 _lastScale = Vector2.one;
        bool _dropHandled;
        Coroutine _pendingDropRoutine;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Initialize(CrewHUDController controller, CrewMember crew)
        {
            _controller = controller;
            Crew = crew;
            UpdateVisuals();
            ClearPendingState();
        }

        void OnDisable()
        {
            if (_pendingDropRoutine != null)
            {
                StopCoroutine(_pendingDropRoutine);
                _pendingDropRoutine = null;
            }
        }

        public void AttachToParent(RectTransform parent, Vector2 scale)
        {
            if (parent == null)
                return;

            _rectTransform.SetParent(parent, worldPositionStays: false);
            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);
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

            if (_pendingDropRoutine != null)
            {
                StopCoroutine(_pendingDropRoutine);
                _pendingDropRoutine = null;
            }

            _dropHandled = false;
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

            if (_pendingDropRoutine != null)
            {
                StopCoroutine(_pendingDropRoutine);
            }
            _pendingDropRoutine = StartCoroutine(EnsureDropHandled());
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Crew != null)
            {
                DebugMessage($"Pointer enter: {Crew.displayName} ({Crew.crewId})");
            }
            _controller?.ShowTooltip(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Crew != null)
            {
                DebugMessage($"Pointer exit: {Crew.displayName} ({Crew.crewId})");
            }
            _controller?.HideTooltip(this);
        }

        public void NotifyDropHandled()
        {
            _dropHandled = true;
            if (_pendingDropRoutine != null)
            {
                StopCoroutine(_pendingDropRoutine);
                _pendingDropRoutine = null;
            }
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

            CrewSkillUtility.GetDominantSkill(Crew);
        }

        IEnumerator EnsureDropHandled()
        {
            yield return null;

            if (!_dropHandled)
            {
                _controller?.HandleReturnToPool(this);
                _dropHandled = true;
            }

            _pendingDropRoutine = null;
        }

        void DebugMessage(string message)
        {
            if (!debugLog)
                return;

            string formatted = $"[CrewHUDCrewIcon] {message}";
            Debug.Log(formatted, this);
            FileLogger.Log(formatted, "CrewHUDCrewIcon");
        }

    }
}
