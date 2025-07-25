using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace League
{
    public class LeagueController : MonoBehaviour
    {
        public static LeagueController Instance { get; private set; }
        public League currentLeague;
        public League[] leagues;
        
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            if(leagues == null || leagues.Length == 0)
            {
                Debug.LogWarning("No leagues assigned! Please assign leagues in the inspector.");
            }
            else
            {
                // Automatically set the first league as current if none is set
                if (currentLeague == null)
                {
                    currentLeague = leagues[0];
                }
            }
            
            if (currentLeague != null)
            {
                ClearLeague();
                RegenerateRaceSchedule();
            }
            else
            {
                Debug.LogWarning("Current league is not set! Please assign a league in the inspector.");
            }
        }


        private void Start()
        {
            // Generate raw schedule and enforce repeat and limit
            var rawSchedule = currentLeague.GenerateRaceSchedule(currentLeague.teams, currentLeague.maxNumberOfBoatsPerRace, currentLeague.repeatCount).ToList();
            if (rawSchedule.Count > currentLeague.maxRaces)
                rawSchedule = rawSchedule.Take(currentLeague.maxRaces).ToList();
            // Initialize race days array
            currentLeague.raceDays = new RaceDayFormation[rawSchedule.Count];
            for (int i = 0; i < rawSchedule.Count; i++)
            {
                currentLeague.raceDays[i] = new RaceDayFormation
                {
                    teams = rawSchedule[i],
                    positions = new int[rawSchedule[i].Length]
                };
            }
        }

        /// <summary>
        /// Advances to the next race in the league schedule.
        /// Automatically simulates AI-only races.
        /// </summary>
        public void AdvanceToNextRace()
        {
            if (currentLeague == null || currentLeague.raceDays == null) return;
            
            // Simulate current race if it's AI-only
            AIRaceSimulator.SimulateWeeklyAIRaces(currentLeague);
            
            // Move to next race
            currentLeague.currentRace++;
            
            // Check if season is complete
            if (currentLeague.currentRace >= currentLeague.raceDays.Length)
            {
                CompleteSeasonInternal();
                return;
            }
            
            // Update standings after race results
            currentLeague.RecalculateStandings();
            
            // Simulate next race if it's also AI-only (for multiple AI races per week)
            AIRaceSimulator.SimulateWeeklyAIRaces(currentLeague);
        }

        /// <summary>
        /// Gets the current race that the player should participate in.
        /// Returns null if no player races remaining.
        /// </summary>
        public RaceDayFormation? GetCurrentPlayerRace()
        {
            if (currentLeague?.raceDays == null) return null;
            
            // Look for next race containing the player
            for (int i = currentLeague.currentRace; i < currentLeague.raceDays.Length; i++)
            {
                var raceDay = currentLeague.raceDays[i];
                if (raceDay.teams?.Any(t => t.teamType == TeamType.Player) == true)
                {
                    return raceDay;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Records the player's race result and advances the league.
        /// </summary>
        public void RecordPlayerRaceResult(int playerPosition, Team[] raceTeams, int[] allPositions)
        {
            if (currentLeague?.raceDays == null) return;
            
            // Find and update the current race day
            var currentRaceDay = currentLeague.raceDays[currentLeague.currentRace];
            currentRaceDay.positions = allPositions;
            currentLeague.raceDays[currentLeague.currentRace] = currentRaceDay;
            
            // Record results for all teams in this race
            for (int i = 0; i < raceTeams.Length && i < allPositions.Length; i++)
            {
                raceTeams[i].RecordRaceFinish(allPositions[i]);
            }
            
            // Advance to next race
            AdvanceToNextRace();
        }

        private void CompleteSeasonInternal()
        {
            currentLeague.isFinished = true;
            currentLeague.RecalculateStandings();
            Debug.Log($"Season {currentLeague.currentSeason} completed for {currentLeague.leagueName}!");
            
            // TODO: Handle promotion/relegation, start new season, etc.
        }

        /// <summary>
        /// Clears the current league and resets all race data.
        /// This method can be called from the inspector button.
        /// </summary>
        [ContextMenu("Clear League")]
        public void ClearLeague()
        {
            if (currentLeague != null)
            {
                currentLeague.currentRace = 0;
                currentLeague.isFinished = false;
                currentLeague.raceDays = null;
                currentLeague.standings = null;
                
                // Reset all team stats
                if (currentLeague.teams != null)
                {
                    foreach (var team in currentLeague.teams)
                    {
                        if (team != null)
                        {
                            team.ResetCurrentSeasonStats();
                        }
                    }
                }
                
                Debug.Log($"League '{currentLeague.leagueName}' has been cleared and reset!");
            }
            else
            {
                Debug.LogWarning("No current league to clear!");
            }
        }

        /// <summary>
        /// Regenerates the race schedule for the current league.
        /// Useful for testing different race combinations.
        /// </summary>
        [ContextMenu("Regenerate Race Schedule")]
        public void RegenerateRaceSchedule()
        {
            if (currentLeague != null)
            {
                Start(); // Calls the race generation logic
                Debug.Log($"Race schedule regenerated for '{currentLeague.leagueName}'!");
            }
            else
            {
                Debug.LogWarning("No current league to regenerate schedule for!");
            }
        }
    }
}