using System;
using UnityEngine;
using UnityEngine.Localization;
using Random = UnityEngine.Random;

public enum Attitude
{
    Positive,    // generally upbeat and optimistic
    Neutral,     // balanced, neither overly positive nor negative
    Negative,    // pessimistic or downbeat
    Competitive, // thrives on competition and challenges
    Cooperative, // works well with others, team-oriented
    Cautious,    // careful and risk-averse
    Aggressive,  // assertive and driven
    Energetic,   // high energy, enthusiastic
    Lazy         // low motivation, needs encouragement
}


 



[CreateAssetMenu(fileName = "NewTeamMember", menuName = "League/TeamMember")]
public class TeamMember : ScriptableObject
{
    [Header("Team Member Info")]
    [Tooltip("The display name of the team member.")]
    public string memberName;

    public Sprite memberIcon;
    
    [Tooltip("The in-game model or avatar for this member.")]
    public GameObject memberPrefab; // Prefab for in-game representation

    [Tooltip("A short biography or backstory for this member.")]
    public string memberDescription;

    public int age =23; // Default age

    [Tooltip("The general attitude of this member during races/events.")]
    public Attitude attitude = Attitude.Neutral;

    [Header("Team Member Stats")]
    [Tooltip("Base stats: strength, stamina, technique, team work.")]
    public CharacterStats characterStats = new CharacterStats(5f, 5f, 5f, 5f); // default stats: strength, stamina, technique, teamWork

    [Header("Progression")]
    [Tooltip("Current level of this team member.")]
    public int level = 1;

    [Tooltip("Current XP earned towards next level.")]
    public int experience = 0;

    [Tooltip("XP required to reach the next level.")]
    public int xpToNextLevel = 100;

    [Header("Economy")]
    [Tooltip("Base salary or cost for hiring this member.")]
    public int salary = 50;
    
    [Tooltip("Races available for.")]
    public int racesAvailableFor = 999999; // Default
    
    [Header("Happiness && Fitness")]
    public TeamMemberFitness fitness = new TeamMemberFitness();
    [Tooltip("Happiness level of the team member (0-100).")]
    public Happiness Happiness = new Happiness(); // Default happiness level
    
    [SerializeField] private LocalizedString _localizedAttitudePositive = new LocalizedString { TableReference = "TeamMemberDetailCards", TableEntryReference = "TeamMemberAttitude.Positive" };
    [SerializeField] private LocalizedString _localizedAttitudeNegative = new LocalizedString { TableReference = "TeamMemberDetailCards", TableEntryReference = "TeamMemberAttitude.Negative" };
    [SerializeField] private LocalizedString _localizedAttitudeNeutral = new LocalizedString { TableReference = "TeamMemberDetailCards", TableEntryReference = "TeamMemberAttitude.Neutral" };
    [SerializeField] private LocalizedString _localizedAttitudeCompetitive = new LocalizedString { TableReference = "TeamMemberDetailCards", TableEntryReference = "TeamMemberAttitude.Competitive" };
    [SerializeField] private LocalizedString _localizedAttitudeCooperative = new LocalizedString { TableReference = "TeamMemberDetailCards", TableEntryReference = "TeamMemberAttitude.Cooperative" };
    [SerializeField] private LocalizedString _localizedAttitudeCautious = new LocalizedString { TableReference = "TeamMemberDetailCards", TableEntryReference = "TeamMemberAttitude.Cautious" };
    [SerializeField] private LocalizedString _localizedAttitudeAggressive = new LocalizedString { TableReference = "TeamMemberDetailCards", TableEntryReference = "TeamMemberAttitude.Aggressive" };
    [SerializeField] private LocalizedString _localizedAttitudeEnergetic = new LocalizedString { TableReference = "TeamMemberDetailCards", TableEntryReference = "TeamMemberAttitude.Energetic" };
    [SerializeField] private LocalizedString _localizedAttitudeLazy = new LocalizedString { TableReference = "TeamMemberDetailCards", TableEntryReference = "TeamMemberAttitude.Lazy" };
    [SerializeField] private LocalizedString _localizedStatStrength = new LocalizedString { TableReference = "TeamMemberDetailCards", TableEntryReference = "TeamMember.Stat.Strength" };
    [SerializeField] private LocalizedString _localizedStatStamina = new LocalizedString { TableReference = "TeamMemberDetailCards", TableEntryReference = "TeamMember.Stat.Stamina" };
    [SerializeField] private LocalizedString _localizedStatTechnique = new LocalizedString { TableReference = "TeamMemberDetailCards", TableEntryReference = "TeamMember.Stat.Technique" };
    [SerializeField] private LocalizedString _localizedStatTeamWork = new LocalizedString { TableReference = "TeamMemberDetailCards", TableEntryReference = "TeamMember.Stat.TeamWork" };
    [SerializeField] private LocalizedString _localizedStatUnknown = new LocalizedString { TableReference = "TeamMemberDetailCards", TableEntryReference = "TeamMember.Stat.UnknownStat" };
    
    
    public CharacterStats GetStats()
    {
        return characterStats;
    }
    
    public void ReduceRacesAvailable()
    {
        if (racesAvailableFor > 0)
        {
            racesAvailableFor--;
        }
    }
    
    
    
    
    public void ResetAllStats(int teamQuality = 1)
    {
        characterStats = GetDefaultStatsBasedOnAttitude(teamQuality);
        level = 1;
        experience = 0;
        xpToNextLevel = 100;
        racesAvailableFor = 999999; // Reset to default
        fitness.ResetFitness();
        Happiness = new Happiness(); // Reset to default happiness
    }
    
    public virtual CharacterStats GetDefaultStatsBasedOnAttitude(int teamQuality = 1)
    {
      
        switch (attitude)
        {
            case Attitude.Positive:
                return new CharacterStats(6f * teamQuality, 5f * teamQuality, 6f * teamQuality, 7f * teamQuality);
            case Attitude.Competitive:
                return new CharacterStats(7f * teamQuality, 5f * teamQuality, 6f * teamQuality, 4f * teamQuality);
            case Attitude.Cooperative:
                return new CharacterStats(5f * teamQuality, 5f * teamQuality, 6f * teamQuality, 8f * teamQuality);
            case Attitude.Aggressive:
                return new CharacterStats(7f * teamQuality, 6f * teamQuality, 5f * teamQuality, 3f * teamQuality);
            case Attitude.Energetic:
                return new CharacterStats(6f * teamQuality, 7f * teamQuality, 5f * teamQuality, 5f * teamQuality);
            case Attitude.Cautious:
                return new CharacterStats(5f * teamQuality, 6f * teamQuality, 7f * teamQuality, 6f * teamQuality);
            case Attitude.Lazy:
                return new CharacterStats(4f * teamQuality, 4f * teamQuality, 5f * teamQuality, 5f * teamQuality);
            case Attitude.Negative:
                return new CharacterStats(5f * teamQuality, 5f * teamQuality, 5f * teamQuality, 3f * teamQuality);
            case Attitude.Neutral:
            default:
                return new CharacterStats(5f * teamQuality, 5f * teamQuality, 5f * teamQuality, 5f * teamQuality);
        }
    }
    
    
    public string GetLocalizedAttitudeString()
    {
        switch (attitude)
        {
            case Attitude.Positive:
               if (!_localizedAttitudePositive.IsEmpty)
                {
                    return _localizedAttitudePositive.GetLocalizedString();
                }
                return "Positive";
            case Attitude.Negative:
                if (!_localizedAttitudeNegative.IsEmpty)
                {
                    return _localizedAttitudeNegative.GetLocalizedString();
                }
                return "Negative";
            case Attitude.Neutral:
                if (!_localizedAttitudeNeutral.IsEmpty)
                {
                    return _localizedAttitudeNeutral.GetLocalizedString();
                }
                return "Neutral";
            case Attitude.Competitive:
                if (!_localizedAttitudeCompetitive.IsEmpty)
                {
                    return _localizedAttitudeCompetitive.GetLocalizedString();
                }
                return "Competitive";
            case Attitude.Cooperative:
                if (!_localizedAttitudeCooperative.IsEmpty)
                {
                    return _localizedAttitudeCooperative.GetLocalizedString();
                }
                return "Cooperative";
            case Attitude.Aggressive:
                if (!_localizedAttitudeAggressive.IsEmpty)
                {
                    return _localizedAttitudeAggressive.GetLocalizedString();
                }
                return "Aggressive";
            case Attitude.Energetic:
                if (!_localizedAttitudeEnergetic.IsEmpty)
                {
                    return _localizedAttitudeEnergetic.GetLocalizedString();
                }
                return "Energetic";
            case Attitude.Cautious:
                if (!_localizedAttitudeCautious.IsEmpty)
                {
                    return _localizedAttitudeCautious.GetLocalizedString();
                }
                return "Cautious";
            case Attitude.Lazy:
                if (!_localizedAttitudeLazy.IsEmpty)
                {
                    return _localizedAttitudeLazy.GetLocalizedString();
                }
                return "Lazy";
            default:
                return "Unknown";
        }
    }
    
    public int ExperienceNeededForNextLevel()
    {
        return xpToNextLevel - experience;
    }
    
    public void UpdateExperience(int xpGained)
    {
        experience += xpGained;
        while (experience >= xpToNextLevel)
        {
            experience -= xpToNextLevel;
            LevelUp();
          
        }
    }

    public void LevelUp()
    {
        level++;
        xpToNextLevel = Mathf.RoundToInt((100 * level )); // Increase XP requirement for next level
        
        // Base stat increases per level
        float baseIncrease = 0.5f + (.1f * level); // Scales with level
        float primaryIncrease = 1.0f + (0.3f * level);
        float secondaryIncrease = 0.8f+(0.2f * level);
        
        // Apply stat increases based on attitude
        switch (attitude)
        {
            case Attitude.Positive:
                // Positive attitude boosts teamwork and technique
                characterStats.teamWork += primaryIncrease;
                characterStats.technique += secondaryIncrease;
                characterStats.strength += baseIncrease;
                characterStats.stamina += baseIncrease;
                break;
                
            case Attitude.Competitive:
                // Competitive attitude focuses on strength and technique
                characterStats.strength += primaryIncrease;
                characterStats.technique += secondaryIncrease;
                characterStats.stamina += baseIncrease;
                characterStats.teamWork += baseIncrease * 0.5f; // Less teamwork focus
                break;
                
            case Attitude.Cooperative:
                // Cooperative attitude maximizes teamwork
                characterStats.teamWork += primaryIncrease * 1.2f;
                characterStats.technique += secondaryIncrease;
                characterStats.strength += baseIncrease;
                characterStats.stamina += baseIncrease;
                break;
                
            case Attitude.Aggressive:
                // Aggressive attitude prioritizes strength and stamina
                characterStats.strength += primaryIncrease;
                characterStats.stamina += secondaryIncrease;
                characterStats.technique += baseIncrease;
                characterStats.teamWork += baseIncrease * 0.3f; // Much less teamwork
                break;
                
            case Attitude.Energetic:
                // Energetic attitude focuses on stamina and strength
                characterStats.stamina += primaryIncrease;
                characterStats.strength += secondaryIncrease;
                characterStats.technique += baseIncrease;
                characterStats.teamWork += baseIncrease;
                break;
                
            case Attitude.Cautious:
                // Cautious attitude emphasizes technique and teamwork
                characterStats.technique += primaryIncrease;
                characterStats.teamWork += secondaryIncrease;
                characterStats.strength += baseIncrease * 0.8f;
                characterStats.stamina += baseIncrease;
                break;
                
            case Attitude.Lazy:
                // Lazy attitude has reduced growth overall but balanced
                characterStats.strength += baseIncrease * 0.6f;
                characterStats.stamina += baseIncrease * 0.4f; // Very low stamina growth
                characterStats.technique += baseIncrease * 0.8f;
                characterStats.teamWork += baseIncrease * 0.7f;
                break;
                
            case Attitude.Negative:
                // Negative attitude has poor teamwork but decent individual stats
                characterStats.strength += secondaryIncrease;
                characterStats.stamina += baseIncrease;
                characterStats.technique += baseIncrease;
                characterStats.teamWork += baseIncrease * 0.2f; // Very poor teamwork growth
                break;
                
            case Attitude.Neutral:
            default:
                // Neutral attitude gets balanced growth
                characterStats.strength += baseIncrease;
                characterStats.stamina += baseIncrease;
                characterStats.technique += baseIncrease;
                characterStats.teamWork += baseIncrease;
                break;
        }
        
        // Apply some randomness to make each level up feel unique
        float randomFactor = Random.Range(0.8f, 1.2f);
        characterStats.strength *= randomFactor;
        characterStats.stamina *= randomFactor;
        characterStats.technique *= randomFactor;
        characterStats.teamWork *= randomFactor;
        
        // Ensure stats don't go below minimum values
        characterStats.strength = Mathf.Max(characterStats.strength, 1f);
        characterStats.stamina = Mathf.Max(characterStats.stamina, 1f);
        characterStats.technique = Mathf.Max(characterStats.technique, 1f);
        characterStats.teamWork = Mathf.Max(characterStats.teamWork, 1f);
        
        // Optional: Cap maximum stats to prevent overpowered characters
        float maxStat = 50f + (level * 2f); // Increases with level
        characterStats.strength = Mathf.Min(characterStats.strength, maxStat);
        characterStats.stamina = Mathf.Min(characterStats.stamina, maxStat);
        characterStats.technique = Mathf.Min(characterStats.technique, maxStat);
        characterStats.teamWork = Mathf.Min(characterStats.teamWork, maxStat);
        
        Debug.Log($"{memberName} leveled up to {level}! Stats updated based on {attitude} attitude.");
        
    }
    
    public void ImproveStat(StatType statType, float amount)
      {
          float modifier = 1f;
          switch (attitude)
          {
              case Attitude.Energetic:
                  if (statType == StatType.Stamina) modifier = 1.3f;
                  if (statType == StatType.Strength) modifier = 1.1f;
                  break;
              case Attitude.Competitive:
                  if (statType == StatType.Strength) modifier = 1.2f;
                  if (statType == StatType.Technique) modifier = 1.1f;
                  break;
              case Attitude.Cooperative:
                  if (statType == StatType.TeamWork) modifier = 1.3f;
                  break;
              case Attitude.Aggressive:
                  if (statType == StatType.Strength) modifier = 1.2f;
                  break;
              case Attitude.Cautious:
                  if (statType == StatType.Technique) modifier = 1.2f;
                  break;
              case Attitude.Lazy:
                  modifier = 0.7f;
                  break;
              case Attitude.Positive:
                  if (statType == StatType.TeamWork) modifier = 1.2f;
                  break;
              case Attitude.Negative:
                  if (statType == StatType.TeamWork) modifier = 0.7f;
                  break;
              // Neutral and default: no modifier
          }
      
          float finalAmount = amount * modifier;
          switch (statType)
          {
              case StatType.Strength:
                  characterStats.strength += finalAmount;
                  break;
              case StatType.Stamina:
                  characterStats.stamina += finalAmount;
                  break;
              case StatType.Technique:
                  characterStats.technique += finalAmount;
                  break;
              case StatType.TeamWork:
                  characterStats.teamWork += finalAmount;
                  break;
          }
      }

    public int GetTeamMemberStat(StatType statType)
    {
        switch (statType)
        {
            case StatType.Strength:
                return Mathf.RoundToInt(characterStats.strength);
            case StatType.Stamina:
                return Mathf.RoundToInt(characterStats.stamina);
            case StatType.Technique:
                return Mathf.RoundToInt(characterStats.technique);
            case StatType.TeamWork:
                return Mathf.RoundToInt(characterStats.teamWork);
            default:
                Debug.LogWarning("Unknown stat type requested.");
                return 0;
        }
    }


    
    public enum StatType
    {
        Strength,
        Stamina,
        Technique,
        TeamWork
    }
    
    public string GetLocalizedStatName(StatType statType)
    {
        switch (statType)
        {
            case StatType.Strength:
                if (!_localizedStatStrength.IsEmpty)
                {
                    return _localizedStatStrength.GetLocalizedString();
                }
                return "Strength"; 
            case StatType.Stamina:
                if (!_localizedStatStamina.IsEmpty)
                {
                    return _localizedStatStamina.GetLocalizedString();
                }
                return "Stamina"; 
            case StatType.Technique:
                if (!_localizedStatTechnique.IsEmpty)
                {
                    return _localizedStatTechnique.GetLocalizedString();
                }
                return "Technique"; 
            case StatType.TeamWork:
                if (!_localizedStatTeamWork.IsEmpty)
                {
                    return _localizedStatTeamWork.GetLocalizedString();
                }
                return "Team Work"; 
            default:
                if (!_localizedStatUnknown.IsEmpty)
                {
                    return _localizedStatUnknown.GetLocalizedString();
                }
                return "Unknown Stat";
        }
    }
    
}


public class TeamMemberFitness
{
    public float currentFitness;
    public float maxFitness = 100f;
    public float recoveryRate = 5f; // Fitness points recovered per hour
    public float HungerLevel;
    public float maxHungerLevel = 100f;
    public bool isInjured = false;
    
    public enum PhysicalState
    {
        Energetic,
        Tired,
        Exhausted,
        Hungry,
        Thirsty
    }
    
    public void AdjustFitness(float amount)
    {
        currentFitness = Mathf.Clamp(currentFitness + amount, 0, maxFitness);
    }
    
    public TeamMemberFitness()
    {
        currentFitness = maxFitness; // Start fully fit
        HungerLevel = 100f; // Start fully satiated
    }

   public enum InjuryStatus
   {
       Healthy,
       Minor,
       Major,
       Critical
   }
   
   public void Injure(InjuryStatus status)
   {
       injuryStatus = status;
       switch (status)
       {
           case InjuryStatus.Healthy:
               isInjured = false;
               break;
           case InjuryStatus.Minor:
               isInjured = true;
               AdjustFitness(-10f);
               break;
           case InjuryStatus.Major:
               isInjured = true;
               AdjustFitness(-30f);
               break;
           case InjuryStatus.Critical:
               isInjured = true;
               AdjustFitness(-50f);
               break;
       }
   }
   
   public bool IsPlayerFitToRace()
   {
       return currentFitness >= 30f && injuryStatus == InjuryStatus.Healthy || injuryStatus == InjuryStatus.Minor;
   }
   
   
   public InjuryStatus injuryStatus = InjuryStatus.Healthy;

   public PhysicalState currentPhysicalState = PhysicalState.Energetic;
   
   public void ResetFitness()
   {
       currentFitness = maxFitness;
       HungerLevel = maxHungerLevel;
       injuryStatus = InjuryStatus.Healthy;
       currentPhysicalState = PhysicalState.Energetic;
   }
  
 
   
}

public class Happiness
{
    public float currentHappiness;
    public float maxHappiness = 100f;

    public Happiness()
    {
        currentHappiness = maxHappiness; // Start fully happy
    }
    
    public void AdjustHappiness(float amount)
    {
        currentHappiness = Mathf.Clamp(currentHappiness + amount, 0, maxHappiness);
    }
    
    public enum Mood
    {
        Happy,
        Neutral,
        Sad,
        Angry,
        Excited
    }
    
    public Mood currentMood = Mood.Happy;
}