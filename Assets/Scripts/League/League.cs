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
        public int maxRaces = 10;
        public int currentRace; // Current race index
        public int currentSeason = 1; // Current season number
        public bool isActive = true;
        public bool isFinished;
        public bool isPromotionRelegation = true; // If true, this league has promotion/relegation rules
        public int maxNumberOfBoatsPerRace = 4; // Maximum number of boats
        [Tooltip("How many times to repeat each race combination.")]
        public int repeatCount = 1;
        [Header("Schedule")]
        [Tooltip("All generated race days for this league season.")]
        public RaceDayFormation[] raceDays;

        public List<Team[]> GenerateRaceSchedule(Team[] teams, int boatsPerRace, int repeatCount)
        {
            var combos = new List<Team[]>();
            void Recurse(List<Team> pool, int start, List<Team> current)
            {
                if (current.Count == boatsPerRace)
                {
                    combos.Add(current.ToArray());
                    return;
                }
                for (int i = start; i < pool.Count; i++)
                {
                    current.Add(pool[i]);
                    Recurse(pool, i + 1, current);
                    current.RemoveAt(current.Count - 1);
                }
            }
            Recurse(new List<Team>(teams), 0, new List<Team>());
            var schedule = new List<Team[]>();
            for (int r = 0; r < repeatCount; r++)
            {
                combos.Shuffle();
                schedule.AddRange(combos);
            }
            return schedule;
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
