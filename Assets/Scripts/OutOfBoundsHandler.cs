using UnityEngine;

// Attach to player-controlled entities (e.g., Ship). Periodically checks if they remain
// inside the GameFieldBounds. If they cross any boundary (x/z lateral walls, below ground, or above ceiling)
// they are considered eliminated. Optionally offers grace period and visual warning.
public class OutOfBoundsHandler : MonoBehaviour
{
    [Header("Elimination Settings")] 
    [Tooltip("Seconds the player can remain out of bounds before elimination. 0 = immediate.")] public float graceSeconds = 0f;
    [Tooltip("If true, we clamp vertical position to ceiling rather than instant eliminate when exceeding ceiling.")] public bool clampCeilingInsteadOfEliminate = false;

    [Header("Warning UI (Optional)")] 
    public string warningMessage = "WARNING: Leaving Playfield!";

    private float outOfBoundsTimer = 0f;
    private bool wasOutLastFrame = false;

    void Update()
    {
        var field = GameFieldBounds.Instance;
        if (field == null) return; // Field not set yet

        bool inside = field.ContainsPoint(transform.position);
        if (inside)
        {
            outOfBoundsTimer = 0f;
            wasOutLastFrame = false;
            return;
        }

        // If only above ceiling and clamping is enabled, clamp and treat as inside.
        if (clampCeilingInsteadOfEliminate && transform.position.y > field.size.y)
        {
            Vector3 p = transform.position;
            p.y = field.size.y;
            transform.position = p;
            outOfBoundsTimer = 0f;
            wasOutLastFrame = false;
            return;
        }

        // Out of bounds logic
        if (!wasOutLastFrame)
        {
            wasOutLastFrame = true;
            outOfBoundsTimer = 0f;
            // TODO: Hook to UI / HUD to display warningMessage
            Debug.Log(warningMessage);
        }

        outOfBoundsTimer += Time.deltaTime;
        if (outOfBoundsTimer >= graceSeconds)
        {
            Eliminate();
        }
    }

    private void Eliminate()
    {
        Debug.Log($"{name} eliminated for leaving playfield.");
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnPlayerDestroyed();
        }
        // For now, destroy object.
        Destroy(gameObject);
    }
}
