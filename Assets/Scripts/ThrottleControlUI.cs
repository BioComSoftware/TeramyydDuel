using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// ThrottleControlUI: Interactive throttle lever with 9 snap positions.
/// Naval-style engine telegraph control for airship speed.
/// Positions: Full Ahead, Half Ahead, Slow Ahead, Dead Slow Ahead, Full Stop,
///            Dead Slow Astern, Slow Astern, Half Astern, Full Astern
/// </summary>
[AddComponentMenu("Teramyyd/HUD/Throttle Control UI")]
public class ThrottleControlUI : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("References")]
    [Tooltip("The throttle controller to command")]
    public ThrottleController throttleController;
    
    [Tooltip("The lever/handle image that rotates or moves")]
    public RectTransform leverTransform;
    
    [Tooltip("Text label showing current position name (optional)")]
    public TextMeshProUGUI positionLabel;
    
    [Header("Visual Configuration")]
    [Tooltip("Lever control type: Rotation (like telegraph) or Vertical (slider)")]
    public ControlType controlType = ControlType.Vertical;
    
    [Tooltip("Rotation angles for each position (if using Rotation mode)")]
    public float fullAheadRotation = -90f;
    public float fullAsternRotation = 90f;
    
    [Tooltip("Y positions for each position (if using Vertical mode)")]
    public float fullAheadYPos = 200f;
    public float fullAsternYPos = -200f;
    
    [Header("Interaction")]
    [Tooltip("Snap to nearest position when released")]
    public bool snapToPosition = true;
    
    [Tooltip("Enable click on specific positions to jump directly")]
    public bool allowDirectSelection = true;
    
    [Header("Debug")]
    public bool debugLog = false;
    
    public enum ControlType
    {
        Rotation,  // Rotates like ship's telegraph
        Vertical   // Moves vertically like a slider
    }
    
    private ThrottleController.ThrottlePosition currentPosition;
    private bool isDragging = false;
    private Vector2 dragStartPos;
    
    private void Start()
    {
        // Auto-find throttle controller if not assigned
        if (throttleController == null)
        {
            throttleController = FindFirstObjectByType<ThrottleController>();
        }
        
        if (throttleController == null)
        {
            Debug.LogError("ThrottleControlUI: No ThrottleController found!");
        }
        
        if (leverTransform == null)
        {
            Debug.LogError("ThrottleControlUI: No lever transform assigned!");
        }
        
        // Initialize to current throttle position
        if (throttleController != null)
        {
            currentPosition = throttleController.CurrentPosition;
            UpdateLeverVisual();
            UpdateLabel();
        }
    }
    
    private void Update()
    {
        // Sync with throttle controller if changed externally
        if (throttleController != null && throttleController.CurrentPosition != currentPosition)
        {
            currentPosition = throttleController.CurrentPosition;
            UpdateLeverVisual();
            UpdateLabel();
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        dragStartPos = eventData.position;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || leverTransform == null)
            return;
        
        // Calculate position based on drag
        Vector2 dragDelta = eventData.position - dragStartPos;
        
        if (controlType == ControlType.Rotation)
        {
            // Map horizontal drag to rotation
            float rotationDelta = dragDelta.x * 0.5f; // Sensitivity
            float currentRot = leverTransform.localEulerAngles.z;
            if (currentRot > 180f) currentRot -= 360f;
            
            float newRot = Mathf.Clamp(currentRot + rotationDelta, fullAheadRotation, fullAsternRotation);
            leverTransform.localRotation = Quaternion.Euler(0f, 0f, newRot);
            
            // Update position based on rotation
            UpdatePositionFromRotation(newRot);
        }
        else // Vertical
        {
            // Map vertical drag to position
            float yDelta = dragDelta.y;
            Vector3 currentPos = leverTransform.anchoredPosition;
            float newY = Mathf.Clamp(currentPos.y + yDelta, fullAsternYPos, fullAheadYPos);
            leverTransform.anchoredPosition = new Vector2(currentPos.x, newY);
            
            // Update position based on Y
            UpdatePositionFromY(newY);
        }
        
        dragStartPos = eventData.position;
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        
        if (snapToPosition)
        {
            // Snap to nearest position
            UpdateLeverVisual();
        }
    }
    
    /// <summary>
    /// Update lever visual to match current position
    /// </summary>
    private void UpdateLeverVisual()
    {
        if (leverTransform == null)
            return;
        
        if (controlType == ControlType.Rotation)
        {
            float targetRotation = GetRotationForPosition(currentPosition);
            leverTransform.localRotation = Quaternion.Euler(0f, 0f, targetRotation);
        }
        else // Vertical
        {
            float targetY = GetYPositionForPosition(currentPosition);
            Vector3 currentPos = leverTransform.anchoredPosition;
            leverTransform.anchoredPosition = new Vector2(currentPos.x, targetY);
        }
    }
    
    /// <summary>
    /// Update text label
    /// </summary>
    private void UpdateLabel()
    {
        if (positionLabel != null && throttleController != null)
        {
            positionLabel.text = throttleController.GetPositionName();
        }
    }
    
    /// <summary>
    /// Map rotation angle to throttle position
    /// </summary>
    private void UpdatePositionFromRotation(float rotationZ)
    {
        // Normalize rotation
        if (rotationZ > 180f) rotationZ -= 360f;
        
        // Map to position (9 positions, evenly spaced)
        float range = fullAsternRotation - fullAheadRotation;
        float step = range / 8f; // 8 steps between 9 positions
        
        float normalized = (rotationZ - fullAheadRotation) / range;
        int posIndex = Mathf.RoundToInt(normalized * 8f);
        
        ThrottleController.ThrottlePosition newPos = (ThrottleController.ThrottlePosition)Mathf.Clamp(posIndex, 0, 8);
        
        if (newPos != currentPosition)
        {
            currentPosition = newPos;
            if (throttleController != null)
            {
                throttleController.SetThrottlePosition(currentPosition);
            }
            UpdateLabel();
        }
    }
    
    /// <summary>
    /// Map Y position to throttle position
    /// </summary>
    private void UpdatePositionFromY(float yPos)
    {
        // Map to position (9 positions, evenly spaced)
        float range = fullAheadYPos - fullAsternYPos;
        float step = range / 8f; // 8 steps between 9 positions
        
        float normalized = (yPos - fullAsternYPos) / range;
        int posIndex = 8 - Mathf.RoundToInt(normalized * 8f); // Inverted (top = ahead)
        
        ThrottleController.ThrottlePosition newPos = (ThrottleController.ThrottlePosition)Mathf.Clamp(posIndex, 0, 8);
        
        if (newPos != currentPosition)
        {
            currentPosition = newPos;
            if (throttleController != null)
            {
                throttleController.SetThrottlePosition(currentPosition);
            }
            UpdateLabel();
        }
    }
    
    /// <summary>
    /// Get rotation angle for specific position
    /// </summary>
    private float GetRotationForPosition(ThrottleController.ThrottlePosition position)
    {
        float range = fullAsternRotation - fullAheadRotation;
        float step = range / 8f;
        return fullAheadRotation + ((int)position * step);
    }
    
    /// <summary>
    /// Get Y position for specific position
    /// </summary>
    private float GetYPositionForPosition(ThrottleController.ThrottlePosition position)
    {
        float range = fullAheadYPos - fullAsternYPos;
        float step = range / 8f;
        return fullAheadYPos - ((int)position * step);
    }
    
    /// <summary>
    /// Public methods for button controls
    /// </summary>
    public void SetFullAhead() { SetPosition(ThrottleController.ThrottlePosition.FullAhead); }
    public void SetHalfAhead() { SetPosition(ThrottleController.ThrottlePosition.HalfAhead); }
    public void SetSlowAhead() { SetPosition(ThrottleController.ThrottlePosition.SlowAhead); }
    public void SetDeadSlowAhead() { SetPosition(ThrottleController.ThrottlePosition.DeadSlowAhead); }
    public void SetFullStop() { SetPosition(ThrottleController.ThrottlePosition.FullStop); }
    public void SetDeadSlowAstern() { SetPosition(ThrottleController.ThrottlePosition.DeadSlowAstern); }
    public void SetSlowAstern() { SetPosition(ThrottleController.ThrottlePosition.SlowAstern); }
    public void SetHalfAstern() { SetPosition(ThrottleController.ThrottlePosition.HalfAstern); }
    public void SetFullAstern() { SetPosition(ThrottleController.ThrottlePosition.FullAstern); }
    
    public void IncreaseThrottle()
    {
        if (throttleController != null)
        {
            throttleController.IncreaseThrottle();
        }
    }
    
    public void DecreaseThrottle()
    {
        if (throttleController != null)
        {
            throttleController.DecreaseThrottle();
        }
    }
    
    private void SetPosition(ThrottleController.ThrottlePosition position)
    {
        if (throttleController != null)
        {
            currentPosition = position;
            throttleController.SetThrottlePosition(position);
            UpdateLeverVisual();
            UpdateLabel();
        }
    }
}
