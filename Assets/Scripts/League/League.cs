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
        public int leagueAbove;
        [Tooltip("League below this one, if any.")]
        public int leagueBelow;
        public int maxRaces = 10;
        public int currentRace; // Current race index
        public int currentSeason = 1; // Current season number
        public bool isActive = true;
        public bool isFinished;

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
