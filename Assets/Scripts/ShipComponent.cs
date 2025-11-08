using UnityEngine;

public class ShipComponent : MonoBehaviour
{
    public Health healthSystem;     // Reference to this component's health
    private Renderer[] renderers;   // All renderers in children (for visual feedback)
    
    void Start()
    {
        // Get the Health component if not already assigned
        if (healthSystem == null)
            healthSystem = GetComponent<Health>();
            
        // Get all renderers in children (cubes, models, etc.)
        renderers = GetComponentsInChildren<Renderer>();
            
        // Subscribe to health changes to update visuals
        if (healthSystem != null)
        {
            healthSystem.onHealthChanged.AddListener(OnHealthChanged);
            healthSystem.onDeath.AddListener(OnComponentDestroyed);
        }
    }
    
    void OnHealthChanged(int newHealth)
    {
        if (healthSystem != null && renderers != null && renderers.Length > 0)
        {
            // Update visual model based on health
            // Change material color based on damage percentage
            float healthPercentage = (float)newHealth / healthSystem.maxHealth;
            Color damageColor = Color.Lerp(Color.red, Color.white, healthPercentage);
            
            foreach (var rend in renderers)
            {
                if (rend != null)
                {
                    rend.material.color = damageColor;
                }
            }
        }
    }
    
    void OnComponentDestroyed()
    {
        if (renderers != null && renderers.Length > 0)
        {
            // Change the visual to show destruction
            foreach (var rend in renderers)
            {
                if (rend != null)
                {
                    rend.material.color = Color.black;
                }
            }
        }
    }
}