using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;

namespace League
{
    public class LeagueController : MonoBehaviour
    {
        public static LeagueController Instance { get; private set; }
        public League currentLeague;
        public League[] leagues;
        public LeagueInviteCardsUi leagueInviteCardsUi;
        public UnityEvent onPlayerJoinedLeague;
        FMOD.Studio.EventInstance UIClick1;
        //FMOD.Studio.EventInstance ShowInvite;


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
            
        }

        public void Start()
        {
            if (currentLeague != null && !GameManager.Instance.GameStarted)
            {
                ClearLeague();
                RegenerateRaceSchedule();
                currentLeague.RecalculateStandings();
                GameManager.Instance.OnGameStarted.AddListener(ShowLeagueInvite);
                
              
            }
            else
            {
                Debug.LogWarning("Current league is not set! Please assign a league in the inspector.");
            }

            
            
           
        }



        public void ShowLeagueInvite()
        {
            if (currentLeague != null)
            {

                if(leagueInviteCardsUi != null && !currentLeague.playerHasJoined)
                {
                    LeagueInviteCardsUi leaguecard  = Instantiate(leagueInviteCardsUi);
                    leaguecard.gameObject.SetActive(true);
                    leaguecard.SetLeagueData(currentLeague);
                }
                else
                {
                    Debug.LogWarning("LeagueInviteCardsUi reference is not set in the inspector.");
                    GenerateRaceSchedule();
                    SetPlayerHasAcceptedInvite();
                
                }
                //ShowInvite = FMODUnity.RuntimeManager.CreateInstance("event:/Main Menu/Show Invite");
                //ShowInvite.start();
            }

        }

        
        public void SetPlayerHasAcceptedInvite()
        {
            currentLeague.playerHasJoined = true;

            UIClick1 = FMODUnity.RuntimeManager.CreateInstance("event:/UI/Click 1");
            UIClick1.start();

            // Set the tournament start date when player joins (this should be fixed and not change)
            currentLeague.tournamentStartDate = TimeManager.Instance.GetCurrentDate();
            onPlayerJoinedLeague?.Invoke();
        }

        public void GenerateRaceSchedule()
        {
            // Generate complete race schedule
            currentLeague.raceDays = currentLeague.GenerateRaceSchedule(
                currentLeague.teams,
                currentLeague.maxNumberOfBoatsPerRace,
                currentLeague.repeatCount).ToArray();
                
                
            if ( currentLeague.raceDays.Length > currentLeague.maxRaceDays)
            {
                // Trim excess races
                int racesToKeep = currentLeague.maxRaceDays;
                List<RaceDayFormation> trimmedDays = new List<RaceDayFormation>();

                foreach (var day in currentLeague.raceDays)
                {
                    if (day.races.Count <= racesToKeep)
                    {
                        trimmedDays.Add(day);
                        racesToKeep -= day.races.Count;
                    }
                    else if (racesToKeep > 0)
                    {
                        // Take partial day
                        RaceDayFormation partialDay = new RaceDayFormation();
                        partialDay.races = day.races.Take(racesToKeep).ToList();
                        trimmedDays.Add(partialDay);
                        break;
                    }
                    else
                    {
                        break;
                    }
                }

                currentLeague.raceDays = trimmedDays.ToArray();
            }

        }

        /// <summary>
        /// Advances to the next race in the league schedule.
        /// Simulates AI-only races for the current race day, then moves to next race.
        /// </summary>
        public void AdvanceToNextRace()
        {
            if (currentLeague == null || currentLeague.raceDays == null) return;
            
            // Simulate any remaining AI-only races for the current race day
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
            
            
        }

        /// <summary>
        /// Gets the current race that the player should participate in.
        /// Returns null if no player races remaining.
        /// </summary>
        public Race GetCurrentPlayerRace()
        {
            if (currentLeague?.raceDays == null) return null;
            
            // Look for next race containing the player
            for (int i = currentLeague.currentRace; i < currentLeague.raceDays.Length; i++)
            {
                var raceDay = currentLeague.raceDays[i];
                foreach (var race in raceDay.races)
                {
                    if (race.teams?.Any(t => t.teamType == TeamType.Player) == true && !race.processed)
                    {
                        return race;
                    }
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
            
            // Find the current player race and update it
            var currentRaceDay = currentLeague.raceDays[currentLeague.currentRace];
            foreach (var race in currentRaceDay.races)
            {
                if (race.teams?.Any(t => t.teamType == TeamType.Player) == true && !race.processed)
                {
                    race.positions = allPositions;
                    race.processed = true;
                    break;
                }
            }
            
            // Record results for all teams in this race
            for (int i = 0; i < raceTeams.Length && i < allPositions.Length; i++)
            {
                raceTeams[i].RecordRaceFinish(allPositions[i]);
                raceTeams[i].GiveExperience(currentLeague.maxExperienceGivenPerRace / allPositions[i]); // More experience for better positions
            }
            
            
            
            // Advance to next race
            AdvanceToNextRace();
        }

        private void CompleteSeasonInternal()
        {
            currentLeague.isFinished = true;
            currentLeague.RecalculateStandings();
            Debug.Log($"Season {currentLeague.currentSeason} completed for {currentLeague.leagueName}!");
            
            
            
        }

        /// <summary>
        /// Clears the current league and resets all race data.
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
                currentLeague.playerHasJoined = false;
                
                currentLeague.tournamentStartDate = default;
                
                // Reset all team stats
                if (currentLeague.teams != null)
                {
                    foreach (var team in currentLeague.teams)
                    {
                        if (team != null)
                        {
                            team.ResetCurrentSeasonStats();
                            team.ResetLifetimeStats();
                            team.ResetAllPlayerStats();
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
                GenerateRaceSchedule(); // Calls the race generation logic
                Debug.Log($"Race schedule regenerated for '{currentLeague.leagueName}'!");
            }
            else
            {
                Debug.LogWarning("No current league to regenerate schedule for!");
            }
        }
    }
}