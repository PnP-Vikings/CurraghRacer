using UnityEngine;
using UnityEngine.Serialization;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    public CharacterStats playerStats;
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
        playerStats = new CharacterStats(10,10,10,10);
    }
    
    
    public void SetPlayerStrength(int strength)
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
    }
    
    public CharacterStats GetPlayerStats()
    {
        return playerStats;
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
        
        ModifyPlayerEnergy(-energyRequired);
        
        bool canAfford = PurchaseItem(strength * 3);
        if (!canAfford)
        {
            Debug.LogWarning("Not enough coins to modify strength.");
            return;
        }
        
        playerStats.strength += strength;
        Debug.Log("Player strength modified: " + playerStats.strength);
        
        
    }
    
    public void ModifyPlayerStamina(int stamina)
    {
        
        float energyRequired = 30f; // Energy required to modify strength
        if (!playerHasEnoughEnergy(energyRequired))
        {
            return; // Not enough energy to modify strength
        }
        
        ModifyPlayerEnergy(-energyRequired);
        
        
        bool canAfford = PurchaseItem(stamina * 3);
        if (!canAfford)
        {
            Debug.LogWarning("Not enough coins to modify strength.");
            return;
        }
        playerStats.stamina += stamina;
        Debug.Log("Player stamina modified: " + playerStats.stamina);
    }
    public void ModifyPlayerTechnique(int technique)
    {
        float energyRequired = 30f; // Energy required to modify strength
        if (!playerHasEnoughEnergy(energyRequired))
        {
            return; // Not enough energy to modify strength
        }
        
        ModifyPlayerEnergy(-energyRequired);
        
        bool canAfford = PurchaseItem(technique * 3);
        if (!canAfford)
        {
            Debug.LogWarning("Not enough coins to modify strength.");
            return;
        }
        playerStats.technique += technique;
        Debug.Log("Player technique modified: " + playerStats.technique);
    }
    public void ModifyPlayerTeamWork(int teamWork)
    {
        float energyRequired = 30f; // Energy required to modify strength
        if (!playerHasEnoughEnergy(energyRequired))
        {
            return; // Not enough energy to modify strength
        }
        
        ModifyPlayerEnergy(-energyRequired);
        
        bool canAfford = PurchaseItem(teamWork * 3);
        if (!canAfford)
        {
            Debug.LogWarning("Not enough coins to modify strength.");
            return;
        }
        playerStats.teamWork += teamWork;
        Debug.Log("Player teamwork modified: " + playerStats.teamWork);
    }
    
    public void UpdatePlayerStats(int strength, int stamina, int technique, int teamWork)
    {
        playerStats.strength = strength;
        playerStats.stamina = stamina;
        playerStats.technique = technique;
        playerStats.teamWork = teamWork;
    }

    
    
    
}
