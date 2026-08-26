using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using MiniGames;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class RowingRhythmController : MonoBehaviour
{
  private static readonly int IsRowing = Animator.StringToHash("isRowing");
  private static readonly int RowSpeed = Animator.StringToHash("RowSpeed");
  public Button leftButton, rightButton;
  private PlayerInputs playerInputs;
  
  public RectTransform leftPaddleTarget,leftPaddleSpawnPoint;
  public RectTransform rightPaddleTarget,rightPaddleSpawnPoint;
  
  public GameObject rightPaddlePrefab,leftPaddlePrefab;
  
  public RhythmCanvas rhythmCanvas;
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

  private bool hasStartedRowing = false;
  private bool rowingAudioHasStarted = false;

  [SerializeField] RowingGameAudio RowingGameAudio;

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
    StartCoroutine(IncreaseRowingSpeedOverTime(speedIncreaseInterval));
  }
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

  public void OnEnable()
  {
    
    playerInputs = new PlayerInputs();
    playerInputs.RythmGame.Enable();


    playerInputs.RythmGame.LeftPaddle.performed += OnLeftInput;
    playerInputs.RythmGame.RightPaddle.performed +=  OnRightInput;
  }
  
  private void OnRightInput(InputAction.CallbackContext context)
  {
    CheckPaddleOnRight();
  }

  private void OnLeftInput(InputAction.CallbackContext context)
  {
    CheckPaddleOnLeft();
  }

  public void OnDisable()
  {
    if (playerInputs != null)
    {
      playerInputs.RythmGame.Disable();
      playerInputs.RythmGame.LeftPaddle.performed -= OnLeftInput;
      playerInputs.RythmGame.RightPaddle.performed -= OnRightInput;
    }
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

    if (AudioManager.instance != null & hasStartedRowing & !rowingAudioHasStarted)
        {
            AudioManager.instance.rowing.start();
            rowingAudioHasStarted = true;
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
        ProcessHitBasedHitFeedback(PaddleHitResult.Miss);
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
        ProcessHitBasedHitFeedback(PaddleHitResult.Miss);
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
  
  private void HandleMissedPaddle(int scorePenalty = -5)
  {
    score += scorePenalty;
    currentLives--;
    Debug.Log("Lives remaining: " + currentLives);
    paddlesHitInRow = 0f; // Reset row counter on miss
    StopPaddleAnimations();
    
    if (currentLives <= 0)
    {
      EndGame();
    }

    if (RowingGameAudio != null)
    {  
        RowingGameAudio.PlayRowingGameFail();
    }
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
    
    if (MiniGameManager.Instance != null)
    {
      Debug.Log($"Calling MiniGameManager.CompleteGame with score: {score}");
      MiniGameManager.Instance.CompleteGame((int)Math.Round(score));
    }

    if (AudioManager.instance != null)
    {
        AudioManager.instance.miniGame_Over.start();
    }

    if (RowingGameAudio != null)
    {
        RowingGameAudio.rowingAmbienceEmitter.SetParameter("Rowing Game Ambient Encouragement", 0f, false);
        //Debug.Log("Rowing game ended, ambient encouragement muted - AudioDebug");
    }
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
      
      // Define hit zones
      float perfectRange = 20f;
      float goodRange = 50f;
      float acceptableRange = 100f;
      
      if (closestPaddle != null && closestDistance <= acceptableRange)
      {
        PaddleHitResult hitResult;
        int scoreGained = 0;
        
        // Determine hit quality based on distance
        if (closestDistance <= perfectRange)
        {
          hitResult = PaddleHitResult.Perfect;
        }
        else if (closestDistance <= goodRange)
        {
          // Check if paddle is above or below target to determine Early/Late
          float verticalDiff = closestPaddle.transform.position.y - leftPaddleTarget.position.y;
          if (verticalDiff > 0)
          {
            hitResult = PaddleHitResult.Early;
          }
          else
          {
            hitResult = PaddleHitResult.Late;
          }
        }
        else
        {
          // Too far but still within acceptable range
          float verticalDiff = closestPaddle.transform.position.y - leftPaddleTarget.position.y;
          hitResult = verticalDiff > 0 ? PaddleHitResult.Early : PaddleHitResult.Late;
          Debug.Log("Barely Hit! Distance: " + closestDistance);
        }
        scoreGained = ProcessHitBasedHitFeedback(hitResult, closestDistance,closestPaddle);
        // Apply hit
        score += scoreGained;
        activeLeftPaddle.RemoveAt(closestIndex);
        closestPaddle.SetActive(false);
        inactiveleftPaddle.Add(closestPaddle);
        
        if (hitResult == PaddleHitResult.Perfect)
        {
          paddlesHitInRow++;
        }
        else
        {
          paddlesHitInRow = 0; // Reset combo on non-perfect hits
        }
        
        AnimatePaddles();
        SpawnRandomPaddles();

        if (RowingGameAudio != null)
        {
           RowingGameAudio.PlayRowingGameSuccess();
        }
      }
      else
      {
        Debug.Log("Missed Left Paddle - Auto detected");
        if (closestPaddle != null)
        {
          ProcessHitBasedHitFeedback(PaddleHitResult.Miss);
          activeLeftPaddle.Remove(closestPaddle);

          closestPaddle.SetActive(false);
          inactiveleftPaddle.Add(closestPaddle);

          if (gameActive) // Only spawn new paddles if game is still active
          {
            SpawnRandomPaddles();
          }
        }

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
      
      // Define hit zones
      float perfectRange = 20f;
      float goodRange = 50f;
      float acceptableRange = 100f;
      
      if (closestPaddle != null && closestDistance <= acceptableRange)
      {
        PaddleHitResult hitResult;
        int scoreGained = 0;
        
        // Determine hit quality based on distance
        if (closestDistance <= perfectRange)
        {
          hitResult = PaddleHitResult.Perfect;
         
        }
        else if (closestDistance <= goodRange)
        {
          // Check if paddle is above or below target to determine Early/Late
          float verticalDiff = closestPaddle.transform.position.y - rightPaddleTarget.position.y;
          if (verticalDiff > 0)
          {
            hitResult = PaddleHitResult.Early;
          }
          else
          {
            hitResult = PaddleHitResult.Late;
          
          }
         
        }
        else
        {
          // Too far but still within acceptable range
          float verticalDiff = closestPaddle.transform.position.y - rightPaddleTarget.position.y;
          hitResult = verticalDiff > 0 ? PaddleHitResult.Early : PaddleHitResult.Late;
          Debug.Log("Barely Hit! Distance: " + closestDistance);
        }
        
        scoreGained = ProcessHitBasedHitFeedback(hitResult, closestDistance,closestPaddle);
        // Apply hit
        score += scoreGained;
        activeRightPaddle.RemoveAt(closestIndex);
        closestPaddle.SetActive(false);
        inactiveRightPaddle.Add(closestPaddle);
        
        if (hitResult == PaddleHitResult.Perfect)
        {
          paddlesHitInRow++;
        }
        else
        {
          paddlesHitInRow = 0; // Reset combo on non-perfect hits
        }
        
        AnimatePaddles();
        SpawnRandomPaddles();

        if (RowingGameAudio != null)
        {
           RowingGameAudio.PlayRowingGameSuccess();
        }
      }
      else
      {
        Debug.Log("Missed Right Paddle - Auto detected");
        if (closestPaddle != null)
        {
          ProcessHitBasedHitFeedback(PaddleHitResult.Miss);
          activeRightPaddle.Remove(closestPaddle);

          closestPaddle.SetActive(false);
          inactiveRightPaddle.Add(closestPaddle);

          if (gameActive) // Only spawn new paddles if game is still active
          {
            SpawnRandomPaddles();
          }
        }

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

    hasStartedRowing = true;

    if (AudioManager.instance != null)
    {
        AudioManager.instance.rowing.setParameterByName("Rowing Volume (Minigame)", 1f);
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

    if (AudioManager.instance != null)
    {
        AudioManager.instance.rowing.setParameterByName("Rowing Volume (Minigame)", 0f);
    }
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

  IEnumerator IncreaseRowingSpeedOverTime(float interval)
  {
    if (AudioManager.instance != null)
    {
            AudioManager.instance.rowing.getParameterByName("Rowing Speed", out float paramValue);

            while (gameActive & paramValue <= 1)
            {
                yield return new WaitForSeconds(interval);
                paramValue = paramValue + 0.5f;
                AudioManager.instance.rowing.setParameterByName("Rowing Speed", paramValue);
                Debug.Log("Rowing Speed " + paramValue);
            }
    } 
  }
  
  public int ProcessHitBasedHitFeedback(PaddleHitResult feedback, float closestDistance = 0f,GameObject paddle = null)
  {
    int scoreGained = 0;
    switch (feedback)
    {
      case PaddleHitResult.Miss:
        scoreGained = -5;
        HandleMissedPaddle(scoreGained);
        break;
      case PaddleHitResult.Early:
        scoreGained = 5;
        Debug.Log("Early Hit! Distance: " + closestDistance);
        break;
      case PaddleHitResult.Perfect:
        scoreGained = 10;
        Debug.Log("Perfect Hit! Distance: " + closestDistance);
        break;
      case PaddleHitResult.Late:
        scoreGained = 5;
        Debug.Log("Late Hit! Distance: " + closestDistance);
        break;
    } 
    ShowHitFeedback(feedback);
   return scoreGained;
  }
  
  public void ShowHitFeedback(PaddleHitResult feedback)
  {
    if (rhythmCanvas != null)
    {
      switch (feedback)
      {
        case PaddleHitResult.Miss:
          rhythmCanvas.ShowHitFeedback("Miss!", feedback, 1f);
          break;
        case PaddleHitResult.Early:
          rhythmCanvas.ShowHitFeedback("Early!", feedback, 1f);
          break;
        case PaddleHitResult.Perfect:
          rhythmCanvas.ShowHitFeedback("Perfect!", feedback, 1f);
          break;
        case PaddleHitResult.Late:
          rhythmCanvas.ShowHitFeedback("Late!", feedback, 1f);
          break;
      }
    }
  }
  
  private IEnumerator FlashPaddleColor(GameObject paddle, Color flashColor, float duration)
  {
    Image paddleImage = paddle.GetComponent<Image>();
    if (paddleImage != null)
    {
      Color originalColor = paddleImage.color;
      paddleImage.color = flashColor;
      yield return new WaitForSeconds(duration);
      paddleImage.color = originalColor;
    }
  }
}

public enum PaddleHitResult
{
  Miss,
  Early,
  Perfect,
  Late
}

