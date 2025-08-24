using System.Collections.Generic;
using UnityEngine;
using League;

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
            
            // Check custom calendar events first (existing functionality)
            foreach (var evt in calendarDayEvents)
            {
                if (evt.OccursOnDate(date) && evt.eventActive)
                    events.Add(evt);
            }
            
            // Check common holidays (existing functionality)
            foreach (var hol in commonHolidays)
            {
                if (hol.OccursOnDate(date) && hol.eventActive)
                    events.Add(hol);
            }
            
            // Check for tournament race days (NEW: only if player has joined a tournament)
            var tournamentRaceEvent = CheckForTournamentRaceDay(date);
            if (tournamentRaceEvent != null)
            {
                events.Add(tournamentRaceEvent);
            }
            
            return events;
        }
        
        /// <summary>
        /// Checks for tournament race day and returns race event if player participated
        /// </summary>
        private DayEventType CheckForTournamentRaceDay(System.DateTime date)
        {
            // Only check if LeagueController exists and player has joined a tournament
            if (LeagueController.Instance == null || 
                LeagueController.Instance.currentLeague == null || 
                !LeagueController.Instance.currentLeague.playerHasJoined)
                return null;
                
            var currentLeague = LeagueController.Instance.currentLeague;
            
            // Check if this date matches any scheduled race days
            if (currentLeague.raceDays != null)
            {
                for (int i = 0; i < currentLeague.raceDays.Length; i++)
                {
                    var raceDay = currentLeague.raceDays[i];
                    
                    // Calculate the actual date for this race (assuming races are on Sundays)
                    var raceDates = TimeManager.Instance.ReturnAllSundaysDuringTournament(currentLeague.tournamentStartDate,currentLeague.raceDays.Length);
                    
                    if (i < raceDates.Length && raceDates[i].Date == date.Date)
                    {
                        // Check if any race on this day includes the player
                        bool playerParticipating = false;
                        bool raceCompleted = false;
                        
                        foreach (var race in raceDay.races)
                        {
                            if (race.teams != null)
                            {
                                foreach (var team in race.teams)
                                {
                                    if (team.teamType == TeamType.Player)
                                    {
                                        playerParticipating = true;
                                        raceCompleted = race.processed;
                                        break;
                                    }
                                }
                            }
                        }
                        
                        // Only return race event if player is actually participating
                        if (playerParticipating)
                        {
                            return CreateRaceEvent(currentLeague.leagueName, raceCompleted);
                        }
                    }
                }
            }
            
            return null; // No race event for this date
        }
        
        /// <summary>
        /// Creates a dynamic race event for the calendar
        /// </summary>
        private DayEventType CreateRaceEvent(string leagueName, bool completed)
        {
            var raceEvent = ScriptableObject.CreateInstance<DayEventType>();
            raceEvent.eventName = completed ? $"{leagueName} Race (Completed)" : $"{leagueName} Race";
            raceEvent.description = completed ? "You participated in this race" : "Upcoming race day";
            raceEvent.OccasionType = OccasionType.Race;
            raceEvent.eventActive = true;
            raceEvent.playerHasTakenPart = true;
            raceEvent.haspassed = completed;
            
            // Set colors based on completion status
            if (completed)
            {
                raceEvent.color = raceEvent.hasPassedcolor;
                raceEvent.textColor = raceEvent.hasPassedtextColor;
            }
            else
            {
                raceEvent.color = Color.red; // Upcoming race color
                raceEvent.textColor = Color.white;
            }
            
            return raceEvent;
        }
    }
}
