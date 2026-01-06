using System.Collections.Generic;
using JetBrains.Annotations;
using MiniGames;
using UnityEngine;
using UnityEngine.UI;

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
    
    private List<RockVisual> spawnedRockVisuals = new List<RockVisual>();
    private RockVisual currentHoveredRock;
    
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
           confirmSelectionButton.gameObject.SetActive(false);
       }
       
       
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
        
        Debug.Log($"Rock selected: {rockVisual.rockData.rockType}");

        if(currentSelectedRock!= null)
        {
            currentSelectedRock.ResetVisuals();
        }
        
        if (currentRock == rockVisual.rockData)
        { 
            currentRock = null;
        }
        else
        {
            
            // Set as current rock
            currentRock = rockVisual.rockData;
            currentSelectedRock = rockVisual;
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
    }
    
    private void StartAimingStage()
    {
        Debug.Log($"Starting aiming stage: {stage}");
        
        stage = Stages.Aiming;
        
        // Spawn the selected rock for throwing
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

        if (rockInfoUI != null)
        {
            rockCase.OnCaseClosed -= StartAimingStage;
        }
    }
    
    private void OnDestroy()
    {
        EndGame();
        
        if (Instance == this)
        {
            Instance = null;
        }
        
        
    }
    

    public enum Stages
    {
        RockPicking,
        Aiming,
        Observing,
        GameOver
    }
}




