using UnityEngine;

/// <summary>
/// Extremely simple placeholder AI that keeps an enemy aircraft pointed at a target and
/// drifts forward at a constant speed. This replaces the missing legacy script so the
/// project can compile again. Feel free to expand the behaviour as needed for gameplay.
/// </summary>
[AddComponentMenu("Teramyyd/AI/Enemy Aircraft (Placeholder)")]
[RequireComponent(typeof(Rigidbody))]
public class EnemyAircraft : MonoBehaviour
{
    [Tooltip("Optional target for this enemy to pursue.")]
    public Transform pursuitTarget;

    [Header("Movement")]
    [Tooltip("Forward speed in meters per second while pursuing.")]
    public float cruiseSpeed = 20f;
    [Tooltip("How quickly the enemy rotates to face its target (degrees per second).")]
    public float turnRate = 45f;

    Rigidbody _rigidbody;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (_rigidbody == null)
            return;

        UpdateHeading();
        MaintainCruiseSpeed();
    }

    void UpdateHeading()
    {
        if (pursuitTarget == null)
            return;

        Vector3 toTarget = pursuitTarget.position - transform.position;
        if (toTarget.sqrMagnitude < 0.0001f)
            return;

        Quaternion desiredRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        float step = turnRate * Mathf.Deg2Rad * Time.fixedDeltaTime;
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, step);
    }

    void MaintainCruiseSpeed()
    {
        Vector3 desiredVelocity = transform.forward * Mathf.Max(0f, cruiseSpeed);
        Vector3 currentVelocity = _rigidbody.linearVelocity;
        Vector3 acceleration = (desiredVelocity - currentVelocity);
        _rigidbody.AddForce(acceleration, ForceMode.Acceleration);
    }
}
