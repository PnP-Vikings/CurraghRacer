using System.Collections.Generic;
using JetBrains.Annotations;
using MiniGames;
using UnityEngine;

public class RockSkippingGameController : MonoBehaviour,MiniGame
{
    Stages stage = Stages.RockPicking;
    public int roundsToPlay = 3;
    public int currentRound = 0;
    public List<Rock> rocksTypes;
    public List<Rock> availableRocksForThisSession;
    public Rock currentRock;
    public Transform rockSpawnPoint;
    public Dictionary<int, (Rock rock, int score)> rockScores = new Dictionary<int, (Rock, int)>();
    public RockCase rockCase;
    
    [Header("Optional UI")]
    [SerializeField] private RockInfoUI rockInfoUI;
    
    [Header("Input System Selection")]
    [SerializeField] private RockSelectionManager rockSelectionManager;
    
    private List<RockVisual> spawnedRockVisuals = new List<RockVisual>();
    private RockVisual currentHoveredRock;
    
    public void Awake()
    {
        availableRocksForThisSession = new List<Rock>();
        
       int rockCounter = 0;
       while (rockCounter < 4)
       {
              int randomIndex = Random.Range(0, rocksTypes.Count);
              Rock selectedRockPrefab = rocksTypes[randomIndex];
              selectedRockPrefab.Initialize(selectedRockPrefab.rockType);
              availableRocksForThisSession.Add(selectedRockPrefab);
              rockCounter++;
       }
       
       List<RockVisual> rocksToSpawn = new List<RockVisual>();
       foreach (var rock in availableRocksForThisSession)
         {
             rock.rockVisual.Initialize(rock);
             rocksToSpawn.Add(rock.rockVisual);
         }
       
       // This will instantiate the RockVisuals and call SetupAfterInstantiation
       rockCase.SpawnRocksInCase(rocksToSpawn);
       
       // Subscribe to rock events after spawning
       StartCoroutine(SubscribeToRockEvents());
    }
    
    private System.Collections.IEnumerator SubscribeToRockEvents()
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
        
        // Hide rock stats UI if available
        if (rockInfoUI != null)
        {
            rockInfoUI.HideInfo();
        }
    }
    
    private void OnRockSelected(RockVisual rockVisual)
    {
        if (stage != Stages.RockPicking) return;
        
        Debug.Log($"Rock selected: {rockVisual.rockData.rockType}");
        
        // Set as current rock
        currentRock = rockVisual.rockData;
        
        // Disable interaction on all rocks
        foreach (var visual in spawnedRockVisuals)
        {
            visual.SetInteractable(false);
        }
        
        // Move to next stage
        stage = Stages.Aiming;
        
        // Optional: Spawn the rock at the throw position
        SpawnSelectedRockForThrowing();
    }
    
    private void SpawnSelectedRockForThrowing()
    {
        if (currentRock == null || rockSpawnPoint == null) return;
        
        // Instantiate the actual throwable rock at the spawn point
        Rock throwableRock = Instantiate(currentRock, rockSpawnPoint.position, rockSpawnPoint.rotation);
        throwableRock.gameObject.SetActive(true);
        
        Debug.Log("Rock ready to throw!");
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
    
    public void Initialize(MiniGameManager manager, MiniGameData gameData)
    {
        
    }

    public void StartGame()
    {
        stage = Stages.RockPicking;
    }

    public void UpdateGame()
    {
        // Handle game state updates
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
    }
    
    private void OnDestroy()
    {
        EndGame();
    }

    public enum Stages
    {
        RockPicking,
        Aiming,
        Observing,
        GameOver
    }
}




