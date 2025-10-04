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
        public LeagueCompleteCard leagueCompleteCardPrefab;
        public UnityEvent onPlayerJoinedLeague;
        public  League nextLeague = null;
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
                // Only clear and regenerate if this is a fresh game start (not loaded from save)
                if (SaveSystem.Instance != null)
                {
                    if (!SaveSystem.Instance.IsNewGame || SaveSystem.Instance.WasLoadedFromSave)
                        return;
                    ClearLeague();
                    RegenerateRaceSchedule();
                    currentLeague.RecalculateStandings();
                }
                else if (!IsGameLoadedFromSave())
                {
                    ClearLeague();
                    RegenerateRaceSchedule();
                    currentLeague.RecalculateStandings();
                }
                else
                {
                    Debug.Log("Game loaded from save - preserving existing league data");
                }
            }
            else if (currentLeague == null)
            {
                Debug.LogWarning("Current league is not set! Please assign a league in the inspector.");
            }
        }
        
        /// <summary>
        /// Check if the game was loaded from a save file
        /// </summary>
        private bool IsGameLoadedFromSave()
        {
            // Check if SaveSystem exists and has been used to load data
            if (SaveSystem.Instance != null)
            {
                // If the player has joined a league, it indicates saved progress
                if (currentLeague != null && currentLeague.playerHasJoined)
                {
                    return true;
                }
                
                // Check if there's any race progress
                if (currentLeague != null && currentLeague.currentRace > 0)
                {
                    return true;
                }
                
                // Check if any teams have race history (indicating saved progress)
                if (currentLeague?.teams != null)
                {
                    foreach (var team in currentLeague.teams)
                    {
                        if (team?.currentSeasonStats?.finishes != null && team.currentSeasonStats.finishes.Count > 0)
                        {
                            return true;
                        }
                    }
                }
                
                // Check if tournament start date has been set
                if (currentLeague != null && currentLeague.tournamentStartDate != default(System.DateTime))
                {
                    return true;
                }
            }
            
            return false;
        }
        public void ShowLeagueInviteAfterDelay()
        {
                StartCoroutine(StartLeagueInviteMessageAfterDelay(25f));
        }


        public void ShowLeagueInvite()
        {
            if (currentLeague != null && TimeManager.Instance != null)
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
                    RegenerateRaceSchedule();
                    SetPlayerHasAcceptedInvite();
                
                }
                //ShowInvite = FMODUnity.RuntimeManager.CreateInstance("event:/Main Menu/Show Invite");
                //ShowInvite.start();
            }
           

        }

        public System.Collections.IEnumerator StartLeagueInviteMessageAfterDelay(float delaySeconds)
        {
            Debug.Log($"Waiting {delaySeconds} seconds before showing league invite message...");
            if (!GameManager.Instance.playerIsBusy && TimeManager.Instance != null && currentLeague != null)
            {
                /*if (PlayerStatsView.Instance != null)
                {
                    yield return new WaitForSeconds(delaySeconds / 3);

                //    PlayerStatsView.Instance.DisplayInfo("You are not in a league, join a league to proceed.", 3);
                }*/
                        
                yield return new WaitForSeconds(delaySeconds);
           
                ShowLeagueInvite();
            }
            else
            {
                if (TimeManager.Instance == null)
                    Debug.LogWarning("TimeManager instance is null, cannot show league invite message.");
                if (currentLeague == null)
                    Debug.LogWarning("Current league is null, cannot show league invite message.");
                if (GameManager.Instance.playerIsBusy)
                    Debug.Log("Player is busy, delaying league invite message.");
                // Retry after some time
                yield return new WaitForSeconds(10f);
                StartCoroutine(StartLeagueInviteMessageAfterDelay(25f));
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
            
            // Recheck if today is a race day now that the player has joined the league
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.RecheckIfRaceDay();
            }
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
            Team playerTeam = currentLeague.GetPlayerTeam();
            int playerFinalPosition = currentLeague.GetTeamPosition(playerTeam);
            Debug.Log($"Player's final position: {playerFinalPosition}");

            nextLeague =  currentLeague.GetNextLeague();
            
            LeagueCompleteCard leagueCompleteCard = Instantiate(leagueCompleteCardPrefab);
            if (nextLeague != null && nextLeague != currentLeague)
            {
                bool playerWasRelegated = false;
                bool playerWasPromoted = false;
                
              
                playerWasPromoted = currentLeague.DidPlayerGetPromoted();
                playerWasRelegated = currentLeague.DidPlayerGetRelegated();
                
                
                leagueCompleteCard.SetLeagueCompletionData(currentLeague, playerFinalPosition, currentLeague.teams.Length, currentLeague.GetTeamPoints(playerTeam), currentLeague.GetTeamWins(playerTeam),playerWasRelegated,playerWasPromoted);
            }
            else
            {
                leagueCompleteCard.SetLeagueCompletionData(currentLeague, playerFinalPosition, currentLeague.teams.Length, currentLeague.GetTeamPoints(playerTeam), currentLeague.GetTeamWins(playerTeam),false,false);
            }
            
        }
        
        public void StartNewSeason()
        {
            if (currentLeague == null)
            {
                Debug.LogWarning("No current league to start a new season in!");
                return;
            }
            
            if (!currentLeague.isFinished)
            {
                Debug.LogWarning("Current league season is not finished yet!");
                return;
            }

            // Move to next league if applicable
            if (nextLeague != null && nextLeague != currentLeague)
            {
                currentLeague = nextLeague;
                nextLeague = null;
            }

            foreach (var league in leagues)
            {
                league.ResetLeagueForNewSeason();
            }
            currentLeague.standings = null;
            RegenerateRaceSchedule();
            
            Debug.Log($"Starting new season {currentLeague.currentSeason} in league '{currentLeague.leagueName}'");
            
            
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