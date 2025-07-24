using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



[Serializable]
public class SeasonStats
{
    [Tooltip("List of finishing positions for the season.")]
    public List<int> finishes;

    // Points distribution similar to F1: 1st to 10th
    private static readonly int[] PointsDistribution = {25, 18, 15, 12, 10, 8, 6, 4, 2, 1};

    public SeasonStats()
    {
        finishes = new List<int>();
    }

    public int Points
    {
        get
        {
            int total = 0;
            foreach (var pos in finishes)
            {
                if (pos >= 1 && pos <= PointsDistribution.Length)
                    total += PointsDistribution[pos - 1];
            }
            return total;
        }
    }

    public int Wins => finishes.Count(f => f == 1);
}

[CreateAssetMenu(fileName = "NewTeam", menuName = "League/Team")]
public class Team : ScriptableObject
{
    [Header("Team Details")]
    public string teamName;
    public string teamDescription;
    public Sprite teamLogo;
    public Color teamColor;
    public int teamQuality;

    [Header("Team Members")]
    public TeamMember[] teamMembers;

    [Header("Current Season Stats")]
    [Tooltip("Wins, draws, losses, and points for the current season.")]
    public SeasonStats currentSeasonStats;

    [Header("All-Time Stats")]
    [Tooltip("Cumulative wins, draws, losses, and points across all seasons.")]
    public SeasonStats lifetimeStats;

    /// <summary>
    /// Records a race finish position, updating both season and lifetime stats.
    /// </summary>
    public void RecordRaceFinish(int position)
    {
        currentSeasonStats.finishes.Add(position);
        lifetimeStats.finishes.Add(position);
    }

    /// <summary>
    /// Resets only the current season stats (e.g. at season start).
    /// </summary>
    public void ResetCurrentSeasonStats()
    {
        currentSeasonStats = new SeasonStats();
    }
}

