using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// RudderWheelUI: Interactive ship's wheel for rudder control.
/// Rotates based on drag input, controls rudder angle from 0Â° to 45Â° in each direction.
/// Provides visual feedback of current rudder position.
/// </summary>
[AddComponentMenu("Teramyyd/HUD/Rudder Wheel UI")]
public class RudderWheelUI : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("References")]
    [Tooltip("The rudder controller to command")]
    public RudderController rudderController;
    
    [Tooltip("The wheel image that rotates")]
    public RectTransform wheelTransform;
    
    [Tooltip("Text label showing current rudder angle (optional)")]
    public TextMeshProUGUI angleLabel;
    
    [Tooltip("Center indicator showing neutral position (optional)")]
    public GameObject centerIndicator;
    
    [Header("Visual Configuration")]
    [Tooltip("Maximum wheel rotation in degrees (wheel visual range, not rudder angle)")]
    public float maxWheelRotation = 180f;
    
    [Tooltip("Return to center when released")]
    public bool returnToCenter = false;
    
    [Tooltip("Speed of return to center animation")]
    public float returnSpeed = 5f;
    
    [Header("Interaction")]
    [Tooltip("Drag sensitivity (degrees per pixel)")]
    public float dragSensitivity = 0.5f;
    
    [Tooltip("Enable click-to-turn (click left/right of wheel)")]
    public bool allowClickTurn = true;
    
    [Header("Debug")]
    public bool debugLog = false;
    
    private float currentWheelRotation = 0f; // Visual wheel rotation (-maxWheelRotation to +maxWheelRotation)
    private bool isDragging = false;
    private Vector2 lastDragPos;
    
    private void Start()
    {
        // Auto-find rudder controller if not assigned
        if (rudderController == null)
        {
            rudderController = FindFirstObjectByType<RudderController>();
        }
        
        if (rudderController == null)
        {
            Debug.LogError("RudderWheelUI: No RudderController found!");
        }
        
        if (wheelTransform == null)
        {
            Debug.LogError("RudderWheelUI: No wheel transform assigned!");
        }
        
        // Initialize wheel to center
        currentWheelRotation = 0f;
        UpdateWheelVisual();
        UpdateLabel();
    }
    
    private void Update()
    {
        // Return to center if not dragging
        if (returnToCenter && !isDragging && Mathf.Abs(currentWheelRotation) > 0.1f)
        {
            currentWheelRotation = Mathf.Lerp(currentWheelRotation, 0f, Time.deltaTime * returnSpeed);
            UpdateWheelVisual();
            UpdateRudderController();
        }
        
        UpdateLabel();
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        lastDragPos = eventData.position;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || wheelTransform == null)
            return;
        
        // Calculate drag relative to wheel center
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            wheelTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localPoint
        );
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            wheelTransform, 
            lastDragPos, 
            eventData.pressEventCamera, 
            out Vector2 lastLocalPoint
        );
        
        // Calculate angular drag (using cross product for rotation direction)
        float currentAngle = Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
        float lastAngle = Mathf.Atan2(lastLocalPoint.y, lastLocalPoint.x) * Mathf.Rad2Deg;
        
        float angleDelta = Mathf.DeltaAngle(lastAngle, currentAngle);
        
        // Update wheel rotation
        currentWheelRotation = Mathf.Clamp(currentWheelRotation + angleDelta, -maxWheelRotation, maxWheelRotation);
        
        UpdateWheelVisual();
        UpdateRudderController();
        
        lastDragPos = eventData.position;
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!allowClickTurn || wheelTransform == null)
            return;
        
        // Get click position relative to wheel center
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            wheelTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localPoint
        );
        
        // Click left = port (negative), click right = starboard (positive)
        if (localPoint.x < 0f)
        {
            SetHardToPort();
        }
        else
        {
            SetHardToStarboard();
        }
    }
    
    /// <summary>
    /// Update wheel visual rotation
    /// </summary>
    private void UpdateWheelVisual()
    {
        if (wheelTransform != null)
        {
            wheelTransform.localRotation = Quaternion.Euler(0f, 0f, -currentWheelRotation);
        }
        
        // Update center indicator
        if (centerIndicator != null)
        {
            centerIndicator.SetActive(Mathf.Abs(currentWheelRotation) < 5f);
        }
    }
    
    /// <summary>
    /// Send current rotation to rudder controller
    /// </summary>
    private void UpdateRudderController()
    {
        if (rudderController == null)
            return;
        
        // Map wheel rotation to rudder angle
        // Wheel rotation range (-maxWheelRotation to +maxWheelRotation) maps to rudder range (-45Â° to +45Â°)
        float maxRudderAngle = RudderController.GetMaxRudderAngle();
        float rudderAngle = (currentWheelRotation / maxWheelRotation) * maxRudderAngle;
        
        rudderController.SetRudderAngle(rudderAngle);
    }
    
    /// <summary>
    /// Update angle label
    /// </summary>
    private void UpdateLabel()
    {
        if (angleLabel != null && rudderController != null)
        {
            float rudderAngle = rudderController.CurrentRudderAngle;
            string direction = "";
            
            if (Mathf.Abs(rudderAngle) < 1f)
            {
                direction = "AMIDSHIPS";
            }
            else if (rudderAngle > 0f)
            {
                direction = "STARBOARD";
            }
            else
            {
                direction = "PORT";
            }
            
            angleLabel.text = $"{Mathf.Abs(rudderAngle):F0}Â° {direction}";
        }
    }
    
    /// <summary>
    /// Public methods for button controls
    /// </summary>
    public void SetHardToPort()
    {
        currentWheelRotation = -maxWheelRotation;
        UpdateWheelVisual();
        UpdateRudderController();
    }
    
    public void SetHardToStarboard()
    {
        currentWheelRotation = maxWheelRotation;
        UpdateWheelVisual();
        UpdateRudderController();
    }
    
    public void SetAmidships()
    {
        currentWheelRotation = 0f;
        UpdateWheelVisual();
        UpdateRudderController();
    }
    
    /// <summary>
    /// Set rudder to specific angle (for keyboard/gamepad input)
    /// </summary>
    public void SetRudderAngle(float angleDegrees)
    {
        float maxRudderAngle = RudderController.GetMaxRudderAngle();
        currentWheelRotation = (angleDegrees / maxRudderAngle) * maxWheelRotation;
        currentWheelRotation = Mathf.Clamp(currentWheelRotation, -maxWheelRotation, maxWheelRotation);
        
        UpdateWheelVisual();
        UpdateRudderController();
    }
}
