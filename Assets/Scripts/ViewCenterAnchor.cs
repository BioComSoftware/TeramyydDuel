using UnityEngine;

// Keeps this transform positioned at the geometric center of a model hierarchy
// by computing the combined bounds of all child Renderers (or Colliders as a fallback).
// Place this as a child of your ship root, assign sourceRoot to the ship root, and
// set CameraViewManager.followTarget to this object so cameras orbit the true center.
[ExecuteAlways]
public class ViewCenterAnchor : MonoBehaviour
{
    [Tooltip("Root to scan for bounds. If null, uses this object's parent; if no parent, uses this object.")]
    public Transform sourceRoot;

    [Tooltip("Apply an additional local-space offset after centering (useful to bias framing).")]
    public Vector3 localOffset = Vector3.zero;

    [Tooltip("Recompute every frame in Edit Mode. In Play Mode this always updates on Start and when calling RecenterNow().")]
    public bool updateContinuouslyInEditMode = true;

    void OnEnable()
    {
        RecenterNow();
    }

    void Start()
    {
        // Ensure correct at runtime start
        RecenterNow();
    }

    void Update()
    {
        if (!Application.isPlaying && updateContinuouslyInEditMode)
        {
            RecenterNow();
        }
    }

    public void RecenterNow()
    {
        Transform root = sourceRoot != null ? sourceRoot : (transform.parent != null ? transform.parent : transform);
        if (root == null) return;

        // Try Renderers first
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        Bounds b;
        bool hasBounds = false;
        if (renderers != null && renderers.Length > 0)
        {
            b = new Bounds(renderers[0].bounds.center, Vector3.zero);
            for (int i = 0; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            hasBounds = true;
        }
        else
        {
            // Fallback to Colliders
            var colliders = root.GetComponentsInChildren<Collider>(true);
            if (colliders != null && colliders.Length > 0)
            {
                b = new Bounds(colliders[0].bounds.center, Vector3.zero);
                for (int i = 0; i < colliders.Length; i++) b.Encapsulate(colliders[i].bounds);
                hasBounds = true;
            }
            else
            {
                return; // nothing to center on
            }
        }

        if (!hasBounds) return;

        // Place this transform at the bounds center with optional local offset
        Vector3 worldCenter = b.center;
        Vector3 localCenter = root.InverseTransformPoint(worldCenter);
        Vector3 desiredLocal = localCenter + localOffset;
        Vector3 desiredWorld = root.TransformPoint(desiredLocal);
        transform.position = desiredWorld;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.DrawSphere(transform.position, 0.15f);
        Gizmos.DrawLine(transform.position + Vector3.left * 0.5f, transform.position + Vector3.right * 0.5f);
        Gizmos.DrawLine(transform.position + Vector3.forward * 0.5f, transform.position + Vector3.back * 0.5f);
        Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, transform.position + Vector3.down * 0.5f);
    }
}
