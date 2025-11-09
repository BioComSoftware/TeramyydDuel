#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class AddVisualGround
{
    private const string MenuPath = "Tools/Playfield/Add Visual Ground (Grass)";

    [MenuItem(MenuPath)]
    public static void CreateVisualGround()
    {
        var field = GetOrFindField();
        if (field == null)
        {
            EditorUtility.DisplayDialog("Add Visual Ground",
                "No GameField found in the scene. Use Tools > Playfield > Create 1000x1000x1000 Game Field first.",
                "OK");
            return;
        }

        // Create or find texture and material
        string texturesDir = "Assets/Textures";
        string materialsDir = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(texturesDir)) AssetDatabase.CreateFolder("Assets", "Textures");
        if (!AssetDatabase.IsValidFolder(materialsDir)) AssetDatabase.CreateFolder("Assets", "Materials");

        string texPath = Path.Combine(texturesDir, "GrassGenerated.png");
        string matPath = Path.Combine(materialsDir, "GrassGenerated.mat");

        // Prefer using a user-selected texture if available; otherwise generate one.
        Texture2D grassTex = Selection.activeObject as Texture2D;
        if (grassTex == null)
        {
            grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (grassTex == null)
            {
                grassTex = GenerateGrassTexture(1024, 1024);
                // Save PNG
                byte[] png = grassTex.EncodeToPNG();
                File.WriteAllBytes(texPath, png);
                AssetDatabase.ImportAsset(texPath);
                // Set texture import settings
                var ti = (TextureImporter)TextureImporter.GetAtPath(texPath);
                if (ti != null)
                {
                    ti.mipmapEnabled = true;
                    ti.wrapMode = TextureWrapMode.Clamp; // single stretched image across the field
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    ti.sRGBTexture = true;
                    ti.SaveAndReimport();
                }
                grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            }
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null)
            {
                EditorUtility.DisplayDialog("Add Visual Ground", "Could not find a Lit or Standard shader.", "OK");
                return;
            }
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        // Assign texture to the proper slot depending on shader
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", grassTex);
            mat.SetTextureScale("_BaseMap", Vector2.one); // no tiling
        }
        if (mat.HasProperty("_MainTex"))
        {
            mat.SetTexture("_MainTex", grassTex);
            mat.SetTextureScale("_MainTex", Vector2.one);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.8f, 1f, 0.8f, 1f));
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.8f, 1f, 0.8f, 1f));

        // Create or reuse a VisualGround under the field root
        Transform root = field.transform; // logical field object
        var existing = root.Find("VisualGround");
        GameObject groundGO;
        if (existing == null)
        {
            groundGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            groundGO.name = "VisualGround";
            Undo.RegisterCreatedObjectUndo(groundGO, "Create Visual Ground");
            groundGO.transform.SetParent(root, false);
            // Lay flat on Y=0 (Quad faces +Z by default)
            groundGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            groundGO.transform.position = new Vector3(0f, 0f, 0f);
            // Remove collider if any (Quad has MeshCollider in newer Unity versions only if you add it)
            var col = groundGO.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }
        else
        {
            groundGO = existing.gameObject;
        }

        // Size the quad to field size
        Vector3 size = field.size;
        // Quad base size is 1x1, localScale sets world size directly (ignoring rotation)
        groundGO.transform.localScale = new Vector3(size.x, size.z, 1f);

        // Assign material
        var renderer = groundGO.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = mat;

        Selection.activeGameObject = groundGO;
    }

    private static GameFieldBounds GetOrFindField()
    {
        var inst = GameFieldBounds.Instance;
        if (inst != null) return inst;
#if UNITY_2023_1_OR_NEWER
        var arr = Object.FindObjectsByType<GameFieldBounds>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return (arr != null && arr.Length > 0) ? arr[0] : null;
#else
        return Object.FindObjectOfType<GameFieldBounds>();
#endif
    }

    private static Texture2D GenerateGrassTexture(int width, int height)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        tex.name = "GrassGenerated";

        // Simple procedural: soft noise-based green texture
        Color c1 = new Color(0.20f, 0.55f, 0.20f); // darker
        Color c2 = new Color(0.35f, 0.75f, 0.35f); // lighter
        var rnd = new System.Random(1337);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // low-frequency blotchy noise
                float nx = (float)x / width;
                float ny = (float)y / height;
                float f = Mathf.PerlinNoise(nx * 6.3f, ny * 6.3f);
                // small random grain
                float g = (float)rnd.NextDouble() * 0.1f;
                float t = Mathf.Clamp01(f * 0.8f + g * 0.2f);
                Color col = Color.Lerp(c1, c2, t);
                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply();
        return tex;
    }
}
#endif
