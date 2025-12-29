using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class FloatEvent : UnityEvent<float> { }

// Generic Health component usable by player and enemies.
public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth { get; private set; }

    public FloatEvent onHealthChanged;
    public UnityEvent onDeath;

    [Header("Debug")]
    public bool debugLog = false;
    
    [Tooltip("Enable detailed collision debugging (shows what hit this object and damage calculations).")]
    public bool debugCollisions = false;

    void Awake()
    {
        currentHealth = maxHealth;
        if (debugLog)
            FileLogger.Log($"{gameObject.name} initialized - Health: {currentHealth}/{maxHealth}", "Health");
        onHealthChanged?.Invoke(currentHealth);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0) return;
        
        float oldHealth = currentHealth;
        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;
        
        if (debugLog || debugCollisions)
        {
            string path = GetGameObjectPath(gameObject);
            Debug.Log($"[HEALTH DEBUG] {gameObject.name} took {amount:F2} damage | Health: {oldHealth:F2} -> {currentHealth:F2}/{maxHealth:F2} ({(currentHealth/maxHealth) * 100f:F1}%) | Path: {path}");
            FileLogger.Log($"{gameObject.name} took {amount:F2} damage - Health: {oldHealth:F2} -> {currentHealth:F2}/{maxHealth:F2} ({(currentHealth/maxHealth) * 100f:F1}%) | Path: {path}", "Health");
        }
        
        onHealthChanged?.Invoke(currentHealth);
        if (currentHealth == 0) Die();
    }

    public void Heal(float amount)
    {
        if (amount <= 0) return;
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth);
    }

    public void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        onHealthChanged?.Invoke(currentHealth);
        if (currentHealth == 0) Die();
    }

    void Die()
    {
        if (debugLog)
            FileLogger.Log($"{gameObject.name} died - destroying GameObject", "Health");
        onDeath?.Invoke();
        
        // Find the root weapon object (has ProjectileLauncher) to destroy the entire weapon
        // This ensures mounted weapons are fully removed, not just damaged parts
        ProjectileLauncher launcher = GetComponentInParent<ProjectileLauncher>();
        if (launcher != null)
        {
            if (debugLog)
                FileLogger.Log($"Found launcher in parent {launcher.gameObject.name}, destroying root weapon instead", "Health");
            Destroy(launcher.gameObject);
        }
        else
        {
            // Check if this is a child of a larger object (like Target)
            // If parent exists and has specific components, destroy the parent instead
            Transform parent = transform.parent;
            if (parent != null)
            {
                // If parent has TargetCannonAim, this is the Target enemy - destroy the whole Target
                if (parent.GetComponent<TargetCannonAim>() != null)
                {
                    if (debugLog)
                        FileLogger.Log($"Found Target parent {parent.name}, destroying entire Target including all children", "Health");
                    Destroy(parent.gameObject);
                    return;
                }
            }
            
            // No special parent, just destroy this GameObject
            Destroy(gameObject);
        }
    }
    
    string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }
}
