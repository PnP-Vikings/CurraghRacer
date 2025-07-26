using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace League
{
    public static class AIRaceSimulator
    {
        private static System.Random random = new System.Random();

        /// <summary>
        /// Simulates a race between AI teams and returns the finishing positions.
        /// Teams with higher quality + form have better chances of winning.
        /// </summary>
        /// <param name="teams">Teams participating in the race</param>
        /// <returns>Dictionary with team and their finishing position</returns>
        public static Dictionary<Team, int> SimulateRace(Team[] teams)
        {
            if (teams == null || teams.Length == 0)
                return new Dictionary<Team, int>();

            var teamPerformances = new List<TeamRacePerformance>();

            // Calculate race performance for each team with some randomness
            foreach (var team in teams)
            {
                float basePerformance = team.GetRacePerformance();
                
                // Add randomness (±15 points) to make races unpredictable
                float randomVariation = (float)(random.NextDouble() * 30 - 15);
                float finalPerformance = basePerformance + randomVariation;
                
                // Add small boost for teams currently in good league position (championship pressure)
                float positionBonus = GetPositionBonus(team);
                finalPerformance += positionBonus;

                teamPerformances.Add(new TeamRacePerformance
                {
                    team = team,
                    performance = finalPerformance
                });
            }

            // Sort by performance (highest first = best finishing position)
            teamPerformances = teamPerformances
                .OrderByDescending(tp => tp.performance)
                .ToList();

            // Assign finishing positions and record results
            var results = new Dictionary<Team, int>();
            for (int i = 0; i < teamPerformances.Count; i++)
            {
                int position = i + 1;
                var team = teamPerformances[i].team;
                
                results[team] = position;
                team.RecordRaceFinish(position);
            }

            return results;
        }

        /// <summary>
        /// Gives teams currently higher in standings a small performance boost
        /// to help maintain competitive balance.
        /// </summary>
        private static float GetPositionBonus(Team team)
        {
            var league = LeagueController.Instance?.currentLeague;
            if (league?.standings == null) return 0f;

            var standing = league.standings.FirstOrDefault(s => s.team == team);
            if (standing.team == null) return 0f;

            // Top 3 teams get small bonus, bottom teams get slight penalty
            int totalTeams = league.standings.Length;
            if (standing.position <= 3)
                return 2f; // Small boost for leaders
            else if (standing.position >= totalTeams - 2)
                return -1f; // Small penalty for bottom teams
            
            return 0f; // No bonus for mid-table teams
        }

        /// <summary>
        /// Simulates all non-player races for the current race week.
        /// Call this when advancing the league to the next race.
        /// </summary>
        public static void SimulateWeeklyAIRaces(League league)
        {
            if (league?.raceDays == null || league.currentRace >= league.raceDays.Length)
                return;
        
            var currentRaceDay = league.raceDays[league.currentRace];
    
            // Find player race (if any)
            Race playerRace = null;
            foreach (var race in currentRaceDay.races)
            {
                if (!race.processed && race.teams.Any(t => t.teamType == TeamType.Player))
                {
                    playerRace = race;
                    break;
                }
            }
    
            // Process all other races that aren't the player's race
            foreach (var race in currentRaceDay.races)
            {
                if (!race.processed && race != playerRace)
                {
                    var results = SimulateRace(race.teams);
            
                    // Initialize positions array if not already done
                    if (race.positions == null || race.positions.Length != race.teams.Length)
                    {
                        race.positions = new int[race.teams.Length];
                    }
                    
                    // Update race positions
                    for (int i = 0; i < race.teams.Length; i++)
                    {
                        if (results.ContainsKey(race.teams[i]))
                        {
                            race.positions[i] = results[race.teams[i]];
                        }
                    }
            
                    race.processed = true;
                    Debug.Log($"Simulated AI race: {string.Join(", ", results.Select(r => $"{r.Key.teamName}: {r.Value}"))}");
                }
            }
    
            // If all races are processed, mark the day as processed
            if (currentRaceDay.races.All(r => r.processed))
            {
                currentRaceDay.processed = true;
            }
        }

        /// <summary>
        /// Simulates all AI-only races that should happen during the same week as the player's race.
        /// This ensures AI teams that aren't racing the player still compete in their own heats.
        /// Each team races exactly once per race button press.
        /// </summary>
        public static void SimulateAllAIRacesForWeek(League league)
        {
            if (league?.raceDays == null || league.currentRace >= league.raceDays.Length)
                return;

            // Get all teams in the league
            var allTeams = league.teams.ToList();
            var playerTeam = allTeams.FirstOrDefault(t => t.teamType == TeamType.Player);
            
            if (playerTeam == null) return;

            // Get current player race day
            var currentRaceDay = league.raceDays[league.currentRace];
            var teamsInPlayerRaces = new List<Team>();
            
            // Collect all teams already in races for this race day
            foreach (var race in currentRaceDay.races)
            {
                teamsInPlayerRaces.AddRange(race.teams);
            }

            // Find teams NOT in any race this day - these need to race in their own heats
            var teamsNotRacing = allTeams.Where(t => !teamsInPlayerRaces.Contains(t)).ToList();

            // Create exactly ONE heat for the remaining teams
            if (teamsNotRacing.Count >= 2)
            {
                // Create a single AI-only race with all remaining teams (up to max boats per race)
                int raceSize = Mathf.Min(league.maxNumberOfBoatsPerRace, teamsNotRacing.Count);
                var raceTeams = teamsNotRacing.Take(raceSize).ToArray();
                
                if (raceTeams.Length >= 2) // Need at least 2 teams to race
                {
                    var results = SimulateRace(raceTeams);
                    Debug.Log($"Simulated AI heat: {string.Join(", ", results.Select(r => $"{r.Key.teamName}: {r.Value}"))}");
                    
                    // Log points awarded to verify max 25 points
                    foreach (var result in results)
                    {
                        int points = CalculatePointsForPosition(result.Value, raceTeams.Length);
                        Debug.Log($"{result.Key.teamName} earned {points} points for position {result.Value}");
                    }
                }

                // If there are still teams left over (more than max boats per race), they don't race this week
                if (teamsNotRacing.Count > league.maxNumberOfBoatsPerRace)
                {
                    var leftOverTeams = teamsNotRacing.Skip(raceSize).ToList();
                    Debug.Log($"Teams sitting out this week: {string.Join(", ", leftOverTeams.Select(t => t.teamName))}");
                }
            }

            // Handle scheduled AI-only races (if any exist in the race days)
            var currentRaceDay2 = league.raceDays[league.currentRace];
            foreach (var race in currentRaceDay2.races)
            {
                bool playerInRace = race.teams.Any(t => t.teamType == TeamType.Player);

                if (!playerInRace && !race.processed)
                {
                    // This is a scheduled AI-only race - simulate it
                    var results = SimulateRace(race.teams);
                    
                    // Update the race with results
                    race.positions = new int[race.teams.Length];
                    for (int i = 0; i < race.teams.Length; i++)
                    {
                        if (results.ContainsKey(race.teams[i]))
                            race.positions[i] = results[race.teams[i]];
                    }
                    race.processed = true;
                    
                    Debug.Log($"Simulated scheduled AI race - Results: {string.Join(", ", results.Select(r => $"{r.Key.teamName}: {r.Value}"))}");
                }
            }
        }

        /// <summary>
        /// Calculates points for a finishing position (F1-style scoring).
        /// 1st = 25, 2nd = 18, 3rd = 15, 4th = 12, etc.
        /// </summary>
        private static int CalculatePointsForPosition(int position, int totalRacers)
        {
            return position switch
            {
                1 => 25,
                2 => 18,
                3 => 15,
                4 => 12,
                5 => 10,
                6 => 8,
                7 => 6,
                8 => 4,
                9 => 2,
                10 => 1,
                _ => 0
            };
        }

        private struct TeamRacePerformance
        {
            public Team team;
            public float performance;
        }
    }
}
