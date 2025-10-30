using System;
using System.Collections.Generic;
using MiniGames;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BeerGameController : MonoBehaviour
{
    public GameObject beerPrefab; // Prefab for the beer object
    public Transform pourPoint; // Point where beer is poured
    public Transform SpawnPoint; // Point where spam is spawned
    public Transform FinishPoint; // Point where the beer is finished
    public BeerEnterBoxCollider beerEnterBoxCollider; // Reference to the beer enter box collider
    public static BeerGameController Instance { get; private set; }
    public List<BeerShaderPour> beers; // List of beer shader pours
    public List<BeerShaderPour> Completedbeers; // List of beer shader pours
    public bool gameCompleted = false; // Flag to indicate if the game is completed


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
       StartGameSpawn();
       beerEnterBoxCollider.onBeerCompleted.AddListener(BeerDone); // Subscribe to the BeerDone event
      // MoveNextBeer();
    }

    public void StartGameSpawn()
    {
        for (int i = 0; i < 5; i++) // Spawn 5 beers at the start
        {
            SpawnBeer();
        }
    }

    private void Update()
    {
        GameCompleted();
    }
    
    void OnDisable()
    {
        if (beerEnterBoxCollider != null)
            beerEnterBoxCollider.onBeerCompleted.RemoveListener(BeerDone);
    }

    public void SpawnBeer()
    {
        if (beerPrefab != null && SpawnPoint != null)
        {
            GameObject beer = Instantiate(beerPrefab, SpawnPoint.position, Quaternion.identity);
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
        if (beerEnterBoxCollider != null && beerEnterBoxCollider.beerShaderPour == null)
        {
            foreach (var beer in beers)
            {
                if (beer != null && !beer.beerComplete) // Check if the beer is not complete
                {
                    beer.transform.parent.position = pourPoint.position; // Move the beer's parent to the pour point
                    Debug.Log("Next beer assigned: " + beer.name);
                    break; // Exit the loop after assigning the first available beer
                }
            }
            
        }
        
    }
    
    
    public void BeerDone()
    {
        if (beerEnterBoxCollider != null)
        {
            foreach (var beer in beers)
            {
                if (beer != null && beer.beerComplete)
                {
                    beer.transform.parent.position = FinishPoint.position; // Move the beer to the finish point
                    beerEnterBoxCollider.beerShaderPour.isActive = false; // Stop pouring
                    beerEnterBoxCollider.beerShaderPour.beerComplete = true; // beer is complete
                    beerEnterBoxCollider.ClearBeerShaderPour(); // Clear the beer shader pour reference
                    Debug.Log("Beer pouring completed.");
                    Debug.Log("Beer is complete: " + beer.name);
                    Completedbeers.Add(beer); // Add the completed beer to the completed list
                    beers.Remove(beer); // Remove the completed beer from the list
                  //  MoveNextBeer();
                    break; // Exit the loop after processing the first completed beer
                }
            }  
        }
    }


    public void GameCompleted()
    {

        if (Completedbeers.Count >= 5 && !gameCompleted) // Check if 5 beers are completed
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
