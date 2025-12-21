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
        public TMP_Text gunneryLabel;
        public TMP_Text navigationLabel;
        public TMP_Text repairLabel;
        public TMP_Text engineLabel;
        public TMP_Text liftLabel;
        public TMP_Text fightingLabel;
        public Image healthFill;
        [Tooltip("Optional portrait shown on the tooltip. Leave null to skip portrait visuals.")]
        public Image portraitImage;
        [Tooltip("Canvas-space offset applied after aligning to an anchor.")]
        public Vector2 anchorOffset = Vector2.zero;
        [Tooltip("When enabled the tooltip canvas group ignores raycasts so it never blocks the underlying icons.")]
        public bool ignoreRaycasts = true;
        [Tooltip("Enables debug logging to console and Logs/game_debug.log.")]
        public bool debugLog = false;

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
            
            // Move tooltip to last sibling (render on top)
            if (root != null)
            {
                root.transform.SetAsLastSibling();
                if (debugLog)
                {
                    Debug.Log($"[CrewHUDTooltip] Set tooltip as last sibling, new index={root.transform.GetSiblingIndex()}");
                    FileLogger.Log($"Set tooltip as last sibling, new index={root.transform.GetSiblingIndex()}", "CrewHUD");
                }
            }

            if (!PositionTooltipAtAnchor(anchor))
            {
                if (debugLog)
                {
                    Debug.LogWarning("[CrewHUDTooltip] PositionTooltipAtAnchor failed, hiding");
                    FileLogger.Log("PositionTooltipAtAnchor failed, hiding", "CrewHUD");
                }
                Hide(null);
                return;
            }
            
            if (debugLog)
            {
                Debug.Log("[CrewHUDTooltip] Tooltip positioned successfully, populating data");
                FileLogger.Log("Tooltip positioned successfully, populating data", "CrewHUD");
            }

            ApplyPortrait(portraitSprite);

            if (nameLabel != null)
            {
                nameLabel.text = crew.displayName;
            }

            // Populate individual stat fields
            if (gunneryLabel != null)
            {
                gunneryLabel.text = $"Gunnery: {crew.gunnery:0.0}";
            }
            
            if (navigationLabel != null)
            {
                navigationLabel.text = $"Navigation: {crew.navigation:0.0}";
            }
            
            if (repairLabel != null)
            {
                repairLabel.text = $"Repair: {crew.repair:0.0}";
            }
            
            if (engineLabel != null)
            {
                engineLabel.text = $"Power Eng: {crew.powerEngineering:0.0}";
            }
            
            if (liftLabel != null)
            {
                liftLabel.text = $"Lift Eng: {crew.liftEngineering:0.0}";
            }
            
            if (fightingLabel != null)
            {
                fightingLabel.text = $"Fighting: {crew.fighting:0.0}";
            }

            ApplyHealthDetails(crew);
        }

        public void Hide(object source)
        {
            if (debugLog)
            {
                Debug.Log($"[CrewHUDTooltip] Hide called from {source?.GetType().Name ?? "null"}");
                FileLogger.Log($"Hide called from {source?.GetType().Name ?? "null"}", "CrewHUD");
            }
            
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
            float healthPercent = (health != null && max > 0f) ? Mathf.Clamp01(current / max) : 0f;

            if (debugLog)
            {
                Debug.Log($"[CrewHUDTooltip] ApplyHealthDetails: current={current}, max={max}, percent={healthPercent}, healthFill={(healthFill != null ? "assigned" : "NULL")}");
                FileLogger.Log($"ApplyHealthDetails: current={current}, max={max}, percent={healthPercent}, healthFill={(healthFill != null ? "assigned" : "NULL")}", "CrewHUD");
            }

            // Update health bar with green-to-red gradient
            if (healthFill != null)
            {
                healthFill.fillAmount = healthPercent;
                
                // Interpolate from green (100% health) to red (0% health)
                healthFill.color = Color.Lerp(Color.red, Color.green, healthPercent);
                
                if (debugLog)
                {
                    Debug.Log($"[CrewHUDTooltip] HealthFill updated: fillAmount={healthPercent}, color={healthFill.color}, type={healthFill.type}");
                    FileLogger.Log($"HealthFill updated: fillAmount={healthPercent}, color={healthFill.color}, type={healthFill.type}", "CrewHUD");
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
            if (debugLog)
            {
                Debug.Log($"[CrewHUDTooltip] PositionTooltipAtAnchor: _rect={_rect != null}, anchor={anchor != null}, canvas refs check...");
                FileLogger.Log($"PositionTooltipAtAnchor: _rect={_rect != null}, anchor={anchor != null}, canvas refs check...", "CrewHUD");
            }
            
            if (_rect == null)
            {
                if (debugLog)
                {
                    Debug.LogWarning("[CrewHUDTooltip] PositionTooltipAtAnchor: _rect is null");
                    FileLogger.Log("PositionTooltipAtAnchor: _rect is null", "CrewHUD");
                }
                return false;
            }
            
            if (anchor == null)
            {
                if (debugLog)
                {
                    Debug.LogWarning("[CrewHUDTooltip] PositionTooltipAtAnchor: anchor is null");
                    FileLogger.Log("PositionTooltipAtAnchor: anchor is null", "CrewHUD");
                }
                return false;
            }
            
            if (!EnsureCanvasReferences())
            {
                if (debugLog)
                {
                    Debug.LogWarning($"[CrewHUDTooltip] PositionTooltipAtAnchor: EnsureCanvasReferences failed - _canvas={_canvas != null}, _canvasRect={_canvasRect != null}");
                    FileLogger.Log($"PositionTooltipAtAnchor: EnsureCanvasReferences failed - _canvas={_canvas != null}, _canvasRect={_canvasRect != null}", "CrewHUD");
                }
                return false;
            }

            // Convert anchor world position to tooltip's parent local space
            RectTransform tooltipParent = _rect.parent as RectTransform;
            if (tooltipParent == null)
            {
                Debug.LogError("[CrewHUDTooltip] Tooltip parent is not a RectTransform!");
                return false;
            }
            
            if (debugLog)
            {
                string parentInfo = $"[CrewHUDTooltip] Parent={tooltipParent.name}, anchoredPos={tooltipParent.anchoredPosition}, " +
                                   $"localPos={tooltipParent.localPosition}, pivot={tooltipParent.pivot}, anchorMin={tooltipParent.anchorMin}, anchorMax={tooltipParent.anchorMax}";
                Debug.Log(parentInfo);
                FileLogger.Log(parentInfo, "CrewHUD");
                
                string anchorInfo = $"[CrewHUDTooltip] Anchor={anchor.name}, anchoredPos={anchor.anchoredPosition}, " +
                                   $"pivot={anchor.pivot}, anchorMin={anchor.anchorMin}, anchorMax={anchor.anchorMax}";
                Debug.Log(anchorInfo);
                FileLogger.Log(anchorInfo, "CrewHUD");
            }
            
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
            
            if (debugLog)
            {
                Debug.Log($"[CrewHUDTooltip] Matched anchor mode (min={anchor.anchorMin}, max={anchor.anchorMax}), pivot={anchor.pivot}, " +
                             $"copied anchoredPos={anchor.anchoredPosition} + offset={anchorOffset} = final={finalPos}");
                FileLogger.Log($"Matched anchor mode (min={anchor.anchorMin}, max={anchor.anchorMax}), pivot={anchor.pivot}, " +
                             $"copied anchoredPos={anchor.anchoredPosition} + offset={anchorOffset} = final={finalPos}", "CrewHUD");
                
                Debug.Log($"[CrewHUDTooltip] PositionTooltipAtAnchor: SUCCESS - positioned at {finalPos}");
                FileLogger.Log($"PositionTooltipAtAnchor: SUCCESS - positioned at {finalPos}", "CrewHUD");
                
                // Additional diagnostics
                Debug.Log($"[CrewHUDTooltip] Root active={root.activeSelf}, scale={root.transform.localScale}, " +
                             $"canvasGroup={((_canvasGroup != null) ? $"alpha={_canvasGroup.alpha}, interactable={_canvasGroup.interactable}" : "null")}");
                FileLogger.Log($"Root active={root.activeSelf}, scale={root.transform.localScale}, " +
                             $"canvasGroup={((_canvasGroup != null) ? $"alpha={_canvasGroup.alpha}, interactable={_canvasGroup.interactable}" : "null")}", "CrewHUD");
                
                Debug.Log($"[CrewHUDTooltip] Canvas size={_canvasRect.rect.size}, tooltip size={_rect.rect.size}, " +
                             $"tooltip pivot={_rect.pivot}, sibling index={root.transform.GetSiblingIndex()}");
                FileLogger.Log($"Canvas size={_canvasRect.rect.size}, tooltip size={_rect.rect.size}, " +
                             $"tooltip pivot={_rect.pivot}, sibling index={root.transform.GetSiblingIndex()}", "CrewHUD");
                
                // Check for background Image component
                Image bgImage = root.GetComponent<Image>();
                Debug.Log($"[CrewHUDTooltip] Root Image={bgImage != null}, " +
                              (bgImage != null ? $"sprite={bgImage.sprite != null}, color={bgImage.color}, enabled={bgImage.enabled}" : "N/A"));
                FileLogger.Log($"Root Image={bgImage != null}, " +
                              (bgImage != null ? $"sprite={bgImage.sprite != null}, color={bgImage.color}, enabled={bgImage.enabled}" : "N/A"), "CrewHUD");
                
                // Final check: world position and screen rect
                Vector3[] worldCorners = new Vector3[4];
                _rect.GetWorldCorners(worldCorners);
                Debug.Log($"[CrewHUDTooltip] World corners: BL={worldCorners[0]}, TR={worldCorners[2]}, " +
                              $"Root world pos={root.transform.position}, localScale={root.transform.localScale}");
                FileLogger.Log($"World corners: BL={worldCorners[0]}, TR={worldCorners[2]}, " +
                              $"Root world pos={root.transform.position}, localScale={root.transform.localScale}", "CrewHUD");
            }
            
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
