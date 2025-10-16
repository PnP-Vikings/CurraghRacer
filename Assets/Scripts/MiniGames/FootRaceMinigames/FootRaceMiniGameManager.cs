using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FootRaceMiniGameManager : MonoBehaviour
{
    public static FootRaceMiniGameManager Instance;
    public Button jumpButton;
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
