using System.Collections.Generic;
using League;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Serialization;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    public  Team playerTeam; 
    public List<TeamMember> team;
    public float energy= 100f; // Default energy value
    public float coins = 50f; // Default coins value
    public float maxAmountOfDebt = -400f; // Maximum debt allowed
    public PlayerStatsView playerStatsView;
    public UnityEvent onDebtWarning;
    
    
    [Header("Localization")]
    [SerializeField] private LocalizedString localizedStatGainedText = new LocalizedString { TableReference = "PlayerManager", TableEntryReference = "PlayerManager.TeamMemberStatGained" };
    [SerializeField] private LocalizedString localizedDebtWarningEarnMoneyText = new LocalizedString { TableReference = "PlayerManager", TableEntryReference = "PlayerManager.DebtWarning.EarnMoney" };
    [SerializeField] private LocalizedString localizedDebtWarningReachedMaximumDebtLimitText = new LocalizedString { TableReference = "PlayerManager", TableEntryReference = "PlayerManager.DebtWarning.ReachedMaximumDebtLimit" };
    
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
        
        if(TimeManager.Instance != null)
            TimeManager.Instance.onNewDay.AddListener(CheckForMaxDebt);
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
        CheckForDebtWarnings();
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
            return true;
        }
        else if(!CanAffordPurchase(cost) && purchaseType == PurchaseType.BillAutoPay)
        {
            
            Debug.Log($"Couldn't purchase {purchaseType}. You are now in debt by {coins-cost} coins.");
            ModifyPlayerCoins(-cost);
            return true; 
        }
        else if(!CanAffordPurchase(cost) && purchaseType == PurchaseType.Cards)
        {
            
            Debug.Log($"Couldn't purchase {purchaseType}. You are now in debt by {coins-cost} coins.");
            ModifyPlayerCoins(cost);
            return true; 
        }
        else if(CanAffordPurchase(cost) && purchaseType == PurchaseType.Cards)
        {
            
            Debug.Log($"Couldn't purchase {purchaseType}. You are now in debt by {coins-cost} coins.");
            ModifyPlayerCoins(cost);
            return true; 
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
    public void ModifyTeamMemberStat(TeamMember member, TeamMember.StatType statType, int amount)
    {
        List<TeamMember> tempList = new List<TeamMember>();
        
        tempList.AddRange(playerTeam.teamMembers);
        tempList.AddRange(TeamManager.Instance.benchTeamMembers);
        
        if (tempList.Contains(member))
        {
            member.ImproveStat(statType, amount);
            PlayerStatsView.Instance.ClearInfo();
            string statGainedMessage = $"{member.memberName} gained {amount} {member.GetLocalizedStatName(statType)}";
            if (localizedStatGainedText != null && !localizedStatGainedText.IsEmpty)
            {
             localizedStatGainedText.Arguments = new object[] { member.memberName, amount, member.GetLocalizedStatName(statType) };
             localizedStatGainedText.Arguments[0] = member.memberName;
             localizedStatGainedText.Arguments[1] = amount;
             localizedStatGainedText.Arguments[2] = member.GetLocalizedStatName(statType);
             localizedStatGainedText.RefreshString();
             statGainedMessage = localizedStatGainedText.GetLocalizedString();
            }
            PlayerStatsView.Instance.DisplayInfo(statGainedMessage, 3);
            Debug.Log($"{member.memberName}'s {statType} modified: " + member.GetTeamMemberStat(statType));
        }
        else
        {
            Debug.LogWarning("The specified member is not in the player's team.");
        }
    }

    public void CheckForDebtWarnings()
    {
        if (coins < 0)
        {
            onDebtWarning.Invoke();
            if (PlayerStatsView.Instance != null)
            {
                string warningMessage = "Warning: You are in debt! Earn more money to avoid penalties.";
                if (localizedDebtWarningEarnMoneyText != null && !localizedDebtWarningEarnMoneyText.IsEmpty)
                {
                    warningMessage = localizedDebtWarningEarnMoneyText.GetLocalizedString();
                }
                PlayerStatsView.Instance.DisplayInfo(warningMessage, 5);
            }
            Debug.LogWarning("Player is in debt!");
        }
    }
    
    public void CheckForMaxDebt()
    {
        if (coins <= maxAmountOfDebt)
        {
            if (PlayerStatsView.Instance != null)
            {
                string warningMessage = "You have reached the maximum debt limit! Game Over.";
                if (localizedDebtWarningReachedMaximumDebtLimitText != null && !localizedDebtWarningReachedMaximumDebtLimitText.IsEmpty)
                {
                    warningMessage = localizedDebtWarningReachedMaximumDebtLimitText.GetLocalizedString();
                }
                PlayerStatsView.Instance.DisplayInfo(warningMessage, 5);
            }
            Debug.LogError("Player has reached maximum debt limit!");
            GameManager.Instance.TriggerGameOver();
        }
    }
    
    
}


public enum PurchaseType
{
        RaceEntry,
        HireRacer,
        Bill,
        BillAutoPay,
        Cards,
        Sleep,
        Training,
        Item
}