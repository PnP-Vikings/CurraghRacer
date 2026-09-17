using Calendar;
using UnityEngine;
using System;
using League;
using UnityEngine.Localization;

namespace Calendar
{
    [System.Serializable]
    public class CompletedRaceData
    {
        public string leagueName;
        public LocalizedString localizedLeagueName;
        public string raceName;
        public LocalizedString localizedRaceName;
        public DateTime raceDate;
        public int playerPosition;
        public int totalParticipants;
        public string trackName;
        public float raceTime;
        public int pointsEarned;
        public string[] participantNames;
        public bool playerWon;

        
        
        public CompletedRaceData(string leagueName, string raceName, DateTime raceDate, 
                               int playerPosition, int totalParticipants, string trackName, 
                               float raceTime, int pointsEarned, string[] participantNames, LocalizedString localizedLeagueName = null, LocalizedString localizedRaceName = null)
        {
            this.leagueName = leagueName;
            this.raceName = raceName;
            this.raceDate = raceDate;
            this.playerPosition = playerPosition;
            this.totalParticipants = totalParticipants;
            this.trackName = trackName;
            this.raceTime = raceTime;
            this.pointsEarned = pointsEarned;
            this.participantNames = participantNames;
            this.playerWon = playerPosition == 1;
            this.localizedLeagueName = localizedLeagueName;
            this.localizedRaceName = localizedRaceName;
        }
        
        public string GetFormattedTime()
        {
            TimeSpan time = TimeSpan.FromSeconds(raceTime);
            return string.Format("{0:D2}:{1:D2}:{2:D3}", time.Minutes, time.Seconds, time.Milliseconds);
        }
        
        public string GetPositionText()
        {
            string suffix = "th";
            if (playerPosition == 1) suffix = "st";
            else if (playerPosition == 2) suffix = "nd";
            else if (playerPosition == 3) suffix = "rd";
            
            return $"{playerPosition}{suffix}";
        }
        
        public string GetRaceWinner()
        {
            return participantNames != null && participantNames.Length >= 1 ? participantNames[0] : "N/A";
        }
    }
    
    public class CompletedRaces : MonoBehaviour
    {
        [Header("Race Event Configuration")]
        public DayEventType dayEventType;
        
        [Header("Completed Race Details")]
        public CompletedRaceData raceData;
        
        [Header("Visual Configuration")]
        public Color winColor = new Color(1f, 0.84f, 0f); // Gold
        public Color podiumColor = new Color(0.75f, 0.75f, 0.75f); // Silver
        public Color participatedColor = new Color(0.68f, 0.85f, 0.9f); // Light blue
        
        [Header("Localization")]
        LocalizedString localizedRaceDescription = new LocalizedString { TableReference = "CalendarEvents", TableEntryReference = "CalendarEvents.CompletedRaces.RaceDescription" };
        LocalizedString localizedDetailedRaceResult = new LocalizedString { TableReference = "CalendarEvents", TableEntryReference = "CalendarEvents.CompletedRaces.GetDetailedTooltip.Result" };
        LocalizedString localizedDetailedRaceTime = new LocalizedString { TableReference = "CalendarEvents", TableEntryReference = "CalendarEvents.CompletedRaces.GetDetailedTooltip.Time" };
        LocalizedString localizedDetailedRaceTrack = new LocalizedString { TableReference = "CalendarEvents", TableEntryReference = "CalendarEvents.CompletedRaces.GetDetailedTooltip.Track" };
        LocalizedString localizedDetailedRacePoints = new LocalizedString { TableReference = "CalendarEvents", TableEntryReference = "CalendarEvents.CompletedRaces.GetDetailedTooltip.Points" };
        LocalizedString localizedDetailedRaceDate = new LocalizedString { TableReference = "CalendarEvents", TableEntryReference = "CalendarEvents.CompletedRaces.GetDetailedTooltip.Date" };
        LocalizedString localizedDetailedRaceParticipants = new LocalizedString { TableReference = "CalendarEvents", TableEntryReference = "CalendarEvents.CompletedRaces.GetDetailedTooltip.Participants" };

        
        
        
        public void Initialize(CompletedRaceData data)
        {
            raceData = data;
            
            // Create or update the day event type for this completed race
            if (dayEventType == null)
            {
                dayEventType = ScriptableObject.CreateInstance<DayEventType>();
            }
            
            SetupDayEvent();
        }
        
        private void SetupDayEvent()
        {
            if (dayEventType == null || raceData == null) return;
            
            string leagueDisplayName = raceData.localizedLeagueName != null && !raceData.localizedLeagueName.IsEmpty ? raceData.localizedLeagueName.GetLocalizedString() : raceData.leagueName;
            string raceDisplayName = raceData.localizedRaceName != null && !raceData.localizedRaceName.IsEmpty ? raceData.localizedRaceName.GetLocalizedString(LeagueController.Instance.currentLeague.currentRace) : raceData.raceName;
            
            dayEventType.eventName = $"{leagueDisplayName} - {raceDisplayName}";
            dayEventType.description = GetRaceDescription();
            dayEventType.OccasionType = OccasionType.Race;
            dayEventType.eventActive = true;
            dayEventType.playerHasTakenPart = true;
            dayEventType.haspassed = true;
            
            // Set the specific date for this completed race
            dayEventType.recurrenceType = RecurrenceType.None;
            dayEventType.dayOfMonth = raceData.raceDate.Day;
            dayEventType.month = raceData.raceDate.Month;
            dayEventType.year = raceData.raceDate.Year;
            
            // Set colors based on performance
            SetEventColors();
        }
        
        private void SetEventColors()
        {
            if (raceData.playerWon)
            {
                dayEventType.hasPassedcolor = winColor;
                dayEventType.color = winColor;
            }
            else if (raceData.playerPosition <= 3)
            {
                dayEventType.hasPassedcolor = podiumColor;
                dayEventType.color = podiumColor;
            }
            else
            {
                dayEventType.hasPassedcolor = participatedColor;
                dayEventType.color = participatedColor;
            }
            
            dayEventType.hasPassedtextColor = Color.white;
            dayEventType.textColor = Color.white;
        }
        
        private string GetRaceDescription()
        {
            string raceDescription =  localizedRaceDescription != null && !localizedRaceDescription.IsEmpty ?  localizedRaceDescription.GetLocalizedString(raceData.GetPositionText(),raceData.totalParticipants,raceData.GetFormattedTime(),raceData.pointsEarned,raceData.trackName) :
                $"Finished {raceData.GetPositionText()} out of {raceData.totalParticipants} participants\n" +
                $"Time: {raceData.GetFormattedTime()}\n" +
                $"Points Earned: {raceData.pointsEarned}\n" +
                $"Track: {raceData.trackName}";
            
            return raceDescription;
        }
        
        public string GetDetailedTooltip()
        {
            string leagueDisplayName = raceData.localizedLeagueName != null && !raceData.localizedLeagueName.IsEmpty ? raceData.localizedLeagueName.GetLocalizedString() : raceData.leagueName;
            string raceDisplayName = raceData.localizedRaceName != null && !raceData.localizedRaceName.IsEmpty ? raceData.localizedRaceName.GetLocalizedString(LeagueController.Instance.currentLeague.currentRace) : raceData.raceName;
            
            string tooltip = $"<b>{leagueDisplayName}</b>\n";
             
            tooltip += $"<i>{raceDisplayName}</i>\n\n";
            tooltip += localizedDetailedRaceResult != null && !localizedDetailedRaceResult.IsEmpty ?  localizedDetailedRaceResult.GetLocalizedString(raceData.GetPositionText(),raceData.totalParticipants) : $"<b>Result:</b> {raceData.GetPositionText()} / {raceData.totalParticipants}\n";
            tooltip += localizedDetailedRaceTime != null && !localizedDetailedRaceTime.IsEmpty ? localizedDetailedRaceTime.GetLocalizedString(raceData.GetFormattedTime()) : $"<b>Time:</b> {raceData.GetFormattedTime()}\n";
            tooltip += localizedDetailedRaceTrack != null && !localizedDetailedRaceTrack.IsEmpty ? localizedDetailedRaceTrack.GetLocalizedString(raceData.trackName) : $"<b>Track:</b> {raceData.trackName}\n";
            tooltip += localizedDetailedRacePoints != null && !localizedDetailedRacePoints.IsEmpty ? localizedDetailedRacePoints.GetLocalizedString(raceData.pointsEarned) : $"<b>Points:</b> {raceData.pointsEarned}\n";
            tooltip += localizedDetailedRaceDate != null && !localizedDetailedRaceDate.IsEmpty ? localizedDetailedRaceDate.GetLocalizedString(raceData.raceDate.ToString("MMM dd, yyyy")) : $"<b>Date:</b> {raceData.raceDate.ToString("MMM dd, yyyy")}\n\n";
            
            if (raceData.participantNames != null && raceData.participantNames.Length > 0)
            {
                tooltip += localizedDetailedRaceParticipants != null && !localizedDetailedRaceParticipants.IsEmpty ?  localizedDetailedRaceParticipants.GetLocalizedString() :"<b>Participants:</b>\n";
                for (int i = 0; i < raceData.participantNames.Length; i++)
                {
                    tooltip += $"{i + 1}. {raceData.participantNames[i]}\n";
                }
            }
            
            return tooltip;
        }
    }
}
