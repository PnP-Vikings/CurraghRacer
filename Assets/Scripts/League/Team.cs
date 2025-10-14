using System;
using System.Collections.Generic;
using System.Linq;
using League;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "NewTeam", menuName = "League/Team")]
public class Team : ScriptableObject
{
    [Header("Team Details")]
    public string teamName;
    public string teamDescription;
    public Image teamLogo;
    public Color teamColor;
    public int teamQuality;
    public TeamType teamType = TeamType.AI; // Type of team (Player, AI, Custom)
    public int teamExperience;

    [Header("Form & Performance")]
    [Tooltip("Current form rating (0-100). Higher form = better recent performance.")]
    [Range(0, 100)]
    public float currentForm = 50f;
    [Tooltip("Recent race results used to calculate form (last 5 races).")]
    public List<int> recentResults = new List<int>();

    [Header("Team Members")]
    [Tooltip("Team Manager.")]
    public TeamMember teamManager;
        
    public List<TeamMember> teamMembers = new List<TeamMember>();

    public List<TeamMember> bench = new List<TeamMember>();
    
    [Header("Default Team Setup (for new games)")]
    [Tooltip("Original default team members - used to restore team when starting a new game.")]
    public List<TeamMember> defaultTeamMembers = new List<TeamMember>();
    
    [Tooltip("Original default bench members - used to restore bench when starting a new game.")]
    public List<TeamMember> defaultBench = new List<TeamMember>();

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
        UpdateForm(position);
    }

    /// <summary>
    /// Updates team form based on recent race results.
    /// </summary>
    private void UpdateForm(int position)
    {
        recentResults.Add(position);
        
        // Keep only last 5 results for form calculation
        if (recentResults.Count > 5)
            recentResults.RemoveAt(0);
        
        // Calculate form based on recent results (1st = 100 points, 2nd = 90, etc.)
        float formTotal = 0f;
        foreach (int result in recentResults)
        {
            // Convert position to form points (1st = 100, 2nd = 90, 3rd = 80, etc.)
            float formPoints = Mathf.Max(0, 110 - (result * 10));
            formTotal += formPoints;
        }
        
        currentForm = formTotal / recentResults.Count;
        currentForm = Mathf.Clamp(currentForm, 0f, 100f);
    }

    /// <summary>
    /// Gets the effective race performance combining quality and form.
    /// </summary>
    public float GetRacePerformance()
    {
        // Combine base quality (60%) with current form (40%)
        return (teamQuality * 0.6f) + (currentForm * 0.4f);
    }
    
    public float GetRacePerformanceWithStarRating()
    {
        // Combine base quality (50%) with current form (30%) and star rating (20%)
        float starRating = 0f;
        if(LeagueController.Instance != null)
        {
            starRating = LeagueController.Instance.CalculateTeamStarRating(this);
        }
        
        return (teamQuality * 0.10f) + (currentForm * 0.20f) + (starRating * 0.70f);
    }

    /// <summary>
    /// Resets only the current season stats (e.g. at season start).
    /// </summary>
    public void ResetCurrentSeasonStats()
    {
        currentSeasonStats = new SeasonStats();
        // Reset form to base level at season start
        currentForm = 50f;
        recentResults.Clear();
    }

    public void ResetAllPlayerStats()
    {
        for (int i = 0; i < teamMembers.Count; i++)
        {
            if (teamMembers[i] != null)
            {
                teamMembers[i].ResetAllStats(teamQuality);
            }
        }
        
        for (int i = 0; i < bench.Count; i++)
        {
            if (bench[i] != null)
            {
                bench[i].ResetAllStats(teamQuality);
            }
        }
        
    }
    
    
    public void GiveExperience(int xp)
    {
        teamExperience += xp;
        // Optionally, increase team quality based on experience thresholds
        /*
        if (teamExperience >= 1000)
        {
            teamQuality = Mathf.Min(100, teamQuality + 1);
            teamExperience = 0; // Reset experience after quality increase
        }
        */
        
        UseExperience();
    }

    public void UseExperience()
    {
        for (int i = 0; i < teamMembers.Count; i++)
        {

            if (teamMembers[i] != null)
            {
                
                teamMembers[i].UpdateExperience(teamExperience / teamMembers.Count);
                teamExperience -= (teamExperience / teamMembers.Count);
            }

        }
        
    }

    public void ResetLifetimeStats()
    {
        lifetimeStats = new SeasonStats();
    }
    
    /// <summary>
    /// Restores the team to its default state for a new game.
    /// This includes resetting team members and bench to their original defaults.
    /// </summary>
    public void RestoreDefaultTeamSetup()
    {
        // Restore team members from defaults
        if (defaultTeamMembers != null && defaultTeamMembers.Count > 0)
        {
            teamMembers = new List<TeamMember>(defaultTeamMembers);
        }
        
        // Restore bench from defaults
        if (defaultBench != null && defaultBench.Count > 0)
        {
            bench = new List<TeamMember>(defaultBench);
        }
        
        Debug.Log($"Restored default team setup for {teamName}. Team Members: {teamMembers.Count}, Bench: {bench.Count}");
    }
    
    /// <summary>
    /// Gets the team's combined stats from all team members.
    /// Returns average stats if team has members, otherwise generates stats based on team quality.
    /// </summary>
    public CharacterStats GetTeamStats()
    {
        if (teamMembers != null && teamMembers.Count > 0)
        {
            // Calculate average stats from all team members
            float totalStrength = 0f;
            float totalStamina = 0f;
            float totalTechnique = 0f;
            float totalTeamWork = 0f;
            int memberCount = 0;

            foreach (var member in teamMembers)
            {
                if (member != null)
                {
                    var memberStats = member.GetStats();
                    totalStrength += memberStats.strength;
                    totalStamina += memberStats.stamina;
                    totalTechnique += memberStats.technique;
                    totalTeamWork += memberStats.teamWork;
                    memberCount++;
                }
            }

            if (memberCount > 0)
            {
                return new CharacterStats(
                    strength: totalStrength / memberCount,
                    stamina: totalStamina / memberCount,
                    technique: totalTechnique / memberCount,
                    teamWork: totalTeamWork / memberCount
                );
            }
        }

        // Fallback: Generate stats based on team quality and form
        float baseValue = teamQuality / 10f; // Convert 0-100 to 0-10 range
        float formModifier = (currentForm - 50f) / 50f; // -1 to +1 range
        float variation = 0.2f; // 20% variation between stats

        return new CharacterStats(
            strength: baseValue + (formModifier * 1f) + UnityEngine.Random.Range(-variation, variation),
            stamina: baseValue + (formModifier * 1f) + UnityEngine.Random.Range(-variation, variation),
            technique: baseValue + (formModifier * 0.8f) + UnityEngine.Random.Range(-variation, variation),
            teamWork: baseValue + (formModifier * 0.8f) + UnityEngine.Random.Range(-variation, variation)
        );
    }
    
    
}

public enum TeamType
{
    None,
    Player,
    AI,
    Custom
}
