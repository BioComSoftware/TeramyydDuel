using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Teramyyd.UI
{
    /// <summary>
    /// Simple tooltip that surfaces crew stats when the player hovers over a portrait slot.
    /// </summary>
    [AddComponentMenu("Teramyyd/UI/Crew HUD Tooltip")]
    public class CrewHUDTooltip : MonoBehaviour
    {
        public GameObject root;
        public TMP_Text nameLabel;
        public TMP_Text specializationLabel;
        public TMP_Text statsLabel;
        public TMP_Text stationLabel;
        public TMP_Text healthLabel;
        public Image healthFill;
        [Tooltip("Optional portrait shown on the tooltip. Leave null to skip portrait visuals.")]
        public Image portraitImage;
        [Tooltip("Canvas-space offset applied after aligning to an anchor.")]
        public Vector2 anchorOffset = Vector2.zero;
        [Tooltip("When enabled the tooltip canvas group ignores raycasts so it never blocks the underlying icons.")]
        public bool ignoreRaycasts = true;

        Canvas _canvas;
        CanvasGroup _canvasGroup;
        RectTransform _rect;
        RectTransform _canvasRect;

        void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            _rect = root.GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            _canvasRect = _canvas != null ? _canvas.transform as RectTransform : null;
            _canvasGroup = root.GetComponent<CanvasGroup>();
            ApplyRaycastSettings();
            HideImmediate();
        }

        public void Show(CrewMember crew, CrewStation station, RectTransform anchor, Sprite portraitSprite)
        {
            if (crew == null || anchor == null)
            {
                Hide(null);
                return;
            }

            if (root != null && !root.activeSelf)
            {
                root.SetActive(true);
            }

            if (!PositionTooltipAtAnchor(anchor))
            {
                Hide(null);
                return;
            }

            ApplyPortrait(portraitSprite);

            if (nameLabel != null)
            {
                nameLabel.text = crew.displayName;
            }

            if (specializationLabel != null)
            {
                CrewSkill topSkill = CrewSkillUtility.GetDominantSkill(crew);
                specializationLabel.text = $"Focus: {CrewSkillUtility.GetShortLabel(topSkill)}";
            }

            if (statsLabel != null)
            {
                statsLabel.text = CrewSkillUtility.BuildStatSummary(crew);
            }

            if (stationLabel != null)
            {
                if (station != null)
                {
                    stationLabel.text = $"Assigned: {station.displayName}";
                }
                else if (!string.IsNullOrEmpty(crew.PendingStationId))
                {
                    stationLabel.text = $"Pending: {crew.PendingStationId}";
                }
                else
                {
                    stationLabel.text = "Unassigned";
                }
            }

            ApplyHealthDetails(crew);
        }

        public void Hide(object source)
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        void HideImmediate()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        void ApplyRaycastSettings()
        {
            if (!ignoreRaycasts)
                return;

            if (_canvasGroup == null && root != null)
            {
                _canvasGroup = root.AddComponent<CanvasGroup>();
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.ignoreParentGroups = true;
            }
        }

        void ApplyHealthDetails(CrewMember crew)
        {
            Health health = crew != null ? crew.Health : null;
            float current = health != null ? health.currentHealth : 0f;
            float max = health != null ? health.maxHealth : 0f;

            if (healthLabel != null)
            {
                if (health != null)
                {
                    healthLabel.text = $"Health {current:0}/{max:0}";
                }
                else
                {
                    healthLabel.text = "Health N/A";
                }
            }

            if (healthFill != null)
            {
                if (health != null && health.maxHealth > 0f)
                {
                    healthFill.fillAmount = Mathf.Clamp01(current / max);
                }
                else
                {
                    healthFill.fillAmount = 0f;
                }
            }
        }

        void ApplyPortrait(Sprite portrait)
        {
            if (portraitImage == null)
                return;

            if (portrait != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.enabled = true;
            }
            else
            {
                portraitImage.enabled = false;
            }
        }

        bool PositionTooltipAtAnchor(RectTransform anchor)
        {
            if (_rect == null || anchor == null || !EnsureCanvasReferences())
                return false;

            Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            Vector3 worldCenter = anchor.TransformPoint(anchor.rect.center);
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, cam, out var localPoint))
                return false;

            localPoint += anchorOffset;
            localPoint = ClampToCanvas(localPoint);
            _rect.anchoredPosition = localPoint;
            return true;
        }

        Vector2 ClampToCanvas(Vector2 desired)
        {
            if (_canvasRect == null || _rect == null)
                return desired;

            Vector2 canvasSize = _canvasRect.rect.size;
            Vector2 tooltipSize = _rect.rect.size;
            Vector2 pivot = _rect.pivot;

            float minX = -canvasSize.x * 0.5f + tooltipSize.x * pivot.x;
            float maxX = canvasSize.x * 0.5f - tooltipSize.x * (1f - pivot.x);
            float minY = -canvasSize.y * 0.5f + tooltipSize.y * pivot.y;
            float maxY = canvasSize.y * 0.5f - tooltipSize.y * (1f - pivot.y);

            desired.x = Mathf.Clamp(desired.x, minX, maxX);
            desired.y = Mathf.Clamp(desired.y, minY, maxY);

            return desired;
        }

        bool EnsureCanvasReferences()
        {
            if (_canvas != null && _canvasRect != null)
                return true;

            _canvas = GetComponentInParent<Canvas>();
            _canvasRect = _canvas != null ? _canvas.transform as RectTransform : null;
            return _canvasRect != null;
        }
    }
}
