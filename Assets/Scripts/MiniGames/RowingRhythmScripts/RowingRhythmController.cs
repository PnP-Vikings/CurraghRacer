using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class RowingRhythmController : MonoBehaviour
{
  private static readonly int IsRowing = Animator.StringToHash("isRowing");
  private static readonly int RowSpeed = Animator.StringToHash("RowSpeed");
  public Button leftButton, rightButton;
  
  public RectTransform leftPaddleTarget,leftPaddleSpawnPoint;
  public RectTransform rightPaddleTarget,rightPaddleSpawnPoint;
  
  public GameObject rightPaddlePrefab,leftPaddlePrefab;
  
  public Canvas rowingRhythmCanvas;
  public MinigameCanvasUI minigameCanvasUI;
  
  public List<GameObject> inactiveRightPaddle;
  public List<GameObject> inactiveleftPaddle;
  public List<GameObject> activeRightPaddle;
  public List<GameObject> activeLeftPaddle;
  
  public List<Animator> paddleAnimations;
  
  public int spawnAmount = 30;
  public float startingSpeed = 200f; // Starting speed in units per second
  public float maxSpeed = 800f; // Maximum speed cap
  public float speedIncrement = 25f; // How much speed increases each interval
  public float speedIncreaseInterval = 2f; // How often speed increases (seconds)
  public float currentSpeed;
  public bool gameActive;
  public float score;
  public float paddlesHitInRow=0f;
  
  public int maxLives = 5; // Maximum lives before game over
  public int currentLives; // Current remaining lives

  private FMOD.Studio.EventInstance rowingGameSuccessDup;
  private FMOD.Studio.EventInstance rowingGameFailDup;

    private void PlaceInitialObstacles()
  {
    for (int i = 0; i < spawnAmount; i++)
    {
      SpawnRandomPaddle();
    }
    
    // Spawn initial set of paddles
    SpawnRandomPaddles();
  }
  
  public void SpawnRandomPaddle()
  {
    int ran = Random.Range(0,2);
    if (ran == 0)
    {
      GameObject paddle;
      paddle = Instantiate(rightPaddlePrefab, rightPaddleSpawnPoint.position, Quaternion.identity,rowingRhythmCanvas.transform);
      
      paddle.transform.position = rightPaddleSpawnPoint.position;
      inactiveRightPaddle.Add(paddle);
      paddle.gameObject.SetActive(false);
    }
    else
    {
      GameObject paddle;
      paddle = Instantiate(leftPaddlePrefab, leftPaddleSpawnPoint.position, Quaternion.identity,rowingRhythmCanvas.transform);
      paddle.transform.position = leftPaddleSpawnPoint.position;
      inactiveleftPaddle.Add(paddle);
      paddle.gameObject.SetActive(false);
    }
  }
  
  private void SpawnPaddle(bool isLeft, float yOffset = 0f)
  {
    if(isLeft)
    {
      GameObject paddle;
      if(inactiveleftPaddle.Count >0)
      {
        paddle = inactiveleftPaddle[0];
        inactiveleftPaddle.RemoveAt(0);
      }
      else
      {
        paddle = Instantiate(leftPaddlePrefab, leftPaddleSpawnPoint.position, Quaternion.identity, rowingRhythmCanvas.transform);
      }
      paddle.transform.position = leftPaddleSpawnPoint.position + new Vector3(0, yOffset, 0);
      activeLeftPaddle.Add(paddle);
      paddle.SetActive(true);
    }
    else
    {
      GameObject paddle;
      if(inactiveRightPaddle.Count >0)
      {
        paddle = inactiveRightPaddle[0];
        inactiveRightPaddle.RemoveAt(0);
      }
      else
      {
        paddle = Instantiate(rightPaddlePrefab, rightPaddleSpawnPoint.position, Quaternion.identity, rowingRhythmCanvas.transform);
      }
      paddle.transform.position = rightPaddleSpawnPoint.position + new Vector3(0, yOffset, 0);
      activeRightPaddle.Add(paddle);
      paddle.SetActive(true);
    }
  }
  
  // Spawns 1-2 paddles at once with spacing (focus on speed for difficulty)
  private void SpawnRandomPaddles()
  {
    // Mostly spawn 1 paddle, occasionally 2 for variety
    int numPaddles = Random.Range(0, 10) < 7 ? 1 : 2; // 70% chance of 1 paddle, 30% chance of 2
    float minSpacing = 200f; // Minimum vertical spacing between paddles
    
    for (int i = 0; i < numPaddles; i++)
    {
      bool isLeft = Random.Range(0, 2) == 0;
      float yOffset = FindSafeSpawnOffset(isLeft, minSpacing);
      minSpacing += 250f; // Increase spacing for next paddle if spawning multiple
      if(numPaddles != 1)
      {
        yOffset += i * 220f; // Slight additional offset for multiple paddles
      }
      SpawnPaddle(isLeft, yOffset);
    }
  }
  
  // Finds a safe spawn offset that doesn't overlap with existing paddles
  private float FindSafeSpawnOffset(bool isLeft, float minSpacing)
  {
    List<GameObject> activePaddles = isLeft ? activeLeftPaddle : activeRightPaddle;
    RectTransform spawnPoint = isLeft ? leftPaddleSpawnPoint : rightPaddleSpawnPoint;
    
    float baseYOffset = 0f;
    bool positionIsSafe = false;
    int maxAttempts = 10;
    int attempts = 0;
    
    while (!positionIsSafe && attempts < maxAttempts)
    {
      positionIsSafe = true;
      Vector3 testPosition = spawnPoint.position + new Vector3(0, baseYOffset, 0);
      
      // Check if this position is too close to any existing active paddle
      foreach (GameObject activePaddle in activePaddles)
      {
        float distance = Mathf.Abs(activePaddle.transform.position.y - testPosition.y);
        if (distance < minSpacing)
        {
          positionIsSafe = false;
          // Move the spawn position up to avoid overlap
          baseYOffset += minSpacing;
          break;
        }
      }
      
      attempts++;
    }
    
    return baseYOffset;
  }

  private void Start()
  {
    SwipeGesture swipeGesture = GetComponent<SwipeGesture>();
    if (swipeGesture != null)
    {
      swipeGesture.OnSwipeLeft += CheckPaddleOnLeft;
      swipeGesture.OnSwipeRight += CheckPaddleOnRight;
    }
    
    if(leftButton != null)
    {
      leftButton.onClick.AddListener(CheckPaddleOnLeft);
    }
    if(rightButton != null)
    {
      rightButton.onClick.AddListener(CheckPaddleOnRight);
    }
    
    StartGame();
  }
  
  private void Update()
  {
    if (gameActive)
    {
      // Move all active paddles downward
      foreach (GameObject leftPaddle in activeLeftPaddle)
      {
        leftPaddle.transform.position += Vector3.down * currentSpeed * Time.deltaTime;
      }

      foreach (GameObject rightPaddle in activeRightPaddle)
      {
        rightPaddle.transform.position += Vector3.down * currentSpeed * Time.deltaTime;
      }
      
      if (minigameCanvasUI != null)
      {
        minigameCanvasUI.UpdateScore( "Score: " + score);
        minigameCanvasUI.UpdateTimer(Time.timeSinceLevelLoad.ToString("F1") + "s");
        minigameCanvasUI.UpdatePlayerLives("Lives: " + currentLives);
      }
      // Check for missed paddles
      CheckForMissedPaddles();
    }
  }
  
  private void CheckForMissedPaddles()
  {
    // Check left paddles for misses
    for (int i = activeLeftPaddle.Count - 1; i >= 0; i--)
    {
      GameObject paddle = activeLeftPaddle[i];
      if (paddle.transform.position.y < leftPaddleTarget.position.y - 100f)
      {
        Debug.Log("Missed Left Paddle - Auto detected");
        HandleMissedPaddle();
        if(activeLeftPaddle == null || i >= activeLeftPaddle.Count) continue;
        activeLeftPaddle.RemoveAt(i);
        paddle.SetActive(false);
        inactiveleftPaddle.Add(paddle);
        
        if (gameActive) // Only spawn new paddles if game is still active
        {
          SpawnRandomPaddles();
        }
      }
    }
    
    // Check right paddles for misses
    for (int i = activeRightPaddle.Count - 1; i >= 0; i--)
    {
      GameObject paddle = activeRightPaddle[i];
      if (paddle.transform.position.y < rightPaddleTarget.position.y - 100f)
      {
        Debug.Log("Missed Right Paddle - Auto detected");
        HandleMissedPaddle();
        if(activeRightPaddle == null || i >= activeRightPaddle.Count) continue;
        activeRightPaddle.RemoveAt(i);
        paddle.SetActive(false);
        inactiveRightPaddle.Add(paddle);
        
        if (gameActive) // Only spawn new paddles if game is still active
        {
          SpawnRandomPaddles();
        }
      }
    }
  }
  
  private void HandleMissedPaddle()
  {
    score -= 5;
    currentLives--;
    Debug.Log("Lives remaining: " + currentLives);
    paddlesHitInRow = 0f; // Reset row counter on miss
    StopPaddleAnimations();
    
    if (currentLives <= 0)
    {
      EndGame();
    }

    //if (AudioManager.instance != null)
    //{
    //    AudioManager.instance.rowingGameFail.start();
    //}

    rowingGameFailDup = RuntimeManager.CreateInstance("event:/Rowing Rhythm Game/Fail");
    rowingGameFailDup.start();
   }
  
  private void EndGame()
  {
    gameActive = false;
    Debug.Log("Game Over! Final Score: " + score);
  
    
    // Clear all active paddles
    foreach (GameObject paddle in activeLeftPaddle)
    {
      paddle.SetActive(false);
      inactiveleftPaddle.Add(paddle);
    }
    activeLeftPaddle.Clear();
    
    foreach (GameObject paddle in activeRightPaddle)
    {
      paddle.SetActive(false);
      inactiveRightPaddle.Add(paddle);
    }
    activeRightPaddle.Clear();
    if (minigameCanvasUI != null)
    {
      minigameCanvasUI.UpdateScore( "Score: " + score);
      minigameCanvasUI.UpdateTimer(Time.timeSinceLevelLoad.ToString("F1") + "s");
      minigameCanvasUI.UpdatePlayerLives("Lives: " + currentLives);
    }
    StopPaddleAnimations();
    minigameCanvasUI.ShowGameOver();
  }
  
  private void CheckPaddleOnLeft()
  {
    if (activeLeftPaddle.Count > 0)
    {
      Debug.Log("Checking Left Paddle");
      
      // Find the closest paddle to the target
      GameObject closestPaddle = null;
      float closestDistance = float.MaxValue;
      int closestIndex = -1;
      
      for (int i = 0; i < activeLeftPaddle.Count; i++)
      {
        float distance = Vector3.Distance(activeLeftPaddle[i].transform.position, leftPaddleTarget.position);
        if (distance < closestDistance)
        {
          closestDistance = distance;
          closestPaddle = activeLeftPaddle[i];
          closestIndex = i;
        }
      }
      
      if (closestPaddle != null && closestDistance <= 50f) // Acceptable hit range
      {
        Debug.Log("Hit Left Paddle - Distance: " + closestDistance);
        // Successful hit
        score += 10;
        activeLeftPaddle.RemoveAt(closestIndex);
        closestPaddle.SetActive(false);
        inactiveleftPaddle.Add(closestPaddle);
        paddlesHitInRow++;
        AnimatePaddles();
        SpawnRandomPaddles(); // Spawn new random paddles

        //if (AudioManager.instance != null)
        //{
        //   AudioManager.instance.rowingGameSuccess.start();
        //}

        rowingGameSuccessDup = RuntimeManager.CreateInstance("event:/Rowing Rhythm Game/Success");
        rowingGameSuccessDup.start();
      }
      else
      {
        Debug.Log("Swipe too early or too late - Distance: " + closestDistance);
      }
    }
  }
  
  
  private void CheckPaddleOnRight()
  {
    if (activeRightPaddle.Count > 0)
    {
      Debug.Log("Checking Right Paddle");
      
      // Find the closest paddle to the target
      GameObject closestPaddle = null;
      float closestDistance = float.MaxValue;
      int closestIndex = -1;
      
      for (int i = 0; i < activeRightPaddle.Count; i++)
      {
        float distance = Vector3.Distance(activeRightPaddle[i].transform.position, rightPaddleTarget.position);
        if (distance < closestDistance)
        {
          closestDistance = distance;
          closestPaddle = activeRightPaddle[i];
          closestIndex = i;
        }
      }
      
      if (closestPaddle != null && closestDistance <= 50f) // Acceptable hit range
      {
        Debug.Log("Hit Right Paddle - Distance: " + closestDistance);
        // Successful hit
        score += 10;
        activeRightPaddle.RemoveAt(closestIndex);
        closestPaddle.SetActive(false);
        inactiveRightPaddle.Add(closestPaddle);
        SpawnRandomPaddles(); // Spawn new random paddles
        paddlesHitInRow++;
        AnimatePaddles();

        //if (AudioManager.instance != null)
        //{
        //   AudioManager.instance.rowingGameSuccess.start();
        //}

        rowingGameSuccessDup = RuntimeManager.CreateInstance("event:/Rowing Rhythm Game/Success");
        rowingGameSuccessDup.start();
      }
      else
      {
        Debug.Log("Swipe too early or too late - Distance: " + closestDistance);
      }
    }
  }
  
  private void StartAnimatePaddles()
  {
    foreach (Animator animator in paddleAnimations)
    {
      animator.SetBool(IsRowing, true);
      Debug.Log("Paddle animation started");
      animator.SetFloat(RowSpeed, 0f); // Initial rowing speed
    }
  }
  public void AnimatePaddles()
  {
    foreach (Animator animator in paddleAnimations)
    {
      animator.SetBool(IsRowing, true);
      Debug.Log("Paddle animation started");
      animator.SetFloat(RowSpeed, .3f+paddlesHitInRow/10f); // Increase rowing speed slightly with each successful hit in a row
    }
  }

  private void StopPaddleAnimations()
  {
    foreach (Animator animator in paddleAnimations)
    {
     // animator.SetBool(IsRowing, false);
      Debug.Log("Paddle animation stopped");
      animator.SetFloat(RowSpeed, 0f);
    }
  }
  
  private void StartGame()
  {
    if (minigameCanvasUI != null)
    {
      minigameCanvasUI.SetUpUI(true,true,true,true);
    }
    
    currentSpeed = startingSpeed;
    gameActive = true;
    score = 0;
    currentLives = maxLives; // Reset lives
    StartAnimatePaddles();
    PlaceInitialObstacles();
    StartCoroutine(IncreaseSpeedOverTime(speedIncreaseInterval));
  }
  
  IEnumerator IncreaseSpeedOverTime(float interval)
  {
    while(gameActive)
    {
      yield return new WaitForSeconds(interval);
      
      // Increase speed up to max speed cap
      if (currentSpeed < maxSpeed)
      {
        currentSpeed += speedIncrement;
        currentSpeed = Mathf.Min(currentSpeed, maxSpeed); // Cap at max speed
        Debug.Log("Speed increased to: " + currentSpeed);
      }
    }
  }
}

