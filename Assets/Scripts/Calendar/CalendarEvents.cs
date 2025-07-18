using System.Collections.Generic;
using UnityEngine;

namespace Calendar
{
    [CreateAssetMenu(fileName = "CalendarEvents", menuName = "Scriptable Objects/CalendarEvents")]
    public class CalendarEvents : ScriptableObject
    {
        public List<DayEventType> calendarDayEvents = new List<DayEventType>();
        
        [Header("Common Holidays")]
        public List<DayEventType> commonHolidays = new List<DayEventType>();

        /// <summary>
        /// Add a custom event to the calendar
        /// </summary>
        public void AddEvent(DayEventType eventType)
        {
            if (!calendarDayEvents.Contains(eventType))
            {
                calendarDayEvents.Add(eventType);
            }
        }
        
        /// <summary>
        /// Remove an event from the calendar
        /// </summary>
        public void RemoveEvent(DayEventType eventType)
        {
            calendarDayEvents.Remove(eventType);
        }

        /// <summary>
        /// Get all events (custom and common holidays) occurring on the specified date
        /// </summary>
        public List<DayEventType> GetEventsOnDate(System.DateTime date)
        {
            List<DayEventType> events = new List<DayEventType>();
            foreach (var evt in calendarDayEvents)
            {
                if (evt.OccursOnDate(date))
                    events.Add(evt);
            }
            foreach (var hol in commonHolidays)
            {
                if (hol.OccursOnDate(date))
                    events.Add(hol);
            }
            return events;
        }
    }
}
