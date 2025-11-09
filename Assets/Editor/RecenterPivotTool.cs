#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Utility to recenter the pivot (Transform position) of selected root GameObjects
/// to the combined bounds center of their visible / collidable geometry without
/// visually moving them in the scene.
///
/// How it works:
/// 1. Compute combined bounds (Renderers + Colliders) in world space.
/// 2. Determine offset = bounds.center - root.position.
/// 3. For each child (including nested), adjust localPosition to subtract the offset transformed
///    into that child's parent space.
/// 4. Move root Transform by +offset. World positions of children remain effectively unchanged,
///    but the root pivot is now centered.
///
/// Edge cases:
/// - If no renderers/colliders are found, operation is skipped.
/// - Multiple selection supported.
/// - Undo recorded for full hierarchy move.
/// </summary>
public static class RecenterPivotTool
{
    private const string MenuPath = "Tools/Pivot/Recenter Selected Root Pivot To Bounds Center";

    [MenuItem(MenuPath, priority = 1000)]
    private static void RecenterSelected()
    {
        var selection = Selection.gameObjects;
        if (selection == null || selection.Length == 0)
        {
            Debug.LogWarning("Recenter Pivot: No GameObjects selected.");
            return;
        }

        foreach (var root in selection)
        {
            RecenterRootPivot(root);
        }
    }

    [MenuItem(MenuPath, validate = true)]
    private static bool ValidateRecenterSelected()
    {
        return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
    }

    public static void RecenterRootPivot(GameObject root)
    {
        // Compute combined bounds of renderers & colliders under root.
        var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
        var colliders = root.GetComponentsInChildren<Collider>(includeInactive: true);

        if (renderers.Length == 0 && colliders.Length == 0)
        {
            Debug.LogWarning($"Recenter Pivot: '{root.name}' has no renderers or colliders; skipping.");
            return;
        }

        Bounds? maybeBounds = null;
        foreach (var r in renderers)
        {
            if (maybeBounds == null) maybeBounds = r.bounds; else maybeBounds = Encapsulate(maybeBounds.Value, r.bounds);
        }
        foreach (var c in colliders)
        {
            if (maybeBounds == null) maybeBounds = c.bounds; else maybeBounds = Encapsulate(maybeBounds.Value, c.bounds);
        }

        var combinedBounds = maybeBounds.Value;
        var offset = combinedBounds.center - root.transform.position; // world space offset
        if (offset.sqrMagnitude < 1e-10f)
        {
            Debug.Log($"Recenter Pivot: '{root.name}' already centered.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(root, "Recenter Pivot");

        // Safer algorithm using a temporary container to preserve world space exactly
        // even with complex rotations/scales: reparent, offset container, move root, restore.
        var originalChildren = new List<Transform>();
        for (int i = 0; i < root.transform.childCount; i++)
        {
            originalChildren.Add(root.transform.GetChild(i));
        }

        var temp = new GameObject("__PivotTempContents");
        Undo.RegisterCreatedObjectUndo(temp, "Recenter Pivot");
        temp.transform.SetPositionAndRotation(root.transform.position, root.transform.rotation);
        temp.transform.localScale = Vector3.one;
        temp.transform.SetParent(root.transform, worldPositionStays: true);

        // Move all current children under the temp container, keeping their world transforms intact.
        foreach (var child in originalChildren)
        {
            child.SetParent(temp.transform, worldPositionStays: true);
        }

        // Offset the container opposite to the desired root movement (in root local space)
        var localOffset = root.transform.InverseTransformVector(offset);
        temp.transform.localPosition -= localOffset;

        // Move the root itself to the new pivot position.
        root.transform.position += offset;

        // Restore children back to the root (keep world positions), then remove temp container.
        foreach (var child in originalChildren)
        {
            child.SetParent(root.transform, worldPositionStays: true);
        }
        Undo.DestroyObjectImmediate(temp);

        Debug.Log($"Recenter Pivot: '{root.name}' pivot moved by {offset} to bounds center at {combinedBounds.center}.");
    }

    private static void CollectChildren(Transform parent, List<Transform> list)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            list.Add(c);
            CollectChildren(c, list);
        }
    }

    private static Bounds Encapsulate(Bounds a, Bounds b)
    {
        a.Encapsulate(b.min);
        a.Encapsulate(b.max);
        return a;
    }
}

/// <summary>
/// Alternative helper component: Attach to root, right-click context menu to recenter.
/// </summary>
public class RecenterPivotHelper : MonoBehaviour
{
    [ContextMenu("Recenter Pivot To Bounds Center")] 
    private void RecenterViaContextMenu()
    {
#if UNITY_EDITOR
        RecenterPivotTool.RecenterRootPivot(gameObject);
#endif
    }
#if UNITY_EDITOR
    private static void Collect(Transform p, List<Transform> list)
    {
        for (int i = 0; i < p.childCount; i++) { var c = p.GetChild(i); list.Add(c); Collect(c, list); }
    }
    private static Bounds Enc(Bounds a, Bounds b) { a.Encapsulate(b.min); a.Encapsulate(b.max); return a; }
#endif
}
#endif
 