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
        
        public void SetEvent(DayEventType eventType, DateTime currentDate, DateTime eventDate)
        {
            dayEventType = eventType;
            
            if (eventNameText != null)
                eventNameText.text = dayEventType.eventName;
            
            if (descriptionText != null)
                descriptionText.text = dayEventType.description;
            
            // Use the appropriate icon based on whether this specific occurrence has passed
            Image eventIcon = dayEventType.GetEventIcon(eventDate, currentDate);
            if (imageRender != null && eventIcon != null)
                imageRender.sprite = eventIcon.sprite;
            
            // Use the appropriate background color based on whether this specific occurrence has passed
            if (backgroundColorImage != null)
                backgroundColorImage.color = dayEventType.GetEventColor(eventDate, currentDate);

            // Use the appropriate text color based on whether this specific occurrence has passed
            Color textColor = dayEventType.GetEventTextColor(eventDate, currentDate);
            if (textColor != Color.clear)
            {
                SetAllTextColor(textColor);
            }
        }
        
        public void Initialize()
        {
            if (dayEventType == null) return;
            
            // Use current game time if no specific date is provided
            DateTime currentDate = TimeManager.Instance.GetCurrentDate();
            Initialize();
        }
        
        public void Initialize(DateTime currentDate,DateTime eventDate)
        {
            if (dayEventType == null) return;
            
            UpdateUI(currentDate,eventDate);
            
            // Use the appropriate text color based on whether event has passed
            Color textColor = dayEventType.GetEventTextColor(currentDate,eventDate);
            if (textColor != Color.clear)
            {
                SetAllTextColor(textColor);
            }
        }
        
        public void UpdateUI()
        {
            if (dayEventType == null) return;
            
            // Use current game time if no specific date is provided
            DateTime currentDate = TimeManager.Instance.GetCurrentDate();
            UpdateUI();
        }
        
        public void UpdateUI(DateTime currentDate,DateTime eventDate)
        {
            if (dayEventType == null) return;
            
            if (eventNameText != null)
                eventNameText.text = dayEventType.eventName;
            
            if (descriptionText != null)
                descriptionText.text = dayEventType.description;
            

            // Use the appropriate icon based on whether this specific occurrence has passed
            Image eventIcon = dayEventType.GetEventIcon(eventDate, currentDate);
            if (imageRender != null && eventIcon != null)
                imageRender.sprite = eventIcon.sprite;
            
         // Use the appropriate background color based on whether this specific occurrence has passed
            if (backgroundColorImage != null)
                backgroundColorImage.color = dayEventType.GetEventColor(eventDate, currentDate);

            if (eventNameText != null)
            {
                Color textColor = dayEventType.GetEventTextColor(eventDate,currentDate);
                SetAllTextColor(textColor);
            }


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
