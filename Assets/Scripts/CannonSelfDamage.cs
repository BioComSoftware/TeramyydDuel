using UnityEngine;

// Applies self-damage to the cannon each time it fires.
// Attach this to the same GameObject as the Cannon component.
// Health is expected to live on the visual 3D child (e.g., Cylinder). You can
// explicitly assign it, or leave empty to auto-find the first Health in children.
// Supports fractional damage via internal accumulation before applying to Health (int-based).
public class CannonSelfDamage : MonoBehaviour
{
    [Header("References")]
    public Cannon cannon;          // If null, auto-find on this GameObject
    [Tooltip("Health component to damage (usually on the visual 3D child). If null, auto-find in children.")]
    public Health health;          // If null, auto-find in children first, then on self

    [Header("Damage Per Shot (can be fractional)")]
    [Tooltip("Amount of health removed each time the cannon fires. Accepts fractional values (e.g., 0.5).")]
    public float damagePerShot = 0.5f;

    [Header("Filtering")]
    [Tooltip("Optional cooldown window (seconds) to ignore rapid duplicate key-down events.")]
    public float minShotInterval = 0.05f;

    private float _fractionalCarry; // carries fractional remainder until it sums to an integer
    private float _lastShotTime = -999f;

    void Awake()
    {
        if (cannon == null) cannon = GetComponent<Cannon>();
        if (health == null)
        {
            // Prefer a child Health (visual mesh child) over self
            health = GetComponentInChildren<Health>();
            if (health == null) health = GetComponent<Health>();
        }
    }

    void Update()
    {
        if (cannon == null) return;

        // Mirror the firing trigger: when the cannon's fire key is pressed, apply wear
        if (Input.GetKeyDown(cannon.fireKey))
        {
            float t = Time.time;
            if (t - _lastShotTime < minShotInterval) return;
            _lastShotTime = t;

            ApplyWear(damagePerShot);
        }
    }

    private void ApplyWear(float amount)
    {
        if (health == null || amount <= 0f) return;

        // Accumulate fractional damage and apply only whole points to Health
        _fractionalCarry += amount;
        int whole = Mathf.FloorToInt(_fractionalCarry);
        if (whole > 0)
        {
            health.TakeDamage(whole);
            _fractionalCarry -= whole;
        }
    }
}
