using UnityEngine;

// Smooth follow camera for the player aircraft.
// Attach to the Main Camera and assign the player's transform as Target.
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 5f, -12f);
    [Range(1f, 20f)] public float followSpeed = 6f;
    public bool useLateUpdate = true;

    void LateUpdate()
    {
        if (!useLateUpdate) return;
        DoFollow();
    }

    void Update()
    {
        if (useLateUpdate) return;
        DoFollow();
    }

    void DoFollow()
    {
        if (target == null) return;

        // Position follows behind the target using its local forward/right
        Vector3 desiredPos = target.position + target.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);

        // Look at the target smoothly
        Vector3 toTarget = target.position - transform.position;
        if (toTarget.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRot = Quaternion.LookRotation(toTarget, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, followSpeed * 0.6f * Time.deltaTime);
        }
    }
}
