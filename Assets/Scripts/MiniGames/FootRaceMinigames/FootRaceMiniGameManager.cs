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
    public Vector3 lastSpawnedObstacleTransform;
   
    
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
    public float maxSlideDuration = 3f; // Maximum total slide duration
    public float slideTransitionSpeed = 10f; // How quickly to transition between positions
    private bool isSliding = false;
    private float slideStartTime;
    private float currentSlideEndTime; // Tracks when the current slide should end
    private Coroutine slideCoroutineHandle;

    private FMOD.Studio.EventInstance runningDup;
    
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
        
        SwipeGesture swipeGesture = GetComponent<SwipeGesture>();
        if (swipeGesture != null)
        {
            swipeGesture.OnSwipeUp += Jump;
            swipeGesture.OnSwipeDown += Slide;
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

        //if (AudioManager.instance != null)
        //{
        //    if (isSliding | !isGrounded)
        //    {
        //        AudioManager.instance.running.setParameterByName("Running Volume", 0.0f);
        //    }
        //    else
        //    {
        //        AudioManager.instance.running.setParameterByName("Running Volume", 1.0f);
        //    }
        //}

        if (isSliding | !isGrounded)
        {
            runningDup.setParameterByName("Running Volume", 0.0f);
        }
        else
        {
            runningDup.setParameterByName("Running Volume", 1.0f);
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

        //if (AudioManager.instance != null)
        //{
        //    AudioManager.instance.running.start();
        //}

        runningDup = FMODUnity.RuntimeManager.CreateInstance("event:/Foot Race/Running");
        runningDup.start();
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
            bool wantFloat = UnityEngine.Random.Range(0, 4) == 0 && i != 1 && (floatObstaclePrefab != null && floatObstaclePrefab.Count > 0);
            
            // Enforce rule: no more than two floating platforms back-to-back
            if (wantFloat && consecutiveFloatCount >= maxConsecutiveFloats)
            {
                wantFloat = false;
            }
            
            if (wantFloat)
            {
                Vector3 spawnPos = obstacleSpawnPoint.position + new Vector3(0, 2.4f, zCursor);
                int prefabIndex = UnityEngine.Random.Range(0, floatObstaclePrefab.Count);
                Instantiate(floatObstaclePrefab[prefabIndex], spawnPos, Quaternion.identity);
                consecutiveFloatCount++;
                lastSpawnedObstacleTransform = spawnPos;
                // Advance base spacing and add extra spacing after a floating obstacle
                zCursor += obstacleSpawnDistance + Mathf.Max(0f, extraSpacingAfterFloat);
            }
            else
            {
                Vector3 spawnPos = obstacleSpawnPoint.position + new Vector3(0, .45f, zCursor);
                int prefabIndex = UnityEngine.Random.Range(0, groundObstaclePrefab.Count);
                if (prefabIndex == 0)
                {
                    Instantiate(groundObstaclePrefab[prefabIndex], spawnPos, Quaternion.Euler(0, -90, 0));
                }
                else
                {
                    Instantiate(groundObstaclePrefab[prefabIndex], spawnPos, Quaternion.identity);
                }
                consecutiveFloatCount = 0; // reset on ground spawn
                lastSpawnedObstacleTransform = spawnPos;
                // Advance base spacing
                zCursor += obstacleSpawnDistance;
            }
        }
    }
    
    public void MovePassedObstacleToEnd(Transform obstacleTransform, ObstacleType obstacleType)
    {
       if(obstacleType == ObstacleType.FloatingObstacle)
       {
           Vector3 newPos = new Vector3(lastSpawnedObstacleTransform.x, 2.4f, obstacleSpawnDistance +lastSpawnedObstacleTransform.z + Mathf.Max(0f, extraSpacingAfterFloat));
           obstacleTransform.position = newPos;
           lastSpawnedObstacleTransform = newPos;
       }
       else if(obstacleType == ObstacleType.GroundObstacle)
       {
           Vector3 newPos = new Vector3(lastSpawnedObstacleTransform.x, 1.1f, obstacleSpawnDistance +lastSpawnedObstacleTransform.z);
           obstacleTransform.position = newPos;
           lastSpawnedObstacleTransform = newPos;
       }
       obstacleTransform.gameObject.SetActive(true);
    }
    
    
    public void Slide()
    {
        if (!gameActive) return;
        
        // If already sliding, extend the slide duration
        if (isSliding)
        {
            float totalSlideTime = Time.time - slideStartTime;
            float remainingAllowedTime = maxSlideDuration - totalSlideTime;
            
            // Only extend if we haven't reached the max duration
            if (remainingAllowedTime > 0f)
            {
                // Extend by slideDuration or remaining time, whichever is smaller
                float extensionAmount = Mathf.Min(slideDuration, remainingAllowedTime);
                currentSlideEndTime += extensionAmount;
                Debug.Log($"Slide extended by {extensionAmount}s. Total slide time will be: {currentSlideEndTime - slideStartTime}s");
            }
            else
            {
                Debug.Log("Cannot extend slide - max duration reached");
            }
            return;
        }
        
        // Start new slide
        isSliding = true;
        slideStartTime = Time.time;
        currentSlideEndTime = Time.time + slideDuration; // Set initial end time
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
        float startY = startPos.y;
        
        // Smoothly transition INTO slide
        while (elapsedTime < transitionDuration)
        {
            if (!isSliding) yield break;
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            
            // Only control Y position, let X and Z be handled by physics/velocity
            Vector3 currentPos = playerRigidbody.transform.localPosition;
            float targetY = Mathf.Lerp(startY, slidingHeight, t);
            playerRigidbody.transform.localPosition = new Vector3(currentPos.x, targetY, currentPos.z);
            playerRigidbody.transform.localRotation = Quaternion.Lerp(startRot, targetSlideRot, t);
            
            yield return null;
        }
        
        if (!isSliding) yield break;
        
        // Ensure we're at exact sliding rotation
        playerRigidbody.transform.localRotation = targetSlideRot;
        
        // Hold the slide position until currentSlideEndTime (which can be extended dynamically)
        while (Time.time < currentSlideEndTime)
        {
            if (!isSliding) yield break;
            
            // Keep player at sliding Y height and rotation, but allow forward movement
            Vector3 currentPos = playerRigidbody.transform.localPosition;
            playerRigidbody.transform.localPosition = new Vector3(currentPos.x, slidingHeight, currentPos.z);
            playerRigidbody.transform.localRotation = targetSlideRot;
            
            yield return null;
        }
        
        if (!isSliding) yield break;
        
        // Transition back to standing position using the same speed-scaled duration
        Quaternion currentRot2 = playerRigidbody.transform.localRotation;
        Quaternion targetStandRot = Quaternion.Euler(standingRotation, 0, 0);
        
        elapsedTime = 0f;
        
        while (elapsedTime < transitionDuration)
        {
            if (!isSliding) yield break;
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            
            // Only control Y position during stand-up transition
            Vector3 currentPos = playerRigidbody.transform.localPosition;
            float targetY = Mathf.Lerp(slidingHeight, standingHeight, t);
            playerRigidbody.transform.localPosition = new Vector3(currentPos.x, targetY, currentPos.z);
            playerRigidbody.transform.localRotation = Quaternion.Lerp(currentRot2, targetStandRot, t);
            
            yield return null;
        }
        
        // Ensure we're exactly at standing rotation
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
public enum ObstacleType
{
    FloatingObstacle,
    GroundObstacle
}