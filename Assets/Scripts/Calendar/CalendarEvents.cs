using System.Collections.Generic;
using UnityEngine;

namespace Calendar
{
    [CreateAssetMenu(fileName = "CalendarEvents", menuName = "Scriptable Objects/CalendarEvents")]
    public class CalendarEvents : ScriptableObject
    {
        public List<DayEventType> calendarDayEvents = new List<DayEventType>();
        
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
    }
}
