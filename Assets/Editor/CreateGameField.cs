#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class CreateGameField
{
    private const string MenuPath = "Tools/Playfield/Create 1000x1000x1000 Game Field";

    [MenuItem(MenuPath)]
    public static void Create()
    {
        const float sizeX = 1000f, sizeY = 1000f, sizeZ = 1000f;
        const float halfX = sizeX * 0.5f, halfZ = sizeZ * 0.5f;
        // Root logical bounds only (no colliders, no renderers)
        var root = new GameObject("GameField_Logical");
        Undo.RegisterCreatedObjectUndo(root, "Create Logical Game Field");
        var bounds = root.AddComponent<GameFieldBounds>();
        bounds.size = new Vector3(sizeX, sizeY, sizeZ);
        bounds.drawGroundGizmo = true; // optional visual guide

        Selection.activeGameObject = root;
    }

    // (Removed physical wall creation; logical bounds rely purely on numeric checks.)
}
#endif
