using System.Collections;
using System.Collections.Generic;
using MiniGames;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BoxingMinigameManager : MonoBehaviour
{
    public static BoxingMinigameManager Instance;
    
    [Header("Target Setup")]
    public List<Transform> spawnPoints;
    public Transform BoxingBag; // Changed from Rigidbody to Transform for position-based animation
    public BoxingTarget targetPrefab;
    public BoxingTarget targetSpecialPrefab;
    public BoxingTarget targetEvenMoreSpecialPrefab;
    public Transform targetParent; // UI Canvas or panel to spawn targets under
    public int rightSideSpawnCount = 4; // How many spawn points are on the right side (first N points)
    public float bagSwingDistance = 0.3f; // How far the bag swings horizontally
    public float bagSwingDuration = 0.5f; // How long the swing animation takes
    
    [Header("Game Settings")]
    public int targetPoolSize = 100; // Initial pool size
    [SerializeField] public float targetLifetime = 2f; // How long targets stay visible
    
    [Header("Difficulty Settings")]
    public int scorePerExtraTarget = 10; // Score needed to add another target
    public int maxSimultaneousTargets = 5; // Maximum number of targets at once
    public float speedIncreasePerScore = 0.02f; // Reduce lifetime by this much per score (slow scaling)
    public float minTargetLifetime = 0.8f; // Minimum time a target can stay visible
    
    [Header("Special Target Settings")]
    public int scoreForSpecialTargets = 20; // Score needed before special targets start appearing
    public int scoreForEvenMoreSpecialTargets = 50; // Score needed before even more special targets start appearing
    public float baseSpecialTargetChance = 0.1f; // 10% chance at minimum score
    public float maxSpecialTargetChance = 0.3f; // 30% max chance for special targets
    public float baseEvenMoreSpecialTargetChance = 0.05f; // 5% chance at minimum score
    public float maxEvenMoreSpecialTargetChance = 0.15f; // 15% max chance for even more special targets
    public float chanceIncreasePerScore = 0.001f; // How much the chance increases per point
    
    private List<BoxingTarget> pooledTargets = new List<BoxingTarget>();
    private List<BoxingTarget> pooledSpecialTargets = new List<BoxingTarget>();
    private List<BoxingTarget> pooledEvenMoreSpecialTargets = new List<BoxingTarget>();
    private List<BoxingTarget> activeTargets = new List<BoxingTarget>();
    private HashSet<int> occupiedSpawnPoints = new HashSet<int>(); // Track which spawn points are in use
    [SerializeField] private int score = 0;
    [SerializeField] private int playerLives =3;
    private int currentMaxTargets = 1; // Start with 1 target
    [SerializeField] private float currentTargetLifetime; // Current difficulty-adjusted lifetime for all targets
    private int lastUsedSpawnPoint = -1; // Track the last spawn point used to avoid consecutive repeats
    
    private Vector3 bagOriginalPosition; // Store the bag's starting position
    private Sequence bagSwingSequence; // Track the current swing animation sequence
    private bool isGameOver = false; // Track if the game is over
    
    [Header("Camera Settings")]
    public Camera gameCamera; // Reference to the camera for shake effect
    public float cameraShakeIntensity = 0.15f; // How strong the shake is
    public float cameraShakeDuration = 0.2f; // How long the shake lasts
    private Vector3 cameraOriginalPosition; // Store camera's starting position
    
    [Header("Ui Settings")]
    public BoxingUiCanvas boxingUiCanvas;

    bool hasBoxingSuccessAfterFailPlayed = false;

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
        
        // Store the bag's original position
        if (BoxingBag != null)
        {
            bagOriginalPosition = BoxingBag.position;
        }
        
        // Get camera reference if not assigned
        if (gameCamera == null)
        {
            gameCamera = Camera.main;
        }
        
        // Store camera's original position
        if (gameCamera != null)
        {
            cameraOriginalPosition = gameCamera.transform.localPosition;
        }
        
        // Spawn initial targets
        for (int i = 0; i < currentMaxTargets; i++)
        {
            SpawnTargetAtRandomPoint();
        }
        
        if(boxingUiCanvas != null)
        {
            boxingUiCanvas.SetUpUI(true,false,true,true);
            boxingUiCanvas.UpdatePlayerLives(playerLives);
            boxingUiCanvas.UpdateScore(score);
        }
    }
    
    public void InitializePool(int initialSize)
    {
        // Create regular targets
        for (int i = 0; i < initialSize; i++)
        {
            BoxingTarget target = Instantiate(targetPrefab, targetParent);
            target.gameObject.SetActive(false);
            pooledTargets.Add(target);
        }
        
        // Create special targets pool
        for (int i = 0; i < initialSize / 5; i++)
        {
            BoxingTarget specialTarget = Instantiate(targetSpecialPrefab, targetParent);
            specialTarget.gameObject.SetActive(false);
            pooledSpecialTargets.Add(specialTarget);
        }
        
        // Create even more special targets pool
        for (int i = 0; i < initialSize / 10; i++)
        {
            BoxingTarget evenMoreSpecialTarget = Instantiate(targetEvenMoreSpecialPrefab, targetParent);
            evenMoreSpecialTarget.gameObject.SetActive(false);
            pooledEvenMoreSpecialTargets.Add(evenMoreSpecialTarget);
        }
    }
    
    public void SpawnTargetAtRandomPoint()
    {
        // Don't spawn targets if game is over
        if (isGameOver)
        {
            return;
        }
        
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
        bool avoidLastSpawn = (currentMaxTargets <= 6 && lastUsedSpawnPoint >= 0);
        
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
        BoxingTarget target = null;
        List<BoxingTarget> sourcePool = null;
        
        // Determine which type of target to spawn based on score and random chance
        if (score >= scoreForEvenMoreSpecialTargets && pooledEvenMoreSpecialTargets.Count > 0)
        {
            // Calculate chance for even more special targets (increases with score)
            float evenMoreSpecialChance = Mathf.Min(
                baseEvenMoreSpecialTargetChance + (score * chanceIncreasePerScore),
                maxEvenMoreSpecialTargetChance
            );
            
            if (Random.value < evenMoreSpecialChance)
            {
                sourcePool = pooledEvenMoreSpecialTargets;
                Debug.Log($"Spawning EVEN MORE SPECIAL target! (Chance: {evenMoreSpecialChance:P1})");
            }
        }
        
        // If we didn't pick even more special, try for special target
        if (sourcePool == null && score >= scoreForSpecialTargets && pooledSpecialTargets.Count > 0)
        {
            // Calculate chance for special targets (increases with score)
            float specialChance = Mathf.Min(
                baseSpecialTargetChance + (score * chanceIncreasePerScore),
                maxSpecialTargetChance
            );
            
            if (Random.value < specialChance)
            {
                sourcePool = pooledSpecialTargets;
                Debug.Log($"Spawning SPECIAL target! (Chance: {specialChance:P1})");
            }
        }
        
        // If no special target selected, use regular target
        if (sourcePool == null)
        {
            sourcePool = pooledTargets;
        }
        
        // Get target from the selected pool
        if (sourcePool.Count > 0)
        {
            target = sourcePool[0];
            sourcePool.RemoveAt(0);
            activeTargets.Add(target);
            return target;
        }
        
        // Fallback to regular targets if special pools are empty
        if (pooledTargets.Count > 0)
        {
            target = pooledTargets[0];
            pooledTargets.RemoveAt(0);
            activeTargets.Add(target);
            return target;
        }
        
        Debug.LogWarning("All target pools exhausted!");
        return null;
    }
    
    public void TargetHit(BoxingTarget target,int pointsToAdd)
    {
        // Don't process hits if game is over
        if (isGameOver)
        {
            return;
        }
        
        // Stop the fade coroutine so lives aren't lost
        if (target.fadeCoroutine != null)
        {
            StopCoroutine(target.fadeCoroutine);
            target.fadeCoroutine = null;
        }
        
        // Determine which side was hit and swing the boxing bag
        if (BoxingBag != null)
        {
            // Check if the hit spawn point is on the right side (first N spawn points)
            bool isRightSide = target.spawnPointIndex < rightSideSpawnCount;
            
            // If hit on right side, swing bag left. If hit on left side, swing bag right
            float swingDirection = isRightSide ? -1f : 1f;
            
            // Stop any existing swing animation
            if (bagSwingSequence != null && bagSwingSequence.IsActive())
            {
                bagSwingSequence.Kill();
            }
            
            // Start new swing animation
            SwingBagWithDOTween(swingDirection);
            
            Debug.Log($"Hit on {(isRightSide ? "RIGHT" : "LEFT")} side (spawn point {target.spawnPointIndex}), swinging bag {(swingDirection > 0 ? "right" : "left")}");
        }
        
        // Add camera shake effect for impact
        ShakeCamera();
        
        // Increase score
        score+= pointsToAdd;
        Debug.Log($"Target Hit! Score: {score}");

        if (AudioManager.instance != null)
        {
            AudioManager.instance.punchBagAudio.start();

            if (playerLives == 1 & !hasBoxingSuccessAfterFailPlayed)
            {
                AudioManager.instance.boxingSuccessAfterFail.start();
                hasBoxingSuccessAfterFailPlayed = true;
            }
        }

        if (boxingUiCanvas != null)
        {
            boxingUiCanvas.UpdateScore(score);
        }
        
        // Adjust difficulty based on score
        // Decrease target lifetime (increase speed)
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
    
    void SwingBagWithDOTween(float direction)
    {
        // Calculate target position and rotation
        Vector3 targetPosition = bagOriginalPosition + new Vector3(direction * bagSwingDistance, 0f, 0f);
        float targetRotationZ = direction * -2.71f; // Rotate -2.71 degrees in swing direction
        
        // Get current rotation components (preserve X and Y)
        Vector3 currentRotation = BoxingBag.eulerAngles;
        Vector3 targetRotationVector = new Vector3(currentRotation.x, currentRotation.y, targetRotationZ);
        Vector3 originalRotationVector = new Vector3(currentRotation.x, currentRotation.y, 0f);
        
        // Create a sequence for smooth animation
        bagSwingSequence = DOTween.Sequence();
        
        // Swing to the target position and rotation (first half)
        bagSwingSequence.Append(BoxingBag.DOMove(targetPosition, bagSwingDuration / 2f).SetEase(Ease.OutQuad));
        bagSwingSequence.Join(BoxingBag.DORotate(targetRotationVector, bagSwingDuration / 2f, RotateMode.Fast).SetEase(Ease.OutQuad));
        
        // Swing back to original position and rotation (second half)
        bagSwingSequence.Append(BoxingBag.DOMove(bagOriginalPosition, bagSwingDuration / 2f).SetEase(Ease.InQuad));
        bagSwingSequence.Join(BoxingBag.DORotate(originalRotationVector, bagSwingDuration / 2f, RotateMode.Fast).SetEase(Ease.InQuad));
        
        // Clean up the sequence reference when complete
        bagSwingSequence.OnComplete(() => bagSwingSequence = null);
    }
    
    void ShakeCamera()
    {
        if (gameCamera == null) return;
        
        // Kill any existing shake animations
        gameCamera.transform.DOKill();
        
        // Reset to original position first
        gameCamera.transform.localPosition = cameraOriginalPosition;
        
        // Create a punch/shake effect using DOTween
        // We'll shake the camera randomly and then return it to original position
        gameCamera.transform.DOShakePosition(
            cameraShakeDuration, 
            cameraShakeIntensity, 
            vibrato: 10, 
            randomness: 90, 
            fadeOut: true
        ).OnComplete(() => {
            // Ensure camera returns exactly to original position
            gameCamera.transform.localPosition = cameraOriginalPosition;
        });
    }

    public void GameOver()
    {
        if(isGameOver)
        {
            if (MiniGameManager.Instance != null)
            {
                Debug.Log($"Calling MiniGameManager.CompleteGame with score: {score}");
                MiniGameManager.Instance.CompleteGame((score));
                StartCoroutine(BoxingAudio.instance.BoxingGameOverAudio());
            };
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
        if(boxingUiCanvas != null)
        {
            boxingUiCanvas.UpdatePlayerLives(playerLives);
        }
        if (playerLives <= 0)
        {
            isGameOver = true;
            
            // Stop all active target fade coroutines
            foreach (BoxingTarget activeTarget in activeTargets)
            {
                if (activeTarget.fadeCoroutine != null)
                {
                    StopCoroutine(activeTarget.fadeCoroutine);
                    activeTarget.fadeCoroutine = null;
                }
            }
            
            // Deactivate all active targets
            while (activeTargets.Count > 0)
            {
                ReturnTargetToPool(activeTargets[0]);
            }
            
            if(boxingUiCanvas != null)
            {
                boxingUiCanvas.ShowGameOver();
            }
            Debug.Log("Game Over! Final Score: " + score);
            GameOver();
            yield break; // Stop this coroutine
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
            
            // Return to the correct pool based on target type
            if (target.gameObject.name.Contains("EvenMoreSpecial"))
            {
                pooledEvenMoreSpecialTargets.Add(target);
            }
            else if (target.gameObject.name.Contains("Special"))
            {
                pooledSpecialTargets.Add(target);
            }
            else
            {
                pooledTargets.Add(target);
            }
            
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
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
