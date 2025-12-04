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
        public Vector2 screenOffset = new Vector2(20f, -20f);

        Canvas _canvas;

        void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            _canvas = GetComponentInParent<Canvas>();
            HideImmediate();
        }

        public void Show(CrewMember crew, CrewStation station, Vector2 screenPosition, Sprite portraitSprite)
        {
            if (crew == null)
            {
                Hide(null);
                return;
            }

            if (root != null && !root.activeSelf)
            {
                root.SetActive(true);
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
            PositionTooltip(screenPosition + screenOffset);
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

        void PositionTooltip(Vector2 screenPos)
        {
            if (root == null)
                return;

            RectTransform rect = root.GetComponent<RectTransform>();
            if (rect == null)
                return;

            Canvas canvas = _canvas != null ? _canvas : GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null)
                return;

            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, cam, out var localPoint))
            {
                rect.anchoredPosition = localPoint;
            }
        }
    }
}
