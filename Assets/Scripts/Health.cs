using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class IntEvent : UnityEvent<int> { }

// Generic Health component usable by player and enemies.
public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth { get; private set; }

    public IntEvent onHealthChanged;
    public UnityEvent onDeath;

    [Header("Debug")]
    public bool debugLog = false;

    void Awake()
    {
        currentHealth = maxHealth;
        if (debugLog)
            FileLogger.Log($"{gameObject.name} initialized - Health: {currentHealth}/{maxHealth}", "Health");
        onHealthChanged?.Invoke(currentHealth);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        
        int oldHealth = currentHealth;
        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;
        
        if (debugLog)
            FileLogger.Log($"{gameObject.name} took {amount} damage - Health: {oldHealth} -> {currentHealth}/{maxHealth} ({(float)currentHealth/maxHealth * 100f:F1}%)", "Health");
        
        onHealthChanged?.Invoke(currentHealth);
        if (currentHealth == 0) Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth);
    }

    public void SetHealth(int value)
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
        // Default: destroy gameobject. Components can override by subscribing to onDeath.
        Destroy(gameObject);
    }
}
