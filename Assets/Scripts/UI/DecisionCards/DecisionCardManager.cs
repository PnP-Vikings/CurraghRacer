using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class DecisionCardManager : MonoBehaviour
{
    public static DecisionCardManager Instance { get; private set; }
    
    [Header("Card Pool")]
    [Tooltip("All available decision cards")]
    public List<DecisionCard> allDecisionCards = new List<DecisionCard>();
    
    [Header("Card Settings")]
    [Tooltip("Maximum cards shown per day")]
    public int maxCardsPerDay = 3;
    
    [Tooltip("Minimum cards shown per day")]
    public int minCardsPerDay = 1;
    
    [Header("Events")]
    public UnityEvent<DecisionCard> OnCardPresented;
    public UnityEvent<DecisionOption, bool> OnDecisionMade; // option, wasSuccess
    public UnityEvent OnAllCardsProcessed;
    
    
    // Tracking
    private Dictionary<DecisionCard, int> cardLastShownDay = new Dictionary<DecisionCard, int>();
    private Dictionary<DecisionCard, int> scheduledFollowUps = new Dictionary<DecisionCard, int>();
    private List<DecisionCard> todaysCards = new List<DecisionCard>();
    private DecisionCard currentCard;
    private TeamMember currentTargetMember;
    [SerializeField] DecisionCardUiMaster uiMaster;
    private bool cardsPresentedToUi = false;
    
    
    public void SetUiMaster(DecisionCardUiMaster master)
    {
        uiMaster = master;
        
        // If we have cards waiting to be shown and we just got a UI master, show them now
        if (uiMaster != null && todaysCards.Count > 0)
        {
            Debug.Log($"DecisionCardUiMaster assigned. We have {todaysCards.Count} pending cards. Showing them now.");
            /*uiMaster.gameObject.SetActive(true);
            uiMaster.GenerateTodaysCards();*/
        }
    }
    
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
    
    private void Start()
    {
        // Subscribe to new day event
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.onNewDay.AddListener(OnNewDay);
            OnAllCardsProcessed.AddListener(RestartTimeAfterCards);
            Debug.Log("DecisionCardManager subscribed to TimeManager.onNewDay");
        }
        else
        {
            Debug.LogWarning("TimeManager not found! DecisionCardManager won't generate daily cards automatically.");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.onNewDay.RemoveListener(OnNewDay);
        }
    }
    
    /// <summary>
    /// Called when a new day starts - generates and presents decision cards
    /// </summary>
    private void OnNewDay()
    {
        Debug.Log("New day started - generating decision cards");
        cardsPresentedToUi = false;
        
        if(GameManager.Instance != null && GameManager.Instance.IsGameOver())
        {
            Debug.Log("Game is over - skipping decision card generation");
            return;
        }
        
        // Generate today's cards
        GenerateDailyCards();
        
        // Process follow-up cards AFTER generating daily cards
        ProcessScheduledFollowUps();
        
        if (todaysCards.Count > 0)
        {
            if (uiMaster != null)
            {
                uiMaster.gameObject.SetActive(true);
                Debug.Log($"Generated {todaysCards.Count} decision cards for today (including follow-ups)");
                
                // Tell the UI Master to show the cards
                cardsPresentedToUi = true;
                uiMaster.GenerateTodaysCards();
                
                if(TimeManager.Instance != null)
                {
                   TimeManager.Instance.SetTimePauseState(true);
                }
            }
            else
            {
                Debug.LogWarning("DecisionCardUiMaster not assigned yet! Cards are generated and pending assignment of UI master.");
            }
        }
        else
        {
            if (uiMaster != null)
            {
                uiMaster.gameObject.SetActive(false);
            }
            Debug.Log("No decision cards to show today");
        }
    }
    
    /// <summary>
    /// Get today's cards (without regenerating)
    /// </summary>
    public List<DecisionCard> GetTodaysCardList()
    {
        return todaysCards;
    }
    
    /// <summary>
    /// Generate cards for the current day
    /// </summary>
    public List<DecisionCard> GenerateDailyCards()
    {
        todaysCards.Clear();
        int currentDay = TimeManager.Instance != null ? TimeManager.Instance.GetCurrentDay() : 1;
        
        Debug.Log($"GenerateDailyCards called for day {currentDay}. Total cards in pool: {allDecisionCards.Count}");
        
        // Get eligible cards
        List<DecisionCard> eligibleCards = GetEligibleCards(currentDay);
        
        Debug.Log($"Eligible cards: {eligibleCards.Count}");
        
        if (eligibleCards.Count == 0)
        {
            Debug.LogWarning("No eligible cards for today! Check your DecisionCard settings (minDayToAppear, cooldowns, etc.)");
            return todaysCards;
        }
        
        // Determine number of cards
        int numCards = Random.Range(minCardsPerDay, maxCardsPerDay + 1);
        numCards = Mathf.Min(numCards, eligibleCards.Count);
        
        Debug.Log($"Selecting {numCards} cards from {eligibleCards.Count} eligible cards");
        
        // Select cards based on weight
        for (int i = 0; i < numCards; i++)
        {
            DecisionCard selectedCard = SelectWeightedCard(eligibleCards);
            if (selectedCard != null)
            {
                todaysCards.Add(selectedCard);
                eligibleCards.Remove(selectedCard);
                Debug.Log($"Selected card {i + 1}/{numCards}: {selectedCard.cardTitle}");
            }
        }
        
        return todaysCards;
    }
    
    /// <summary>
    /// Get all cards that can appear today
    /// </summary>
    private List<DecisionCard> GetEligibleCards(int currentDay)
    {
        List<DecisionCard> eligible = new List<DecisionCard>();
        
        Debug.Log($"Checking {allDecisionCards.Count} cards for eligibility on day {currentDay}");
        
        foreach (var card in allDecisionCards)
        {
            if (card == null)
            {
                Debug.LogWarning("Found null card in allDecisionCards list!");
                continue;
            }
            
            // Skip cards marked as follow-up only
            if (card.isFollowUpOnly)
            {
                Debug.Log($"Card '{card.cardTitle}' not eligible: marked as follow-up only");
                continue;
            }
            
            // Check day requirement
            if (currentDay < card.minDayToAppear)
            {
                Debug.Log($"Card '{card.cardTitle}' not eligible: current day {currentDay} < minDayToAppear {card.minDayToAppear}");
                continue;
            }
            
            // Check cooldown
            if (cardLastShownDay.ContainsKey(card))
            {
                int daysSinceShown = currentDay - cardLastShownDay[card];
                if (daysSinceShown < card.cooldownDays)
                {
                    Debug.Log($"Card '{card.cardTitle}' not eligible: cooldown ({daysSinceShown} days since shown < {card.cooldownDays} cooldown)");
                    continue;
                }
                    
                // Check if can repeat
                if (!card.canRepeat && daysSinceShown >= 0)
                {
                    Debug.Log($"Card '{card.cardTitle}' not eligible: cannot repeat and was already shown");
                    continue;
                }
            }
            
            // Check if we have a valid target (if needed)
            if (card.targetsSpecificMember)
            {
                TeamMember target = FindValidTarget(card);
                if (target == null)
                {
                    Debug.Log($"Card '{card.cardTitle}' not eligible: no valid target member found");
                    continue;
                }
            }
            
            eligible.Add(card);
            Debug.Log($"Card '{card.cardTitle}' is ELIGIBLE (weight: {card.appearanceWeight})");
        }
        
        Debug.Log($"Total eligible cards: {eligible.Count}");
        return eligible;
    }
    
    /// <summary>
    /// Select a card based on appearance weight
    /// </summary>
    private DecisionCard SelectWeightedCard(List<DecisionCard> cards)
    {
        if (cards.Count == 0) return null;
        
        int totalWeight = cards.Sum(c => c.appearanceWeight);
        int randomValue = Random.Range(0, totalWeight);
        
        int currentWeight = 0;
        foreach (var card in cards)
        {
            currentWeight += card.appearanceWeight;
            if (randomValue < currentWeight)
                return card;
        }
        
        return cards[cards.Count - 1];
    }
    
    /// <summary>
    /// Find a valid team member target for a card
    /// </summary>
    private TeamMember FindValidTarget(DecisionCard card)
    {
        if (card.specificTargetMember != null)
            return card.specificTargetMember;
        
        // Get team members
        List<TeamMember> teamMembers = PlayerManager.Instance?.playerTeam?.teamMembers;
        if (teamMembers == null || teamMembers.Count == 0)
            return null;
        
        // Filter out temporary racers (racesAvailableFor <= 1000)
        // Permanent racers have racesAvailableFor set to 99999
        List<TeamMember> permanentMembers = teamMembers.Where(m => m != null && m.racesAvailableFor > 1000).ToList();
        
        if (permanentMembers.Count == 0)
        {
            Debug.LogWarning("No permanent team members found for decision card targeting");
            return null;
        }
        
        // Filter by required attitudes
        List<TeamMember> validMembers = permanentMembers;
        if (card.requiredAttitudes.Count > 0)
        {
            validMembers = permanentMembers.Where(m => card.requiredAttitudes.Contains(m.attitude)).ToList();
        }
        
        if (validMembers.Count == 0)
            return null;
        
        // Return random valid member
        return validMembers[Random.Range(0, validMembers.Count)];
    }
    
    /// <summary>
    /// Present a card to the player
    /// </summary>
    public void PresentCard(DecisionCard card)
    {
        currentCard = card;
        
        // Find target if needed
        if (card.targetsSpecificMember)
        {
            currentTargetMember = FindValidTarget(card);
        }
        
        // Mark as shown
        int currentDay = TimeManager.Instance != null ? TimeManager.Instance.GetCurrentDay() : 1;
        if (cardLastShownDay.ContainsKey(card))
            cardLastShownDay[card] = currentDay;
        else
            cardLastShownDay.Add(card, currentDay);
        
        OnCardPresented?.Invoke(card);
    }
    
    /// <summary>
    /// Process the player's decision
    /// </summary>
    public void MakeDecision(DecisionOption option)
    {
        if (currentCard == null || option == null)
        {
            Debug.LogWarning("No current card or option to process");
            return;
        }
        
        bool wasSuccess = true;
        
        // Check for risk-based outcome
        if (option.hasRisk)
        {
            int roll = Random.Range(0, 100);
            wasSuccess = roll < option.successChance;
            
            ConsequenceOutcome outcome = wasSuccess ? option.successOutcome : option.failureOutcome;
            ApplyConsequence(outcome);
        }
        else
        {
            // Apply immediate effects
            ApplyImmediateEffects(option);
        }
        
        // Apply availability changes
        if (option.affectsAvailability && currentTargetMember != null)
        {
            currentTargetMember.racesAvailableFor = option.daysUnavailable;
            Debug.Log($"{currentTargetMember.memberName} unavailable for {option.daysUnavailable} days");
        }
        
        // Schedule follow-up card if enabled and probability check passes
        if (option.hasFollowUp && option.followUpCard != null)
        {
            int roll = Random.Range(0, 100);
            if (roll < option.followUpChance)
            {
                ScheduleFollowUpCard(option.followUpCard, option.daysUntilFollowUp);
            }
        }
        
        OnDecisionMade?.Invoke(option, wasSuccess);

        if (PlayerStatsView.Instance != null)
        {
            PlayerStatsView.Instance.UpdatePlayerStats();
        }
    }
    
    /// <summary>
    /// Apply immediate effects from an option
    /// </summary>
    private void ApplyImmediateEffects(DecisionOption option)
    {
        // Apply coins
        if (option.coinsChange != 0 && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.PurchaseItem(option.coinsChange ,PurchaseType.Cards);
            Debug.Log($"Coins changed by {option.coinsChange}. New balance: {PlayerManager.Instance.coins}");
        }
        
        // Apply energy
        if (option.energyChange != 0 && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.energy = Mathf.Clamp(
                PlayerManager.Instance.energy + option.energyChange, 
                0, 100
            );
        }
        
        // Apply morale change to ALL team members (active + bench)
        if (option.moraleChange != 0)
        {
            ApplyMoraleToAllMembers(option.moraleChange);
        }
        
        if (option.timeLost != 0 && TimeManager.Instance != null)
        {
            TimeManager.Instance.AdjustTimeOfDay(+option.timeLost);
            Debug.Log($"Time adjusted by {+option.timeLost} hours");
        }
        
        // Apply to target member (individual effects)
        if (currentTargetMember != null)
        {
            // Happiness (individual change for the targeted member)
            if (option.happinessChange != 0)
            {
                currentTargetMember.Happiness.currentHappiness = Mathf.Clamp(
                    currentTargetMember.Happiness.currentHappiness + option.happinessChange,
                    0, 100
                );
                Debug.Log($"{currentTargetMember.memberName} happiness changed by {option.happinessChange}");
            }
            
            // Stats
            if (option.affectsStats)
            {
                ApplyStatChanges(currentTargetMember, option.statChanges);
            }
        }
    }
    
    /// <summary>
    /// Apply consequence outcome (success or failure)
    /// </summary>
    private void ApplyConsequence(ConsequenceOutcome outcome)
    {
        if (outcome == null) return;
        
        // Show outcome message
        Debug.Log($"Outcome: {outcome.outcomeMessage}");
        
        // Apply resource changes
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.PurchaseItem(outcome.coinsChange ,PurchaseType.Cards);
            
            PlayerManager.Instance.energy = Mathf.Clamp(
                PlayerManager.Instance.energy + outcome.energyChange,
                0, 100
            );
        }
        
        // Time adjustment
        if (outcome.timeLost != 0 && TimeManager.Instance != null)
        {
            TimeManager.Instance.AdjustTimeOfDay(+outcome.timeLost);
            Debug.Log($"Time adjusted by {+outcome.timeLost} hours");
        }
        
        // Apply morale change to ALL team members
        if (outcome.moraleChange != 0)
        {
            ApplyMoraleToAllMembers(outcome.moraleChange);
        }
        
        // Apply to target member
        if (currentTargetMember != null)
        {
            // Happiness (individual change for the targeted member)
            if (outcome.happinessChange != 0)
            {
                currentTargetMember.Happiness.currentHappiness = Mathf.Clamp(
                    currentTargetMember.Happiness.currentHappiness + outcome.happinessChange,
                    0, 100
                );
                Debug.Log($"{currentTargetMember.memberName} happiness changed by {outcome.happinessChange}");
            }
            
            // Experience
            currentTargetMember.experience += outcome.experienceChange;
            
            // Stats
            ApplyStatChanges(currentTargetMember, outcome.statChanges);
            
            // Injury check
            if (outcome.injuryChance > 0)
            {
                int roll = Random.Range(0, 100);
                if (roll < outcome.injuryChance)
                {
                    // Randomize injury severity between Minor and Critical (values 1-3)
                    TeamMemberFitness.InjuryStatus randomInjury = 
                        (TeamMemberFitness.InjuryStatus)Random.Range(1, 4); // 1=Minor, 2=Major, 3=Critical
                    
                    currentTargetMember.fitness.Injure(randomInjury);
                    Debug.Log($"{currentTargetMember.memberName} suffered a {randomInjury} injury!");
                }
            }
        }
        
        // Schedule follow-up card if enabled and probability check passes
        if (outcome.hasFollowUp && outcome.followUpCard != null)
        {
            int roll = Random.Range(0, 100);
            if (roll < outcome.followUpChance)
            {
                ScheduleFollowUpCard(outcome.followUpCard, outcome.daysUntilFollowUp);
            }
        }
    }
    
    /// <summary>
    /// Apply stat changes to a team member
    /// </summary>
    private void ApplyStatChanges(TeamMember member, CharacterStats statChanges)
    {
        CharacterStats current = member.characterStats;
        
        member.characterStats = new CharacterStats(
            Mathf.Max(0, current.strength + statChanges.strength),
            Mathf.Max(0, current.stamina + statChanges.stamina),
            Mathf.Max(0, current.technique + statChanges.technique),
            Mathf.Max(0, current.teamWork + statChanges.teamWork)
        );
    }
    
    /// <summary>
    /// Apply morale change (happiness) to all team members (active crew + bench)
    /// </summary>
    private void ApplyMoraleToAllMembers(int moraleChange)
    {
        if (PlayerManager.Instance?.playerTeam == null)
        {
            Debug.LogWarning("Cannot apply morale - no player team found");
            return;
        }
        
        int membersAffected = 0;
        
        // Apply to active crew members
        if (PlayerManager.Instance.playerTeam.teamMembers != null)
        {
            foreach (var member in PlayerManager.Instance.playerTeam.teamMembers)
            {
                if (member != null && member.Happiness != null)
                {
                    member.Happiness.currentHappiness = Mathf.Clamp(
                        member.Happiness.currentHappiness + moraleChange,
                        0, 100
                    );
                    membersAffected++;
                }
                if (member.Happiness != null)
                    Debug.Log($"Applied morale change to {member.memberName}, new happiness: {member.Happiness.currentHappiness}");
            }
        }
        
        // Apply to bench members
        if (PlayerManager.Instance.playerTeam.bench != null)
        {
            foreach (var member in PlayerManager.Instance.playerTeam.bench)
            {
                if (member != null && member.Happiness != null)
                {
                    member.Happiness.currentHappiness = Mathf.Clamp(
                        member.Happiness.currentHappiness + moraleChange,
                        0, 100
                    );
                    membersAffected++;
                }
                if (member.Happiness != null)
                    Debug.Log($"Applied morale change to {member.memberName}, new happiness: {member.Happiness.currentHappiness}");
            }
        }
        
        Debug.Log($"Team morale changed by {moraleChange} for {membersAffected} team members");
    }
    
    /// <summary>
    /// Schedule a follow-up card to appear
    /// </summary>
    private void ScheduleFollowUpCard(DecisionCard followUp, int daysUntil)
    {
        // This would integrate with your calendar/event system
        Debug.Log($"Follow-up card '{followUp.cardTitle}' scheduled in {daysUntil} days");
        
        if(!scheduledFollowUps.ContainsKey(followUp))
        {
            scheduledFollowUps.Add(followUp, daysUntil);
        }
    }
    
    private void ProcessScheduledFollowUps()
    {
        List<DecisionCard> cardsToPresent = new List<DecisionCard>();
        
        List<DecisionCard> keys = new List<DecisionCard>(scheduledFollowUps.Keys);
        foreach (var card in keys)
        {
            scheduledFollowUps[card]--;
            if (scheduledFollowUps[card] <= 0)
            {
                cardsToPresent.Add(card);
                scheduledFollowUps.Remove(card);
            }
        }
        
        // Add follow-up cards to today's cards so they appear
        foreach (var card in cardsToPresent)
        {
            todaysCards.Add(card);
            Debug.Log($"Follow-up card '{card.cardTitle}' added to today's cards");
        }
    }
    
    /// <summary>
    /// Get the current target member for UI display
    /// </summary>
    public TeamMember GetCurrentTargetMember()
    {
        return currentTargetMember;
    }
    
    /// <summary>
    /// Get card history for saving
    /// </summary>
    public List<CardHistoryEntry> GetCardHistory()
    {
        List<CardHistoryEntry> history = new List<CardHistoryEntry>();
        
        foreach (var kvp in cardLastShownDay)
        {
            if (kvp.Key != null)
            {
                history.Add(new CardHistoryEntry(kvp.Key.cardTitle, kvp.Value));
            }
        }
        
        return history;
    }
    
    /// <summary>
    /// Restore card history from save data
    /// </summary>
    public void RestoreCardHistory(List<CardHistoryEntry> history)
    {
        cardLastShownDay.Clear();
        
        if (history == null || history.Count == 0)
        {
            Debug.Log("No card history to restore");
            return;
        }
        
        foreach (var entry in history)
        {
            // Find the matching card by title
            DecisionCard card = allDecisionCards.Find(c => c != null && c.cardTitle == entry.cardTitle);
            
            if (card != null)
            {
                cardLastShownDay[card] = entry.lastShownDay;
                Debug.Log($"Restored card '{entry.cardTitle}' last shown on day {entry.lastShownDay}");
            }
            else
            {
                Debug.LogWarning($"Could not find card with title '{entry.cardTitle}' to restore history");
            }
        }
        
        Debug.Log($"Restored {cardLastShownDay.Count} card history entries");
    }
    
    /// <summary>
    /// Reset the DecisionCardManager for a new game
    /// </summary>
    public void ResetForNewGame()
    {
        // Clear all card tracking
        cardLastShownDay.Clear();
        todaysCards.Clear();
        currentCard = null;
        currentTargetMember = null;
        
        Debug.Log("DecisionCardManager reset - all card history cleared for new game");
    }
    
    private void RestartTimeAfterCards()
    {
        if (TimeManager.Instance != null )
        {
            TimeManager.Instance.SetTimePauseState(false);
        }
    }
    private void OnDisable()
    {
        OnCardPresented.RemoveAllListeners();
        OnDecisionMade.RemoveAllListeners();
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.onNewDay.RemoveListener(OnNewDay);
        }
    }
}

