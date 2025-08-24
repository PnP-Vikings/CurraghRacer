using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace League
{
    [CreateAssetMenu(fileName = "NewLeague", menuName = "League/League")]
    public class League : ScriptableObject
    {
        [Header("Runtime Standings")]
        [Tooltip("Calculated standings for this league, sorted by points.")]
        public TeamStanding[] standings;

        [Header("League Settings")]
        public string leagueName;
        public string description;
        public Sprite leagueIcon;
        public Color leagueColor;
        public Team[] teams;
        [Tooltip("League above this one, if any.")]
        public League leagueAbove;
        [Tooltip("League below this one, if any.")]
        public League leagueBelow;
        public int maxRaceDays = 10;
        public int currentRace; // Current race index
        public int currentSeason = 1; // Current season number
        public bool isActive = true;
        public bool playerHasJoined;
        public bool isFinished;
        public bool isPromotionRelegation = true; // If true, this league has promotion/relegation rules
        public int maxNumberOfBoatsPerRace = 4; // Maximum number of boats
        [Tooltip("How many times to repeat each race combination.")]
        public int repeatCount = 1;
        [Header("Schedule")]
        [Tooltip("All generated race days for this league season.")]
        public RaceDayFormation[] raceDays;
        
        [Tooltip("Tournament start date - set when player joins tournament")]
        public DateTime tournamentStartDate;
        
        [Tooltip("List of Tracks available for this league.")]
        public RaceDetails[] raceTracks;

        public List<RaceDayFormation> GenerateRaceSchedule(Team[] teams, int boatsPerRace, int repeatCount)
        {
            // Find player team if it exists
            Team playerTeam = teams.FirstOrDefault(t => t.teamType == TeamType.Player);
            List<RaceDayFormation> raceDays = new List<RaceDayFormation>();
            
            // Track face-offs between teams
            Dictionary<Team, Dictionary<Team, int>> faceCount = new Dictionary<Team, Dictionary<Team, int>>();
            Dictionary<Team, int> raceCount = new Dictionary<Team, int>();
            
            // Initialize tracking dictionaries
            foreach (var team in teams)
            {
                faceCount[team] = new Dictionary<Team, int>();
                foreach (var opponent in teams)
                {
                    if (team != opponent)
                        faceCount[team][opponent] = 0;
                }
                raceCount[team] = 0;
            }

            // Continue generating race days until we reach maxRaceDays
            while (raceDays.Count < maxRaceDays)
            {
                // Create race days for each round
                for (int round = 0; round < repeatCount && raceDays.Count < maxRaceDays; round++)
                {
                    // Create race days until all teams have raced in this round
                    List<Team> teamsInRound = new List<Team>(teams);
                    
                    while (teamsInRound.Count >= boatsPerRace && raceDays.Count < maxRaceDays)
                    {
                        // Create a new race day
                        RaceDayFormation raceDay = new RaceDayFormation();
                        
                        // Keep creating races until we can't form any more full races
                        while (teamsInRound.Count >= boatsPerRace)
                        {
                            // Create a balanced race
                            List<Team> raceTeams = new List<Team>();
                            
                            // Prioritize player team if available
                            if (playerTeam != null && teamsInRound.Contains(playerTeam))
                            {
                                raceTeams.Add(playerTeam);
                                teamsInRound.Remove(playerTeam);
                            }
                            
                            // Add teams that have faced current teams the least
                            while (raceTeams.Count < boatsPerRace && teamsInRound.Count > 0)
                            {
                                Team nextTeam = null;
                                int lowestFaceCount = int.MaxValue;
                                
                                foreach (var candidate in teamsInRound)
                                {
                                    int totalFaceCount = 0;
                                    foreach (var teamInRace in raceTeams)
                                    {
                                        totalFaceCount += faceCount[candidate][teamInRace];
                                    }
                                    
                                    if (totalFaceCount < lowestFaceCount ||
                                        (totalFaceCount == lowestFaceCount && raceCount[candidate] < raceCount[nextTeam ?? candidate]))
                                    {
                                        lowestFaceCount = totalFaceCount;
                                        nextTeam = candidate;
                                    }
                                }
                                
                                raceTeams.Add(nextTeam);
                                teamsInRound.Remove(nextTeam);
                            }
                            
                            // Update face count tracking
                            for (int i = 0; i < raceTeams.Count; i++)
                            {
                                raceCount[raceTeams[i]]++;
                                for (int j = i + 1; j < raceTeams.Count; j++)
                                {
                                    faceCount[raceTeams[i]][raceTeams[j]]++;
                                    faceCount[raceTeams[j]][raceTeams[i]]++;
                                }
                            }
                            
                            // Add this race to the race day
                            raceDay.races.Add(new Race
                            {
                                teams = raceTeams.ToArray(),
                                positions = new int[raceTeams.Count]
                            });
                        }
                        
                        // Only add non-empty race days
                        if (raceDay.races.Count > 0)
                        {
                            raceDays.Add(raceDay);
                        }
                    }
                }
                
                // If we've completed all rounds but still need more race days,
                // continue with additional rounds until we reach maxRaceDays
                if (raceDays.Count < maxRaceDays)
                {
                    // Reset for additional rounds if needed
                    continue;
                }
            }
            
            return raceDays;
        }


        /// <summary>
        /// Recalculates the league standings based on each team's wins,and points.
        /// Points are calculated from the current season stats of each team.
        /// </summary>
        public void RecalculateStandings()
        {
            var list = new List<TeamStanding>();
            foreach (var team in teams)
            {
                var stats = team.currentSeasonStats;
                var s = new TeamStanding
                {
                    team = team,
                    wins = stats.Wins,
                    points = stats.Points
                };
                list.Add(s);
            }
            var sorted = list.OrderByDescending(s => s.points)
                             .ThenByDescending(s => s.wins)
                             .ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                var entry = sorted[i];
                entry.position = i + 1;
                sorted[i] = entry;
            }
            standings = sorted.ToArray();
        }
        
        public int GetTeamPosition(Team team)
        {
            if (standings == null || standings.Length == 0)
            {
                return -1; // Not found
            }
            for (int i = 0; i < standings.Length; i++)
            {
                if (standings[i].team == team)
                {
                    return standings[i].position;
                }
            }
            return -1; // Not found
        }
        
        public int GetTeamPoints(Team team)
        {
            if (standings == null || standings.Length == 0)
            {
                return 0; // No points if standings are empty
            }
            for (int i = 0; i < standings.Length; i++)
            {
                if (standings[i].team == team)
                {
                    return standings[i].points;
                }
            }
            return 0; // No points if team not found
        }
        
        public int GetTeamWins(Team team)
        {
            if (standings == null || standings.Length == 0)
            {
                return 0; // No wins if standings are empty
            }
            for (int i = 0; i < standings.Length; i++)
            {
                if (standings[i].team == team)
                {
                    return standings[i].wins;
                }
            }
            return 0; // No wins if team not found
        }
    }

    [Serializable]
    public struct TeamStanding
    {
        public Team team;
        public int points;
        public int wins;
        public int position;
    }


}
