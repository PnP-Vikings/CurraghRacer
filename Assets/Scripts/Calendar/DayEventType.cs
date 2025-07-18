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

    [CreateAssetMenu(fileName = "DayEventType", menuName = "Scriptable Objects/DayEventType")]
    public class DayEventType : ScriptableObject
    {
        [Header("Event Info")]
        public string eventName;
        public string description;
        public Image icon;
        public Color color = Color.white;
        public Color textColor = Color.black;
        public bool isHoliday;
        public bool isSpecialEvent;
        
        [Header("Date Configuration")]
        public RecurrenceType recurrenceType = RecurrenceType.None;
        
        [Header("Specific Date (for one-time or yearly events)")]
        public int dayOfMonth = 1;
        public int month = 1; // 1-12
        public int year; // 0 for recurring events
        
        [Header("Weekly Recurrence")]
        public DayOfWeek dayOfWeek = DayOfWeek.Sunday;
        
        [Header("Custom Recurrence")]
        public int intervalDays = 1; // For custom intervals
        public DateTime StartDate; // When the pattern starts
        
        /// <summary>
        /// Checks if this event occurs on the given date
        /// </summary>
        public bool OccursOnDate(DateTime date)
        {
            switch (recurrenceType)
            {
                case RecurrenceType.None:
                    return date.Day == dayOfMonth && date.Month == month && date.Year == year;
                    
                case RecurrenceType.Daily:
                    return date >= StartDate;
                    
                case RecurrenceType.Weekly:
                    return date.DayOfWeek == (System.DayOfWeek)dayOfWeek && date >= StartDate;
                    
                case RecurrenceType.Monthly:
                    return date.Day == dayOfMonth && date >= StartDate;
                    
                case RecurrenceType.Yearly:
                    return date.Day == dayOfMonth && date.Month == month && date >= StartDate;
                    
                case RecurrenceType.Custom:
                    if (date < StartDate) return false;
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
            switch (recurrenceType)
            {
                case RecurrenceType.Weekly:
                    DateTime nextWeek = afterDate.AddDays(1);
                    while (nextWeek.DayOfWeek != (System.DayOfWeek)dayOfWeek)
                    {
                        nextWeek = nextWeek.AddDays(1);
                    }
                    return nextWeek;
                    
                case RecurrenceType.Yearly:
                    DateTime nextYear = new DateTime(afterDate.Year, month, dayOfMonth);
                    if (nextYear <= afterDate)
                        nextYear = nextYear.AddYears(1);
                    return nextYear;
                    
                // Add more cases as needed
                default:
                    return null;
            }
        }
    }
}
