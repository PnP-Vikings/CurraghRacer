using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoxingMinigameManager : MonoBehaviour
{
    public static BoxingMinigameManager Instance;
    
    [Header("Target Setup")]
    public List<Transform> spawnPoints;
    public BoxingTarget targetPrefab;
    public Transform targetParent; // UI Canvas or panel to spawn targets under
    
    [Header("Game Settings")]
    public int targetPoolSize = 100; // Initial pool size
    [SerializeField] public float targetLifetime = 2f; // How long targets stay visible
    
    [Header("Difficulty Settings")]
    public int scorePerExtraTarget = 10; // Score needed to add another target
    public int maxSimultaneousTargets = 5; // Maximum number of targets at once
    public float speedIncreasePerScore = 0.02f; // Reduce lifetime by this much per score (slow scaling)
    public float minTargetLifetime = 0.8f; // Minimum time a target can stay visible
    
    private List<BoxingTarget> pooledTargets = new List<BoxingTarget>();
    private List<BoxingTarget> activeTargets = new List<BoxingTarget>();
    private HashSet<int> occupiedSpawnPoints = new HashSet<int>(); // Track which spawn points are in use
    [SerializeField] private int score = 0;
    [SerializeField] private int playerLives =3;
    private int currentMaxTargets = 1; // Start with 1 target
    [SerializeField] private float currentTargetLifetime; // Current difficulty-adjusted lifetime for all targets
    private int lastUsedSpawnPoint = -1; // Track the last spawn point used to avoid consecutive repeats
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        InitializePool(targetPoolSize);
        
        // Initialize the current lifetime
        currentTargetLifetime = targetLifetime;
        
        // Spawn initial targets
        for (int i = 0; i < currentMaxTargets; i++)
        {
            SpawnTargetAtRandomPoint();
        }
    }
    
    public void InitializePool(int initialSize)
    {
        for (int i = 0; i < initialSize; i++)
        {
            BoxingTarget target = Instantiate(targetPrefab, targetParent);
            target.gameObject.SetActive(false);
            pooledTargets.Add(target);
        }
    }
    
    public void SpawnTargetAtRandomPoint()
    {
        if (spawnPoints.Count == 0 || targetPrefab == null)
        {
            Debug.LogWarning("No spawn points or target prefab assigned.");
            return;
        }

        // Don't spawn more targets than allowed
        if (activeTargets.Count >= currentMaxTargets)
        {
            return;
        }

        // Get target from pool
        BoxingTarget target = GetTargetFromPool();
        if (target == null)
        {
            Debug.LogWarning("No targets available in pool!");
            return;
        }

        // Find an unoccupied spawn point
        int randomIndex = GetRandomUnoccupiedSpawnPoint();
        if (randomIndex == -1)
        {
            Debug.LogWarning("All spawn points are occupied!");
            ReturnTargetToPool(target);
            return;
        }

        // Position at the spawn point
        Transform spawnPoint = spawnPoints[randomIndex];
        target.transform.position = spawnPoint.position;
        target.transform.rotation = spawnPoint.rotation;
        
        // Mark spawn point as occupied and store it on the target
        occupiedSpawnPoints.Add(randomIndex);
        target.spawnPointIndex = randomIndex;
        
        // Track this as the last used spawn point
        lastUsedSpawnPoint = randomIndex;
        
        // Reset opacity to full before showing
        Image targetImage = target.GetComponent<Image>();
        if (targetImage != null)
        {
            Color color = targetImage.color;
            targetImage.color = new Color(color.r, color.g, color.b, 1f);
        }
        
        target.gameObject.SetActive(true);
        
        // All targets use the current shared lifetime (same for all active targets)
        // Store the coroutine reference so we can stop it when the target is hit
        target.fadeCoroutine = StartCoroutine(DecreaseOpacityOverTime(target, currentTargetLifetime));
    }
    
    private int GetRandomUnoccupiedSpawnPoint()
    {
        // If all spawn points are occupied, return -1
        if (occupiedSpawnPoints.Count >= spawnPoints.Count)
        {
            return -1;
        }

        // Find an unoccupied spawn point
        int attempts = 0;
        int maxAttempts = spawnPoints.Count * 3; // Prevent infinite loop
        
        // When we have few targets (1-2), try to avoid the last used spawn point for variety
        bool avoidLastSpawn = (currentMaxTargets <= 2 && lastUsedSpawnPoint >= 0);
        
        while (attempts < maxAttempts)
        {
            int randomIndex = Random.Range(0, spawnPoints.Count);
            
            // Check if this spawn point is available
            if (!occupiedSpawnPoints.Contains(randomIndex))
            {
                // If we're avoiding the last spawn and this IS the last spawn, try again (unless we've tried many times)
                if (avoidLastSpawn && randomIndex == lastUsedSpawnPoint && attempts < maxAttempts - 5)
                {
                    attempts++;
                    continue;
                }
                
                return randomIndex;
            }
            attempts++;
        }
        
        return -1;
    }
    
    private BoxingTarget GetTargetFromPool()
    {
        if (pooledTargets.Count > 0)
        {
            BoxingTarget target = pooledTargets[0];
            pooledTargets.RemoveAt(0);
            activeTargets.Add(target);
            return target;
        }
        
        Debug.LogWarning("Target pool exhausted!");
        return null;
    }
    
    public void TargetHit(BoxingTarget target)
    {
        // Stop the fade coroutine so lives aren't lost
        if (target.fadeCoroutine != null)
        {
            StopCoroutine(target.fadeCoroutine);
            target.fadeCoroutine = null;
        }
        
        // Increase score
        score++;
        Debug.Log($"Target Hit! Score: {score}");
        
        // Calculate what the new lifetime should be based on score
        float newLifetime = Mathf.Max(targetLifetime - (score * speedIncreasePerScore), minTargetLifetime);
        
        // Only update if it's actually lower (difficulty increase)
        if (newLifetime < currentTargetLifetime)
        {
            currentTargetLifetime = newLifetime;
            Debug.Log($"Speed increased! New target lifetime: {currentTargetLifetime:F2}s");
        }
        
        // Check if we should increase the number of simultaneous targets
        int newMaxTargets = Mathf.Min(1 + (score / scorePerExtraTarget), maxSimultaneousTargets);
        if (newMaxTargets > currentMaxTargets)
        {
            currentMaxTargets = newMaxTargets;
            Debug.Log($"Difficulty increased! Now spawning up to {currentMaxTargets} targets at once.");
        }
        
        // TODO: Add particle effects, sound, etc.
        
        // Return the target to the pool
        ReturnTargetToPool(target);
        
        // Spawn new targets to reach the current max
        int targetsToSpawn = currentMaxTargets - activeTargets.Count;
        for (int i = 0; i < targetsToSpawn; i++)
        {
            SpawnTargetAtRandomPoint();
        }
    }
    
    IEnumerator DecreaseOpacityOverTime(BoxingTarget target, float duration)
    {
        float elapsed = 0f;
        Image sr = target.GetComponent<Image>();
        Color originalColor = sr.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        
        sr.color = originalColor; // Reset color
        playerLives--;
        if (playerLives <= 0)
        {
            Debug.Log("Game Over!");
        }
        Debug.Log($"Target Missed! Lives remaining: {playerLives}");
        ReturnTargetToPool(target);
        
        // Spawn a new target to maintain the current max
        if (activeTargets.Count < currentMaxTargets)
        {
            SpawnTargetAtRandomPoint();
        }
    }
    
    public void ReturnTargetToPool(BoxingTarget target)
    {
        if (activeTargets.Contains(target))
        {
            // Free up the spawn point
            if (target.spawnPointIndex >= 0)
            {
                occupiedSpawnPoints.Remove(target.spawnPointIndex);
                target.spawnPointIndex = -1;
            }
            
            activeTargets.Remove(target);
            pooledTargets.Add(target);
            // Reset opacity to full before showing
            Image targetImage = target.GetComponent<Image>();
            if (targetImage != null)
            {
                Color color = targetImage.color;
                targetImage.color = new Color(color.r, color.g, color.b, 1f);
            }
            target.gameObject.SetActive(false);
        }
    }
    
    public int GetScore()
    {
        return score;
    }
}
