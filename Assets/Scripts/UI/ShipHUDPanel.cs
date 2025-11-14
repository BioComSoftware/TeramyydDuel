using System;
using System.Collections.Generic;
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
    public RectTransform markersRoot; // parent for marker images

    [Header("Defaults")] 
    public Color fallbackColor = Color.white;

    [Header("Debug")]
    public bool debugLog = false;

    private ShipHUDRepresentation _current;
    private readonly Dictionary<string, Image> _markerImages = new Dictionary<string, Image>();

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

        if (markersRoot == null)
        {
            var go = new GameObject("ShipHUD_Markers", typeof(RectTransform));
            go.transform.SetParent(container, false);
            markersRoot = go.GetComponent<RectTransform>();
            markersRoot.anchorMin = Vector2.zero;
            markersRoot.anchorMax = Vector2.one;
            markersRoot.offsetMin = Vector2.zero;
            markersRoot.offsetMax = Vector2.zero;
        }
    }

    public void Show(ShipHUDRepresentation rep)
    {
        EnsureUI();
        _current = rep;
        Apply(rep);
        RebuildMarkers(rep);
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
        if (_current != null)
        {
            Apply(_current);
            RebuildMarkers(_current); // cheap UI update per frame; markers are reused
        }
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

    void RebuildMarkers(ShipHUDRepresentation rep)
    {
        if (rep == null || markersRoot == null) return;

        // Build a lookup of live mounts by id → (occupied, typeKey)
        Transform searchRoot = rep.mountSearchRoot != null ? rep.mountSearchRoot : rep.transform.root;
        var mounts1 = searchRoot.GetComponentsInChildren<WeaponMount>(true);
        var mounts2 = searchRoot.GetComponentsInChildren<ProjectileLauncherMount>(true);
        var mountInfo = new Dictionary<string, (bool occupied, string typeKey)>();
        
        if (debugLog) Debug.Log($"[HUD] RebuildMarkers: searching from '{GetHierarchyPath(searchRoot)}', found {mounts1.Length} WeaponMounts, {mounts2.Length} ProjectileLauncherMounts");
        
        foreach (var m in mounts1)
        {
            if (string.IsNullOrEmpty(m.mountId)) continue;
            
            if (debugLog)
            {
                Debug.Log($"[HUD] Checking WeaponMount '{m.mountId}' @ '{GetHierarchyPath(m.transform)}'");
                Debug.Log($"[HUD]   isOccupied={m.isOccupied}, currentLauncher={m.currentLauncher}");
                if (m.yawBase != null) Debug.Log($"[HUD]   yawBase @ '{GetHierarchyPath(m.yawBase)}'");
                if (m.pitchBarrel != null) Debug.Log($"[HUD]   pitchBarrel @ '{GetHierarchyPath(m.pitchBarrel)}'");
            }
            
            // Occupancy detection: prefer explicit flags, else probe children for a ProjectileLauncher placed manually
            bool occ = m.isOccupied || (m.currentLauncher != null);
            ProjectileLauncher pl = m.currentLauncher;
            if (!occ)
            {
                // Try searching from the mount itself
                pl = m.GetComponentInChildren<ProjectileLauncher>(true);
                if (debugLog) Debug.Log($"[HUD]   Search mount children: found={pl}");
                
                if (pl == null && m.pitchBarrel != null)
                {
                    // If pitchBarrel is set but not a child, search it directly
                    pl = m.pitchBarrel.GetComponentInChildren<ProjectileLauncher>(true);
                    if (debugLog) Debug.Log($"[HUD]   Search pitchBarrel children: found={pl}");
                }
                if (pl == null && m.yawBase != null)
                {
                    // If yawBase is set but not a child, search it directly
                    pl = m.yawBase.GetComponentInChildren<ProjectileLauncher>(true);
                    if (debugLog) Debug.Log($"[HUD]   Search yawBase children: found={pl}");
                }
                if (pl != null) occ = true;
            }

            string typeKey = ResolveTypeKey(
                declaredType: m.mountType,
                launcherObject: pl != null ? pl.gameObject : null,
                launcherTypeName: pl != null ? pl.GetType().Name : null
            );
            
            // Warn about duplicate mount IDs
            if (mountInfo.ContainsKey(m.mountId))
            {
                Debug.LogWarning($"[HUD] Duplicate mountId '{m.mountId}' found! Mount @ '{GetHierarchyPath(m.transform)}' will overwrite previous entry. Each mount should have a unique ID.");
            }
            
            mountInfo[m.mountId] = (occ, typeKey);
            if (debugLog) Debug.Log($"[HUD] → Result: occupied={occ}, typeKey='{typeKey}'");
        }
        foreach (var m in mounts2)
        {
            if (string.IsNullOrEmpty(m.mountId)) continue;
            // Occupancy detection: explicit fields or fallback to finding a ProjectileLauncher under this mount
            bool occ = m.isOccupied || (m.currentObject != null) || (m.currentLauncher != null);
            ProjectileLauncher pl = m.currentLauncher;
            if (!occ)
            {
                pl = m.GetComponentInChildren<ProjectileLauncher>(true);
                if (pl != null) occ = true;
            }
            GameObject obj = (m.currentObject != null) ? m.currentObject : (pl != null ? pl.gameObject : null);

            string typeKey = ResolveTypeKey(
                declaredType: m.acceptedType,
                launcherObject: obj,
                launcherTypeName: pl != null ? pl.GetType().Name : null
            );
            mountInfo[m.mountId] = (occ, typeKey);
            if (debugLog) Debug.Log($"[HUD] ProjectileLauncherMount '{m.mountId}': occupied={occ}, typeKey='{typeKey}', isOccupied={m.isOccupied}, currentLauncher={m.currentLauncher}");
        }

        // Create or update marker images
        if (debugLog) Debug.Log($"[HUD] RebuildMarkers: {rep.markers.Count} markers defined, {mountInfo.Count} mounts found");
        
        if (rep.markers.Count == 0)
        {
            Debug.LogWarning($"[HUD] ShipHUDRepresentation on '{rep.gameObject.name}' has NO markers defined! Add markers in the inspector to show mount icons on the HUD.");
        }
        
        for (int i = 0; i < rep.markers.Count; i++)
        {
            var mk = rep.markers[i];
            if (debugLog) Debug.Log($"[HUD] Processing marker {i}: mountId='{mk.mountId}'");
            if (string.IsNullOrEmpty(mk.mountId)) continue;
            Image img;
            if (!_markerImages.TryGetValue(mk.mountId, out img))
            {
                var go = new GameObject($"Marker_{mk.mountId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(markersRoot, false);
                img = go.GetComponent<Image>();
                _markerImages[mk.mountId] = img;
            }

            // Choose sprite based on occupancy and type
            bool isOcc = mountInfo.TryGetValue(mk.mountId, out var info) && info.occupied;
            Sprite spr = null;
            if (isOcc)
            {
                // 1) Per-marker override if provided
                if (mk.populatedSprite != null)
                {
                    spr = mk.populatedSprite;
                }
                else
                {
                    // 2) Find a sprite by resolved type key (robust against (Clone) suffix and casing)
                    spr = GetSpriteForType(rep, info.typeKey);
                }
                if (debugLog) Debug.Log($"[HUD] Marker '{mk.mountId}' OCCUPIED: typeKey='{info.typeKey}', sprite={spr}");
            }
            else
            {
                spr = mk.emptySprite != null ? mk.emptySprite : rep.defaultEmptySprite;
                if (debugLog) Debug.Log($"[HUD] Marker '{mk.mountId}' EMPTY: sprite={spr}");
            }
            img.sprite = spr;
            img.enabled = spr != null; // if no sprite, hide

            // Position/size inside container using selected coordinate mode
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            var size = mk.sizePx.sqrMagnitude > 0.0001f ? mk.sizePx : new Vector2(20f, 20f);
            rt.sizeDelta = size;

            // Convert to anchored offset, origin at container center
            var csize = container.sizeDelta;
            Vector2 anchored;
            if (mk.coordMode == MountMarker.CoordMode.Normalized01)
            {
                // Clamp to [0,1] to avoid accidental off-rect values (e.g., y=2)
                float nx = Mathf.Clamp01(mk.pos01.x);
                float ny = Mathf.Clamp01(mk.pos01.y);
                float ax = (nx - 0.5f) * csize.x;
                float ay = (ny - 0.5f) * csize.y;
                anchored = new Vector2(ax, ay);
            }
            else // Pixels
            {
                anchored = mk.posPx;
            }
            rt.anchoredPosition = anchored;
        }
    }

    // --- Helpers ---
    static string GetHierarchyPath(Transform t)
    {
        if (t == null) return "null";
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    static string CleanKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        var k = key.Replace("(Clone)", string.Empty).Trim();
        return k;
    }

    static string ResolveTypeKey(string declaredType, GameObject launcherObject, string launcherTypeName)
    {
        // Priority: declared mount type → prefab name → component type name
        string k = declaredType;
        if (string.IsNullOrEmpty(k) && launcherObject != null) k = launcherObject.name;
        if (string.IsNullOrEmpty(k) && !string.IsNullOrEmpty(launcherTypeName)) k = launcherTypeName;
        return CleanKey(k);
    }

    static Sprite GetSpriteForType(ShipHUDRepresentation rep, string typeKey)
    {
        if (rep == null || rep.typeSprites == null) return null;
        var k1 = CleanKey(typeKey);
        for (int t = 0; t < rep.typeSprites.Count; t++)
        {
            var map = rep.typeSprites[t];
            if (map == null || map.sprite == null) continue;
            var mk = CleanKey(map.typeKey);
            if (string.Equals(mk, k1, StringComparison.OrdinalIgnoreCase)) return map.sprite;
        }
        // Debug: list all available mappings if not found
        Debug.LogWarning($"[HUD] No sprite found for typeKey '{typeKey}' (cleaned: '{k1}'). Available mappings: {string.Join(", ", rep.typeSprites.ConvertAll(m => $"'{m.typeKey}'"))}");
        return null;
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
