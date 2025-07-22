using System;
using UnityEngine;


[Serializable]
public struct SeasonStats
{
    public int wins;
    public int draws;
    public int losses;
    public int Points => wins * 3 + draws;
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
    /// Records a match result, updating both season and lifetime stats.
    /// </summary>
    public void RecordMatchResult(bool isWin, bool isDraw)
    {
        if (isWin) {
            currentSeasonStats.wins++;
            lifetimeStats.wins++;
        } else if (isDraw) {
            currentSeasonStats.draws++;
            lifetimeStats.draws++;
        } else {
            currentSeasonStats.losses++;
            lifetimeStats.losses++;
        }
    }

    /// <summary>
    /// Resets only the current season stats (e.g. at season start).
    /// </summary>
    public void ResetSeasonStats()
    {
        currentSeasonStats = new SeasonStats();
    }

}
