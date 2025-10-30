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
                Debug.Log($"[LeagueController.Start] GameStarted = false. Checking if should reset league...");
                
                // Only clear and regenerate if this is a fresh game start (not loaded from save)
                if (SaveSystem.Instance != null)
                {
                    Debug.Log($"[LeagueController.Start] SaveSystem exists. IsNewGame={SaveSystem.Instance.IsNewGame}, WasLoadedFromSave={SaveSystem.Instance.WasLoadedFromSave}");
                    
                    if (!SaveSystem.Instance.IsNewGame || SaveSystem.Instance.WasLoadedFromSave)
                    {
                        Debug.Log("[LeagueController.Start] Existing game detected - preserving league data");
                        return;
                    }
                    
                    Debug.Log("[LeagueController.Start] NEW GAME - Clearing league and regenerating schedule");
                    /*ClearLeague();
                    RegenerateRaceSchedule();
                    currentLeague.RecalculateStandings();*/
                }
                else if (!IsGameLoadedFromSave())
                {
                    Debug.Log("[LeagueController.Start] No SaveSystem - checking IsGameLoadedFromSave()");
                    Debug.Log("[LeagueController.Start] NEW GAME - Clearing league and regenerating schedule");
                    /*ClearLeague();
                    RegenerateRaceSchedule();
                    currentLeague.RecalculateStandings();*/
                }
                else
                {
                    Debug.Log("[LeagueController.Start] Game loaded from save - preserving existing league data");
                }
            }
            else if (currentLeague != null && GameManager.Instance.GameStarted)
            {
                Debug.Log($"[LeagueController.Start] GameStarted = true. Current race: {currentLeague.currentRace}, Player joined: {currentLeague.playerHasJoined}");
                Debug.Log("[LeagueController.Start] Game already in progress - PRESERVING ALL DATA");
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

            if(AudioManager.instance != null)
            {
                AudioManager.instance.UIClick1.start();
            }

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
                    if (race.teams?.Any(t => t != null && t.teamType == TeamType.Player) == true && !race.processed)
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
            
            Debug.Log($"=== RecordPlayerRaceResult called for Race Day {currentLeague.currentRace} ===");
            
            // Find the current player race and update it
            var currentRaceDay = currentLeague.raceDays[currentLeague.currentRace];
            foreach (var race in currentRaceDay.races)
            {
                if (race.teams?.Any(t => t != null && t.teamType == TeamType.Player) == true && !race.processed)
                {
                    race.positions = allPositions;
                    race.processed = true;
                    Debug.Log($"Marked player race as processed for Race Day {currentLeague.currentRace}");
                    break;
                }
            }
            
            // Record results for all teams in this race
            // IMPORTANT: We need to find the actual team in the league by name, not use the Race's team reference
            // because Race objects may have deserialized copies that aren't the same instances
            for (int i = 0; i < raceTeams.Length && i < allPositions.Length; i++)
            {
                // Find the actual team in the current league by matching team name
                Team actualTeam = System.Array.Find(currentLeague.teams, t => t != null && t.teamName == raceTeams[i].teamName);
                
                if (actualTeam == null)
                {
                    Debug.LogWarning($"Could not find team '{raceTeams[i].teamName}' in current league teams. Stats/XP not updated.");
                    continue;
                }
                
                Debug.Log($"Recording race finish for {actualTeam.teamName}: Position {allPositions[i]} (Total finishes before: {actualTeam.currentSeasonStats.finishes.Count})");
                
                // Now update the ACTUAL team, not the race's copy
                actualTeam.RecordRaceFinish(allPositions[i]);
                
                Debug.Log($"After recording: {actualTeam.teamName} now has {actualTeam.currentSeasonStats.finishes.Count} finishes, {actualTeam.currentSeasonStats.Points} points, {actualTeam.currentSeasonStats.Wins} wins");
                
                if (actualTeam.teamType == TeamType.Player)
                {
                    for (int j = 0; j < leagues.Length; j++)
                    {
                        if (leagues[j] == currentLeague)
                        {
                            // Give extra XP for higher leagues
                            int xpToGive = currentLeague.maxExperienceGivenPerRace * (j * 5);
                            actualTeam.GiveExperience(xpToGive);
                            actualTeam.ReduceRacesAvailableForActiveTeamMembers();
                            Debug.Log($"Player team earned {xpToGive} XP for race finish in position {allPositions[i]}");
                            break;
                        }
                    }
                }
                else
                {
                    int pos = allPositions[i];
                    int xpToGive;

                    if (pos >= 3)
                    {
                        xpToGive = (currentLeague.maxExperienceGivenPerRace / pos) + 20*pos; // Bonus XP for participation
                    }
                    else
                    {
                        xpToGive = currentLeague.maxExperienceGivenPerRace / pos; // More XP for top positions
                    }
                    
                    actualTeam.GiveExperience(xpToGive);
                    Debug.Log($"Team '{actualTeam.teamName}' earned {xpToGive} XP for finishing in position {pos}");
                }
            }
            
            
            
            if (currentLeague == null) return;
            currentLeague.RecalculateStandings();
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
        
        public int CalculateTeamStarRating(Team team)
        {
            if (team == null || team.teamMembers == null || team.teamMembers.Count == 0)
            {
                return 0;
            }

            int totalStars = 0;
            int memberCount = 0;

            foreach (var member in team.teamMembers)
            {
                if (member != null)
                {
                    int memberStars = CalculateTeamMemberStarRating(member);
                    totalStars += memberStars;
                    memberCount++;
                }
            }

            if (memberCount == 0) return 0;

            // Average star rating across all members
            float averageStars = totalStars / (float)memberCount;

            // Round to nearest whole number for team star rating
            return Mathf.RoundToInt(averageStars);
        }
        
        /// <summary>
        /// Calculates star rating based on CharacterStats without requiring a full TeamMember
        /// Returns 1-5 star rating based on stats compared to current league members
        /// </summary>
        public int CalculateTeamMemberStarRatingByStats(CharacterStats stats)
        {
            if (currentLeague == null || currentLeague.teams == null || currentLeague.teams.Length == 0)
            {
                // Default rating if no league context
                return 3;
            }

            // Collect all team members in the league
            List<TeamMember> allMembers = new List<TeamMember>();
            foreach (var team in currentLeague.teams)
            {
                if (team != null && team.teamMembers != null)
                {
                    foreach (var tm in team.teamMembers)
                    {
                        if (tm != null)
                        {
                            allMembers.Add(tm);
                        }
                    }
                }
                
                if (team != null && team.bench != null)
                {
                    foreach (var tm in team.bench)
                    {
                        if (tm != null && !allMembers.Contains(tm))
                        {
                            allMembers.Add(tm);
                        }
                    }
                }
            }

            if (allMembers.Count == 0) return 3;

            // Calculate percentile for each stat
            float strengthPercentile = CalculateStatPercentile(stats.strength, allMembers, TeamMember.StatType.Strength);
            float techniquePercentile = CalculateStatPercentile(stats.technique, allMembers, TeamMember.StatType.Technique);
            float staminaPercentile = CalculateStatPercentile(stats.stamina, allMembers, TeamMember.StatType.Stamina);
            float teamWorkPercentile = CalculateStatPercentile(stats.teamWork, allMembers, TeamMember.StatType.TeamWork);

            // Average the percentiles
            float averagePercentile = (strengthPercentile + techniquePercentile + staminaPercentile + teamWorkPercentile) / 4f;

            // Convert percentile to star rating (1-5)
            if (averagePercentile >= 80f) return 5;
            if (averagePercentile >= 60f) return 4;
            if (averagePercentile >= 40f) return 3;
            if (averagePercentile >= 20f) return 2;
            return 1;
        }
        
        /// <summary>
        /// Helper method to calculate percentile for a specific stat value compared to league members
        /// </summary>
        private float CalculateStatPercentile(float statValue, List<TeamMember> allMembers, TeamMember.StatType statType)
        {
            int betterThanCount = 0;

            foreach (var otherMember in allMembers)
            {
                if (otherMember.GetTeamMemberStat(statType) < statValue)
                {
                    betterThanCount++;
                }
            }

            // Calculate percentile (percentage of members this stat is better than)
            return (betterThanCount / (float)allMembers.Count) * 100f;
        }
        
        
        public int CalculateTeamMemberStarRating(TeamMember member)
        {
            if (currentLeague == null || currentLeague.teams == null || currentLeague.teams.Length == 0)
            {
                return 0;
            }

            // Collect all team members in the league
            List<TeamMember> allMembers = new List<TeamMember>();
            foreach (var team in currentLeague.teams)
            {
                if (team != null && team.teamMembers != null)
                {
                    foreach (var tm in team.teamMembers)
                    {
                        if (tm != null)
                        {
                            allMembers.Add(tm);
                        }
                    }
                }
                
                if (team != null && team.bench != null)
                {
                    foreach (var tm in team.bench)
                    {
                        if (tm != null && !allMembers.Contains(tm))
                        {
                            allMembers.Add(tm);
                        }
                    }
                }
            }

            if (allMembers.Count == 0) return 0;

            // Calculate percentile for each stat
            float strengthPercentile = CalculatePercentile(member, allMembers, TeamMember.StatType.Strength);
            float techniquePercentile = CalculatePercentile(member, allMembers, TeamMember.StatType.Technique);
            float staminaPercentile = CalculatePercentile(member, allMembers, TeamMember.StatType.Stamina);
            float teamWorkPercentile = CalculatePercentile(member, allMembers, TeamMember.StatType.TeamWork);

            // Average the percentiles
            float averagePercentile = (strengthPercentile + techniquePercentile + staminaPercentile + teamWorkPercentile) / 4f;

            Debug.Log($"Team Member {member.memberName} Percentiles - Strength: {strengthPercentile:F1}%, Technique: {techniquePercentile:F1}%, Stamina: {staminaPercentile:F1}%, Teamwork: {teamWorkPercentile:F1}% | Average: {averagePercentile:F1}%");
            
            // Convert percentile to star rating (1-5)
            // 0-20%: 1 star (bottom tier)
            // 20-40%: 2 stars (below average)
            // 40-60%: 3 stars (average)
            // 60-80%: 4 stars (above average)
            // 80-100%: 5 stars (top tier)
            if (averagePercentile >= 80f) return 5;
            if (averagePercentile >= 60f) return 4;
            if (averagePercentile >= 40f) return 3;
            if (averagePercentile >= 20f) return 2;
            return 1;
        }

        /// <summary>
        /// Calculates the percentile ranking of a team member for a specific stat
        /// Returns a value from 0-100 representing the percentage of members this player is better than
        /// </summary>
        private float CalculatePercentile(TeamMember member, List<TeamMember> allMembers, TeamMember.StatType statType)
        {
            float memberStatValue = member.GetTeamMemberStat(statType);
            int betterThanCount = 0;

            foreach (var otherMember in allMembers)
            {
                if (otherMember.GetTeamMemberStat(statType) < memberStatValue)
                {
                    betterThanCount++;
                }
            }

            // Calculate percentile (percentage of members this player is better than)
            return (betterThanCount / (float)allMembers.Count) * 100f;
        }

        /// <summary>
        /// Gets a detailed breakdown of the team member's rating
        /// </summary>
        public string GetTeamMemberRatingBreakdown(TeamMember member)
        {
            if (currentLeague == null || currentLeague.teams == null || currentLeague.teams.Length == 0)
            {
                return "N/A";
            }

            // Collect all team members in the league
            List<TeamMember> allMembers = new List<TeamMember>();
            foreach (var team in currentLeague.teams)
            {
                if (team != null && team.teamMembers != null)
                {
                    foreach (var tm in team.teamMembers)
                    {
                        if (tm != null)
                        {
                            allMembers.Add(tm);
                        }
                    }
                }
                if (team != null && team.bench != null)
                {
                    foreach (var tm in team.bench)
                    {
                        if (tm != null && !allMembers.Contains(tm))
                        {
                            allMembers.Add(tm);
                        }
                    }
                }
            }

            if (allMembers.Count == 0) return "N/A";

            // Calculate percentile for each stat
            float strengthPercentile = CalculatePercentile(member, allMembers, TeamMember.StatType.Strength);
            float techniquePercentile = CalculatePercentile(member, allMembers, TeamMember.StatType.Technique);
            float staminaPercentile = CalculatePercentile(member, allMembers, TeamMember.StatType.Stamina);
            float teamWorkPercentile = CalculatePercentile(member, allMembers, TeamMember.StatType.TeamWork);

            int starRating = CalculateTeamMemberStarRating(member);

            return $"Overall: {starRating}★ | Strength: {strengthPercentile:F0}% | Technique: {techniquePercentile:F0}% | Stamina: {staminaPercentile:F0}% | Teamwork: {teamWorkPercentile:F0}%";
        }
    }
}

