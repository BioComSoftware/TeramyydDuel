using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// PitchRollControlUI: Joystick-style control for ship attitude (pitch and roll).
/// Vertical axis controls pitch (up/down tilt), horizontal axis controls roll (left/right tilt).
/// VISUAL ONLY - does not affect flight path or physics, only ship's visual orientation.
/// </summary>
[AddComponentMenu("Teramyyd/HUD/Pitch Roll Control UI")]
public class PitchRollControlUI : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("References")]
    [Tooltip("The attitude controller to command")]
    public ShipAttitudeController attitudeController;
    
    [Tooltip("The joystick handle/knob that moves")]
    public RectTransform handleTransform;
    
    [Tooltip("The joystick background/base")]
    public RectTransform backgroundTransform;
    
    [Tooltip("Text label showing current pitch (optional)")]
    public TextMeshProUGUI pitchLabel;
    
    [Tooltip("Text label showing current roll (optional)")]
    public TextMeshProUGUI rollLabel;
    
    [Tooltip("Center indicator showing neutral position (optional)")]
    public GameObject centerIndicator;
    
    [Header("Visual Configuration")]
    [Tooltip("Maximum handle offset from center (pixels)")]
    public float handleRange = 100f;
    
    [Tooltip("Return to center when released")]
    public bool returnToCenter = true;
    
    [Tooltip("Speed of return to center animation")]
    public float returnSpeed = 5f;
    
    [Header("Interaction")]
    [Tooltip("Control sensitivity multiplier")]
    public float sensitivity = 1f;
    
    [Header("Debug")]
    public bool debugLog = false;
    
    private Vector2 currentHandlePos = Vector2.zero;
    private bool isDragging = false;
    
    private void Start()
    {
        // Auto-find attitude controller if not assigned
        if (attitudeController == null)
        {
            attitudeController = FindFirstObjectByType<ShipAttitudeController>();
        }
        
        if (attitudeController == null)
        {
            Debug.LogError("PitchRollControlUI: No ShipAttitudeController found!");
        }
        
        if (handleTransform == null)
        {
            Debug.LogError("PitchRollControlUI: No handle transform assigned!");
        }
        
        // Initialize handle to center
        currentHandlePos = Vector2.zero;
        UpdateHandleVisual();
        UpdateLabels();
    }
    
    private void Update()
    {
        // Return to center if not dragging
        if (returnToCenter && !isDragging && currentHandlePos.magnitude > 0.1f)
        {
            currentHandlePos = Vector2.Lerp(currentHandlePos, Vector2.zero, Time.deltaTime * returnSpeed);
            UpdateHandleVisual();
            UpdateAttitudeController();
        }
        
        UpdateLabels();
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        UpdateHandleFromPointer(eventData);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;
        
        UpdateHandleFromPointer(eventData);
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }
    
    /// <summary>
    /// Update handle position from pointer/mouse position
    /// </summary>
    private void UpdateHandleFromPointer(PointerEventData eventData)
    {
        if (backgroundTransform == null || handleTransform == null)
            return;
        
        // Convert screen point to local point in background
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            backgroundTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );
        
        // Clamp to circular range
        currentHandlePos = Vector2.ClampMagnitude(localPoint, handleRange);
        
        UpdateHandleVisual();
        UpdateAttitudeController();
    }
    
    /// <summary>
    /// Update handle visual position
    /// </summary>
    private void UpdateHandleVisual()
    {
        if (handleTransform != null)
        {
            handleTransform.anchoredPosition = currentHandlePos;
        }
        
        // Update center indicator
        if (centerIndicator != null)
        {
            centerIndicator.SetActive(currentHandlePos.magnitude < 5f);
        }
    }
    
    /// <summary>
    /// Send current joystick position to attitude controller
    /// </summary>
    private void UpdateAttitudeController()
    {
        if (attitudeController == null)
            return;
        
        // Map joystick position to normalized values (-1 to +1)
        // Y-axis = pitch (up = positive), X-axis = roll (right = positive)
        float pitchNormalized = (currentHandlePos.y / handleRange) * sensitivity;
        float rollNormalized = (currentHandlePos.x / handleRange) * sensitivity;
        
        // Clamp to valid range
        pitchNormalized = Mathf.Clamp(pitchNormalized, -1f, 1f);
        rollNormalized = Mathf.Clamp(rollNormalized, -1f, 1f);
        
        // Send to attitude controller
        attitudeController.SetPitchNormalized(pitchNormalized);
        attitudeController.SetRollNormalized(rollNormalized);
    }
    
    /// <summary>
    /// Update text labels
    /// </summary>
    private void UpdateLabels()
    {
        if (attitudeController == null)
            return;
        
        if (pitchLabel != null)
        {
            float pitch = attitudeController.CurrentPitch;
            string direction = pitch > 1f ? "UP" : pitch < -1f ? "DOWN" : "LEVEL";
            pitchLabel.text = $"Pitch: {pitch:F0}Â° {direction}";
        }
        
        if (rollLabel != null)
        {
            float roll = attitudeController.CurrentRoll;
            string direction = roll > 1f ? "STBD" : roll < -1f ? "PORT" : "LEVEL";
            rollLabel.text = $"Roll: {roll:F0}Â° {direction}";
        }
    }
    
    /// <summary>
    /// Public methods for button controls
    /// </summary>
    public void LevelShip()
    {
        if (attitudeController != null)
        {
            attitudeController.LevelShip();
        }
        
        currentHandlePos = Vector2.zero;
        UpdateHandleVisual();
    }
    
    public void PitchUp()
    {
        SetPitchRoll(1f, attitudeController != null ? attitudeController.GetRollNormalized() : 0f);
    }
    
    public void PitchDown()
    {
        SetPitchRoll(-1f, attitudeController != null ? attitudeController.GetRollNormalized() : 0f);
    }
    
    public void RollLeft()
    {
        SetPitchRoll(attitudeController != null ? attitudeController.GetPitchNormalized() : 0f, -1f);
    }
    
    public void RollRight()
    {
        SetPitchRoll(attitudeController != null ? attitudeController.GetPitchNormalized() : 0f, 1f);
    }
    
    /// <summary>
    /// Set pitch and roll directly (for keyboard/gamepad input)
    /// </summary>
    public void SetPitchRoll(float pitchNormalized, float rollNormalized)
    {
        // Update visual
        currentHandlePos = new Vector2(rollNormalized * handleRange, pitchNormalized * handleRange);
        currentHandlePos = Vector2.ClampMagnitude(currentHandlePos, handleRange);
        
        UpdateHandleVisual();
        UpdateAttitudeController();
    }
    
    /// <summary>
    /// Set from axis input (e.g., gamepad sticks)
    /// </summary>
    public void SetFromAxisInput(float horizontalAxis, float verticalAxis)
    {
        SetPitchRoll(verticalAxis, horizontalAxis);
    }
}
