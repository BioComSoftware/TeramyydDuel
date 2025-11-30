using UnityEngine;
using UnityEngine.UI;

using System.Collections;

public class CompassHandler : MonoBehaviour 
{
    public float numberOfPixelsNorthToNorth;
    public ShipCharacteristics target;
    RectTransform rectTransform;
    Vector2 startPosition;
    float rationAngleToPixel;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
        rationAngleToPixel = numberOfPixelsNorthToNorth / 360f;
    }

    void Update () 
    {
        float currentHeading = target.currentYawDegrees;
        float offset = -currentHeading * rationAngleToPixel;
        
        // Wrap the offset to stay within one full rotation cycle
        offset = Mathf.Repeat(offset, numberOfPixelsNorthToNorth);
        if (offset > numberOfPixelsNorthToNorth / 2f)
        {
            offset -= numberOfPixelsNorthToNorth;
        }
        
        rectTransform.anchoredPosition = startPosition + new Vector2(offset, 0);
    }
}
