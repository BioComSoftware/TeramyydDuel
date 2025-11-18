using UnityEngine;

/// <summary>
/// ThrottleController: Manages airship throttle with preset speed positions.
/// Naval-style telegraph with Ahead/Astern positions: Full, Half, Slow, Dead Slow, and Full Stop.
/// Integrates with Engine.cs to set speed commands based on preset values.
/// </summary>
[AddComponentMenu("Teramyyd/Ship Systems/Throttle Controller")]
public class ThrottleController : MonoBehaviour
{
    [Header("Speed Presets - Ahead (Knots)")]
    [Tooltip("Dead Slow Ahead - minimum forward speed")]
    public float deadSlowAheadKnots = 2f;
    
    [Tooltip("Slow Ahead - reduced forward speed")]
    public float slowAheadKnots = 5f;
    
    [Tooltip("Half Ahead - half maximum forward speed")]
    public float halfAheadKnots = 10f;
    
    [Tooltip("Full Ahead - maximum forward speed")]
    public float fullAheadKnots = 20f;
    
    [Header("Speed Presets - Astern (Knots)")]
    [Tooltip("Dead Slow Astern - minimum reverse speed")]
    public float deadSlowAsternKnots = 1f;
    
    [Tooltip("Slow Astern - reduced reverse speed")]
    public float slowAsternKnots = 3f;
    
    [Tooltip("Half Astern - half maximum reverse speed")]
    public float halfAsternKnots = 5f;
    
    [Tooltip("Full Astern - maximum reverse speed")]
    public float fullAsternKnots = 10f;
    
    [Header("References")]
    [Tooltip("Engine to control (auto-discovered if not set)")]
    public Engine engine;
    
    [Header("Current State")]
    [SerializeField] private ThrottlePosition _currentPosition = ThrottlePosition.FullStop;
    [SerializeField] private float _currentSpeedKnots = 0f;
    
    [Header("Debug")]
    public bool debugLog = false;
    
    /// <summary>
    /// Throttle position enum - naval engine telegraph positions
    /// </summary>
    public enum ThrottlePosition
    {
        FullAhead,
        HalfAhead,
        SlowAhead,
        DeadSlowAhead,
        FullStop,
        DeadSlowAstern,
        SlowAstern,
        HalfAstern,
        FullAstern
    }
    
    public ThrottlePosition CurrentPosition => _currentPosition;
    public float CurrentSpeedKnots => _currentSpeedKnots;
    
    private void Start()
    {
        // Auto-find engine if not assigned
        if (engine == null)
        {
            engine = FindFirstObjectByType<Engine>();
        }
        
        if (engine == null)
        {
            Debug.LogError($"ThrottleController on {gameObject.name}: Cannot find Engine component!");
        }
        
        // Initialize to Full Stop
        SetThrottlePosition(ThrottlePosition.FullStop);
        
        if (debugLog)
        {
            FileLogger.Log($"ThrottleController initialized - Presets: Full {fullAheadKnots}kt, Half {halfAheadKnots}kt, Slow {slowAheadKnots}kt, DeadSlow {deadSlowAheadKnots}kt", "ThrottleController");
        }
    }
    
    /// <summary>
    /// Set throttle to specific position, applying corresponding speed preset
    /// </summary>
    public void SetThrottlePosition(ThrottlePosition position)
    {
        if (engine == null)
        {
            Debug.LogWarning("ThrottleController: No engine assigned!");
            return;
        }
        
        _currentPosition = position;
        
        // Map position to speed command
        switch (position)
        {
            case ThrottlePosition.FullAhead:
                _currentSpeedKnots = fullAheadKnots;
                engine.SetKnotsAhead(fullAheadKnots);
                break;
                
            case ThrottlePosition.HalfAhead:
                _currentSpeedKnots = halfAheadKnots;
                engine.SetKnotsAhead(halfAheadKnots);
                break;
                
            case ThrottlePosition.SlowAhead:
                _currentSpeedKnots = slowAheadKnots;
                engine.SetKnotsAhead(slowAheadKnots);
                break;
                
            case ThrottlePosition.DeadSlowAhead:
                _currentSpeedKnots = deadSlowAheadKnots;
                engine.SetKnotsAhead(deadSlowAheadKnots);
                break;
                
            case ThrottlePosition.FullStop:
                _currentSpeedKnots = 0f;
                engine.AllStop();
                break;
                
            case ThrottlePosition.DeadSlowAstern:
                _currentSpeedKnots = -deadSlowAsternKnots;
                engine.SetKnotsAstern(deadSlowAsternKnots);
                break;
                
            case ThrottlePosition.SlowAstern:
                _currentSpeedKnots = -slowAsternKnots;
                engine.SetKnotsAstern(slowAsternKnots);
                break;
                
            case ThrottlePosition.HalfAstern:
                _currentSpeedKnots = -halfAsternKnots;
                engine.SetKnotsAstern(halfAsternKnots);
                break;
                
            case ThrottlePosition.FullAstern:
                _currentSpeedKnots = -fullAsternKnots;
                engine.SetKnotsAstern(fullAsternKnots);
                break;
        }
        
        if (debugLog)
        {
            FileLogger.Log($"Throttle set to {position} - Command: {_currentSpeedKnots:F1}kt", "ThrottleController");
        }
    }
    
    /// <summary>
    /// Increase throttle by one position (toward Full Ahead)
    /// </summary>
    public void IncreaseThrottle()
    {
        if ((int)_currentPosition > 0)
        {
            SetThrottlePosition((ThrottlePosition)((int)_currentPosition - 1));
        }
    }
    
    /// <summary>
    /// Decrease throttle by one position (toward Full Astern)
    /// </summary>
    public void DecreaseThrottle()
    {
        if ((int)_currentPosition < 8) // FullAstern = 8
        {
            SetThrottlePosition((ThrottlePosition)((int)_currentPosition + 1));
        }
    }
    
    /// <summary>
    /// Emergency stop - set to Full Stop position
    /// </summary>
    public void EmergencyStop()
    {
        SetThrottlePosition(ThrottlePosition.FullStop);
    }
    
    /// <summary>
    /// Get readable position name for UI display
    /// </summary>
    public string GetPositionName()
    {
        return _currentPosition switch
        {
            ThrottlePosition.FullAhead => "Full Ahead",
            ThrottlePosition.HalfAhead => "Half Ahead",
            ThrottlePosition.SlowAhead => "Slow Ahead",
            ThrottlePosition.DeadSlowAhead => "Dead Slow Ahead",
            ThrottlePosition.FullStop => "Full Stop",
            ThrottlePosition.DeadSlowAstern => "Dead Slow Astern",
            ThrottlePosition.SlowAstern => "Slow Astern",
            ThrottlePosition.HalfAstern => "Half Astern",
            ThrottlePosition.FullAstern => "Full Astern",
            _ => "Unknown"
        };
    }
}
