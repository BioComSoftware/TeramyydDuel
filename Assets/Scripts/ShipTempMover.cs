using UnityEngine;

// Temporary test movement controller for the Ship.
// W: Move forward
// S: Move backward
// A: Turn left with forward arc (yaw left while still translating slightly forward)
// D: Turn right with forward arc
// This does NOT represent final gameplay controls. Remove when real systems arrive.
public class ShipTempMover : MonoBehaviour
{
    [Header("Speeds")] 
    public float forwardSpeed = 30f;          // Units per second forward/back
    public float turnSpeedDegrees = 60f;      // Degrees per second yaw
    [Tooltip("Fraction of forwardSpeed applied while turning for arc motion (0..1).")]
    public float turnForwardFactor = 0.5f;    // Forward fraction during A/D turn

    [Header("Vertical Clamp (optional)")] 
    public bool clampToGround = true;         // Keep ship at y >= groundY
    public float groundY = 0f;                // Ground plane

    [Header("Playfield Respect (optional)")] 
    public bool clampToPlayfield = true;      // Clamp within GameFieldBounds if present

    private GameFieldBounds field;

    void Start()
    {
        if (clampToPlayfield) field = GameFieldBounds.Instance;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        // INPUT
        bool w = Input.GetKey(KeyCode.W);
        bool s = Input.GetKey(KeyCode.S);
        bool a = Input.GetKey(KeyCode.A);
        bool d = Input.GetKey(KeyCode.D);

        // Forward/back movement
        float forwardDir = 0f;
        if (w) forwardDir += 1f;
        if (s) forwardDir -= 1f;
        Vector3 forwardMove = transform.forward * forwardDir * forwardSpeed * dt;

        // Turning arcs: while turning left/right we also move forward at a reduced rate
        float yawDelta = 0f;
        if (a) yawDelta -= turnSpeedDegrees * dt;
        if (d) yawDelta += turnSpeedDegrees * dt;

        if (yawDelta != 0f)
        {
            rot = Quaternion.Euler(0f, rot.eulerAngles.y + yawDelta, 0f);
            // Arc forward motion
            forwardMove += (rot * Vector3.forward) * (forwardSpeed * turnForwardFactor * dt);
        }

        pos += forwardMove;

        // Optional vertical clamp
        if (clampToGround && pos.y < groundY) pos.y = groundY;

        // Optional playfield clamp
        if (clampToPlayfield && field != null)
        {
            pos = field.ClampPoint(pos); // keeps within numeric bounds
        }

        transform.SetPositionAndRotation(pos, rot);
    }
}
