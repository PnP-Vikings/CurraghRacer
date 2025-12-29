using System.Collections.Generic;
using League;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    public  Team playerTeam; 
    public List<TeamMember> team;
    public float energy= 100f; // Default energy value
    public float coins = 50f; // Default coins value
    public PlayerStatsView playerStatsView;
    
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
    }
    void Start()
    {
        // Initialize stats
       // playerStats = new CharacterStats(10,10,10,10);

       if(playerTeam == null)
       {
           playerTeam = LeagueController.Instance.currentLeague.GetPlayerTeam();
       }
       if (playerTeam != null)
       {
           team = playerTeam.teamMembers;

           if (TeamManager.Instance != null)
           {
               TeamManager.Instance.SetPlayerTeam(playerTeam);
               TeamManager.Instance.SetTeamManager(playerTeam.teamManager);
               TeamManager.Instance.SetActiveCrewMembers(playerTeam.teamMembers);
               TeamManager.Instance.SetBenchTeamMembers(playerTeam.bench);
           }
       }

    }

    public CharacterStats GetPlayerStats()
    {
     
        if (team.Count > 0)
        {
            float totalStrength = 0f;
            float totalStamina = 0f;
            float totalTechnique = 0f;
            float totalTeamWork = 0f;
            int memberCount = 0;
    
            foreach (var member in team)
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

        return new CharacterStats(
            strength: 4,
            stamina: 4,
            technique: 4,
            teamWork: 4
        );
    }
    
  public float GetPlayerEnergy()
    {
        return energy;
    }
  
  public float GetPlayerCurrency()
    {
        return coins;
    }
  
    // Method to modify player energy
  public void ModifyPlayerEnergy(float amount)
    {
        energy += amount;
        if (energy < 0) energy = 0; // Prevent negative energy
        if (energy > 100) energy = 100; // Cap energy at 100
        playerStatsView.UpdatePlayerStats();
    }
  
    public float GetPlayerCoins()
        {
            return coins;
        }

    public void ModifyPlayerCoins(float amount)
    {
        coins += amount;
      //  if (coins < 0) coins = 0; // Prevent negative coins
        playerStatsView.UpdatePlayerStats();
    }
    
    private bool CanAffordPurchase(float cost)
    {
        return coins >= cost;
    }

    public bool PurchaseItem(float cost, PurchaseType purchaseType = PurchaseType.Item)
    {
        if (!CanAffordPurchase(cost) && purchaseType == PurchaseType.RaceEntry)
        {
            Debug.Log($"Couldn't purchase {purchaseType}. You are now in debt by {coins-cost}coins.");
            ModifyPlayerCoins(-cost);
            return true; // Changed from false to true - purchase went through despite debt
        }
        else if(!CanAffordPurchase(cost) && purchaseType == PurchaseType.BillAutoPay)
        {
            
            Debug.Log($"Couldn't purchase {purchaseType}. You are now in debt by {coins-cost} coins.");
            ModifyPlayerCoins(-cost);
            return true; // Changed from false to true - purchase went through despite debt
        }
        else if (!CanAffordPurchase(cost) && purchaseType != PurchaseType.RaceEntry)
        {
            Debug.LogWarning("Not enough coins to make this purchase.");
            return false;
        }
        else if (CanAffordPurchase(cost))
        {
            ModifyPlayerCoins(-cost);
            return true;
        }
        return false;
    }
    
    public bool PlayerHasEnoughEnergy(float energyCost)
    {
        return energy >= energyCost;
    }
    // Method to update player stats
    public void ModifyPlayerStrength(int strength)
    {
        playerStatsView.ClearInfo();
        foreach (TeamMember member in team)
        {
            member.ImproveStat(TeamMember.StatType.Strength,strength);
            PlayerStatsView.Instance.DisplayInfo($"{member.memberName} gained {strength} Strength", 3);
            Debug.Log(member.memberName+" strength modified: " + member.GetTeamMemberStat(TeamMember.StatType.Strength));
        }

    }
    
    public void ModifyTeamMemberStat(TeamMember member, TeamMember.StatType statType, int amount)
    {
        List<TeamMember> tempList = new List<TeamMember>();
        
        tempList.AddRange(playerTeam.teamMembers);
        tempList.AddRange(TeamManager.Instance.benchTeamMembers);
        
        if (tempList.Contains(member))
        {
            member.ImproveStat(statType, amount);
            playerStatsView.ClearInfo();
            PlayerStatsView.Instance.DisplayInfo($"{member.memberName} gained {amount} {statType}", 3);
            Debug.Log($"{member.memberName}'s {statType} modified: " + member.GetTeamMemberStat(statType));
        }
        else
        {
            Debug.LogWarning("The specified member is not in the player's team.");
        }
    }
    
    

    public void ModifyPlayerStamina(int stamina)
    {
        playerStatsView.ClearInfo();
        foreach (TeamMember member in team)
        {
            member.ImproveStat(TeamMember.StatType.Stamina,stamina);
            PlayerStatsView.Instance.DisplayInfo($"{member.memberName} gained {stamina} Stamina", 3);
            Debug.Log(member.memberName+" stamina modified: " + member.GetTeamMemberStat(TeamMember.StatType.Stamina));
        }

    }
    public void ModifyPlayerTechnique(int technique)
    {
        playerStatsView.ClearInfo();
        foreach (TeamMember member in team)
        {
            member.ImproveStat(TeamMember.StatType.Technique,technique);
            
            PlayerStatsView.Instance.DisplayInfo($"{member.memberName} gained {technique} Technique", 3);
            Debug.Log(member.memberName+" technique modified: " + member.GetTeamMemberStat(TeamMember.StatType.Technique));
        }

    }
    public void ModifyPlayerTeamWork(int teamWork)
    {
        playerStatsView.ClearInfo();
        foreach (TeamMember member in team)
        {
            member.ImproveStat(TeamMember.StatType.TeamWork,teamWork);
            PlayerStatsView.Instance.DisplayInfo($"{member.memberName} gained {teamWork} TeamWork", 3);
            Debug.Log(member.memberName+" teamwork modified: " + member.GetTeamMemberStat(TeamMember.StatType.TeamWork));
        }
    }



    
}


public enum PurchaseType
{
        RaceEntry,
        HireRacer,
        Bill,
        BillAutoPay,
        Sleep,
        Training,
        Item
}