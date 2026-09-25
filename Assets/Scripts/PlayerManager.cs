using System.Collections.Generic;
using DG.Tweening;
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
    
    [Header("Energy and Currency Costs")]
    [Tooltip("The amount of energy required to participate in a race")]
    [SerializeField] private int energyCostForRace = 25;
    [Tooltip("The amount of energy required to work")]
    [SerializeField] private int energyCostForWork = 25;
    [Tooltip("The amount of currency required to sleep")]
    [SerializeField] private int currencyCostForSleep = 30;
    [Tooltip("The amount of energy required for training")]
    [SerializeField] private int energyCostForTraining = 30;
    [Tooltip("The amount of currency required for training")]
    [SerializeField] private int currencyCostForTraining = 50;
    
    
    
    
    [Header("Localization")]
    [SerializeField] private LocalizedString localizedStatGainedText = new LocalizedString { TableReference = "PlayerManager", TableEntryReference = "PlayerManager.TeamMemberStatGained" };
    [SerializeField] private LocalizedString localizedStatLostText = new LocalizedString { TableReference = "PlayerManager", TableEntryReference = "PlayerManager.TeamMemberStatLost" };
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
       
       if(TimeManager.Instance != null)
           TimeManager.Instance.onNewDay.AddListener(ReduceTeamMemberStat);

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


    public void ReduceTeamMemberStat()
    {
        int numberOfMembersToDecrease = 1;
        int randomChance = Random.Range(0, 100);
        
        if (randomChance < 5) // 5% chance to decrease stats for 2 members
        {
            numberOfMembersToDecrease = 2;
        }
        
        for (int i = 0; i < numberOfMembersToDecrease; i++)
        {
            TeamMember member = GetRandomTeamMember();
            int numberOfStatsToDecrease = 1;
            
            int statDecreaseChance = Random.Range(0, 100);
            
            if (statDecreaseChance < 5) // 5% chance to decrease 2 stats
            {
                numberOfStatsToDecrease = 2;
            }
            int tryDifferentMemberCount = 0;
            for (int j = 0; j < numberOfStatsToDecrease; j++)
            {
              bool statWasDecreased = ProcessChosenStat(member);

              if (!statWasDecreased)
              { 
                  member = GetRandomTeamMember();
                  tryDifferentMemberCount++;
                  if(tryDifferentMemberCount >3)
                  {
                      Debug.LogWarning("Could not find a team member with stats that can be decreased after 3 tries.");
                      break;
                  }
                  else
                  {
                      j--; // Retry the same stat decrease for the new member
                  }
              }
            }
        }
        // Update the cached save data to include the new stats
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.UpdateCachedSaveData();
        }
    }

    public bool ProcessChosenStat(TeamMember member)
    {
        TeamMember.StatType statType = GetRandomStatType();
        bool statCanBeDecreased = false;
        int amount = 1; // Random amount between 1 and 3
        
        int randomChance = Random.Range(0, 100);
        
        if(randomChance > 20) // 50% chance to decrease stats by 1
        {
            amount = 1;
        }
        else if (randomChance < 5) // 5% chance to decrease stats by 3 
        {
            amount = 3;
        }
        else if (randomChance < 20) // 20% chance to decrease stats by 2
        {
            amount = 2;
        }
        
        
        if(member.GetTeamMemberStat(statType)-amount <= 0)
        {
            statType = GetRandomStatType();
        }
        else
        {
            statCanBeDecreased = true;
        }
        
        if(member.GetTeamMemberStat(statType)-amount > 0)
        {
            statCanBeDecreased = true;
        }
        
        
        if (!statCanBeDecreased && member.GetTeamMemberStat(statType)-amount <= 0)
        {
           for (int i = 0; i < 5; i++) // Try 5 times to find a stat that can be decreased
           {
               statType = GetRandomStatType();
               if(member.GetTeamMemberStat(statType)-amount > 0)
               {
                   statCanBeDecreased = true;
                   break;
               }
           }
        }
        
        if(statCanBeDecreased)
        {
            ModifyTeamMemberStat(member, statType, -amount);
            Debug.Log($"{member.memberName}'s {statType} reduced by {amount}. New value: {member.GetTeamMemberStat(statType)}");
            return true;
        }
        else
        {
            Debug.LogWarning($"Could not decrease any stat for {member.memberName} as all stats are at minimum.");
            return false;
        }
    }
    
    public TeamMember.StatType GetRandomStatType()
    {
        TeamMember.StatType[] statTypes = (TeamMember.StatType[])System.Enum.GetValues(typeof(TeamMember.StatType));
        int randomIndex = Random.Range(0, statTypes.Length);
        return statTypes[randomIndex];
    }
    
    public TeamMember GetRandomTeamMember()
    {
        List<TeamMember> teamMembers = new List<TeamMember>(playerTeam.teamMembers);
        teamMembers.AddRange(TeamManager.Instance.benchTeamMembers);

        // Remove team members with racesAvailableFor less than 100 which should only be hireable team members 
        for (int i = teamMembers.Count - 1; i >= 0; i--)
        {
            if (teamMembers[i].racesAvailableFor < 100)
            {
                teamMembers.RemoveAt(i);
            }
        }
        
        if (teamMembers.Count > 0)
        {
            int randomIndex = Random.Range(0, teamMembers.Count);
            return teamMembers[randomIndex];
        }
        Debug.LogWarning("No team members found.");
        return null;
    }
    
    
    public void ModifyTeamMemberStat(TeamMember member, TeamMember.StatType statType, int amount)
    {
        List<TeamMember> tempList = new List<TeamMember>();
        tempList.AddRange(playerTeam.teamMembers);
        tempList.AddRange(TeamManager.Instance.benchTeamMembers);
        
        if (tempList.Contains(member))
        {
            if (amount < 0)
            {
                member.DecreaseStat(statType, -amount);
                string statLostMessage = $"{member.memberName} lost {amount} {member.GetLocalizedStatName(statType)}";
                if (localizedStatLostText != null && !localizedStatLostText.IsEmpty)
                {
                    localizedStatLostText.Arguments = new object[] { member.memberName, amount, member.GetLocalizedStatName(statType) };
                    localizedStatLostText.Arguments[0] = member.memberName;
                    localizedStatLostText.Arguments[1] = amount;
                    localizedStatLostText.Arguments[2] = member.GetLocalizedStatName(statType);
                    localizedStatLostText.RefreshString();
                    statLostMessage = localizedStatLostText.GetLocalizedString();
                }
                DOVirtual.DelayedCall(3f, () =>
                {
                    //PlayerStatsView.Instance.ClearInfo();
                    PlayerStatsView.Instance.DisplayInfo(statLostMessage, 3);
                });
                
            }
            else
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
                
                DOVirtual.DelayedCall(3f, () =>
                {
                   // PlayerStatsView.Instance.ClearInfo();
                    PlayerStatsView.Instance.DisplayInfo(statGainedMessage, 3);
                });
              
            }
            
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
    
    public float GetMaxDebtLimit()
    {
        return maxAmountOfDebt;
    }
    
    public int GetEnergyCostForRace()
    {
        return energyCostForRace;
    }
    
    public int GetEnergyCostForWork()
    {
        return energyCostForWork;
    }

    public int GetCurrencyCostForSleep()
    {
        return currencyCostForSleep;
    }
    
    public int GetEnergyCostForTraining()
    {
        return energyCostForTraining;
    }
    public int GetCurrencyCostForTraining()
    {
        return currencyCostForTraining;
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