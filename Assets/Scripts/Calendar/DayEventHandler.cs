using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DayEventHandler : MonoBehaviour
{
    public DayEventType dayEventType; // Reference to the DayEventType ScriptableObject
    
    public string eventName; // Name of the event
    public string description; // Description of the event
    public Image icon; // Icon representing the event
    public Color color; // Color associated with the event
    public Color textColor; // Color for the text associated with the event
    public bool isRecurring; // Whether the event recurs every year
    public bool isSpecialEvent; // Whether the event is a special event (e.g., holiday, festival)
    
    public TMP_Text dayoftheweekText; // UI Text component for displaying the event name
    public TMP_Text dateText; // UI Text component for displaying the date of the event
    public TMP_Text eventNameText; // UI Text component for displaying the event name
    public TMP_Text descriptionText; // UI Text component for displaying the event description
    public Image imageRender; // SpriteRenderer for displaying the event icon
    public Image backgroundColorImage; 
    
    
    public void Initialize()
    {
       eventName = dayEventType.eventName;
       description = dayEventType.description;
       icon = dayEventType.icon;
       color = dayEventType.color;
       isRecurring = dayEventType.isRecurring; 
       isSpecialEvent = dayEventType.isSpecialEvent;
       textColor = dayEventType.textColor;
       UpdateUI();
       
       if(textColor != null)
       {
           SetAllTextColor(textColor);
       }
    }
    
    public void UpdateUI()
    {
        if (eventNameText != null)
            eventNameText.text = eventName;
        
        if (descriptionText != null)
            descriptionText.text = description;
        
        if (imageRender != null)
            imageRender = icon;
        
        if (backgroundColorImage != null)
            backgroundColorImage.color = color;
    }
    
    public void SetAllTextColor(Color color)
    {
        if (dayoftheweekText != null)
            dayoftheweekText.color = color;
        
        if (dateText != null)
            dateText.color = color;
        
        if (eventNameText != null)
            eventNameText.color = color;
        
        if (descriptionText != null)
            descriptionText.color = color;
    }
    
    
    
    
  
}
