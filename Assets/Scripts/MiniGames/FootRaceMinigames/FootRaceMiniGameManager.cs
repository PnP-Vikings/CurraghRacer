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
    public float laneWidth = 2f;
    public int currentLane = 1; // 0 = left, 1 = center, 2 = right
    public int totalLanes = 3;
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
    public GameObject[] obstaclePrefabs;
    public Transform obstacleSpawnPoint;
    public float obstacleSpawnDistance = 20f;
    public float obstacleDespawnDistance = -10f;
    public int score = 0;
    
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
        StartCoroutine(IncreaseSpeedOverTime(3f)); // Increase speed every 10 seconds
    }
    
    public void Slide()
    {
        if (isSliding || !gameActive) return;
        
        isSliding = true;
        slideStartTime = Time.time;
        StartCoroutine(SlideCoroutine());
    }
    
    IEnumerator SlideCoroutine()
    {
        // Transition to sliding position
        Vector3 startPos = playerRigidbody.transform.localPosition;
        Vector3 targetSlidePos = new Vector3(startPos.x, slidingHeight, startPos.z);
        Quaternion startRot = playerRigidbody.transform.localRotation;
        Quaternion targetSlideRot = Quaternion.Euler(slidingRotation, 0, 0);
        
        float elapsedTime = 0f;
        float transitionDuration = 0.2f; // Quick transition into slide
        
        // Smoothly transition INTO slide
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            
            playerRigidbody.transform.localPosition = Vector3.Lerp(startPos, targetSlidePos, t);
            playerRigidbody.transform.localRotation = Quaternion.Lerp(startRot, targetSlideRot, t);
            
            yield return null;
        }
        
        // Ensure we're exactly at sliding position
        playerRigidbody.transform.localPosition = targetSlidePos;
        playerRigidbody.transform.localRotation = targetSlideRot;
        
        // Hold the slide position for the duration
        yield return new WaitForSeconds(slideDuration - transitionDuration);
        
        
       
        // Transition back to standing position
        Vector3 currentPos = playerRigidbody.transform.localPosition;
        Vector3 targetStandPos = new Vector3(currentPos.x, standingHeight, currentPos.z);
        Quaternion currentRot = playerRigidbody.transform.localRotation;
        Quaternion targetStandRot = Quaternion.Euler(standingRotation, 0, 0);
        
        elapsedTime = 0f;
        
        // Smoothly transition OUT OF slide
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            
            playerRigidbody.transform.localPosition = Vector3.Lerp(currentPos, targetStandPos, t);
            playerRigidbody.transform.localRotation = Quaternion.Lerp(currentRot, targetStandRot, t);
            
            yield return null;
        }
        
        // Ensure we're exactly at standing position
        playerRigidbody.transform.localPosition = targetStandPos;
        playerRigidbody.transform.localRotation = targetStandRot;
        
        isSliding = false;
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
    
    public void Jump()
    {
        Debug.Log("Jump");
        if (!gameActive) return;
        
        // Check if enough time has passed since last jump
        if (Time.time - lastJumpTime < jumpCooldown)
        {
            Debug.Log("Jump on cooldown");
            return;
        }
        
        if (IsGrounded())
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
        }
        else
        {
            Debug.Log("Not grounded - cannot jump");
        }
    }

}
