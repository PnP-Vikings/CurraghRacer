using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDecisionCard", menuName = "DecisionCard/NewCard")]
public class DecisionCard : ScriptableObject
{
    [Header("Card Display")]
    public string cardTitle;
    [TextArea(3, 6)]
    public string cardDescription;
    public Sprite cardImage;
    
    [Header("Card Type & Conditions")]
    [Tooltip("Category of this decision card")]
    public CardCategory category = CardCategory.TeamMember;
    
    [Tooltip("Minimum day for this card to appear")]
    public int minDayToAppear = 1;
    
    [Tooltip("Can this card appear multiple times?")]
    public bool canRepeat = true;
    
    [Tooltip("Cooldown in days before this card can appear again")]
    public int cooldownDays = 7;
    
    [Header("Target")]
    [Tooltip("Does this card target a specific team member?")]
    public bool targetsSpecificMember = false;
    
    [Tooltip("If targeting, which team member? Leave null for random")]
    public TeamMember specificTargetMember;
    
    [Tooltip("Required attitude for random selection (leave empty for any)")]
    public List<Attitude> requiredAttitudes = new List<Attitude>();
    
    [Header("Decision Options")]
    public DecisionOption optionA;
    public DecisionOption optionB;
    
    [Header("Card Weight")]
    [Tooltip("Probability weight for this card appearing (higher = more likely)")]
    [Range(1, 100)]
    public int appearanceWeight = 50;
}

public enum CardCategory
{
    TeamMember,      // Related to team member issues
    Financial,       // Money/business decisions
    Training,        // Training related
    Equipment,       // Boat/equipment decisions
    Event,           // Random events
    Opportunity,     // Special opportunities
    Crisis           // Urgent problems
}

[System.Serializable]
public class DecisionOption
{
    [Header("Option Display")]
    [Tooltip("Text shown on the button/swipe direction")]
    public string optionText;
    
    [TextArea(2, 4)]
    [Tooltip("Detailed description of this choice")]
    public string optionDescription;
    
    [Header("Immediate Effects")]
    [Tooltip("Coins gained/lost (negative for cost)")]
    public int coinsChange = 0;
    
    [Tooltip("Energy gained/lost")]
    public int energyChange = 0;
    
    [Tooltip("Team morale change (0-100)")]
    public int moraleChange = 0;
    
    [Tooltip("Happiness change for targeted team member")]
    public int happinessChange = 0;
    
    [Header("Stat Effects")]
    [Tooltip("Apply stat changes to the targeted team member")]
    public bool affectsStats = false;
    
    public CharacterStats statChanges;
    
    [Header("Risk & Consequences")]
    [Tooltip("Does this option have a chance-based outcome?")]
    public bool hasRisk = false;
    
    [Tooltip("Probability of success (0-100)")]
    [Range(0, 100)]
    public int successChance = 50;
    
    [Tooltip("What happens on success?")]
    public ConsequenceOutcome successOutcome;
    
    [Tooltip("What happens on failure?")]
    public ConsequenceOutcome failureOutcome;
    
    [Header("Time Effects")]
    [Tooltip("Does this affect team member availability?")]
    public bool affectsAvailability = false;
    
    [Tooltip("Days the team member is unavailable (injury/rest)")]
    public int daysUnavailable = 0;
    
    [Header("Follow-up")]
    [Tooltip("Does this decision trigger a follow-up card?")]
    public DecisionCard followUpCard;
    
    [Tooltip("Days until follow-up card appears")]
    public int daysUntilFollowUp = 1;
}

[System.Serializable]
public class ConsequenceOutcome
{
    [TextArea(2, 3)]
    public string outcomeMessage;
    
    public int coinsChange = 0;
    public int energyChange = 0;
    public int moraleChange = 0;
    public int happinessChange = 0;
    public int experienceChange = 0;
    
    [Tooltip("Injury chance (0-100)")]
    [Range(0, 100)]
    public int injuryChance = 0;
    
    [Tooltip("Days injured if injury occurs")]
    public int injuryDays = 0;
    
    public CharacterStats statChanges;
}

