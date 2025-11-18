using System;
using System.Collections;
using System.Collections.Generic;
using MiniGames;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BeerGameController : MonoBehaviour
{
    public GameObject beerPrefab; // Prefab for the beer object
    public List<BeerPourLocation> pourPoint; // Point where beer is poured
    public Transform SpawnPoint; // Point where spam is spawned
    public Transform FinishPoint; // Point where the beer is finished
    public float miniGameDuration = 60f; // Duration of the mini-game in seconds
    public bool timerHasElapsed = false; // Flag to indicate if the timer has elapsed
    public MinigameCanvasUI minigameCanvasUI; // Reference to the mini-game UI canvas
    public static BeerGameController Instance { get; private set; }
    public List<BeerShaderPour> beers; // List of beer shader pours
    public List<BeerShaderPour> Completedbeers; // List of beer shader pours
    public bool gameCompleted = false; // Flag to indicate if the game is completed
    public int spawnBeerCount = 5; // Number of beers to spawn at start
    public float currentSpeed = 2f; // Current speed of spawning beers
    public float minIntervalSpeed = 5f; // Minimum interval speed
    public float speedIncrement = 0.5f; // Speed increment value
    public float beerMoveDuration = 0.5f; // Duration for beer to lerp to pour point

    public void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this; // Set the singleton instance
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
        
        // Subscribe to all pour points' beer completed events
        foreach (var pourP in pourPoint)
        {
            if (pourP != null && pourP.beerEnterBoxCollider != null)
            {
                pourP.beerEnterBoxCollider.onBeerCompleted.AddListener(BeerDone);
            }
        }
        
        StartGameSpawn();
    }

    public void StartGameSpawn()
    {
        for (int i = 0; i < spawnBeerCount; i++) // Spawn 5 beers at the start
        {
            SpawnBeer();
        }
        
        MoveBeerToNextAvailablePourPoint();
       
        StartCoroutine(DecreaseSpeedOverTime(6f)); // Increase speed every 10 seconds
        StartCoroutine(MoveNewBeer());
        minigameCanvasUI.SetUpUI(true,false,true,true);
        StartCoroutine(CountDownTimer(miniGameDuration)); // Start the countdown timer
    }
    
    
    IEnumerator CountDownTimer(float duration)
    {
        float timer = duration;
        while (timer > 0 && !gameCompleted)
        {
            yield return new WaitForSeconds(1f);
            timer -= 1f;
            minigameCanvasUI.UpdatePlayerLives("Time Remaining: " + timer + " seconds");
            Debug.Log("Time remaining: " + timer + " seconds");
        }
        
        if (!gameCompleted)
        {
            Debug.Log("Time's up! Mini-game over.");
            timerHasElapsed = true;
            GameCompleted();
        }
    }
    IEnumerator MoveNewBeer()
    {
        while(!gameCompleted)
        {
            yield return new WaitForSeconds(currentSpeed);
      
            MoveBeerToNextAvailablePourPoint();
        }
    }
    
    IEnumerator DecreaseSpeedOverTime(float interval)
    {
        while(!gameCompleted)
        {
            yield return new WaitForSeconds(interval);
      
            // Increase speed up to max speed cap
            if (currentSpeed < minIntervalSpeed)
            {
                currentSpeed -= speedIncrement;
                //currentSpeed = Mathf.Min(currentSpeed, maxSpeed); // Cap at max speed
                Debug.Log("Speed decreased to: " + currentSpeed);
            }
        }
    }
    
    
    public void MoveBeerToNextAvailablePourPoint()
    {
        MoveNextBeer();
    }

    private void Update()
    {
        GameCompleted();
    }
    
    void OnDisable()
    {
        // Unsubscribe from all pour points' beer completed events
        foreach (var pourP in pourPoint)
        {
            if (pourP != null && pourP.beerEnterBoxCollider != null)
            {
                pourP.beerEnterBoxCollider.onBeerCompleted.RemoveListener(BeerDone);
            }
        }
    }

    public void SpawnBeer()
    {
        if (beerPrefab != null && SpawnPoint != null)
        {
            GameObject beer = Instantiate(beerPrefab, SpawnPoint.position + new Vector3(beers.Count *-2f,0,0), Quaternion.identity);
            beers.Add(beer.GetComponentInChildren<BeerShaderPour>()); // Add the beer to the list
            Debug.Log("Beer spawned at: " + SpawnPoint.position);
        }
        else
        {
            Debug.LogError("Beer prefab or pour point is not set!");
        }
    }
    
    public void MoveNextBeer()
    {
        Debug.Log($"MoveNextBeer called. Total beers: {beers.Count}, Completed: {Completedbeers.Count}");
        
        // Count unplaced beers
        int unplacedBeers = 0;
        foreach (var beer in beers)
        {
            if (beer != null && !beer.beerComplete && !beer.isPlaced)
            {
                unplacedBeers++;
            }
        }
        Debug.Log($"Unplaced beers available: {unplacedBeers}");
        
        // Debug: Check all pour points status
        Debug.Log("=== Checking all pour points ===");
        foreach (var pourP in pourPoint)
        {
            bool isAvailable = pourP.IsAvailable();
            bool hasCurrentBeer = pourP.beerEnterBoxCollider.currentBeerShaderPour != null;
            Debug.Log($"PourPoint: {pourP.name} | IsAvailable: {isAvailable} | HasCurrentBeer: {hasCurrentBeer} | CurrentBeer: {(hasCurrentBeer ? pourP.beerEnterBoxCollider.currentBeerShaderPour.name : "None")}");
        }
        
        // Find ONE available pour point and assign ONE beer to it (fills gradually over time)
        foreach (var pourP in pourPoint)
        {
            if (pourP.IsAvailable() && pourP.beerEnterBoxCollider.currentBeerShaderPour == null)
            {
                Debug.Log($"Found available pour point: {pourP.name}");
                
                // Find an unplaced beer
                foreach (var beer in beers)
                {
                    if (beer != null && !beer.beerComplete && !beer.isPlaced)
                    {
                        // Mark pour point as unavailable IMMEDIATELY to prevent race conditions
                        pourP.isAvailable = false;
                        
                        // Mark the beer as placed IMMEDIATELY to prevent it being selected again
                        beer.isPlaced = true;
                        
                        // IMMEDIATELY assign the beer to the pour point to prevent race conditions
                        pourP.beerEnterBoxCollider.currentBeerShaderPour = beer;
                        
                        // Start coroutine to smoothly move the beer (visual only now)
                        StartCoroutine(MoveBeerToPourPoint(beer, pourP));
                        Debug.Log($"Assigned beer {beer.name} to {pourP.name}");
                        return; // Exit after assigning ONE beer - next beer will be assigned on next call
                    }
                }
                
                Debug.LogWarning($"Could not find a beer to assign to {pourP.name}");
                return; // Exit if no beer found
            }
        }
        
        Debug.Log("No available pour points found");
    }
    
    IEnumerator MoveBeerToPourPoint(BeerShaderPour beer, BeerPourLocation pourP)
    {
        // Beer is already marked as placed and assigned to pour point before this coroutine starts
        
        Vector3 startPos = beer.transform.parent.position;
        Vector3 targetPos = pourP.beerEnterBoxCollider.transform.position;
        float elapsedTime = 0f;
        
        // Smoothly lerp the beer to the target position
        while (elapsedTime < beerMoveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / beerMoveDuration);
            beer.transform.parent.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        
        // Ensure we're at exactly the target position
        beer.transform.parent.position = targetPos;
        
        Debug.Log("Beer movement complete for " + beer.name + " at " + pourP.name);
    }
    
    
    IEnumerator MoveBeerToFinishPoint(BeerShaderPour beer, Transform finishP , BeerPourLocation pourP)
    {
        Vector3 startPos = beer.transform.parent.position;
        Vector3 targetPos = finishP.position +new Vector3(Completedbeers.Count * 3f,0,0 ); // Offset finished beers
        float elapsedTime = 0f;
        
        // Smoothly lerp the beer to the target position
        while (elapsedTime < beerMoveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / beerMoveDuration);
            beer.transform.parent.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        
        // Ensure we're at exactly the target position
        beer.transform.parent.position = targetPos;
        
        // NOW clear the pour point AFTER the beer has moved away
        pourP.Reset();
        
        Debug.Log("Beer moved to finish. Pour point " + pourP.name + " is now available again");
    }
    
    public void BeerDone()
    {
        // Check all pour points for completed beers
        foreach (var pourP in pourPoint)
        {
            if (pourP != null && pourP.beerEnterBoxCollider != null)
            {
                var currentBeer = pourP.beerEnterBoxCollider.currentBeerShaderPour;
                
                if (currentBeer != null && currentBeer.beerComplete && beers.Contains(currentBeer))
                {
                    // Move the completed beer to finish point
                    StartCoroutine(MoveBeerToFinishPoint(currentBeer, FinishPoint, pourP));
                   // currentBeer.transform.parent.position = FinishPoint.position;
                    currentBeer.isActive = false; // Stop pouring
                    
                    Debug.Log("Beer pouring completed.");
                    Debug.Log("Beer is complete: " + currentBeer.name);
                    
                    // Add to completed list and remove from active list
                    Completedbeers.Add(currentBeer);
                    beers.Remove(currentBeer);
                    
                    minigameCanvasUI.UpdateScore(Completedbeers.Count);
                    
                    // Try to move a new beer to this now-available pour point
                //    MoveBeerToNextAvailablePourPoint();
                    
                    break; // Process one beer at a time
                }
            }
        }
    }


    public void GameCompleted()
    {

        if (Completedbeers.Count >= spawnBeerCount || timerHasElapsed  && !gameCompleted) // Check if 5 beers are completed
        {
            Debug.Log("All beers completed!");

            int finalScore = Completedbeers.Count * 100; // 100 points per plate cleaned

            // Let MiniGameManager handle the completion, rewards, and scene transition
            if (MiniGameManager.Instance != null)
            {
                Debug.Log($"Calling MiniGameManager.CompleteGame with score: {finalScore}");
                MiniGameManager.Instance.CompleteGame(finalScore);
                gameCompleted = true; // Set the game completed flag to true
            }
            else
            {
                Debug.LogError("MiniGameManager.Instance is null! Cannot complete minigame properly.");

                // Fallback: manually return to main scene if MiniGameManager is missing
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.PlayerWorked();
                }
                SceneManager.LoadScene(GameManager.Instance.mainSceneName);
                gameCompleted = true; // Set the game completed flag to true
            }
        }

    }

}
