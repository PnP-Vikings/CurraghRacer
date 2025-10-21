using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FootRaceMiniGameManager : MonoBehaviour
{
    public static FootRaceMiniGameManager Instance;
    public Button jumpButton, slideButton;
    public Rigidbody playerRigidbody;
    public float jumpForce = 10f;
    public float moveSpeed = 5f;
    public bool isGrounded = true;
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public float forwardSpeed = 5f;
    public float currentSpeed = 0f;
    
    [Header("Jump Physics")]
    public float gravityMultiplier = 2.5f; // Makes gravity stronger for snappier jumps
    public float fallMultiplier = 3f; // Extra gravity when falling down
    public float lowJumpMultiplier = 2f; // Extra gravity when not holding jump
    public bool gameActive = false;
    public float obstacleSpawnInterval = 2f;
    public int obstacleInitialSpawnQuantity = 50;
    public List<GameObject> groundObstaclePrefab;
    public List<GameObject> floatObstaclePrefab;
    public Transform obstacleSpawnPoint;
    public float obstacleSpawnDistance = 20f;
    public float obstacleDespawnDistance = -10f;
    public int score = 0;
    
    [Header("Obstacle Spacing")]
    [Tooltip("Extra Z distance to add after spawning a floating obstacle, to give the player more time.")]
    public float extraSpacingAfterFloat = 5f;
    
    private float lastJumpTime = -1f;
    private float jumpCooldown = 0.8f; // Minimum time between jumps
    
    [Header("Slide Physics")]
    public float standingHeight = 1.8f;
    public float standingRotation = 0f;
    public float slidingHeight = 1.13f;
    public float slidingRotation = -67.49f;
    
    public float slideDuration = 1f;
    public float slideTransitionSpeed = 10f; // How quickly to transition between positions
    private bool isSliding = false;
    private float slideStartTime;
    private Coroutine slideCoroutineHandle;
    
    
    
    [Header("Ui Elements")]
    public MinigameCanvasUI minigameCanvasUI;
   
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
        if (jumpButton != null)
        {
            jumpButton.onClick.AddListener(Jump);
        }
        else
        {
            Debug.LogWarning("Jump button is not assigned in the inspector.");
        }
        
        if (slideButton != null)
        {
            slideButton.onClick.AddListener(Slide);
        }
        else
        {
            Debug.LogWarning("Slide button is not assigned in the inspector.");
        }
        
        // Validate ground check setup
        if (groundCheck == null)
        {
            Debug.LogError("Ground Check Transform is not assigned! Please assign it in the inspector.");
        }
        
        if (groundLayer == 0)
        {
            Debug.LogWarning("Ground Layer is not set! Make sure to assign the ground layer in the inspector.");
        }

        if (minigameCanvasUI != null)
        {
            minigameCanvasUI.SetUpUI(true,true,true);
        }
        StartGame();
    }
    
    private void Update()
    {
        // Continuously update grounded state
        isGrounded = IsGrounded();
        
        // If we left the ground during a slide (bump/jump), cancel slide without snapping Y
        /*if (isSliding && !isGrounded)
        {
            BreakOutOfSlide("Left ground", snapImmediately: false);
        }*/
        
        if (gameActive)
        {
            score = Mathf.FloorToInt(Time.timeSinceLevelLoad * 3); // score increases over time
            if (minigameCanvasUI != null)
            {
                minigameCanvasUI.UpdateScore(score);
                minigameCanvasUI.UpdateTimer(Time.timeSinceLevelLoad.ToString("F1") + "s");
                minigameCanvasUI.UpdatePlayerLives("Speed: " + playerRigidbody.linearVelocity.z.ToString("F1"));
            }
            
            if (CheckIsPlayerTooSlow() && Time.timeSinceLevelLoad > 3f)
            {
                KillPlayer();
            }
        }
    }

    public void FixedUpdate()
    {
        if (!gameActive) return;
        
        // Maintain forward movement
        playerRigidbody.linearVelocity = new Vector3(0, playerRigidbody.linearVelocity.y, currentSpeed);
        
        // Apply better jump physics for less floaty feeling
        if (playerRigidbody.linearVelocity.y < 0)
        {
            // Falling down - apply extra gravity for snappier landing
            playerRigidbody.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (playerRigidbody.linearVelocity.y > 0 && !isGrounded)
        {
            // Rising up - apply standard gravity multiplier
            playerRigidbody.linearVelocity += Vector3.up * Physics.gravity.y * (gravityMultiplier - 1) * Time.fixedDeltaTime;
        }
    }
    public void StartGame()
    {
        currentSpeed = forwardSpeed;
        gameActive = true;
        score = 0;
        PlaceInitialObstacles();
        StartCoroutine(IncreaseSpeedOverTime(3f)); // Increase speed every 10 seconds
    }
    
    public void PlaceInitialObstacles()
    {
        // Ensure we have prefabs to spawn
        if (groundObstaclePrefab == null || groundObstaclePrefab.Count == 0)
        {
            Debug.LogError("No ground obstacle prefabs assigned!");
            return;
        }
        if (floatObstaclePrefab == null || floatObstaclePrefab.Count == 0)
        {
            Debug.LogWarning("No floating obstacle prefabs assigned. All obstacles will be ground type.");
        }
        
        int maxConsecutiveFloats = 2;
        int consecutiveFloatCount = 0;
        
        // Use a running Z cursor so we can add extra spacing after floats
        float zCursor = obstacleSpawnDistance; // start at 1x base distance
        
        // Place a few obstacles at the start of the game
        for (int i = 1; i <= obstacleInitialSpawnQuantity; i++)
        {
            // Decide type: 0 = float, 1 = ground (first obstacle forced to ground)
            bool wantFloat = UnityEngine.Random.Range(0, 2) == 0 && i != 1 && (floatObstaclePrefab != null && floatObstaclePrefab.Count > 0);
            
            // Enforce rule: no more than two floating platforms back-to-back
            if (wantFloat && consecutiveFloatCount >= maxConsecutiveFloats)
            {
                wantFloat = false;
            }
            
            if (wantFloat)
            {
                Vector3 spawnPos = obstacleSpawnPoint.position + new Vector3(0, 2.29f, zCursor);
                int prefabIndex = UnityEngine.Random.Range(0, floatObstaclePrefab.Count);
                Instantiate(floatObstaclePrefab[prefabIndex], spawnPos, Quaternion.identity);
                consecutiveFloatCount++;
                
                // Advance base spacing and add extra spacing after a floating obstacle
                zCursor += obstacleSpawnDistance + Mathf.Max(0f, extraSpacingAfterFloat);
            }
            else
            {
                Vector3 spawnPos = obstacleSpawnPoint.position + new Vector3(0, 1.1f, zCursor);
                int prefabIndex = UnityEngine.Random.Range(0, groundObstaclePrefab.Count);
                Instantiate(groundObstaclePrefab[prefabIndex], spawnPos, Quaternion.identity);
                consecutiveFloatCount = 0; // reset on ground spawn
                
                // Advance base spacing
                zCursor += obstacleSpawnDistance;
            }
        }
    }
    
    
    public void Slide()
    {
        if (isSliding || !gameActive) return;
        
        isSliding = true;
        slideStartTime = Time.time;
        slideCoroutineHandle = StartCoroutine(SlideCoroutine());
    }
    
    private void BreakOutOfSlide(string reason = "", bool snapImmediately = true)
    {
        if (!isSliding) return;
        
        if (slideCoroutineHandle != null)
        {
            StopCoroutine(slideCoroutineHandle);
            slideCoroutineHandle = null;
        }
        
        // Mark slide ended
        isSliding = false;
        
        if (snapImmediately)
        {
            // Snap back to standing posture immediately
            Vector3 currentPos = playerRigidbody.transform.localPosition;
            playerRigidbody.transform.localPosition = new Vector3(currentPos.x, standingHeight, currentPos.z);
            playerRigidbody.transform.localRotation = Quaternion.Euler(standingRotation, 0, 0);
        }
        else
        {
            // For immediate jump: don't change Y before applying force; optionally reset rotation only
            playerRigidbody.transform.localRotation = Quaternion.Euler(standingRotation, 0, 0);
        }
        // Debug.Log($"Slide cancelled: {reason}");
    }

    private IEnumerator StandUpAfterJump()
    {
        // Wait one physics step so the jump impulse is applied first
        yield return new WaitForFixedUpdate();
        // Then ensure standing posture (rotation only; do not clamp Y height)
        playerRigidbody.transform.localRotation = Quaternion.Euler(standingRotation, 0, 0);
    }

    public void Jump()
    {
        Debug.Log("Jump");
        if (!gameActive) return;
        
        bool cancelledSlideThisFrame = false;
        if (isSliding)
        {
            cancelledSlideThisFrame = true;
            // Do not snap Y yet; we want the jump to occur without losing ground contact due to Y teleport
            BreakOutOfSlide("Jump pressed", snapImmediately: false);
        }
        
        if (!cancelledSlideThisFrame && (Time.time - lastJumpTime < jumpCooldown))
        {
            Debug.Log("Jump on cooldown");
            return;
        }
        
        // Allow jump if grounded OR we just cancelled a slide (to cover 1-frame groundCheck false due to posture change)
        bool canJump = IsGrounded() || cancelledSlideThisFrame;
        if (canJump)
        {
            Debug.Log("Jump executed");
            
            // Reset Y velocity before applying jump force to prevent stacking
            Vector3 velocity = playerRigidbody.linearVelocity;
            velocity.y = 0f;
            playerRigidbody.linearVelocity = velocity;
            
            // Apply jump force
            playerRigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            
            // Track last jump time
            lastJumpTime = Time.time;
            
            // Update grounded state immediately
            isGrounded = false;
            
            // Ensure we stand up right after the impulse is applied
            StartCoroutine(StandUpAfterJump());
        }
        else
        {
            Debug.Log("Not grounded - cannot jump");
        }
    }
    
    IEnumerator SlideCoroutine()
    {
        // Transition to sliding position
        Vector3 startPos = playerRigidbody.transform.localPosition;
        Vector3 targetSlidePos = new Vector3(startPos.x, slidingHeight, startPos.z);
        Quaternion startRot = playerRigidbody.transform.localRotation;
        Quaternion targetSlideRot = Quaternion.Euler(slidingRotation, 0, 0);
        
        // Speed-scaled transition duration: faster at higher forward speeds
        float baseDuration = 0.2f;    // default feel
        float minDuration = 0.08f;    // don't go too snappy
        // Map currentSpeed 0..15 -> 0..1 and lerp duration
        float speedFactor = Mathf.Clamp01(currentSpeed / 15f);
        float transitionDuration = Mathf.Lerp(baseDuration, minDuration, speedFactor);
        
        float elapsedTime = 0f;
        
        // Smoothly transition INTO slide
        while (elapsedTime < transitionDuration)
        {
            if (!isSliding) yield break;
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            
            playerRigidbody.transform.localPosition = Vector3.Lerp(startPos, targetSlidePos, t);
            playerRigidbody.transform.localRotation = Quaternion.Lerp(startRot, targetSlideRot, t);
            
            yield return null;
        }
        
        if (!isSliding) yield break;
        
        // Ensure we're exactly at sliding position
        playerRigidbody.transform.localPosition = targetSlidePos;
        playerRigidbody.transform.localRotation = targetSlideRot;
        
        // Hold the slide position for the duration (keep total slide duration the same)
        float holdTime = Mathf.Max(0f, slideDuration - transitionDuration);
        float holdElapsed = 0f;
        while (holdElapsed < holdTime)
        {
            if (!isSliding) yield break;
            holdElapsed += Time.deltaTime;
            yield return null;
        }
        
        // Transition back to standing position using the same speed-scaled duration
        Vector3 currentPos2 = playerRigidbody.transform.localPosition;
        Vector3 targetStandPos = new Vector3(currentPos2.x, standingHeight, currentPos2.z);
        Quaternion currentRot2 = playerRigidbody.transform.localRotation;
        Quaternion targetStandRot = Quaternion.Euler(standingRotation, 0, 0);
        
        elapsedTime = 0f;
        
        while (elapsedTime < transitionDuration)
        {
            if (!isSliding) yield break;
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            
            playerRigidbody.transform.localPosition = Vector3.Lerp(currentPos2, targetStandPos, t);
            playerRigidbody.transform.localRotation = Quaternion.Lerp(currentRot2, targetStandRot, t);
            
            yield return null;
        }
        
        // Ensure we're exactly at standing position
        playerRigidbody.transform.localPosition = targetStandPos;
        playerRigidbody.transform.localRotation = targetStandRot;
        
        isSliding = false;
        slideCoroutineHandle = null;
    }
    
    public bool IsGrounded()
    {
        if (groundCheck == null)
        {
            Debug.LogError("Ground Check is null!");
            return false;
        }
        
        // Check for ground collision
        bool grounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        
        // Optional: Visual debugging - you can disable this in production
        Debug.DrawRay(groundCheck.position, Vector3.down * groundCheckRadius, grounded ? Color.green : Color.red);
        
        return grounded;
    }
    
    public bool CheckIsPlayerTooSlow()
    {
        return playerRigidbody.linearVelocity.z < 2f;
    }
    
    public void KillPlayer()
    {
        gameActive = false;
        currentSpeed = 0f;
        StopAllCoroutines();
        Debug.Log("Game Over! Final Score: " + score);
        if (minigameCanvasUI != null)
        {
            minigameCanvasUI.ShowGameOver();
        }
    }
    
    IEnumerator IncreaseSpeedOverTime(float interval)
    {
        while (gameActive)
        {
            yield return new WaitForSeconds(interval);
            forwardSpeed += 1f;
            currentSpeed = forwardSpeed;
        }
    }
}
