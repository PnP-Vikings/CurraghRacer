using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Calendar;

namespace Calendar
{
    public class DayEventHandler : MonoBehaviour
    {
        public DayEventType dayEventType; // Reference to the DayEventType ScriptableObject
        
        [Header("UI Components")]
        public TMP_Text dayoftheweekText; // UI Text component for displaying day of week
        public TMP_Text dateText; // UI Text component for displaying the date
        public TMP_Text eventNameText; // UI Text component for displaying the event name
        public TMP_Text descriptionText; // UI Text component for displaying the event description
        public Image imageRender; // Image for displaying the event icon
        public Image backgroundColorImage;
        
        public void SetupDay(DateTime date)
        {
            dateText.text = date.Day.ToString();
            dayoftheweekText.text = date.DayOfWeek.ToString();
            
            // Clear any previous event
            ClearEvent();
        }
        
        public void SetEvent(DayEventType eventType)
        {
            dayEventType = eventType;
            Initialize();
        }
        
        public void Initialize()
        {
            if (dayEventType == null) return;
            
            UpdateUI();
            
            if (dayEventType.textColor != Color.clear)
            {
                SetAllTextColor(dayEventType.textColor);
            }
        }
        
        public void UpdateUI()
        {
            if (dayEventType == null) return;
            
            if (eventNameText != null)
                eventNameText.text = dayEventType.eventName;
            
            if (descriptionText != null)
                descriptionText.text = dayEventType.description;
            
            if (imageRender != null && dayEventType.icon != null)
                imageRender.sprite = dayEventType.icon.sprite;
            
            if (backgroundColorImage != null)
                backgroundColorImage.color = dayEventType.color;
        }
        
        public void ClearEvent()
        {
            dayEventType = null;
            
            if (eventNameText != null)
                eventNameText.text = "";
            
            if (descriptionText != null)
                descriptionText.text = "";
            
            if (imageRender != null)
                imageRender.sprite = null;
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
}
