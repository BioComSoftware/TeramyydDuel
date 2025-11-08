using UnityEngine;

// Basic player aircraft controller (starter)
// Attach to the player GameObject. Configure speed/rotation and the fire point + projectile prefab in the Inspector.
public class PlayerAircraft : MonoBehaviour
{
    [Header("Flight")]
    public float forwardSpeed = 80f;
    public float pitchSpeed = 45f;
    public float yawSpeed = 30f;
    public float rollSpeed = 60f;

    [Header("Combat")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public float fireCooldown = 0.15f;

    [Header("State")]
    public int maxHealth = 100;
    int currentHealth;

    float lastFireTime;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        HandleFlightInput();
        HandleFiring();
    }

    void HandleFlightInput()
    {
        float h = Input.GetAxis("Horizontal"); // roll
        float v = Input.GetAxis("Vertical");   // pitch
        float yaw = 0f; // optional: map to separate axis or keys

        // forward thrust constant for arcade feel — adjust in Inspector
        transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime);

        // rotations
        transform.Rotate(pitchSpeed * v * Time.deltaTime, yawSpeed * yaw * Time.deltaTime, -rollSpeed * h * Time.deltaTime, Space.Self);
    }

    void HandleFiring()
    {
        if (firePoint == null || projectilePrefab == null) return;
        if (Input.GetButton("Fire1") && Time.time - lastFireTime >= fireCooldown)
        {
            Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            lastFireTime = Time.time;
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        GameManager.Instance?.OnPlayerDestroyed();
        Destroy(gameObject);
    }
}
