using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Random = UnityEngine.Random;

/// <summary>
/// Handles AI rock throwing with physical simulation and difficulty-based accuracy
/// Supports both physical throws (with camera follow) and instant calculation (for skip)
/// </summary>
public class AIRockThrower : MonoBehaviour
{
    public static AIRockThrower Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] private RockThrowingController throwingController;
    [SerializeField] private Transform rockSpawnPoint;
    [SerializeField] private Camera mainCamera;
    
    [Header("AI Difficulty Settings")]
    [Tooltip("Difficulty per AI opponent (0 = bad, 1 = perfect)")]
    [SerializeField] private float[] aiDifficulties = { 0.7f, 0.75f, 0.8f };
    
    [Header("Throw Parameters")]
    [SerializeField] private float minPower = 15f;   // Match player settings
    [SerializeField] private float maxPower = 35f;  
    [SerializeField] private float minAngle = -15f; 
    [SerializeField] private float maxAngle = 15f;  
    [SerializeField] private float throwArcHeight = 5f;
    [Tooltip("Base direction for throwing. Set to (0,0,-1) if water is in -Z direction")]
    [SerializeField] private Vector3 baseThrowDirection = new Vector3(0, 0, -1f);
    
    [Header("Timing Simulation")]
    [SerializeField] private float baseBounceWindow = 0.4f;
    [SerializeField] private float windowShrinkPerBounce = 0.05f;
    [SerializeField] private float minWindow = 0.15f;
    
    [Header("Bounce Multipliers")]
    [SerializeField] private float perfectMultiplier = 1.5f;
    [SerializeField] private float goodMultiplier = 1.2f;
    [SerializeField] private float okayMultiplier = 1.0f;
    [SerializeField] private float missMultiplier = 0.7f;
    
    [Header("Visual Settings")]
    [SerializeField] private float delayBetweenAIThrows = 1.5f;
    
    // Events
    public event Action<int, string> OnAITurnStart; // aiIndex, rockType
    public event Action<int, float> OnAIThrowComplete; // aiIndex, distance
    public event Action<int, BounceResult> OnAIBounce; // aiIndex, result
    public event Action OnAllAIThrowsComplete;
    
    // State
    private Rock currentAIRock;
    private int currentAIIndex;
    private float currentAIDistance;
    private int currentAIBounces;
    private float currentBounceWindow;
    private float currentDifficulty;
    private bool isAIThrowing = false;
    private bool skipRequested = false;
    
    // Results storage for skip mode
    private List<(int aiIndex, float distance)> pendingResults = new List<(int, float)>();
    
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
        
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        // Force correct throw values (override any old serialized values)
        minPower = 30f;    // Match player
        maxPower = 40f;    // Match player
        minAngle = -15f;
        maxAngle = 15f;
        throwArcHeight = 5f;
        
        Debug.Log($"AIRockThrower initialized: Power {minPower}-{maxPower}, Angle {minAngle}-{maxAngle}");
    }
    
    #region Public Methods
    
    /// <summary>
    /// Get difficulty for a specific AI opponent
    /// </summary>
    public float GetDifficulty(int aiIndex)
    {
        if (aiIndex >= 0 && aiIndex < aiDifficulties.Length)
            return aiDifficulties[aiIndex];
        return 0.5f;
    }
    
    /// <summary>
    /// Set difficulty for a specific AI opponent
    /// </summary>
    public void SetDifficulty(int aiIndex, float difficulty)
    {
        if (aiIndex >= 0 && aiIndex < aiDifficulties.Length)
            aiDifficulties[aiIndex] = Mathf.Clamp01(difficulty);
    }
    
    /// <summary>
    /// Execute a physical AI throw with full simulation
    /// </summary>
    public void ExecuteAIThrow(Rock rock, int aiIndex, Action<float> onComplete)
    {
        if (isAIThrowing) return;
        
        StartCoroutine(AIThrowCoroutine(rock, aiIndex, onComplete, false));
    }
    
    /// <summary>
    /// Execute multiple AI throws in sequence
    /// </summary>
    public void ExecuteAllAIThrows(List<(Rock rock, int aiIndex)> throws, Action<List<(int, float)>> onAllComplete)
    {
        StartCoroutine(AllAIThrowsCoroutine(throws, onAllComplete));
    }
    
    /// <summary>
    /// Skip remaining AI throws and calculate results instantly
    /// </summary>
    public void SkipAllAIThrows()
    {
        skipRequested = true;
        Debug.Log("Skip requested - calculating remaining AI throws instantly");
    }
    
    /// <summary>
    /// Calculate AI throw result without physics (for skip mode)
    /// </summary>
    public float CalculateInstantResult(Rock rock, int aiIndex)
    {
        float difficulty = GetDifficulty(aiIndex);
        
        // Simulate throw power (higher difficulty = more consistent high power)
        float powerVariance = (1f - difficulty) * 0.4f;
        float basePower = Mathf.Lerp(0.6f, 0.9f, difficulty);
        float normalizedPower = basePower + Random.Range(-.15f, powerVariance);
        normalizedPower = Mathf.Clamp01(normalizedPower);
        
        float actualPower = Mathf.Lerp(minPower, maxPower, normalizedPower);
        
        // Simulate bounces
        int maxBounces = rock.maxBounces;
        float totalDistance = 0f;
        float currentBounceForce = rock.bounceForce;
        float bounceWindow = baseBounceWindow;
        
        // Base distance from throw power
        totalDistance = actualPower * 2f;
        
        for (int i = 0; i < maxBounces; i++)
        {
            BounceResult result = SimulateBounceResult(difficulty, bounceWindow);
            
            float multiplier = result switch
            {
                BounceResult.Perfect => perfectMultiplier,
                BounceResult.Good => goodMultiplier,
                BounceResult.Okay => okayMultiplier,
                BounceResult.Miss => missMultiplier,
                _ => 1f
            };
            
            // Each bounce adds distance based on bounce force and multiplier
            float bounceDistance = currentBounceForce * multiplier * 1.5f;
            totalDistance += bounceDistance;
            
            // Update for next bounce
            currentBounceForce *= multiplier;
            bounceWindow = Mathf.Max(minWindow, bounceWindow - windowShrinkPerBounce);
            
            // Miss might end the run early
            if (result == BounceResult.Miss && Random.value < 0.3f)
            {
                break;
            }
        }
        
        // Add some random variation
        totalDistance *= Random.Range(0.9f, 1.1f);
        
        return Mathf.Max(5f, totalDistance);
    }
    
    public bool IsAIThrowing() => isAIThrowing;
    
    #endregion
    
    #region Coroutines
    
    private IEnumerator AIThrowCoroutine(Rock rock, int aiIndex, Action<float> onComplete, bool skipPhysics)
    {
        isAIThrowing = true;
        currentAIIndex = aiIndex;
        currentAIDistance = 0f;
        currentAIBounces = 0;
        currentDifficulty = GetDifficulty(aiIndex);
        currentBounceWindow = baseBounceWindow;
        
        string rockTypeName = rock.rockType.ToString();
        OnAITurnStart?.Invoke(aiIndex, rockTypeName);
        
        Debug.Log($"AI {aiIndex + 1} preparing to throw {rockTypeName} rock (Difficulty: {currentDifficulty:F2})");
        
        if (skipPhysics || skipRequested)
        {
            // Instant calculation
            float distance = CalculateInstantResult(rock, aiIndex);
            yield return new WaitForSeconds(0.5f); // Brief delay for feedback
            
            OnAIThrowComplete?.Invoke(aiIndex, distance);
            onComplete?.Invoke(distance);
            isAIThrowing = false;
            yield break;
        }
        
        // Calculate throw parameters based on difficulty
        float powerVariance = (1f - currentDifficulty) * 0.1f; // Was 0.15f - tighter variance
        float basePower = Mathf.Lerp(0.85f, 0.98f, currentDifficulty); // Was 0.75-0.95, now higher floor
        float normalizedPower = basePower + Random.Range(-powerVariance, powerVariance);
        normalizedPower = Mathf.Clamp(normalizedPower, 0.75f, 1f); // Was 0.6f min, now 0.75f

        float angleVariance = (1f - currentDifficulty) * 5f; // Was 8f - even tighter angles
        float angle = Random.Range(-angleVariance, angleVariance);
        
        float actualPower = Mathf.Lerp(minPower, maxPower, normalizedPower);
        
        // Brief delay before throw
        yield return new WaitForSeconds(0.5f);
        
        // Spawn and throw the rock
        Vector3 spawnPos = rockSpawnPoint != null ? rockSpawnPoint.position : transform.position;
        
        // Use a flat rotation facing the throw direction - rock should be horizontal like a skipping stone
        // Don't inherit the source rock's rotation which may be weird from preview spinning
        Quaternion flatRotation = Quaternion.LookRotation(baseThrowDirection, Vector3.up);
        
        currentAIRock = Instantiate(rock, spawnPos, flatRotation);
        currentAIRock.gameObject.SetActive(true);
        
        // Kill any DOTween animations that might have been copied from the original rock
        currentAIRock.transform.DOKill();
        
        // Ensure the rock's rigidbody has no angular velocity
        Rigidbody rockRb = currentAIRock.GetComponent<Rigidbody>();
        if (rockRb != null)
        {
            rockRb.angularVelocity = Vector3.zero;
            // Freeze rotation so rock stays flat while skipping
            rockRb.freezeRotation = true;
        }
        
        // Wait a frame to ensure Awake/Start have run
        yield return null;
        
        // Subscribe to rock events
        currentAIRock.OnWaterContact += OnAIRockWaterContact;
        currentAIRock.OnRockSunk += OnAIRockSunk;
        
        // Calculate throw direction using base direction (default -Z towards water)
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        Vector3 throwDirection = rotation * baseThrowDirection.normalized;
        
        // Calculate velocity - need proper arc for skipping
        // Rock needs shallow entry angle (10-20 degrees) to skip well
        Vector3 velocity = throwDirection * actualPower;
        
        // Scale Y velocity based on horizontal speed to maintain good skip angle
        // A ratio of about 0.15-0.2 gives a good shallow angle for skipping
        float arcMultiplier = 0.18f + (currentDifficulty * 0.05f); // Better AI = better angle
        velocity.y = actualPower * arcMultiplier;
        
        // Throw with calculated velocity
        currentAIRock.ThrowRock(velocity);
        
        // Have camera follow AI rock too
        if (RockThrowingController.Instance != null)
        {
            var controller = RockThrowingController.Instance;
            if (controller.cameraFollower != null)
            {
               controller.StartCameraFollow(currentAIRock.transform);
            }
            RockThrowingController.Instance.UiUpdatePlayerThrowing(aiIndex+2);
        }
        
        Debug.Log($"AI {aiIndex + 1} threw rock! Power: {actualPower:F1}, Angle: {angle:F1}°, Velocity: {velocity}");
        
        // Wait for rock to sink
        float timeout = 30f;
        float elapsed = 0f;
        
        while (currentAIRock != null && elapsed < timeout)
        {
            // Check if skip was requested during flight
            if (skipRequested)
            {
                // Cleanup current rock and calculate instant result
                if (currentAIRock != null)
                {
                    currentAIRock.OnWaterContact -= OnAIRockWaterContact;
                    currentAIRock.OnRockSunk -= OnAIRockSunk;
                    Destroy(currentAIRock.gameObject);
                    currentAIRock = null;
                }
                
                float instantDistance = CalculateInstantResult(rock, aiIndex);
                OnAIThrowComplete?.Invoke(aiIndex, instantDistance);
                onComplete?.Invoke(instantDistance);
                throwingController?.UiUpdateAiDistance(instantDistance);
                isAIThrowing = false;
                yield break;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Cleanup if timeout
        if (currentAIRock != null)
        {
            currentAIRock.OnWaterContact -= OnAIRockWaterContact;
            currentAIRock.OnRockSunk -= OnAIRockSunk;
            Destroy(currentAIRock.gameObject);
            currentAIRock = null;
        }
        
        // Use calculated distance or fallback
        float finalDistance = currentAIDistance > 0 ? currentAIDistance : CalculateInstantResult(rock, aiIndex);
        
        OnAIThrowComplete?.Invoke(aiIndex, finalDistance);
        onComplete?.Invoke(finalDistance);
        throwingController?.UiUpdateAiDistance(finalDistance);
        isAIThrowing = false;
        throwingController?.StopCameraFollow();
          
        
    }
    
    private IEnumerator AllAIThrowsCoroutine(List<(Rock rock, int aiIndex)> throws, Action<List<(int, float)>> onAllComplete)
    {
        List<(int, float)> results = new List<(int, float)>();
        skipRequested = false;
        
        foreach (var (rock, aiIndex) in throws)
        {
            if (skipRequested)
            {
                // Calculate remaining results instantly
                float distance = CalculateInstantResult(rock, aiIndex);
                results.Add((aiIndex, distance));
                OnAIThrowComplete?.Invoke(aiIndex, distance);
                continue;
            }
            
            float resultDistance = 0f;
            bool throwComplete = false;
            
            ExecuteAIThrow(rock, aiIndex, (distance) => {
                resultDistance = distance;
                throwComplete = true;
            });
            
            // Wait for throw to complete
            while (!throwComplete)
            {
                yield return null;
            }
            
            results.Add((aiIndex, resultDistance));
            
            // Delay between throws (unless skipping)
            if (!skipRequested)
            {
                yield return new WaitForSeconds(delayBetweenAIThrows);
            }
        }
        
        OnAllAIThrowsComplete?.Invoke();
        onAllComplete?.Invoke(results);
    }
    
    #endregion
    
    #region Rock Event Handlers
    
    private void OnAIRockWaterContact()
    {
        if (currentAIRock == null) return;
        
        currentAIBounces++;
        
        // Simulate timing result based on difficulty
        BounceResult result = SimulateBounceResult(currentDifficulty, currentBounceWindow);
        
        float multiplier = result switch
        {
            BounceResult.Perfect => perfectMultiplier,
            BounceResult.Good => goodMultiplier,
            BounceResult.Okay => okayMultiplier,
            BounceResult.Miss => missMultiplier,
            _ => 1f
        };
        
        currentAIRock.ApplyBounceMultiplier(multiplier);
        
        // Shrink window for next bounce
        currentBounceWindow = Mathf.Max(minWindow, currentBounceWindow - windowShrinkPerBounce);
        
        OnAIBounce?.Invoke(currentAIIndex, result);
        Debug.Log($"AI {currentAIIndex + 1} bounce {currentAIBounces}: {result}");
    }
    
    private void OnAIRockSunk(float totalDistance)
    {
        currentAIDistance = totalDistance;
        
        if (currentAIRock != null)
        {
            currentAIRock.OnWaterContact -= OnAIRockWaterContact;
            currentAIRock.OnRockSunk -= OnAIRockSunk;
            currentAIRock = null;
        }
        
        Debug.Log($"AI {currentAIIndex + 1} rock sunk at {totalDistance:F1}m");
    }
    
    #endregion
    
    #region Simulation Helpers
    
    private BounceResult SimulateBounceResult(float difficulty, float windowSize)
    {
        // Higher difficulty = higher chance of good results
        // Smaller window = harder to get perfect
        
        float windowDifficultyModifier = windowSize / baseBounceWindow;
        float effectiveDifficulty = difficulty * windowDifficultyModifier;
        
        float roll = Random.value;
        
        // Perfect chance: high difficulty, larger window
        float perfectChance = effectiveDifficulty * 0.4f;
        if (roll < perfectChance)
            return BounceResult.Perfect;
        
        // Good chance
        float goodChance = perfectChance + effectiveDifficulty * 0.35f;
        if (roll < goodChance)
            return BounceResult.Good;
        
        // Okay chance
        float okayChance = goodChance + 0.25f;
        if (roll < okayChance)
            return BounceResult.Okay;
        
        // Miss
        return BounceResult.Miss;
    }
    
    #endregion
    
    private void OnDestroy()
    {
        if (currentAIRock != null)
        {
            currentAIRock.OnWaterContact -= OnAIRockWaterContact;
            currentAIRock.OnRockSunk -= OnAIRockSunk;
        }
        
        if (Instance == this)
            Instance = null;
    }
}
