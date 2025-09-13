using League;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    public  Team playerTeam; 
    public TeamMember[] team;
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
        team = playerTeam.teamMembers;
       
    }
    

    /*public void SetPlayerStrength(int strength)
    {
        playerStats.strength = strength;
    }

    public void SetPlayerStamina(int stamina)
    {
        playerStats.stamina = stamina;
    }

    public void SetPlayerTechnique(int technique)
    {
        playerStats.technique = technique;
    }

    public void SetPlayerTeamWork(int teamWork)
    {
        playerStats.teamWork = teamWork;
    }*/



    public CharacterStats GetPlayerStats()
    {
     
        if (team.Length > 0)
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
        if (coins < 0) coins = 0; // Prevent negative coins
        playerStatsView.UpdatePlayerStats();
    }
    
    private bool CanAffordPurchase(float cost)
    {
        return coins >= cost;
    }

    public bool PurchaseItem(float cost)
    {
        if (CanAffordPurchase(cost))
        {
            ModifyPlayerCoins(-cost);
            return true;
        }
        else
        {
            Debug.LogWarning("Not enough coins to make this purchase.");
            return false;
        }
    }
    
    public bool playerHasEnoughEnergy(float energyCost)
    {
        return energy >= energyCost;
    }
    // Method to update player stats
    public void ModifyPlayerStrength(int strength)
    {
        float energyRequired = 30f; // Energy required to modify strength
        if (!playerHasEnoughEnergy(energyRequired))
        {
            return; // Not enough energy to modify strength
        }

        /*ModifyPlayerEnergy(-energyRequired);

        bool canAfford = PurchaseItem(strength * 3);
        if (!canAfford)
        {
            Debug.LogWarning("Not enough coins to modify strength.");
            return;
        }*/

        foreach (TeamMember member in team)
        {
            member.ImproveStat(TeamMember.StatType.Strength,strength);
            Debug.Log(member.memberName+" strength modified: " + member.GetTeamMemberStat(TeamMember.StatType.Strength));
        }

    }

    public void ModifyPlayerStamina(int stamina)
    {

        float energyRequired = 30f; // Energy required to modify strength
        if (!playerHasEnoughEnergy(energyRequired))
        {
            return; // Not enough energy to modify strength
        }

        ModifyPlayerEnergy(-energyRequired);


        /*
        bool canAfford = PurchaseItem(stamina * 3);
        if (!canAfford)
        {
            Debug.LogWarning("Not enough coins to modify strength.");
            return;
        }
        */

        foreach (TeamMember member in team)
        {
            member.ImproveStat(TeamMember.StatType.Stamina,2);
            Debug.Log(member.memberName+" stamina modified: " + member.GetTeamMemberStat(TeamMember.StatType.Stamina));
        }

    }
    public void ModifyPlayerTechnique(int technique,int cost =50)
    {
        float energyRequired = 30f; // Energy required to modify strength
        if (!playerHasEnoughEnergy(energyRequired))
        {
            return; // Not enough energy to modify strength
        }

        /*ModifyPlayerEnergy(-energyRequired);

        bool canAfford = PurchaseItem(cost);
        if (!canAfford)
        {
            Debug.LogWarning("Not enough coins to modify strength." + technique * 3 +" player coins" +coins );
            return;
        }*/

        foreach (TeamMember member in team)
        {
            member.ImproveStat(TeamMember.StatType.Technique,2);
            Debug.Log(member.memberName+" technique modified: " + member.GetTeamMemberStat(TeamMember.StatType.Technique));
        }

    }
    public void ModifyPlayerTeamWork(int teamWork)
    {
        float energyRequired = 30f; // Energy required to modify strength
        if (!playerHasEnoughEnergy(energyRequired))
        {
            return; // Not enough energy to modify strength
        }
        
        /*ModifyPlayerEnergy(-energyRequired);
        
        bool canAfford = PurchaseItem(teamWork * 3);
        if (!canAfford)
        {
            Debug.LogWarning("Not enough coins to modify strength.");
            return;
        }*/
        
        foreach (TeamMember member in team)
        {
            member.ImproveStat(TeamMember.StatType.TeamWork,2);
            Debug.Log(member.memberName+" teamwork modified: " + member.GetTeamMemberStat(TeamMember.StatType.TeamWork));
        }
    }
    
}
