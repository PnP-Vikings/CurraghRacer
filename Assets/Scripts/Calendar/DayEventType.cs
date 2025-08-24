using UnityEngine;
using UnityEngine.UI;
using System;

namespace Calendar
{
    public enum RecurrenceType
    {
        None,           // One-time event
        Daily,          // Every day
        Weekly,         // Every week on the same day
        Monthly,        // Every month on the same date
        Yearly,         // Every year on the same date
        Custom          // Custom pattern
    }

    public enum DayOfWeek
    {
        Sunday = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6
    }

    public enum OccasionType
    {
        None,
        Holiday,
        Work,
        SpecialEvent
        ,Race,
        Custom
    }

    [CreateAssetMenu(fileName = "DayEventType", menuName = "Scriptable Objects/DayEventType")]
    public class DayEventType : ScriptableObject
    {
        [Header("Event Info")]
        public string eventName;
        public string description;
        public Image icon;
        public Color color = Color.white;
        public Color textColor = Color.black;
        
        [Header("Event Type")]
        public OccasionType OccasionType = OccasionType.None; // Type of the event (e.g., Holiday, Work, Special)
        
        [Header("Date Configuration")]
        public RecurrenceType recurrenceType = RecurrenceType.None;
        
        [Header("Specific Date (for one-time or yearly events)")]
        public int dayOfMonth = 1;
        public int month = 1; // 1-12
        public int year; // 0 for recurring events
        public bool eventActive = true; // Whether this event is currently active
        
        public bool hasEndDate = false; // Whether this event has an end date
        
        [Header("Past Event Display")]
        [Tooltip("Whether this event has already passed")]
        public bool haspassed = false; // Whether this event has already passed
        [Tooltip("Icon to use when event has passed")]
        public Image hasPassedicon;
        [Tooltip("Background color when event has passed")]
        public Color hasPassedcolor = Color.grey;
        [Tooltip("Text color when event has passed")]
        public Color hasPassedtextColor = Color.black;
        
        
        
        [Header("Weekly Recurrence")]
        public DayOfWeek dayOfWeek = DayOfWeek.Sunday;
        
        [Header("Custom Recurrence")]
        public int intervalDays = 1; // For custom intervals
        
        [Header("Date Range (for recurring events)")]
        [Tooltip("When the recurring pattern starts")]
        public DateTime StartDate; // When the pattern starts
        [Tooltip("When the recurring pattern ends (leave default for no end date)")]
        public DateTime EndDate; // When the pattern ends, if applicable
        
        public bool playerHasTakenPart = false; // Whether the player is involved in this event
        
        /// <summary>
        /// Checks if this event occurs on the given date
        /// </summary>
        public bool OccursOnDate(DateTime date)
        {
            // First check if the event is active
            if (!eventActive) return false;
            
            switch (recurrenceType)
            {
                case RecurrenceType.None:
                    return date.Day == dayOfMonth && date.Month == month && date.Year == year;
                    
                case RecurrenceType.Daily:
                    return date >= StartDate && (EndDate == default || date <= EndDate);
                    
                case RecurrenceType.Weekly:
                    return date.DayOfWeek == (System.DayOfWeek)dayOfWeek && 
                           date >= StartDate && 
                           (EndDate == default || date <= EndDate);
                    
                case RecurrenceType.Monthly:
                    return date.Day == dayOfMonth && 
                           date >= StartDate && 
                           (EndDate == default || date <= EndDate);
                    
                case RecurrenceType.Yearly:
                    return date.Day == dayOfMonth && date.Month == month && 
                           date >= StartDate && 
                           (EndDate == default || date <= EndDate);
                    
                case RecurrenceType.Custom:
                    if (date < StartDate || (EndDate != default && date > EndDate)) return false;
                    TimeSpan diff = date - StartDate;
                    return diff.Days % intervalDays == 0;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Gets the next occurrence of this event after the given date
        /// </summary>
        public DateTime? GetNextOccurrence(DateTime afterDate)
        {
            if (!eventActive) return null;
            
            switch (recurrenceType)
            {
                case RecurrenceType.Daily:
                    DateTime nextDay = afterDate.AddDays(1);
                    if (EndDate != default && nextDay > EndDate) return null;
                    return nextDay >= StartDate ? nextDay : StartDate;
                    
                case RecurrenceType.Weekly:
                    DateTime nextWeek = afterDate.AddDays(1);
                    while (nextWeek.DayOfWeek != (System.DayOfWeek)dayOfWeek)
                    {
                        nextWeek = nextWeek.AddDays(1);
                    }
                    if (EndDate != default && nextWeek > EndDate) return null;
                    return nextWeek >= StartDate ? nextWeek : null;
                    
                case RecurrenceType.Monthly:
                    DateTime nextMonth = new DateTime(afterDate.Year, afterDate.Month, dayOfMonth);
                    if (nextMonth <= afterDate)
                    {
                        nextMonth = nextMonth.AddMonths(1);
                        // Handle edge case where dayOfMonth doesn't exist in next month
                        if (nextMonth.Month != nextMonth.AddDays(dayOfMonth - nextMonth.Day).Month)
                        {
                            nextMonth = new DateTime(nextMonth.Year, nextMonth.Month, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
                        }
                    }
                    if (EndDate != default && nextMonth > EndDate) return null;
                    return nextMonth >= StartDate ? nextMonth : null;
                    
                case RecurrenceType.Yearly:
                    DateTime nextYear = new DateTime(afterDate.Year, month, dayOfMonth);
                    if (nextYear <= afterDate)
                        nextYear = nextYear.AddYears(1);
                    if (EndDate != default && nextYear > EndDate) return null;
                    return nextYear >= StartDate ? nextYear : null;
                    
                case RecurrenceType.Custom:
                    DateTime nextCustom = afterDate.AddDays(1);
                    while (nextCustom <= (EndDate == default ? DateTime.MaxValue : EndDate))
                    {
                        if (nextCustom >= StartDate)
                        {
                            TimeSpan diff = nextCustom - StartDate;
                            if (diff.Days % intervalDays == 0)
                                return nextCustom;
                        }
                        nextCustom = nextCustom.AddDays(1);
                    }
                    return null;
                    
                case RecurrenceType.None:
                    DateTime oneTimeDate = new DateTime(year, month, dayOfMonth);
                    return oneTimeDate > afterDate ? oneTimeDate : null;
                    
                default:
                    return null;
            }
        }
        
        /// <summary>
        /// Checks if this event is currently active (not expired)
        /// </summary>
        public bool IsCurrentlyActive(DateTime currentDate)
        {
            if (!eventActive) return false;
            
            switch (recurrenceType)
            {
                case RecurrenceType.None:
                    DateTime eventDate = new DateTime(year, month, dayOfMonth);
                    return currentDate <= eventDate;
                    
                default:
                    return EndDate == default || currentDate <= EndDate;
            }
        }
        
        /// <summary>
        /// Checks if this event has passed based on the current date
        /// For recurring events, checks if the specific occurrence on the given date has passed
        /// </summary>
        public bool HasPassed(DateTime eventDate, DateTime currentDate)
        {
            // Simply compare the event occurrence date with the current date
            return currentDate.Date > eventDate.Date;
        }
        
        /// <summary>
        /// Checks if this event has passed based on the current date (legacy method)
        /// </summary>
        public bool HasPassed(DateTime currentDate)
        {
            switch (recurrenceType)
            {
                case RecurrenceType.None:
                    DateTime eventDate = new DateTime(year, month, dayOfMonth);
                    return currentDate.Date > eventDate.Date;
                    
                case RecurrenceType.Daily:
                case RecurrenceType.Weekly:
                case RecurrenceType.Monthly:
                case RecurrenceType.Yearly:
                case RecurrenceType.Custom:
                    // For recurring events, they don't "pass" in the traditional sense
                    // unless they have an end date that has passed
                    return EndDate != default && currentDate.Date > EndDate.Date;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Gets the appropriate color for this event based on whether it has passed
        /// </summary>
        public Color GetEventColor(DateTime eventDate, DateTime currentDate)
        {
            return HasPassed(eventDate, currentDate) ? hasPassedcolor : color;
        }
        
        /// <summary>
        /// Gets the appropriate color for this event based on whether it has passed (legacy)
        /// </summary>
        public Color GetEventColor(DateTime currentDate)
        {
            return HasPassed(currentDate) ? hasPassedcolor : color;
        }
        
        /// <summary>
        /// Gets the appropriate text color for this event based on whether it has passed
        /// </summary>
        public Color GetEventTextColor(DateTime eventDate, DateTime currentDate)
        {
            return HasPassed(eventDate, currentDate) ? hasPassedtextColor : textColor;
        }
        
        /// <summary>
        /// Gets the appropriate text color for this event based on whether it has passed (legacy)
        /// </summary>
        public Color GetEventTextColor(DateTime currentDate)
        {
            return HasPassed(currentDate) ? hasPassedtextColor : textColor;
        }
        
        /// <summary>
        /// Gets the appropriate icon for this event based on whether it has passed
        /// </summary>
        public Image GetEventIcon(DateTime eventDate, DateTime currentDate)
        {
            return HasPassed(eventDate, currentDate) && hasPassedicon != null ? hasPassedicon : icon;
        }
        
        /// <summary>
        /// Gets the appropriate icon for this event based on whether it has passed (legacy)
        /// </summary>
        public Image GetEventIcon(DateTime currentDate)
        {
            return HasPassed(currentDate) && hasPassedicon != null ? hasPassedicon : icon;
        }
    }
}
