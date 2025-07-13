using System;
using UnityEngine;
using Calendar;

namespace Calendar
{
    /// <summary>
    /// Utility class for creating common recurring events and holidays
    /// </summary>
    public static class EventScheduler
    {
        /// <summary>
        /// Creates a weekly recurring event (e.g., races every Sunday)
        /// </summary>
        public static DayEventType CreateWeeklyEvent(string eventName, string description, 
            DayOfWeek dayOfWeek, Color eventColor, DateTime startDate)
        {
            var eventType = ScriptableObject.CreateInstance<DayEventType>();
            eventType.eventName = eventName;
            eventType.description = description;
            eventType.recurrenceType = RecurrenceType.Weekly;
            eventType.dayOfWeek = dayOfWeek;
            eventType.color = eventColor;
            eventType.StartDate = startDate;
            eventType.isSpecialEvent = false;
            
            return eventType;
        }
        
        /// <summary>
        /// Creates a yearly recurring event (e.g., Christmas on December 25th)
        /// </summary>
        public static DayEventType CreateYearlyEvent(string eventName, string description,
            int month, int day, Color eventColor, DateTime startDate)
        {
            var eventType = ScriptableObject.CreateInstance<DayEventType>();
            eventType.eventName = eventName;
            eventType.description = description;
            eventType.recurrenceType = RecurrenceType.Yearly;
            eventType.month = month;
            eventType.dayOfMonth = day;
            eventType.color = eventColor;
            eventType.StartDate = startDate;
            eventType.isSpecialEvent = true;
            
            return eventType;
        }
        
        /// <summary>
        /// Creates a one-time event
        /// </summary>
        public static DayEventType CreateOneTimeEvent(string eventName, string description,
            DateTime eventDate, Color eventColor)
        {
            var eventType = ScriptableObject.CreateInstance<DayEventType>();
            eventType.eventName = eventName;
            eventType.description = description;
            eventType.recurrenceType = RecurrenceType.None;
            eventType.month = eventDate.Month;
            eventType.dayOfMonth = eventDate.Day;
            eventType.year = eventDate.Year;
            eventType.color = eventColor;
            eventType.isSpecialEvent = false;
            
            return eventType;
        }
        
        /// <summary>
        /// Pre-defined common holidays
        /// </summary>
        public static class CommonHolidays
        {
            public static DayEventType Christmas => CreateYearlyEvent(
                "Christmas", 
                "Christmas Day celebration", 
                12, 25, 
                Color.red, 
                new DateTime(1981, 1, 1)
            );
            
            public static DayEventType NewYear => CreateYearlyEvent(
                "New Year's Day", 
                "Start of the new year", 
                1, 1, 
                Color.yellow, 
                new DateTime(1981, 1, 1)
            );
            
            public static DayEventType Halloween => CreateYearlyEvent(
                "Halloween", 
                "Spooky Halloween night", 
                10, 31, 
                new Color(1f, 0.5f, 0f), // Orange
                new DateTime(1981, 1, 1)
            );
            
            public static DayEventType ValentinesDay => CreateYearlyEvent(
                "Valentine's Day", 
                "Day of love and romance", 
                2, 14, 
                new Color(1f, 0.4f, 0.7f), // Pink
                new DateTime(1981, 1, 1)
            );
        }
        
        /// <summary>
        /// Pre-defined racing events
        /// </summary>
        public static class RacingEvents
        {
            public static DayEventType WeeklyRace => CreateWeeklyEvent(
                "Weekly Race",
                "Regular weekly racing competition",
                DayOfWeek.Sunday,
                new Color(0f, 0.8f, 0f), // Green
                new DateTime(1981, 1, 5) // First Sunday 
            );
            
            public static DayEventType ChampionshipRace => CreateWeeklyEvent(
                "Championship Race",
                "High-stakes championship event",
                DayOfWeek.Saturday,
                new Color(1f, 0.8f, 0f), // Gold
                new DateTime(1981, 1, 4) // First Saturday
            );
        }
    }
}
