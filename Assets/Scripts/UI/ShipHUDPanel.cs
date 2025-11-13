using UnityEngine;
using UnityEngine.UI;

// Place this on your HUD Canvas (Screen Space - Overlay). It renders one ship representation
// provided by ShipHUDRepresentation on the active Ship.
[RequireComponent(typeof(Canvas))]
public class ShipHUDPanel : MonoBehaviour
{
    private static ShipHUDPanel _instance;
    public static ShipHUDPanel InstanceOrFind()
    {
        if (_instance != null) return _instance;
        _instance = FindObjectOfType<ShipHUDPanel>(includeInactive: true);
        return _instance;
    }

    [Header("UI Elements (created if null)")]
    public RectTransform container; // parent under the Canvas
    public Image image;             // image displaying the ship sprite

    [Header("Defaults")] 
    public Color fallbackColor = Color.white;

    private ShipHUDRepresentation _current;

    void Awake()
    {
        if (_instance == null) _instance = this;
        EnsureUI();
    }

    void EnsureUI()
    {
        if (container == null)
        {
            var go = new GameObject("ShipHUD_Container", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            container = go.GetComponent<RectTransform>();
            container.anchorMin = new Vector2(1f, 0.5f); // default CenterRight
            container.anchorMax = new Vector2(1f, 0.5f);
            container.pivot = new Vector2(1f, 0.5f);
            container.anchoredPosition = new Vector2(-30f, 0f);
            container.sizeDelta = new Vector2(200f, 200f);
        }
        if (image == null)
        {
            var go = new GameObject("ShipHUD_Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(container, false);
            image = go.GetComponent<Image>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            image.color = fallbackColor;
        }
    }

    public void Show(ShipHUDRepresentation rep)
    {
        EnsureUI();
        _current = rep;
        Apply(rep);
    }

    public void HideIfShowing(ShipHUDRepresentation rep)
    {
        if (_current == rep)
        {
            _current = null;
            if (image != null) image.enabled = false;
        }
    }

    void LateUpdate()
    {
        if (_current != null) Apply(_current);
    }

    void Apply(ShipHUDRepresentation rep)
    {
        if (rep == null || image == null || container == null) return;

        // Sprite + opacity
        image.enabled = rep.hudSprite != null;
        image.sprite = rep.hudSprite;
        var col = image.color; col.a = Mathf.Clamp01(rep.opacity); image.color = col;

        // Layout from anchor + offset + size
        Vector2 anchorMin, anchorMax, pivot;
        MapAnchor(rep.anchor, out anchorMin, out anchorMax, out pivot);
        container.anchorMin = anchorMin;
        container.anchorMax = anchorMax;
        container.pivot = pivot;
        container.anchoredPosition = rep.anchoredOffset;
        container.sizeDelta = rep.size;
    }

    static void MapAnchor(ShipHUDRepresentation.HudAnchor a, out Vector2 min, out Vector2 max, out Vector2 pivot)
    {
        switch (a)
        {
            case ShipHUDRepresentation.HudAnchor.TopLeft:      min = max = pivot = new Vector2(0f, 1f); break;
            case ShipHUDRepresentation.HudAnchor.TopCenter:    min = max = pivot = new Vector2(0.5f, 1f); break;
            case ShipHUDRepresentation.HudAnchor.TopRight:     min = max = pivot = new Vector2(1f, 1f); break;
            case ShipHUDRepresentation.HudAnchor.CenterLeft:   min = max = pivot = new Vector2(0f, 0.5f); break;
            case ShipHUDRepresentation.HudAnchor.Center:       min = max = pivot = new Vector2(0.5f, 0.5f); break;
            case ShipHUDRepresentation.HudAnchor.CenterRight:  min = max = pivot = new Vector2(1f, 0.5f); break;
            case ShipHUDRepresentation.HudAnchor.BottomLeft:   min = max = pivot = new Vector2(0f, 0f); break;
            case ShipHUDRepresentation.HudAnchor.BottomCenter: min = max = pivot = new Vector2(0.5f, 0f); break;
            case ShipHUDRepresentation.HudAnchor.BottomRight:  min = max = pivot = new Vector2(1f, 0f); break;
            default: min = max = pivot = new Vector2(1f, 0.5f); break;
        }
    }
}

