using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using Calendar;

namespace Calendar
{
    public class DayEventHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public DayEventType dayEventType; // Reference to the DayEventType ScriptableObject
        
        [Header("UI Components")]
        public TMP_Text dayoftheweekText; // UI Text component for displaying day of week
        public TMP_Text dateText; // UI Text component for displaying the date
        public TMP_Text eventNameText; // UI Text component for displaying the event name
        public TMP_Text descriptionText; // UI Text component for displaying the event description
        public Image imageRender; // Image for displaying the event icon
        public Image backgroundColorImage;
        
        [Header("Date Information")]
        public DateTime currentDate; // Store the current date for this day
        
        [Header("Calendar Events Reference")]
        public CalendarEvents calendarEvents; // Reference to get tooltip content
        
        private void Awake()
        {
            Debug.Log($"🔧 DayEventHandler Awake() called on {gameObject.name}");
            
            // FORCE a visible, clickable Image component for raycast detection
            Image raycastImage = GetComponent<Image>();
            if (raycastImage == null)
            {
                raycastImage = gameObject.AddComponent<Image>();
                Debug.Log($"➕ Added Image component to {gameObject.name}");
            }
            
            // Make it slightly visible and ensure raycast target is enabled
            raycastImage.raycastTarget = true;
            Debug.Log($"✅ Set raycast target on {gameObject.name} - Color: {raycastImage.color}, RaycastTarget: {raycastImage.raycastTarget}");
            
            // Also ensure background image doesn't block if it exists
            if (backgroundColorImage != null)
            {
                backgroundColorImage.raycastTarget = true;
                Debug.Log($"✅ Background image raycast target set on {gameObject.name}");
            }
            
            // Get CalendarEvents reference if not assigned
            if (calendarEvents == null)
            {
                CalendarUI calendarUI = GetComponentInParent<CalendarUI>();
                if (calendarUI != null)
                {
                    calendarEvents = calendarUI.calendarEvents;
                    Debug.Log($"✅ Found CalendarEvents from parent CalendarUI on {gameObject.name}");
                }
                else
                {
                    Debug.LogError($"❌ Could not find CalendarUI parent for {gameObject.name}");
                }
            }
            else
            {
                Debug.Log($"✅ CalendarEvents already assigned to {gameObject.name}");
            }
        }
        
        private void Start()
        {
            Debug.Log($"🚀 DayEventHandler Start() called on {gameObject.name} for date {currentDate.ToString("MMM dd, yyyy")}");
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            Debug.Log($"🖱️ HOVER ENTER: {currentDate.ToString("MMM dd, yyyy")} on GameObject: {gameObject.name}");
            Debug.Log($"🔍 CalendarTooltip.Instance exists: {CalendarTooltip.Instance != null}");
            Debug.Log($"🔍 calendarEvents exists: {calendarEvents != null}");
            
            if (CalendarTooltip.Instance != null && calendarEvents != null)
            {
                string tooltipContent = calendarEvents.GetDetailedTooltipForDate(currentDate);
                Debug.Log($"🎯 Tooltip content for {currentDate.ToString("MMM dd, yyyy")}: '{tooltipContent}'");
                Debug.Log($"🎯 Tooltip content length: {(tooltipContent?.Length ?? 0)}");
                Debug.Log($"🎯 Tooltip content is null or empty: {string.IsNullOrEmpty(tooltipContent)}");
                
                if (!string.IsNullOrEmpty(tooltipContent))
                {
                    CalendarTooltip.Instance.ShowTooltip(tooltipContent);
                    Debug.Log($"✅ Called ShowTooltip with actual content: {tooltipContent}");
                }
                else
                {
                    Debug.Log($"❌ No tooltip content for {currentDate.ToString("MMM dd, yyyy")} - checking what events exist...");
                    
                    // Let's check what events are available
                    var allEvents = calendarEvents.GetEventsOnDate(currentDate);
                    Debug.Log($"📅 Total events on {currentDate.ToString("MMM dd, yyyy")}: {allEvents?.Count ?? 0}");
                    
                    if (allEvents != null && allEvents.Count > 0)
                    {
                        foreach (var evt in allEvents)
                        {
                            Debug.Log($"  - Event: {evt?.eventName ?? "NULL"}, Active: {evt?.eventActive ?? false}, PlayerTookPart: {evt?.playerHasTakenPart ?? false}, Type: {evt?.OccasionType}");
                        }
                    }
                    
                    // Also check completed races
                    Debug.Log($"📊 Completed races count: {calendarEvents.completedRaces?.Count ?? 0}");
                    if (calendarEvents.completedRaces != null && calendarEvents.completedRaces.Count > 0)
                    {
                        foreach (var race in calendarEvents.completedRaces)
                        {
                            if (race != null && race.raceData.raceDate.Date == currentDate.Date)
                            {
                                Debug.Log($"  - Completed race on this date: {race.dayEventType?.eventName ?? "NULL"}");
                                Debug.Log($"    Race details: Winner: {race.raceData.GetRaceWinner()}, PlayerPosition: {race.raceData.playerPosition}, TotalShips: {race.raceData.totalParticipants}");
                            }
                        }
                    }
                }
            }
            else
            {
                if (CalendarTooltip.Instance == null)
                    Debug.LogError("❌ CalendarTooltip.Instance is NULL!");
                if (calendarEvents == null)
                    Debug.LogError("❌ calendarEvents is NULL!");
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            Debug.Log($"🖱️ Stopped hovering over date: {currentDate.ToString("MMM dd, yyyy")}");
            
            if (CalendarTooltip.Instance != null)
            {
                CalendarTooltip.Instance.HideTooltip();
            }
        }
        
        public void SetupDay(DateTime date)
        {
            currentDate = date; // Store the date so CalendarTooltip can access it
            dateText.text = date.Day.ToString();
            if (TimeManager.Instance != null)
            {
                dayoftheweekText.text = TimeManager.Instance.GetDayOfWeekString((int)date.DayOfWeek);
            }
            else
            {
                dayoftheweekText.text = date.DayOfWeek.ToString();
            }
        
            
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
