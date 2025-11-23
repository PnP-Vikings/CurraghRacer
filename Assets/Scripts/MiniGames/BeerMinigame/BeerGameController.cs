using System.Collections;
using System.Collections.Generic;
using MiniGames;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BeerGameController : MonoBehaviour
{
    [Header("Core References")]
    public GameObject beerPrefab;
    public TapConfiguration[] taps = new TapConfiguration[4];
    public Transform finishPoint;
    public BeerMinigameCanvasUI minigameCanvasUI;
    public static BeerGameController Instance { get; private set; }
    
    [Header("Game State")]
    public bool gameCompleted;
    public List<BeerShaderPour> Completedbeers = new List<BeerShaderPour>();
    
    // Round-based system
    [System.Serializable]
    public class OrderData
    {
        public BeerType beerType;
        public float targetZoneMin;
        public float targetZoneMax;
        public int orderNumber;
        public float customerPatienceTime;
        public string customerName;
    }
    
    private List<OrderData> currentRound = new List<OrderData>();
    private int currentRoundIndex;
    private int totalRounds;
    private Dictionary<int, Coroutine> tapTimers = new Dictionary<int, Coroutine>();
    private Dictionary<int, float> tapTimeRemaining = new Dictionary<int, float>();
    private int completedInRound;
    private int beersInCurrentRound;
    private bool roundInProgress;
    private List<PourQuality> orderResults = new List<PourQuality>();
    private int currentOrderIndex;
    
    [Header("Performance Tracking")]
    [SerializeField] private int totalPerfectPours;
    [SerializeField] private int totalGoodPours;
    [SerializeField] private int totalAcceptablePours;
    [SerializeField] private int totalPoorPours;
    [SerializeField] private int totalBasePoints;
    [SerializeField] private int totalBonusPoints;
    [SerializeField] private int totalCombinedPoints;
    
    
    private int perfectStreak;
    private float performanceMultiplier = 1.0f;
    
    private string[] irishNames = new string[]
    {
        "O'Brien", "Murphy", "Kelly", "Walsh", "Ryan", "O'Sullivan", "McCarthy", "O'Connor",
        "Brennan", "Doyle", "Gallagher", "Doherty", "Kennedy", "Lynch", "Murray", "Quinn",
        "Moore", "McLoughlin", "Carroll", "Connolly", "Daly", "O'Neill", "Fitzpatrick",
        "Griffin", "Hayes", "Martin", "Collins", "Byrne", "Casey"
    };

    public void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        // Subscribe to all pour points' beer completed events
        foreach (var tap in taps)
        {
            if (tap != null && tap.associatedPourPoint != null && tap.associatedPourPoint.beerEnterBoxCollider != null)
            {
                tap.associatedPourPoint.beerEnterBoxCollider.onBeerCompleted.AddListener(BeerDone);
            }
        }
        
        StartOrderRoundSystem();
    }

    private void StartOrderRoundSystem()
    {
        totalRounds = UnityEngine.Random.Range(3, 10); // 3-10 rounds
        currentRoundIndex = 0;
        currentOrderIndex = 0;
        orderResults.Clear();
        tapTimers.Clear();
        tapTimeRemaining.Clear();
        perfectStreak = 0;
        performanceMultiplier = 1.0f;
        Completedbeers.Clear();
        
        minigameCanvasUI.SetUpUI(true, false, true, true);
        minigameCanvasUI.UpdateScore(0);
        minigameCanvasUI.UpdatePerfectStreak(0, 1.0f);
        
        StartNextRound();
    }

    private void Update()
    {
        // Check for game completion once
        if (currentRoundIndex >= totalRounds && !gameCompleted)
        {
            GameCompleted();
        }
    }
    
    void OnDisable()
    {
        // Unsubscribe from all pour points' beer completed events
        foreach (var tap in taps)
        {
            if (tap != null && tap.associatedPourPoint != null && tap.associatedPourPoint.beerEnterBoxCollider != null)
            {
                tap.associatedPourPoint.beerEnterBoxCollider.onBeerCompleted.RemoveListener(BeerDone);
            }
        }
        
        // Stop all timers
        StopAllCoroutines();
    }

    private int CalculateRoundSize(int roundIndex)
    {
        if (roundIndex <= 1)
        {
            return Mathf.Max(1, UnityEngine.Random.Range(1, 3)); // 1-2 beers
        }
        else if (roundIndex <= 3)
        {
            return Mathf.Max(1, UnityEngine.Random.Range(2, 4)); // 1-3 beers
        }
        else
        {
            return Mathf.Max(1, UnityEngine.Random.Range(3, 5)); // 1-4 beers
        }
    }

    private List<OrderData> GenerateNextRoundOrders(int count)
    {
        List<OrderData> orders = new List<OrderData>();
        
        for (int i = 0; i < count; i++)
        {
            OrderData order = new OrderData();
            order.orderNumber = currentOrderIndex + i + 1;
            order.customerName = irishNames[UnityEngine.Random.Range(0, irishNames.Length)];
            
            // Determine beer type by order pattern
            int orderPattern = (currentOrderIndex + i) % 9;
            if (orderPattern < 4)
            {
                // Orders 1-4: Pilsner/Lager
                order.beerType = UnityEngine.Random.value > 0.5f ? BeerType.Pilsner : BeerType.Lager;
                order.targetZoneMin = 0.875f - 0.08f;
                order.targetZoneMax = 0.875f + 0.08f;
                order.customerPatienceTime = UnityEngine.Random.Range(5f, 12f);
            }
            else if (orderPattern < 8)
            {
                // Orders 5-8: IPA/Ale
                order.beerType = UnityEngine.Random.value > 0.5f ? BeerType.IPA : BeerType.Ale;
                order.targetZoneMin = 0.88f - 0.05f;
                order.targetZoneMax = 0.88f + 0.05f;
                order.customerPatienceTime = UnityEngine.Random.Range(8f, 14f);
            }
            else
            {
                // Orders 9+: Stout
                order.beerType = BeerType.Stout;
                order.targetZoneMin = 0.89f - 0.03f;
                order.targetZoneMax = 0.89f + 0.03f;
                order.customerPatienceTime = UnityEngine.Random.Range(10f, 15f);
            }
            
            // Reduce tolerance every 10 orders
            if (currentOrderIndex + i >= 10)
            {
                float reduction = 0.01f * ((float)(currentOrderIndex + i) / 10f);
                float center = (order.targetZoneMin + order.targetZoneMax) / 2f;
                float tolerance = (order.targetZoneMax - order.targetZoneMin) / 2f - reduction;
                order.targetZoneMin = center - tolerance;
                order.targetZoneMax = center + tolerance;
            }
            
            orders.Add(order);
        }
        
        return orders;
    }

    private Color GetFoamColor(BeerType type)
    {
        switch (type)
        {
            case BeerType.Lager:
                return Color.white;
            case BeerType.Stout:
                return new Color32(210, 180, 140, 255);
            case BeerType.Ale:
                return new Color32(255, 253, 208, 255);
            case BeerType.IPA:
                return new Color32(250, 240, 230, 255);
            case BeerType.Pilsner:
                return new Color32(255, 255, 240, 255);
            default:
                return Color.white;
        }
    }
    
    public void BeerDone()
    {
        // Check all active taps for completed (locked) beers
        for (int tapIndex = 0; tapIndex < beersInCurrentRound; tapIndex++)
        {
            if (taps[tapIndex] == null || taps[tapIndex].associatedPourPoint == null)
                continue;
            
            var currentBeer = taps[tapIndex].associatedPourPoint.beerEnterBoxCollider.currentBeerShaderPour;
            
            if (currentBeer != null && currentBeer.isLocked)
            {
                // Stop the timer for this tap
                if (tapTimers.ContainsKey(tapIndex))
                {
                    StopCoroutine(tapTimers[tapIndex]);
                    tapTimers.Remove(tapIndex);
                }
                
                // Get the quality and calculate points
                PourQuality quality = currentBeer.pourQuality;
                int basePoints = 0;
                
                    
                    
                 switch (quality)
                {
                    case PourQuality.Perfect:
                        basePoints = 150;
                        totalPerfectPours++;
                        break;
                    case PourQuality.Good :
                        basePoints = 100;
                        totalGoodPours++;
                        break;
                    case PourQuality.Acceptable :
                        basePoints = 50;
                        totalAcceptablePours++;
                        break;
                    case PourQuality.Poor:
                        basePoints = 20;
                        totalPoorPours++;
                        break;
                    default:
                        basePoints = 0;
                        break;
                };
                
                // Apply multiplier only for Perfect/Good/Acceptable
                int finalPoints = basePoints;
                /*
                if (quality == PourQuality.Perfect || quality == PourQuality.Good || quality == PourQuality.Acceptable)
                {
                    finalPoints = Mathf.RoundToInt(basePoints * performanceMultiplier);
                }
                */
                
                // Update streak
                if (quality == PourQuality.Perfect)
                {
                    perfectStreak++;
                }
                else
                {
                    perfectStreak = 0;
                }
                
                // Recalculate multiplier
                performanceMultiplier = 1.0f + (perfectStreak * 0.1f);
                
                // Add to results
                orderResults.Add(quality);
                
                // Update UI
               // minigameCanvasUI.UpdateScore(Completedbeers.Count + 1);
                
                minigameCanvasUI.UpdatePerfectStreak(perfectStreak, performanceMultiplier);
                minigameCanvasUI.ShowTapPourResult(tapIndex, quality, finalPoints, performanceMultiplier);
                
                // Move to finish
                currentBeer.transform.parent.position = finishPoint.position + new Vector3(Completedbeers.Count * 3f, 0, 0);
                currentBeer.isActive = false;
                
                // Add to completed list
                Completedbeers.Add(currentBeer);
                completedInRound++;
                currentOrderIndex++;
                
                // Clear pour point
                taps[tapIndex].associatedPourPoint.Reset();
                
                // Hide timer UI for this tap
                minigameCanvasUI.UpdateTapTimer(tapIndex, 0, false);
                
                Debug.Log($"Beer done at tap {tapIndex}. Quality: {quality}, Points: {finalPoints}");
                
                totalBasePoints += finalPoints;
                
                // Update overall score
                UpdateScore();
                // Check if round is complete
                CheckRoundComplete();
                
                break; // Process one beer at a time
            }
        }
    }


    public void GameCompleted()
    {
        if (gameCompleted)
            return;

        gameCompleted = true;
        Debug.Log("All rounds completed!");

        // Calculate final score from all order results
        int finalScore = 0;
        foreach (var quality in orderResults)
        {
            int basePoints = quality switch
            {
                PourQuality.Perfect => 150,
                PourQuality.Good => 100,
                PourQuality.Acceptable => 50,
                PourQuality.Poor => 20,
                _ => 0
            };
            finalScore += basePoints;
        }
        
        
        UpdateScore();
        // Show final summary
        minigameCanvasUI.ShowFinalSummary(orderResults, totalCombinedPoints, perfectStreak);

        // Let MiniGameManager handle completion
        if (MiniGameManager.Instance != null)
        {
            Debug.Log($"Calling MiniGameManager.CompleteGame with score: {finalScore}");
            MiniGameManager.Instance.CompleteGame(finalScore);
        }
        else
        {
            Debug.LogError("MiniGameManager.Instance is null!");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerWorked();
            }
            SceneManager.LoadScene(GameManager.Instance.mainSceneName);
        }
    }

    private void StartNextRound()
    {
        if (currentRoundIndex >= totalRounds)
        {
            GameCompleted();
            return;
        }

        beersInCurrentRound = CalculateRoundSize(currentRoundIndex);
        currentRound = GenerateNextRoundOrders(beersInCurrentRound);
        completedInRound = 0;
        roundInProgress = true;

        Debug.Log($"Starting Round {currentRoundIndex + 1}/{totalRounds} with {beersInCurrentRound} beers");

        // Spawn beers for this round
        for (int i = 0; i < beersInCurrentRound; i++)
        {
            var order = currentRound[i];
            var tap = taps[i];

            // Instantiate beer at tap position
            GameObject beerObj = Instantiate(beerPrefab, tap.associatedPourPoint.beerEnterBoxCollider.transform.position, Quaternion.identity);
            BeerShaderPour beer = beerObj.GetComponentInChildren<BeerShaderPour>();

            if (beer != null)
            {
                // Assign beer type
                beer.AssignBeerType(order.beerType);

                // Set order target with tap stream origin
                beer.SetOrderTarget(order.targetZoneMin, order.targetZoneMax, GetFoamColor(order.beerType), tap.GetPourStreamOrigin());

                // Assign to pour point
                tap.associatedPourPoint.beerEnterBoxCollider.currentBeerShaderPour = beer;

                // Update UI
                minigameCanvasUI.UpdateTapOrder(i, order.beerType.ToString(), order.customerName);

                // Start timer
                tapTimers[i] = StartCoroutine(StartTapTimer(i, order.customerPatienceTime));
            }
        }

        // Hide UI for inactive taps
        for (int i = beersInCurrentRound; i < 4; i++)
        {
            minigameCanvasUI.HideTapUI(i);
        }
    }

    private IEnumerator StartTapTimer(int tapIndex, float duration)
    {
        tapTimeRemaining[tapIndex] = duration;

        while (tapTimeRemaining[tapIndex] > 0 && roundInProgress)
        {
            yield return new WaitForSeconds(0.1f);
            tapTimeRemaining[tapIndex] -= 0.1f;
            minigameCanvasUI.UpdateTapTimer(tapIndex, tapTimeRemaining[tapIndex], true);
        }

        // Timer expired - auto-submit
        if (roundInProgress && tapTimeRemaining[tapIndex] <= 0)
        {
            AutoSubmitOrder(tapIndex);
        }
    }

    private void AutoSubmitOrder(int tapIndex)
    {
        var beer = taps[tapIndex].associatedPourPoint.beerEnterBoxCollider.currentBeerShaderPour;
        if (beer != null && !beer.isLocked)
        {
            beer.LockPourAndCalculateQuality();
            beer.pourQuality = PourQuality.Poor;

            // Reset streak on timeout
            perfectStreak = 0;
            performanceMultiplier = 1.0f;

            // Add to results with 0 points
            orderResults.Add(PourQuality.Poor);

            // Update UI
            minigameCanvasUI.UpdatePerfectStreak(0, 1.0f);
            minigameCanvasUI.ShowTapPourResult(tapIndex, PourQuality.Poor, 0, 1.0f);

            // Move to finish
            beer.transform.parent.position = finishPoint.position + new Vector3(Completedbeers.Count * 3f, 0, 0);
            Completedbeers.Add(beer);
            completedInRound++;
            currentOrderIndex++;

            // Clear pour point
            taps[tapIndex].associatedPourPoint.Reset();

            Debug.Log($"Tap {tapIndex} timed out. Beer auto-submitted.");

            CheckRoundComplete();
        }
    }

    private void CheckRoundComplete()
    {
        if (completedInRound >= beersInCurrentRound)
        {
            roundInProgress = false;

            // Stop all remaining timers
            foreach (var timer in tapTimers.Values)
            {
                if (timer != null)
                    StopCoroutine(timer);
            }
            tapTimers.Clear();

            StartCoroutine(ShowRoundFeedback());
        }
    }
    
    public void UpdateScore()
    {
        totalCombinedPoints = totalBasePoints + totalBonusPoints;
        minigameCanvasUI.UpdateScore(totalCombinedPoints);
    }

    private IEnumerator ShowRoundFeedback()
    {
        // Calculate round performance
        int basePoints = 0;
        int bonusPoints = 0;

        // Get last N results (this round's results)
        int startIndex = Mathf.Max(0, orderResults.Count - beersInCurrentRound);
        for (int i = startIndex; i < orderResults.Count; i++)
        {
            int baseP = orderResults[i] switch
            {
                PourQuality.Perfect => 150,
                PourQuality.Good => 100,
                PourQuality.Acceptable => 50,
                PourQuality.Poor => 20,
                _ => 0
            };
            basePoints += baseP;
            
        }
        
        for (int i = startIndex; i < orderResults.Count; i++)
        {
            if (orderResults[i] == PourQuality.Perfect)
            {
                bonusPoints += Mathf.RoundToInt(15 * performanceMultiplier); 
            }
            else if (orderResults[i] == PourQuality.Good)
            {
                bonusPoints += Mathf.RoundToInt(10 * performanceMultiplier); 
            }
            else if (orderResults[i] == PourQuality.Acceptable)
            {
                bonusPoints += Mathf.RoundToInt(5 * performanceMultiplier); 
            }
        }

        int totalPoints = basePoints +  bonusPoints;
        
        totalBonusPoints += bonusPoints;

        UpdateScore();
        // Show summary
        yield return minigameCanvasUI.ShowRoundSummary(currentRoundIndex + 1, totalRounds, basePoints, bonusPoints, totalPoints, perfectStreak);

        // Clear pour points
        for (int i = 0; i < beersInCurrentRound; i++)
        {
            taps[i].associatedPourPoint.Reset();
        }

        // Move to next round
        currentRoundIndex++;
        StartNextRound();
    }
}

