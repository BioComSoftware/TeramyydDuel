using UnityEngine;

/// <summary>
/// Instrument Panel Manager - Coordinates all ship HUD instruments.
/// Ontological structure: The care structure that unifies all ready-to-hand instruments.
/// Provides centralized setup and coordination for the entire instrument panel.
/// 
/// This manager:
/// - Auto-discovers ShipCharacteristics
/// - Links all instrument scripts
/// - Provides unified enable/disable control
/// - Optional: Can handle instrument panel visibility/fade
/// </summary>
[AddComponentMenu("Teramyyd/HUD/Instrument Panel Manager")]
public class InstrumentPanelManager : MonoBehaviour
{
    [Header("Ship Reference")]
    [Tooltip("Reference to the player ship's ShipCharacteristics. Auto-discovered if not set.")]
    public ShipCharacteristics shipCharacteristics;
    
    [Header("Instrument References")]
    [Tooltip("Airspeed indicator component.")]
    public AirspeedIndicator airspeedIndicator;
    
    [Tooltip("Altimeter indicator component.")]
    public AltimeterIndicator altimeterIndicator;
    
    [Tooltip("Vertical speed indicator component.")]
    public VerticalSpeedIndicator verticalSpeedIndicator;
    
    [Tooltip("Attitude indicator component.")]
    public AttitudeIndicator attitudeIndicator;
    
    [Header("Attitude Indicator Configuration")]
    [Tooltip("Pixels to move vertically per degree of pitch. E.g., 3 means 30° pitch = 90 pixels movement.")]
    public float pixelsPerPitchDegree = 3f;
    
    [Header("Panel Control")]
    [Tooltip("Enable/disable all instruments.")]
    public bool instrumentsEnabled = true;
    
    [Tooltip("Optional: Canvas Group for fading the entire panel.")]
    public CanvasGroup panelCanvasGroup;
    
    [Header("Debug")]
    public bool debugLog = false;
    
    private void Start()
    {
        // Auto-discover ShipCharacteristics
        if (shipCharacteristics == null)
        {
            shipCharacteristics = FindFirstObjectByType<ShipCharacteristics>();
            
            if (shipCharacteristics == null)
            {
                Debug.LogError("InstrumentPanelManager: Cannot find ShipCharacteristics in scene!");
            }
            else if (debugLog)
            {
                Debug.Log($"InstrumentPanelManager: Auto-discovered ShipCharacteristics on {shipCharacteristics.gameObject.name}");
            }
        }
        
        // Auto-discover instrument components if not assigned
        if (airspeedIndicator == null)
            airspeedIndicator = GetComponentInChildren<AirspeedIndicator>();
        
        if (altimeterIndicator == null)
            altimeterIndicator = GetComponentInChildren<AltimeterIndicator>();
        
        if (verticalSpeedIndicator == null)
            verticalSpeedIndicator = GetComponentInChildren<VerticalSpeedIndicator>();
        
        if (attitudeIndicator == null)
            attitudeIndicator = GetComponentInChildren<AttitudeIndicator>();
        
        // Link ship to all instruments
        LinkInstruments();
        
        // Log setup
        if (debugLog)
        {
            LogInstrumentSetup();
        }
    }
    
    /// <summary>
    /// Link ShipCharacteristics to all instrument components.
    /// </summary>
    private void LinkInstruments()
    {
        if (shipCharacteristics == null)
            return;
        
        if (airspeedIndicator != null)
            airspeedIndicator.shipCharacteristics = shipCharacteristics;
        
        if (altimeterIndicator != null)
            altimeterIndicator.shipCharacteristics = shipCharacteristics;
        
        if (verticalSpeedIndicator != null)
            verticalSpeedIndicator.shipCharacteristics = shipCharacteristics;
        
        if (attitudeIndicator != null)
        {
            attitudeIndicator.shipCharacteristics = shipCharacteristics;
            
            // Apply pixels per degree setting
            // Calculate maxPitchMovementPixels based on pixelsPerPitchDegree and maxPitchDegrees
            attitudeIndicator.maxPitchMovementPixels = pixelsPerPitchDegree * attitudeIndicator.maxPitchDegrees;
        }
    }
    
    /// <summary>
    /// Enable or disable all instruments.
    /// </summary>
    public void SetInstrumentsEnabled(bool enabled)
    {
        instrumentsEnabled = enabled;
        
        if (airspeedIndicator != null)
            airspeedIndicator.enabled = enabled;
        
        if (altimeterIndicator != null)
            altimeterIndicator.enabled = enabled;
        
        if (verticalSpeedIndicator != null)
            verticalSpeedIndicator.enabled = enabled;
        
        if (attitudeIndicator != null)
            attitudeIndicator.enabled = enabled;
    }
    
    /// <summary>
    /// Fade the entire instrument panel (requires CanvasGroup).
    /// </summary>
    public void SetPanelAlpha(float alpha)
    {
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = Mathf.Clamp01(alpha);
        }
    }
    
    /// <summary>
    /// Show the instrument panel.
    /// </summary>
    public void ShowPanel()
    {
        SetPanelAlpha(1f);
        SetInstrumentsEnabled(true);
    }
    
    /// <summary>
    /// Hide the instrument panel.
    /// </summary>
    public void HidePanel()
    {
        SetPanelAlpha(0f);
        SetInstrumentsEnabled(false);
    }
    
    /// <summary>
    /// Log instrument setup for debugging.
    /// </summary>
    private void LogInstrumentSetup()
    {
        Debug.Log("=== Instrument Panel Setup ===");
        Debug.Log($"Ship: {(shipCharacteristics != null ? shipCharacteristics.gameObject.name : "NOT FOUND")}");
        Debug.Log($"Airspeed Indicator: {(airspeedIndicator != null ? "✓" : "✗")}");
        Debug.Log($"Altimeter: {(altimeterIndicator != null ? "✓" : "✗")}");
        Debug.Log($"Vertical Speed Indicator: {(verticalSpeedIndicator != null ? "✓" : "✗")}");
        Debug.Log($"Attitude Indicator: {(attitudeIndicator != null ? "✓" : "✗")}");
        Debug.Log("=============================");
    }
}
