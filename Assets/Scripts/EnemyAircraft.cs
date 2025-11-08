using UnityEngine;

// Simple enemy aircraft AI (starter)
// Behavior: patrol / chase player when within detection radius; fire at intervals if in range.
public class EnemyAircraft : MonoBehaviour
{
    public float forwardSpeed = 50f;
    public float turnSpeed = 2f;
    public float detectionRadius = 200f;
    public float fireRange = 150f;

    [Header("Combat")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireCooldown = 1.0f;
    public int damage = 10;

    Transform player;
    float lastFire;

    void Start()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO) player = playerGO.transform;
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist < detectionRadius)
        {
            // simple pursuit: rotate toward player and move forward
            Vector3 dir = (player.position - transform.position).normalized;
            Quaternion look = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
            transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime);

            if (dist <= fireRange && Time.time - lastFire >= fireCooldown)
            {
                Fire();
            }
        }
        else
        {
            // idle forward motion
            transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime);
        }
    }

    void Fire()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            lastFire = Time.time;
        }
    }

    public void TakeDamage(int amount)
    {
        // simple destroy on hit — you can add health later
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        // detect collisions with projectiles
        // projectiles should call SendMessage("OnHitTarget", damage) if needed; for now we're simple.
    }
}
