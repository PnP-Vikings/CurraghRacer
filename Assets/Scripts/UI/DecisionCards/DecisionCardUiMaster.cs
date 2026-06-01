using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class DecisionCardUiMaster : MonoBehaviour
{
   [Header("Background Color Management")]
   public Image decisionCardColorImage;
   public Color positiveColor, negativeColor, neutralColor;
   
   [Header("Card Prefab")]
   public DecisionCardUi cardPrefab;
   public Transform cardSpawnParent; // Where to spawn cards (e.g., a Panel in your Canvas)
   
   [Header("Swipe Settings")]
   public float swipeThreshold = 50f; // Minimum x position to consider as a swipe
   
   
   
   // State tracking
   public bool isDragging = false;
   public DecisionCardUi activeDecisionCard;
   private Queue<DecisionCard> cardDataQueue = new Queue<DecisionCard>();
   private DecisionCardUi currentCardInstance;
   
   private void Start()
   {
       // No need to clear cards since we instantiate dynamically
       ResetBackgroundColor();
   }
   
   private void OnEnable()
   {
       /*if (TimeManager.Instance != null)
       {
           TimeManager.Instance.onNewDay.AddListener(GenerateTodaysCards);
       }*/
   }
   
   /// <summary>
   /// Generate and display today's decision cards
   /// </summary>
   public void GenerateTodaysCards()
   {
       Debug.Log("DecisionCardUiMaster.GenerateTodaysCards() called");
       
       if (DecisionCardManager.Instance == null)
       {
           Debug.LogWarning("DecisionCardManager not found!");
           return;
       }
       
       if (cardPrefab == null)
       {
           Debug.LogError("Card Prefab is not assigned!");
           return;
       }
       
       if (cardSpawnParent == null)
       {
           Debug.LogError("Card Spawn Parent is not assigned!");
           return;
       }
       
       // Get cards from manager (without regenerating)
       List<DecisionCard> todaysCards = DecisionCardManager.Instance.GetTodaysCardList();
       
       Debug.Log($"Received {todaysCards.Count} cards from manager");
       
       if (todaysCards.Count == 0)
       {
           Debug.Log("No cards to show today");
           DecisionCardManager.Instance.OnAllCardsProcessed?.Invoke();
           this.gameObject.SetActive(false);
           return;
       }
       
       // Clear and populate card data queue
       cardDataQueue.Clear();
       
       Debug.Log($"Queueing {todaysCards.Count} cards");
       
       foreach (DecisionCard card in todaysCards)
       {
           cardDataQueue.Enqueue(card);
           Debug.Log($"Queued card: {card.cardTitle}");
       }
       
       Debug.Log($"Card data queue has {cardDataQueue.Count} cards. Spawning first card...");
       
       // Show first card
       ShowNextCard();
   }
   
   /// <summary>
   /// Show the next card in the queue
   /// </summary>
   public void ShowNextCard()
   {
       ResetBackgroundColor();
       Debug.Log($"ShowNextCard called. Queue count: {cardDataQueue.Count}");
       
       if (cardDataQueue.Count > 0)
       {
           // Get the next card data
           DecisionCard cardData = cardDataQueue.Dequeue();
           
           Debug.Log($"Dequeued card data: {cardData.cardTitle}. Instantiating new card...");
           
           // Instantiate a new card from the prefab
           currentCardInstance = Instantiate(cardPrefab, cardSpawnParent);
           
           Debug.Log($"Card instantiated: {currentCardInstance.gameObject.name}");
           
           // Get target member for this specific card
           TeamMember targetMember = null;
           if (cardData.targetsSpecificMember)
           {
               if (DecisionCardManager.Instance != null)
               {
                   DecisionCardManager.Instance.PresentCard(cardData);
                   targetMember = DecisionCardManager.Instance.GetCurrentTargetMember();
                   Debug.Log($"Card targets: {(targetMember != null ? targetMember.memberName : "No target found")}");
               }
           }
           
           // Setup the card with data
           currentCardInstance.SetupCard(cardData, targetMember);
           
           // Present card to manager for tracking
           if (DecisionCardManager.Instance != null)
           {
               DecisionCardManager.Instance.PresentCard(cardData);
               Debug.Log($"Presented card to manager: {cardData.cardTitle}");
           }
           
           // The card should be active by default when instantiated
           currentCardInstance.gameObject.SetActive(true);
           
           // Debug info
           RectTransform cardRect = currentCardInstance.GetComponent<RectTransform>();
           Debug.Log($"Card is now active! GameObject: {currentCardInstance.gameObject.name}, Active: {currentCardInstance.gameObject.activeSelf}");
           Debug.Log($"Card Position: {cardRect.anchoredPosition}, Scale: {cardRect.localScale}");

           if (AudioManager.instance != null)
           {
               AudioManager.instance.card.start();
           }
        }
       else
       {
           // No more cards
           Debug.Log("All cards processed for today");
           DecisionCardManager.Instance.OnAllCardsProcessed?.Invoke();
           ResetBackgroundColor();
           this.gameObject.SetActive(false); // Hide themaster UI 
       }
   }
   
   /// <summary>
   /// Called when a card decision is made
   /// </summary>
   public void OnCardDecision(DecisionCardUi card, DecisionOption option, DecisionCard decisionCard, TeamMember targetMember)
   {
       // Communicate decision to manager
       if (DecisionCardManager.Instance != null)
       {
           DecisionCardManager.Instance.MakeDecision(option);
       }
       
       // Animate card away and show next
       StartCoroutine(ProcessCardDecision(card));
   }
   
   /// <summary>
   /// Animate card away and show next card
   /// </summary>
   private IEnumerator ProcessCardDecision(DecisionCardUi card)
   {
       // Animate card off screen
       RectTransform cardRect = card.GetComponent<RectTransform>();
       Vector2 currentPos = cardRect.anchoredPosition;
       Vector2 targetPos = currentPos;
       targetPos.x += Mathf.Sign(currentPos.x) * 1500f;
       
       float elapsed = 0f;
       float duration = 0.3f;
       
       while (elapsed < duration)
       {
           elapsed += Time.deltaTime;
           cardRect.anchoredPosition = Vector2.Lerp(currentPos, targetPos, elapsed / duration);
           yield return null;
       }
       
       // Destroy the card instance
       Destroy(card.gameObject);
       currentCardInstance = null;
       
       // Reset background
       ResetBackgroundColor();
       
       // Show next card after a brief delay
       yield return new WaitForSeconds(0.2f);
       ShowNextCard();
   }
   
   // Background color management
   public void DragStarted(DecisionCardUi card)
   {
       isDragging = true;
       activeDecisionCard = card;
       StartCoroutine(UpdateBackgroundDecisionColor());
   }
   
   public void DragEnded()
   {
       isDragging = false;
       activeDecisionCard = null;
       StopAllCoroutines();
       ResetBackgroundColor();
   }

   public void ResetBackgroundColor()
   {
       if (decisionCardColorImage != null)
       {
           decisionCardColorImage.color = neutralColor;
       }
   }
   
   IEnumerator UpdateBackgroundDecisionColor()
   {
       while (isDragging)
       {
           if (activeDecisionCard != null)
           {
               float xPos = activeDecisionCard.GetComponent<RectTransform>().anchoredPosition.x;
               
               if (xPos > 0 + swipeThreshold)
               {
                   decisionCardColorImage.color = Color.Lerp(decisionCardColorImage.color, positiveColor, Time.deltaTime * 5f);
                   activeDecisionCard.ShowAcceptText();
               }
               else if (xPos < 0 - swipeThreshold)
               {
                   decisionCardColorImage.color = Color.Lerp(decisionCardColorImage.color, negativeColor, Time.deltaTime * 5f);
                   activeDecisionCard.ShowRejectText();
               }
               else
               {
                   decisionCardColorImage.color = Color.Lerp(decisionCardColorImage.color, neutralColor, Time.deltaTime * 5f);
                   activeDecisionCard.HideDecisionText();
               }
           }
           
           yield return null;
       }
   }
   
   
   public void PauseTimeWhileShowingCards()
   {
       if(TimeManager.Instance != null)
       {
           TimeManager.Instance.SetTimePauseState(true);
       }
   }
   
    public void ResumeTimeAfterCards()
    {
         if(TimeManager.Instance != null)
         {
              TimeManager.Instance.SetTimePauseState(false);
         }
    }
   
}
