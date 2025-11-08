using System.Reflection;
using UnityEngine;

// Simple HUD controller showing player health and score.
// Assign any UI component (uGUI Text, TextMeshPro TMP_Text, or custom) to the
// inspector fields. The script will attempt to set a `text` property via reflection.
public class HUDController : MonoBehaviour
{
    [Header("Player")]
    public Health playerHealth;
    [Tooltip("Assign the UI component that displays health (Text, TMP_Text, etc.)")]
    public Component healthTextComponent;

    [Header("Score")]
    [Tooltip("Assign the UI component that displays score (Text, TMP_Text, etc.)")]
    public Component scoreTextComponent;

    void Start()
    {
        if (playerHealth != null)
        {
            playerHealth.onHealthChanged.AddListener(UpdateHealthText);
            UpdateHealthText(playerHealth.currentHealth);
        }
    }

    void UpdateHealthText(int current)
    {
        SetTextOnComponent(healthTextComponent, $"Health: {current}");
    }

    public void UpdateScore(int score)
    {
        SetTextOnComponent(scoreTextComponent, $"Score: {score}");
    }

    void SetTextOnComponent(Component comp, string text)
    {
        if (comp == null) return;

        // Try property 'text'
        var prop = comp.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(comp, text, null);
            return;
        }

        // Try field 'text'
        var field = comp.GetType().GetField("text", BindingFlags.Public | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(comp, text);
            return;
        }

        // Fallback: try a method called SetText(string)
        var method = comp.GetType().GetMethod("SetText", new[] { typeof(string) });
        if (method != null)
        {
            method.Invoke(comp, new object[] { text });
            return;
        }
    }
}
