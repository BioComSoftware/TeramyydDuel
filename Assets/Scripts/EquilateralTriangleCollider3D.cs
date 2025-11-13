using UnityEngine;

// Procedurally builds an equilateral triangular prism and assigns it to a MeshCollider.
// Useful when you need a simple triangle-shaped 3D collider.
// - Side length controls the equilateral edge length in the XY plane.
// - Thickness is the Z depth (small but non-zero for robust collisions).
// - MeshCollider is set to convex (recommended when a Rigidbody is present).
// Add this to any GameObject; adjust in Inspector. Rebuilds on validate.
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshCollider))]
public class EquilateralTriangleCollider3D : MonoBehaviour
{
    [Header("Geometry")]
    [Min(0.0001f)] public float sideLength = 1f;   // legacy: equilateral edge length (used if width/length not set)
    [Tooltip("Base width (X distance between the two base vertices). If > 0 together with Length, overrides Side Length.")]
    [Min(0.0001f)] public float width = 1f;
    [Tooltip("Triangle height (Y distance from base line to apex). If > 0 together with Width, overrides Side Length.")]
    [Min(0.0001f)] public float length = 0.8660254f; // default sqrt(3)/2 for sideLength=1
    [Min(0.0001f)] public float thickness = 0.05f; // prism thickness (depth along Z)
    [Tooltip("If true, Z+ is the outward normal of the front face; otherwise Z-")]
    public bool frontFaceZPositive = true;

    [Tooltip("Local-space offset applied to the collider mesh center (useful for nudging without moving the GameObject).")]
    public Vector3 centerOffset = Vector3.zero;

    [Tooltip("Local-space rotation (degrees) applied to the generated mesh. Defaults to 90 on X to lay triangle upright if needed.")]
    public Vector3 rotationEuler = new Vector3(0f, 0f, 0f);

    [Header("Collider")]
    [Tooltip("Convex is required if a Rigidbody is on the same object.")]
    public bool convex = true;

    private MeshCollider meshCollider;
    private Mesh runtimeMesh;

    void Reset() { Build(); }
    void Awake() { Build(); }
    void OnValidate() { Build(); }

    void OnDestroy()
    {
        if (Application.isPlaying && runtimeMesh != null)
        {
            Destroy(runtimeMesh);
        }
    }

    private void Build()
    {
        if (!isActiveAndEnabled) return;

        if (meshCollider == null) meshCollider = GetComponent<MeshCollider>();

        // Triangle in XY plane centered on origin.
        // If width/length are provided, build an isosceles triangle of that size.
        // Otherwise, fall back to equilateral using sideLength.
        float w = Mathf.Max(0.0001f, width);
        float L = Mathf.Max(0.0001f, length);
        if (Mathf.Approximately(w, 0f) || Mathf.Approximately(L, 0f))
        {
            float s = Mathf.Max(0.0001f, sideLength);
            w = s;
            L = Mathf.Sqrt(3f) * 0.5f * s;
        }

        // Center triangle so centroid is at (0,0,0). Centroid is at 1/3 of height from base.
        Vector3 v0 = new Vector3(-w * 0.5f, -L / 3f, 0f); // base left
        Vector3 v1 = new Vector3( w * 0.5f, -L / 3f, 0f); // base right
        Vector3 v2 = new Vector3( 0f,        2f * L / 3f, 0f); // apex

        float halfT = Mathf.Max(0.00005f, thickness * 0.5f);
        float zFront = frontFaceZPositive ? +halfT : -halfT;
        float zBack  = frontFaceZPositive ? -halfT : +halfT;

        // Front and back vertices
        Vector3 v0f = new Vector3(v0.x, v0.y, zFront);
        Vector3 v1f = new Vector3(v1.x, v1.y, zFront);
        Vector3 v2f = new Vector3(v2.x, v2.y, zFront);
        Vector3 v0b = new Vector3(v0.x, v0.y, zBack);
        Vector3 v1b = new Vector3(v1.x, v1.y, zBack);
        Vector3 v2b = new Vector3(v2.x, v2.y, zBack);

        // Build prism: front, back (reversed winding), and three side quads split into tris
        Vector3[] verts = new Vector3[]
        {
            // front
            v0f, v1f, v2f,           // 0,1,2
            // back (reverse order for outward normals)
            v0b, v2b, v1b,           // 3,4,5
            // side 0-1 (v0->v1)
            v0f, v1f, v1b, v0b,      // 6..9
            // side 1-2 (v1->v2)
            v1f, v2f, v2b, v1b,      // 10..13
            // side 2-0 (v2->v0)
            v2f, v0f, v0b, v2b       // 14..17
        };

        // Apply local rotation and offset to vertices
        if (rotationEuler != Vector3.zero || centerOffset != Vector3.zero)
        {
            Quaternion rot = Quaternion.Euler(rotationEuler);
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = rot * verts[i] + centerOffset;
            }
        }

        int[] tris = new int[]
        {
            // front
            0,1,2,
            // back
            3,4,5,
            // side 0-1 (two tris)
            6,7,8,
            6,8,9,
            // side 1-2
            10,11,12,
            10,12,13,
            // side 2-0
            14,15,16,
            14,16,17
        };

        if (runtimeMesh == null)
        {
            runtimeMesh = new Mesh();
            runtimeMesh.name = "EquilateralTrianglePrism_Collider";
        }
        else
        {
            runtimeMesh.Clear();
        }

        runtimeMesh.SetVertices(verts);
        runtimeMesh.SetTriangles(tris, 0);
        runtimeMesh.RecalculateNormals();
        runtimeMesh.RecalculateBounds();

        meshCollider.sharedMesh = runtimeMesh;
        meshCollider.convex = convex;
    }
}
