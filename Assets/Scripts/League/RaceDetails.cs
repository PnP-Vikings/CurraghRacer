using System;
using System.Collections.Generic;
using UnityEngine;

namespace League
{
    [CreateAssetMenu(fileName = "newRace", menuName = "Race/Create New Race")]
    public class RaceDetails : ScriptableObject
    {
        [Header("Race Details")]
        public string raceName;
        public string raceDescription;
        public Sprite raceIcon;
        public string raceSceneName = "RaceTrack1"; // Changed from Scene to string for better serialization
        public int maxParticipants = 4;

        public int numberOfLaps = 1; // Fixed naming convention - Default number of laps for the race

        [Header("Weather & Conditions")]
        [Tooltip("Weather condition during the race")]
        public WeatherCondition weatherCondition = WeatherCondition.Clear;
        [Tooltip("Wind strength (0-10)")]
        [Range(0, 10)]
        public float windStrength = 5f;
        [Tooltip("Wave height in meters")]
        [Range(0, 5)]
        public float waveHeight = 1f;

        [Header("Timing & Performance")]
        [Tooltip("Finish times for each team in seconds")]
        public float[] finishTimes = new float[4]; // Initialize with default size
        [Tooltip("Best lap times for each team")]
        public float[] bestLapTimes = new float[4]; // Initialize with default size
        [Tooltip("Total race duration in seconds")]
        public float raceDuration;

        [Header("Points & Rewards")]
        [Tooltip("Points awarded for each position")]
        public int[] pointsAwarded = new int[4]; // Initialize with default size
        [Tooltip("Prize money for each position")]
        public int[] prizeMoney = new int[4]; // Initialize with default size

        [Header("Additional Data")]
        [Tooltip("Any notable events during the race")]
        public List<RaceEvent> raceEvents = new List<RaceEvent>();
        [Tooltip("Track/course used for this race")]
        public string trackName;
        [Tooltip("Was this race completed without issues")]
        public bool raceCompleted = false;
        [Tooltip("Reason for incompletion if applicable")]
        public RaceincompletionReason incompletionReason = RaceincompletionReason.None; // Default to None

        [Serializable]
        public enum WeatherCondition
        {
            Clear,
            Cloudy,
            Rainy,
            Stormy,
            Foggy,
            Windy
        }

        [Serializable]
        public class RaceEvent
        {
            public float timeStamp; // When during the race this occurred
            public RaceEventType eventType;
            public string description;
            public Team affectedTeam; // If applicable
        }

        [Serializable]
        public enum RaceEventType
        {
            Collision,
            Overtake,
            BestLap,
            Penalty,
            EquipmentFailure, // Fixed naming convention
            OutstandingPerformance, // Fixed naming convention
            WeatherChange // Fixed naming convention
        }

        public void ResetRaceDetails()
        {
            raceName = string.Empty;
            raceDescription = string.Empty;
            raceIcon = null;
            raceSceneName = string.Empty;
            maxParticipants = 4;
            numberOfLaps = 1;
            weatherCondition = WeatherCondition.Clear;
            windStrength = 5f;
            waveHeight = 1f;

            finishTimes = new float[4];
            bestLapTimes = new float[4];
            raceDuration = 0f;

            pointsAwarded = new int[4];
            prizeMoney = new int[4];
            raceEvents.Clear();
            trackName = string.Empty;
            raceCompleted = false;
            incompletionReason = RaceincompletionReason.None;
        }
       
        public enum RaceincompletionReason
        {
            None,
            WeatherConditions,
            EquipmentFailure,
            Collision,
            SpecialEvent,
            Other
        }
    }

}
