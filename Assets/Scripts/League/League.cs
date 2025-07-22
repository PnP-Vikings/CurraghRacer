using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

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
    public Team[] teams; // Array of teams in the league
    public int LeagueAbove; // League above this one, if any
    public int LeagueBelow; // League below this one, if any
    public int maxRaces = 10;
    public int currentRace = 0; // Current
    public int currentSeason = 1; // Current season number
    public bool isActive = true; // Is this league currently active?
    public bool isFinished = false; // Is this league finished?
    

    /// <summary>
    /// Recalculates the league standings based on each team's wins, draws, and losses.
    /// Win = 3 points, Draw = 1 point.
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
                wins = stats.wins,
                draws = stats.draws,
                losses = stats.losses,
                points = stats.Points
            };
            list.Add(s);
        }
        // Sort by points, then wins
        var sorted = list.OrderByDescending(s => s.points)
                         .ThenByDescending(s => s.wins)
                         .ToList();
        // Assign positions
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
    public int draws;
    public int losses;
    public int position;
}
