using System;
using System.Collections.Generic;
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

    [Header("Mount Discovery")]
    [Tooltip("Optional: where to search for mount components. If not set, the system searches from this object's scene root. Set this to your Ship root if ShipHUDRepresentation is not on the Ship root.")]
    public Transform mountSearchRoot;

    [Header("Markers (mounts on HUD)")]
    [Tooltip("Default sprite used when a mount is empty (can be overridden per marker).")]
    public Sprite defaultEmptySprite;
    [Tooltip("Markers to render over the ship icon. Choose coordinate mode per marker (Normalized or Pixels).")]
    public List<MountMarker> markers = new List<MountMarker>();

    [Header("Type → Sprite Mapping (populated)")]
    [Tooltip("Map weapon type keys (e.g., 'cannon') to sprites for populated mounts.")]
    public List<WeaponTypeSprite> typeSprites = new List<WeaponTypeSprite>();

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

[Serializable]
public class MountMarker
{
    [Tooltip("Must match the mountId on a WeaponMount / ProjectileLauncherMount in the Ship hierarchy")] public string mountId;
    public enum CoordMode { Normalized01, Pixels }
    [Tooltip("How to interpret the position values: Normalized 0..1 or Pixels (anchored from center).")]
    public CoordMode coordMode = CoordMode.Normalized01;
    [Tooltip("Normalized position (0..1) within the ship image: x=0 left..1 right, y=0 bottom..1 top")] public Vector2 pos01 = new Vector2(0.5f, 0.5f);
    [Tooltip("Pixel offset from the center of the ship image (x right, y up). Only used if coordMode=Pixels.")]
    public Vector2 posPx = Vector2.zero;
    [Tooltip("Optional pixel size override (leave 0 for default)")] public Vector2 sizePx = Vector2.zero;
    [Tooltip("Optional sprite shown when this mount is empty (falls back to representation default)")] public Sprite emptySprite;
    [Tooltip("Optional sprite shown when the mount is occupied; if not set, the Type→Sprite mapping is used.")]
    public Sprite populatedSprite;
}

[Serializable]
public class WeaponTypeSprite
{
    public string typeKey;
    public Sprite sprite;
}
