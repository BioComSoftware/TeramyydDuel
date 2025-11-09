using UnityEngine;

// Defines a cubic playfield with ground at y=0 and ceiling at size.y.
// Default size is 1000 x 1000 x 1000 units. The cube is centered around XZ origin
// (x in [-size.x/2, +size.x/2], z in [-size.z/2, +size.z/2]) and spans vertically from 0..size.y.
// Provides helpers to test containment and draws editor gizmos.
[ExecuteAlways]
public class GameFieldBounds : MonoBehaviour
{
    public static GameFieldBounds Instance { get; private set; }

    [Header("Playfield Size (units)")]
    public Vector3 size = new Vector3(1000f, 1000f, 1000f);

    [Header("Visualization (Editor Only)")] 
    public Color wireColor = new Color(0.2f, 1f, 0.2f, 1f);
    [Tooltip("Draw a thin translucent ground surface gizmo for reference only.")]
    public bool drawGroundGizmo = false;
    public Color groundColor = new Color(0.2f, 0.7f, 0.2f, 0.15f);

    // Bottom is y = 0; top is y = size.y. For convenience, the Transform's position is ignored
    // for containment tests; the cube is always at world origin XZ and y range 0..size.y.

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        // Ensure Instance is available in edit mode too
        if (Instance == null) Instance = this;
    }

    public Bounds GetBounds()
    {
        // Center at (0, size.y/2, 0), extents size/2 except Y which is size.y/2 spanning 0..size.y
        Vector3 center = new Vector3(0f, size.y * 0.5f, 0f);
        return new Bounds(center, new Vector3(size.x, size.y, size.z));
    }

    public bool ContainsPoint(Vector3 worldPos)
    {
        return worldPos.x >= -size.x * 0.5f && worldPos.x <= size.x * 0.5f &&
               worldPos.z >= -size.z * 0.5f && worldPos.z <= size.z * 0.5f &&
               worldPos.y >= 0f && worldPos.y <= size.y;
    }

    public Vector3 ClampPoint(Vector3 worldPos)
    {
        return new Vector3(
            Mathf.Clamp(worldPos.x, -size.x * 0.5f, size.x * 0.5f),
            Mathf.Clamp(worldPos.y, 0f, size.y),
            Mathf.Clamp(worldPos.z, -size.z * 0.5f, size.z * 0.5f)
        );
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        var prev = Gizmos.color;
        Gizmos.color = wireColor;
        var b = GetBounds();
        Gizmos.DrawWireCube(b.center, b.size);
        if (drawGroundGizmo)
        {
            Gizmos.color = groundColor;
            var groundCenter = new Vector3(0f, 0f, 0f);
            var groundSize = new Vector3(size.x, 0f, size.z);
            // Draw a thin cube to simulate solid ground gizmo
            Gizmos.DrawCube(groundCenter + Vector3.up * 0.01f, groundSize + Vector3.up * 0.02f);
        }
        Gizmos.color = prev;
    }
#endif
}
