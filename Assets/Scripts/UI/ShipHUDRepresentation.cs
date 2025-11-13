using UnityEngine;

// Attach this to the Ship root to describe how the ship should appear on the HUD.
// This component does not draw by itself; it registers with ShipHUDPanel (on the HUD Canvas)
// which renders a simple Image at the requested anchor/offset/size.
[DisallowMultipleComponent]
public class ShipHUDRepresentation : MonoBehaviour
{
    public enum HudAnchor
    {
        TopLeft, TopCenter, TopRight,
        CenterLeft, Center, CenterRight,
        BottomLeft, BottomCenter, BottomRight
    }

    [Header("Visual")]
    [Tooltip("Sprite used to represent this ship on the HUD. Can be a silhouette or simple icon.")]
    public Sprite hudSprite;
    [Range(0f,1f)] public float opacity = 1f;

    [Header("Layout")]
    [Tooltip("Where on the screen to place the representation. Default is CenterRight per design.")]
    public HudAnchor anchor = HudAnchor.CenterRight;
    [Tooltip("Pixel offset from the chosen anchor (x=right, y=up).")]
    public Vector2 anchoredOffset = new Vector2(-30f, 0f);
    [Tooltip("Size in pixels of the representation on the HUD.")]
    public Vector2 size = new Vector2(200f, 200f);

    void OnEnable()
    {
        var panel = ShipHUDPanel.InstanceOrFind();
        if (panel != null) panel.Show(this);
    }

    void OnDisable()
    {
        var panel = ShipHUDPanel.InstanceOrFind();
        if (panel != null) panel.HideIfShowing(this);
    }
}

