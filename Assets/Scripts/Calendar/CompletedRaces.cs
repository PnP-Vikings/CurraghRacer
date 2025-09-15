using Calendar;
using UnityEngine;
using System;

namespace Calendar
{
    [System.Serializable]
    public class CompletedRaceData
    {
        public string leagueName;
        public string raceName;
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
                               float raceTime, int pointsEarned, string[] participantNames)
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
            
            dayEventType.eventName = $"{raceData.leagueName} - {raceData.raceName}";
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
            return $"Finished {raceData.GetPositionText()} out of {raceData.totalParticipants} participants\n" +
                   $"Time: {raceData.GetFormattedTime()}\n" +
                   $"Points Earned: {raceData.pointsEarned}\n" +
                   $"Track: {raceData.trackName}";
        }
        
        public string GetDetailedTooltip()
        {
            string tooltip = $"<b>{raceData.leagueName}</b>\n";
            tooltip += $"<i>{raceData.raceName}</i>\n\n";
            tooltip += $"<b>Result:</b> {raceData.GetPositionText()} / {raceData.totalParticipants}\n";
            tooltip += $"<b>Time:</b> {raceData.GetFormattedTime()}\n";
            tooltip += $"<b>Track:</b> {raceData.trackName}\n";
            tooltip += $"<b>Points:</b> {raceData.pointsEarned}\n";
            tooltip += $"<b>Date:</b> {raceData.raceDate.ToString("MMM dd, yyyy")}\n\n";
            
            if (raceData.participantNames != null && raceData.participantNames.Length > 0)
            {
                tooltip += "<b>Participants:</b>\n";
                for (int i = 0; i < raceData.participantNames.Length; i++)
                {
                    tooltip += $"{i + 1}. {raceData.participantNames[i]}\n";
                }
            }
            
            return tooltip;
        }
    }
}
