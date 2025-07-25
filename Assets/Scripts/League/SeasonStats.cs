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