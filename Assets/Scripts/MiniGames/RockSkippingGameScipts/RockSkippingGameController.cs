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
        confirmSelectionButton.gameObject.SetActive(false);
    }
    
    private void StartAimingStage()
    {
        if(rockInfoUI != null)
        {
            rockInfoUI.HideInfo();
        }
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




