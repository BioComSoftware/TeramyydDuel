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
            string msg1 = $"[CrewHUDTooltip] Show called: crew={crew?.displayName ?? "null"}, anchor={anchor != null}, portrait={portraitSprite != null}";
            Debug.Log(msg1);
            FileLogger.Log(msg1, "CrewHUD");
            
            if (crew == null || anchor == null)
            {
                string msg2 = $"[CrewHUDTooltip] Show: crew or anchor is null, hiding";
                Debug.LogWarning(msg2);
                FileLogger.Log(msg2, "CrewHUD");
                Hide(null);
                return;
            }

            if (root != null && !root.activeSelf)
            {
                string msg3 = $"[CrewHUDTooltip] Activating root GameObject";
                Debug.Log(msg3);
                FileLogger.Log(msg3, "CrewHUD");
                root.SetActive(true);
            }
            
            // Move tooltip to last sibling (render on top)
            if (root != null)
            {
                root.transform.SetAsLastSibling();
                string msg3b = $"[CrewHUDTooltip] Set tooltip as last sibling, new index={root.transform.GetSiblingIndex()}";
                Debug.Log(msg3b);
                FileLogger.Log(msg3b, "CrewHUD");
            }

            if (!PositionTooltipAtAnchor(anchor))
            {
                string msg4 = $"[CrewHUDTooltip] PositionTooltipAtAnchor failed, hiding";
                Debug.LogWarning(msg4);
                FileLogger.Log(msg4, "CrewHUD");
                Hide(null);
                return;
            }
            
            string msg5 = $"[CrewHUDTooltip] Tooltip positioned successfully, populating data";
            Debug.Log(msg5);
            FileLogger.Log(msg5, "CrewHUD");

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
            string msg = $"[CrewHUDTooltip] Hide called from {source?.GetType().Name ?? "null"}";
            Debug.Log(msg);
            FileLogger.Log(msg, "CrewHUD");
            
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
            string msg1 = $"[CrewHUDTooltip] PositionTooltipAtAnchor: _rect={_rect != null}, anchor={anchor != null}, canvas refs check...";
            Debug.Log(msg1);
            FileLogger.Log(msg1, "CrewHUD");
            
            if (_rect == null)
            {
                string msg2 = "[CrewHUDTooltip] PositionTooltipAtAnchor: _rect is null";
                Debug.LogWarning(msg2);
                FileLogger.Log(msg2, "CrewHUD");
                return false;
            }
            
            if (anchor == null)
            {
                string msg3 = "[CrewHUDTooltip] PositionTooltipAtAnchor: anchor is null";
                Debug.LogWarning(msg3);
                FileLogger.Log(msg3, "CrewHUD");
                return false;
            }
            
            if (!EnsureCanvasReferences())
            {
                string msg4 = $"[CrewHUDTooltip] PositionTooltipAtAnchor: EnsureCanvasReferences failed - _canvas={_canvas != null}, _canvasRect={_canvasRect != null}";
                Debug.LogWarning(msg4);
                FileLogger.Log(msg4, "CrewHUD");
                return false;
            }

            // Convert anchor world position to tooltip's parent local space
            RectTransform tooltipParent = _rect.parent as RectTransform;
            if (tooltipParent == null)
            {
                Debug.LogError("[CrewHUDTooltip] Tooltip parent is not a RectTransform!");
                return false;
            }
            
            string parentInfo = $"[CrewHUDTooltip] Parent={tooltipParent.name}, anchoredPos={tooltipParent.anchoredPosition}, " +
                               $"localPos={tooltipParent.localPosition}, pivot={tooltipParent.pivot}, anchorMin={tooltipParent.anchorMin}, anchorMax={tooltipParent.anchorMax}";
            Debug.Log(parentInfo);
            FileLogger.Log(parentInfo, "CrewHUD");
            
            string anchorInfo = $"[CrewHUDTooltip] Anchor={anchor.name}, anchoredPos={anchor.anchoredPosition}, " +
                               $"pivot={anchor.pivot}, anchorMin={anchor.anchorMin}, anchorMax={anchor.anchorMax}";
            Debug.Log(anchorInfo);
            FileLogger.Log(anchorInfo, "CrewHUD");
            
            // Get anchor's world position
            Vector3 anchorWorldPos = anchor.position;
            
            // Match the tooltip's anchor mode to the anchor's anchor mode
            _rect.anchorMin = anchor.anchorMin;
            _rect.anchorMax = anchor.anchorMax;
            
            // Also match the pivot so positions align correctly
            _rect.pivot = anchor.pivot;
            
            // Now we can directly copy the anchored position since they use the same anchor mode and pivot
            Vector2 finalPos = anchor.anchoredPosition + anchorOffset;
            _rect.anchoredPosition = finalPos;
            
            string msg5 = $"[CrewHUDTooltip] Matched anchor mode (min={anchor.anchorMin}, max={anchor.anchorMax}), pivot={anchor.pivot}, " +
                         $"copied anchoredPos={anchor.anchoredPosition} + offset={anchorOffset} = final={finalPos}";
            Debug.Log(msg5);
            FileLogger.Log(msg5, "CrewHUD");
            
            string msg7 = $"[CrewHUDTooltip] PositionTooltipAtAnchor: SUCCESS - positioned at {finalPos}";
            Debug.Log(msg7);
            FileLogger.Log(msg7, "CrewHUD");
            
            // Additional diagnostics
            string msg8 = $"[CrewHUDTooltip] Root active={root.activeSelf}, scale={root.transform.localScale}, " +
                         $"canvasGroup={((_canvasGroup != null) ? $"alpha={_canvasGroup.alpha}, interactable={_canvasGroup.interactable}" : "null")}";
            Debug.Log(msg8);
            FileLogger.Log(msg8, "CrewHUD");
            
            string msg9 = $"[CrewHUDTooltip] Canvas size={_canvasRect.rect.size}, tooltip size={_rect.rect.size}, " +
                         $"tooltip pivot={_rect.pivot}, sibling index={root.transform.GetSiblingIndex()}";
            Debug.Log(msg9);
            FileLogger.Log(msg9, "CrewHUD");
            
            // Check for background Image component
            Image bgImage = root.GetComponent<Image>();
            string msg10 = $"[CrewHUDTooltip] Root Image={bgImage != null}, " +
                          (bgImage != null ? $"sprite={bgImage.sprite != null}, color={bgImage.color}, enabled={bgImage.enabled}" : "N/A");
            Debug.Log(msg10);
            FileLogger.Log(msg10, "CrewHUD");
            
            // Final check: world position and screen rect
            Vector3[] worldCorners = new Vector3[4];
            _rect.GetWorldCorners(worldCorners);
            string msg11 = $"[CrewHUDTooltip] World corners: BL={worldCorners[0]}, TR={worldCorners[2]}, " +
                          $"Root world pos={root.transform.position}, localScale={root.transform.localScale}";
            Debug.Log(msg11);
            FileLogger.Log(msg11, "CrewHUD");
            
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
