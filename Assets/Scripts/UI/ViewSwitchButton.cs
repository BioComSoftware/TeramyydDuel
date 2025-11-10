using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Identifies a button as a view switcher and handles the view change on click.
/// Attach this to each view switching button (Bridge, Follow, Overhead).
/// </summary>
[RequireComponent(typeof(Button))]
public class ViewSwitchButton : MonoBehaviour
{
    public enum ViewType
    {
        Bridge,
        Follow,
        Overhead
    }
    
    [Header("View Configuration")]
    [Tooltip("Which view this button switches to")]
    public ViewType targetView = ViewType.Bridge;
    
    private Button button;
    private CameraViewManager viewManager;
    
    void Start()
    {
        button = GetComponent<Button>();
        viewManager = FindObjectOfType<CameraViewManager>();
        
        if (viewManager == null)
        {
            Debug.LogError("ViewSwitchButton: CameraViewManager not found in scene!");
            return;
        }
        
        // Add click listener
        button.onClick.AddListener(OnButtonClick);
    }
    
    void OnButtonClick()
    {
        switch (targetView)
        {
            case ViewType.Bridge:
                viewManager.ApplyMode(CameraViewManager.ViewMode.Bridge);
                Debug.Log("Switched to Bridge view");
                break;
            case ViewType.Follow:
                viewManager.ApplyMode(CameraViewManager.ViewMode.Follow);
                Debug.Log("Switched to Follow view");
                break;
            case ViewType.Overhead:
                viewManager.ApplyMode(CameraViewManager.ViewMode.Overhead);
                Debug.Log("Switched to Overhead view");
                break;
        }
    }
}
