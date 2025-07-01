using TMPro;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "DayEventType", menuName = "Scriptable Objects/DayEventType")]
public class DayEventType : ScriptableObject
{
    public string eventName; // Name of the event
    public string description; // Description of the event
    public Image icon; // Icon representing the event
    public Color color; // Color associated with the event
    public Color textColor; // Color for the text associated with the event
    public bool isRecurring; // Whether the event recurs every year
    public bool isSpecialEvent; // Whether the event is a special event (e.g., holiday, festival)

    
}
