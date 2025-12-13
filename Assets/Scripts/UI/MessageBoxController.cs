using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Displays temporary HUD messages to the player. Messages fade in/out and multiple messages
/// can be defined for the same event to provide variety. The GameObject itself should be
/// invisible - only the text is shown.
/// </summary>
public class MessageBoxController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The TextMeshPro component that displays the message")]
    public TextMeshProUGUI messageText;

    [Header("Display Settings")]
    [Tooltip("How long the message stays visible (in seconds)")]
    public float displayDuration = 3f;

    [Tooltip("Fade in duration (in seconds)")]
    public float fadeInDuration = 0.3f;

    [Tooltip("Fade out duration (in seconds)")]
    public float fadeOutDuration = 0.5f;

    private float _currentDisplayTime = 0f;
    private bool _isDisplaying = false;
    private CanvasGroup _canvasGroup;

    void Awake()
    {
        if (messageText == null)
        {
            messageText = GetComponentInChildren<TextMeshProUGUI>();
        }

        // Add or get CanvasGroup for fading
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Start invisible
        _canvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (!_isDisplaying)
            return;

        _currentDisplayTime += Time.deltaTime;

        // Fade in phase
        if (_currentDisplayTime < fadeInDuration)
        {
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, _currentDisplayTime / fadeInDuration);
        }
        // Hold phase
        else if (_currentDisplayTime < displayDuration)
        {
            _canvasGroup.alpha = 1f;
        }
        // Fade out phase
        else if (_currentDisplayTime < displayDuration + fadeOutDuration)
        {
            float fadeOutTime = _currentDisplayTime - displayDuration;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeOutTime / fadeOutDuration);
        }
        // Message complete
        else
        {
            _canvasGroup.alpha = 0f;
            _isDisplaying = false;
        }
    }

    /// <summary>
    /// Display a single message
    /// </summary>
    public void ShowMessage(string message)
    {
        if (messageText == null)
        {
            Debug.LogWarning("MessageBoxController: No TextMeshProUGUI component assigned!");
            return;
        }

        messageText.text = message;
        _currentDisplayTime = 0f;
        _isDisplaying = true;
    }

    /// <summary>
    /// Display a random message from an array of possible messages
    /// </summary>
    public void ShowRandomMessage(string[] messages)
    {
        if (messages == null || messages.Length == 0)
        {
            Debug.LogWarning("MessageBoxController: No messages provided!");
            return;
        }

        int randomIndex = Random.Range(0, messages.Length);
        ShowMessage(messages[randomIndex]);
    }

    /// <summary>
    /// Check if a message is currently being displayed
    /// </summary>
    public bool IsDisplaying()
    {
        return _isDisplaying;
    }

    /// <summary>
    /// Clear the current message immediately
    /// </summary>
    public void ClearMessage()
    {
        _isDisplaying = false;
        _canvasGroup.alpha = 0f;
    }
}
