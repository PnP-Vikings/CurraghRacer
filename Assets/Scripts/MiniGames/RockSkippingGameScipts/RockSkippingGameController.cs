using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using JetBrains.Annotations;
using MiniGames;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class RockSkippingGameController : MonoBehaviour,MiniGame
{
    public static RockSkippingGameController Instance { get; private set; }
    Stages stage = Stages.RockPicking;
    public int roundsToPlay = 3;
    public int currentRound = 0;
    public List<Rock> rocksTypes;
    public List<Rock> availableRocksForThisSession;
    public RockVisual currentSelectedRock;
    public Rock currentRock;
    public Transform rockSpawnPoint;
    public Dictionary<int, (Rock rock, int score)> rockScores = new Dictionary<int, (Rock, int)>();
    public RockCase rockCase;
    
    
    [Header("Optional UI")]
    [SerializeField] private RockInfoUI rockInfoUI;
    [SerializeField] private Button confirmSelectionButton;
    
    [Header("Input System Selection")]
    [SerializeField] private RockSelectionManager rockSelectionManager;
    
    [Header("Throwing System")]
    [SerializeField] private RockThrowingController throwingController;
    [SerializeField] private RockThrowingUI throwingUI;
    [SerializeField] private AIRockThrower aiThrower;
    
    [Header("Skip Button")]
    [SerializeField] private Button skipAIButton;
    
    [Header("Results UI")]
    [SerializeField] private RockSkippingResultsUI resultsUI;
    
    [SerializeField] private List<RockVisual> spawnedRockVisuals = new List<RockVisual>();
    private RockVisual currentHoveredRock;
    
    [Header("Camera Settings")]
    public CameraSmoothlyFollowGameObject cameraFollower; // Reference to the camera for shake effect
    public float cameraShakeIntensity = 0.15f; // How strong the shake is
    public float cameraShakeDuration = 0.2f; // How long the shake lasts
    private Vector3 cameraOriginalPosition; // Store camera's starting position
    [SerializeField] private bool cameraShake = false;
    
    // Scoring - tracks distances per player per round
    // Key: playerIndex (0 = player, 1-3 = AI), Value: list of distances per round
    private Dictionary<int, List<float>> playerRoundDistances = new Dictionary<int, List<float>>();
    private int currentPlayerTurn = 0; // 0 = player, 1-3 = AI opponents
    private const int TOTAL_PLAYERS = 4; // 1 player + 3 AI
    
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        availableRocksForThisSession = new List<Rock>();
        
        // Initialize scoring for all players
        for (int i = 0; i < TOTAL_PLAYERS; i++)
        {
            playerRoundDistances[i] = new List<float>();
        }
        
       int rockCounter = 0;
       while (rockCounter < 4)
       {
              // Pick a random rock type
              int randomIndex = Random.Range(0, rocksTypes.Count);
              Rock selectedRockPrefab = rocksTypes[randomIndex];
              
              // Instantiate the rock prefab 
              Rock rockInstance = Instantiate(selectedRockPrefab);
              rockInstance.gameObject.SetActive(false);
              
              // Initialize with base stats for the rock type
              rockInstance.Initialize(selectedRockPrefab.rockType);
              
              
              
              // IMPORTANT: Instantiate a new RockVisual for this rock to avoid shared references
              if (selectedRockPrefab.rockVisual != null)
              {
                  RockVisual newRockVisual = Instantiate(selectedRockPrefab.rockVisual);
                  newRockVisual.gameObject.SetActive(false);
                  newRockVisual.Initialize(rockInstance);
                  rockInstance.rockVisual = newRockVisual; // Link the new visual to this rock
              }
              
              availableRocksForThisSession.Add(rockInstance);
              rockCounter++;
       }
       
       // Collect the rock visuals to spawn in the case
       List<RockVisual> rocksToSpawn = new List<RockVisual>();
       foreach (var rock in availableRocksForThisSession)
       {
           if (rock.rockVisual != null)
           {
               rocksToSpawn.Add(rock.rockVisual);
           }
           else
           {
               Debug.LogError($"Rock {rock.rockType} has no RockVisual assigned!");
           }
       }
       
       // This will instantiate the RockVisuals and call SetupAfterInstantiation
       if (rockCase != null)
       {
           rockCase.SpawnRocksInCase(rocksToSpawn);
           
           rockCase.OnCaseClosed += StartAimingStage;
       }

       // Subscribe to rock events after spawning
       StartCoroutine(SubscribeToRockEvents());
       if (confirmSelectionButton != null)
       {
           confirmSelectionButton.onClick.AddListener(OnConfirmRockSelection);
           
           // Add hover sound component if it doesn't exist
           if (confirmSelectionButton.GetComponent<ButtonHoverSound>() == null)
           {
               confirmSelectionButton.gameObject.AddComponent<ButtonHoverSound>();
           }
          
           confirmSelectionButton.gameObject.SetActive(false);
       }
       
       // Setup skip button
       if (skipAIButton != null)
       {
           skipAIButton.onClick.AddListener(OnSkipAIButtonPressed);
           skipAIButton.gameObject.SetActive(false);
       }
       
       // Setup throwing controller reference
       if (throwingController != null && rockSpawnPoint != null)
       {
           throwingController.rockSpawnPoint = rockSpawnPoint;
           
           // Auto-find throwingUI if not assigned
           if (throwingUI == null)
           {
               throwingUI = FindFirstObjectByType<RockThrowingUI>();
           }
           
           throwingController.throwingUI = throwingUI;
           
           if (throwingUI != null)
           {
               Debug.Log("ThrowingUI connected to throwing controller");
           }
           else
           {
               Debug.LogWarning("ThrowingUI not found - UI feedback will not work!");
           }
       }
       
       // Setup AI thrower reference
       if (aiThrower != null && rockSpawnPoint != null)
       {
           // AIThrower will use the same spawn point as player
           aiThrower.SetSpawnPoint(rockSpawnPoint);
       }
    }
    
    private IEnumerator SubscribeToRockEvents()
    {
        // Wait one frame for rocks to be spawned
        yield return null;
        
        // Find all spawned rock visuals in the case
        spawnedRockVisuals = rockCase.GetSpawnedRocks();
        
        foreach (var rockVisual in spawnedRockVisuals)
        {
            rockVisual.OnRockHoverEnter += OnRockHoverEnter;
            rockVisual.OnRockHoverExit += OnRockHoverExit;
            rockVisual.OnRockClicked += OnRockSelected;
        }
    }

    public List<RockVisual> GetSpawnedRocksVisuals()
    {
        return spawnedRockVisuals;
    } 
    private void OnRockHoverEnter(RockVisual rockVisual)
    {
        if (stage != Stages.RockPicking) return;
        
        currentHoveredRock = rockVisual;
        Debug.Log($"Hovering over rock: {rockVisual.rockData.rockType}");
        
        // Show rock stats UI if available
        if (rockInfoUI != null)
        {
            rockInfoUI.ShowRockInfo(rockVisual.rockData);
        }
    }
    
    private void OnRockHoverExit(RockVisual rockVisual)
    {
        if (stage != Stages.RockPicking) return;
        
        currentHoveredRock = null;
        
        // Hide rock stats UI if available, but only if the rock is not currently selected
        if (rockInfoUI != null && currentSelectedRock == null)
        {
            rockInfoUI.HideInfo();
        }
    }
    
    private void OnRockSelected(RockVisual rockVisual)
    {
        if (stage != Stages.RockPicking) return;
        
        Debug.Log($"Rock clicked: {rockVisual.rockData.rockType}");
        
        // Check if clicking the same rock (to deselect)
        if (currentSelectedRock == rockVisual)
        {
            // Deselecting current rock
            rockVisual.Deselect();
            currentRock = null;
            currentSelectedRock = null;
            Debug.Log("Rock deselected");
        }
        else
        {
            // Deselect previous rock if any
            if(currentSelectedRock != null)
            {
                currentSelectedRock.Deselect();
            }
            
            // Select the new rock
            rockVisual.Select();
            currentRock = rockVisual.rockData;
            currentSelectedRock = rockVisual;
            Debug.Log($"Rock selected: {rockVisual.rockData.rockType}");
        }
        
        if(confirmSelectionButton != null)
            confirmSelectionButton.gameObject.SetActive(currentRock != null);
    }

    private void OnConfirmRockSelection()
    {
        if(currentRock == null) return;
        
        // Disable interaction on all rocks
        foreach (var visual in spawnedRockVisuals)
        {
            visual.SetInteractable(false);
        }

        rockCase.ResetCase();
        if(rockInfoUI != null)
        {
            rockInfoUI.RockSelected(currentRock);
        }
        if (AudioManager.instance != null)
        {
            AudioManager.instance.UIClick1.start();
        }

        confirmSelectionButton.gameObject.SetActive(false);
    }

    private void SetupScoring()
    {
        Debug.Log($"Starting aiming stage: {stage}");
        int rockPlayerIndex = 0;
        rockScores.Add(rockPlayerIndex, (currentRock, 0));
        rockPlayerIndex++;
        availableRocksForThisSession.Remove(currentRock);
        
        foreach (var visual in availableRocksForThisSession)
        {
            rockScores.Add(rockPlayerIndex, (visual, 0));
            rockPlayerIndex++;

        }
        
        if(rockScores.Count == 0)
        {
            Debug.LogError("No rocks found for scoring!");
            return;
        }
        else if (rockScores.Count>3)
        {
            availableRocksForThisSession = new List<Rock>();
        }
        
        foreach (var entry in rockScores)
        {
            Debug.Log($"Rock Player {entry.Key}: {entry.Value.rock.rockType} with score {entry.Value.score}");
        }

    }
    
    private void StartAimingStage()
    {
        if(rockInfoUI != null)
        {
            rockInfoUI.HideInfo();
        }

        SetupScoring();
        
        stage = Stages.Aiming;
        currentPlayerTurn = 0; // Player goes first
        currentRound = 1;
        
        // Prepare player throw
        StartPlayerThrow();
    }
    
    private void StartPlayerThrow()
    {
        stage = Stages.Throwing;
        
        if (throwingController != null && currentRock != null)
        {
            // Subscribe to rock landed event
            throwingController.OnRockLanded -= OnRockFinished; // Unsubscribe first to avoid duplicates
            throwingController.OnRockLanded += OnRockFinished;
            
            throwingController.PrepareThrow(currentRock);
            Debug.Log($"Round {currentRound}: Player's turn to throw!");
        }
        else
        {
            // Fallback to old behavior if no throwing controller
            SpawnSelectedRockForThrowing();
        }
    }
    
    private void SpawnSelectedRockForThrowing()
    {
        if (currentRock == null || rockSpawnPoint == null) return;
        
        // Instantiate the actual throwable rock at the spawn point
        Rock throwableRock = Instantiate(currentRock, rockSpawnPoint.position, rockSpawnPoint.rotation);
        throwableRock.gameObject.SetActive(true);
        
        Debug.Log("Rock ready to throw!");
    }
    
    /// <summary>
    /// Called when a rock finishes (lands/sinks) - either player or AI
    /// </summary>
    public void OnRockFinished(float distance)
    {
        Debug.Log($"Rock finished! Player {currentPlayerTurn} distance: {distance:F1}m");
        
        // Record the distance
        playerRoundDistances[currentPlayerTurn].Add(distance);
        
        // Update results UI if available
        if (resultsUI != null)
        {
            
            resultsUI.UpdateScore(currentPlayerTurn, currentRound, distance);
        }
        
        // Move to next turn
        StartCoroutine(ProcessNextTurn());
    }
    
    private IEnumerator ProcessNextTurn()
    {
        yield return new WaitForSeconds(2f); // Brief pause between turns
        if(resultsUI != null)
        {
            resultsUI.HideInGameScorePanel();
        }
        currentPlayerTurn++;
        
        if (currentPlayerTurn >= TOTAL_PLAYERS)
        {
            // All players have thrown this round
            currentPlayerTurn = 0;
            currentRound++;
            
            if (currentRound > roundsToPlay)
            {
                // Game over - calculate winner
                EndGameAndShowResults();
                yield break;
            }
            
            // Start next round with player
            Debug.Log($"Starting Round {currentRound}");
            StartPlayerThrow();
        }
        else
        {
            // AI turn
            resultsUI.HideInGameScorePanel();
            StartAITurn(currentPlayerTurn);
        }
        if (resultsUI != null)
        {
            resultsUI.UpdateTurnIndicator(currentPlayerTurn, currentRound);
        }
    }
    
    private void StartAITurn(int aiIndex)
    {
        stage = Stages.AIThrowing;
        
        // Show skip button
        if (skipAIButton != null)
        {
            skipAIButton.gameObject.SetActive(true);
        }
        
        // Get AI's rock (from rockScores)
        Rock aiRock = null;
        if (rockScores.ContainsKey(aiIndex))
        {
            aiRock = rockScores[aiIndex].rock;
        }
        
        if (aiRock == null)
        {
            Debug.LogError($"No rock found for AI {aiIndex}!");
            OnRockFinished(0f);
            return;
        }
        
        if (aiThrower != null)
        {
            Debug.Log($"Round {currentRound}: AI {aiIndex}'s turn to throw!");
            aiThrower.ExecuteAIThrow(aiRock, aiIndex - 1, (distance) => {
                // Hide skip button after AI finishes
                if (skipAIButton != null)
                {
                    skipAIButton.gameObject.SetActive(false);
                }
                OnRockFinished(distance);
            });
        }
        else
        {
            // No AI thrower - simulate result
            float simulatedDistance = Random.Range(15f, 45f);
            OnRockFinished(simulatedDistance);
        }
    }
    
    private void OnSkipAIButtonPressed()
    {
        if (aiThrower != null)
        {
            aiThrower.SkipAllAIThrows();
        }
        
        if (skipAIButton != null)
        {
            skipAIButton.gameObject.SetActive(false);
        }
        
        Debug.Log("Skipping AI throws...");
    }
    
    private void EndGameAndShowResults()
    {
        stage = Stages.GameOver;
        
        // Calculate totals and determine winner
        Dictionary<int, float> totalDistances = new Dictionary<int, float>();
        
        for (int i = 0; i < TOTAL_PLAYERS; i++)
        {
            float total = 0f;
            foreach (float distance in playerRoundDistances[i])
            {
                total += distance;
            }
            totalDistances[i] = total;
            Debug.Log($"Player {i} total distance: {total:F1}m");
        }
        
        // Find winner
        int winner = 0;
        float maxDistance = 0f;
        foreach (var kvp in totalDistances)
        {
            if (kvp.Value > maxDistance)
            {
                maxDistance = kvp.Value;
                winner = kvp.Key;
            }
        }
        
        Debug.Log($"Winner: Player {winner} with {maxDistance:F1}m total!");
        
        // Show results UI
        if (resultsUI != null)
        {
            resultsUI.ShowFinalResults(playerRoundDistances, winner);
        }

        EndGame();
    }
    
    public void ResetRockSelection()
    {
        currentRock = null;
        
        // Re-enable all rocks for selection
        foreach (var visual in spawnedRockVisuals)
        {
            visual.ResetVisuals();
            visual.SetInteractable(true);
        }
        
        stage = Stages.RockPicking;
    }

   public void ShakeCamera()
{
    if (cameraFollower == null || cameraShake == false) return;
    
    // Kill any existing tweens
    cameraFollower.transform.DOKill();
    
    //Camera shake (rotation-based works better for following cameras)
    cameraFollower.transform.DOShakeRotation(
        cameraShakeDuration, 
        cameraShakeIntensity,  // Increased intensity
        vibrato: 15, 
        randomness: 90, 
        fadeOut: true
    );
    
    
    //Time scale hit-stop effect (brief pause for impact feel)
    DOTween.Sequence()
        .AppendCallback(() => Time.timeScale = 0.71f)
        .AppendInterval(0.1f)
        .AppendCallback(() => Time.timeScale = 1f)
        .SetUpdate(true);  // Use unscaled time
}
    
    public void Initialize(MiniGameManager manager, MiniGameData gameData)
    {
        
    }
    
    public void UpdateGame()
    {
        // Game update logic if needed
    }

    public void StartGame()
    {
        stage = Stages.RockPicking;
    }

    public void EndGame()
    {
        // Unsubscribe from events
        foreach (var rockVisual in spawnedRockVisuals)
        {
            rockVisual.OnRockHoverEnter -= OnRockHoverEnter;
            rockVisual.OnRockHoverExit -= OnRockHoverExit;
            rockVisual.OnRockClicked -= OnRockSelected;
        }

        if (rockInfoUI != null)
        {
            rockCase.OnCaseClosed -= StartAimingStage;
        }
        
        if(MiniGameManager.Instance != null)
        {
            int playerTotalScore = Mathf.RoundToInt(playerRoundDistances[0].Sum());
            MiniGameManager.Instance.CompleteGame(playerTotalScore,9);
        }
    }
    
    private void OnDestroy()
    {
        
        
        if (Instance == this)
        {
            Instance = null;
        }
        
        
    }
    

    public enum Stages
    {
        RockPicking,
        Aiming,
        Throwing,      // Player is throwing
        AIThrowing,    // AI is throwing
        Observing,
        GameOver
    }
}




