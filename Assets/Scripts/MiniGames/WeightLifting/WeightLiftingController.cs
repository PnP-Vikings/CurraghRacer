using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class WeightLiftingController : MonoBehaviour
{
    public static WeightLiftingController Instance;
    
    [Header("Game State")]
    public LiftState currentLiftState = LiftState.Idle;
    public bool gameActive;
    
    [Header("Weight Settings")]
    public float currentWeight = 20f; // Starting weight in kg
    public float weightIncrement = 5f;
    public float maxWeight = 200f;
    
    [Header("Phase 1 - Grip Settings")]
    public float gripPhaseDuration = 2f;
    public int gripTapsRequired = 10; // Base taps needed
    public float gripBarPosition; // 0 to 1
    public float gripBarTargetMin = 0.7f; // Green zone min
    public float gripBarTargetMax = 0.9f; // Green zone max
    public float gripBarIncreasePerTap = 0.1f;
    public float gripBarDecaySpeed = 0.15f; // How fast bar falls
    
    [Header("Phase 2 - Lift Settings")]
    public float liftPhaseDuration = 4f;
    public float powerMeterSpeed = 0.5f; // Speed of oscillation
    public float powerMeterPosition; // 0 to 1
    public float liftTargetMin = 0.65f; // Green zone min
    public float liftTargetMax = 0.85f; // Green zone max
    public bool powerMeterGoingUp = true;
    public float acceptableMargin = 0.15f; // Margin for "good" lift
    
    [Header("Phase 3 - Hold Settings")]
    public float holdPhaseDuration = 3f;
    public float balancePosition = 50f; // 0 to 100, goal is to stay around 50
    public float balanceDriftSpeed = 15f; // How fast it drifts away from center
    public float tapCorrectionAmount = 20f; // How much each tap pushes toward center
    public float safeZoneMin = 30f; // Below this = fail
    public float safeZoneMax = 70f; // Above this = fail
    public float driftChangeInterval = 0.8f; // How often drift direction changes
    private bool driftingUp = true; // Which direction the balance is drifting
    
    [Header("Stats")]
    public int successfulReps;
    public int perfectLifts;
    public int failedAttempts;
    public int maxFailedAttempts = 3;
    public float maxWeightLifted;
    public int totalStrengthGained;
    
    [Header("UI References")]
    public Slider gripBar;
    public Image gripTargetZone;
    public Slider powerMeter;
    public Image powerTargetZone;
    public Slider tiltBar;
    public Image tiltBalanceZone;
    public TMP_Text phaseText;
    public TMP_Text weightText;
    public TMP_Text instructionText;
    public TMP_Text statsText;
    public GameObject gripPhaseUI;
    public GameObject liftPhaseUI;
    public GameObject holdPhaseUI;
    public GameObject resultsUI;
    
    [Header("Cameras")]
    public Camera gripCamera;
    public Camera liftCamera;
    public Camera holdCamera;
    
    // Timers
    private float phaseTimer;
    private float tiltChangeTimer;
    private bool hasReleasedInLiftPhase;
    private bool isPerfectLift;
    
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
        UpdateUI();
        SetAllPhasesUIInactive();
        StartGame();
    }
    
    private void Update()
    {
        if (!gameActive) return;
        
        switch (currentLiftState)
        {
            case LiftState.Idle:
                // Waiting to start
                break;
                
            case LiftState.Grip:
                UpdateGripPhase();
                break;
                
            case LiftState.Lift:
                UpdateLiftPhase();
                break;
                
            case LiftState.Hold:
                UpdateHoldPhase();
                break;
        }
        
        // Handle tap input
        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            HandleTapInput();
        }
    }
    
    public void StartGame()
    {
        gameActive = true;
        currentWeight = 20f;
        successfulReps = 0;
        perfectLifts = 0;
        failedAttempts = 0;
        maxWeightLifted = 0f;
        totalStrengthGained = 0;
        
        StartNewLift();
    }
    
    public void StartNewLift()
    {
        if (failedAttempts >= maxFailedAttempts)
        {
            EndGame();
            return;
        }
        
        // Start with Grip phase
        TransitionToGripPhase();
    }
    
    #region Phase 1 - Grip
    
    private void TransitionToGripPhase()
    {
        currentLiftState = LiftState.Grip;
        phaseTimer = 0f;
        gripBarPosition = 0f;
        
        SwitchCamera(gripCamera);
        SetAllPhasesUIInactive();
        if (gripPhaseUI) gripPhaseUI.SetActive(true);
        
        UpdatePhaseUI("PHASE 1: GRIP", "Tap rapidly to grip the bar!");
        UpdateUI();
    }
    
    private void UpdateGripPhase()
    {
        phaseTimer += Time.deltaTime;
        
        // Decay grip bar over time
        gripBarPosition -= gripBarDecaySpeed * Time.deltaTime;
        gripBarPosition = Mathf.Clamp01(gripBarPosition);
        
        // Update UI
        if (gripBar) gripBar.value = gripBarPosition;
        
        // Check if time is up
        if (phaseTimer >= gripPhaseDuration)
        {
            // Check if grip is successful
            if (gripBarPosition >= gripBarTargetMin && gripBarPosition <= gripBarTargetMax)
            {
                Debug.Log("Grip successful!");
                TransitionToLiftPhase();
            }
            else
            {
                Debug.Log("Failed to grip! Try again.");
                FailLift("Failed to establish proper grip!");
            }
        }
    }
    
    private void HandleGripTap()
    {
        // Increase grip bar based on weight (heavier = harder)
        float weightDifficulty = 1f - (currentWeight / maxWeight) * 0.5f; // 1.0 to 0.5
        gripBarPosition += gripBarIncreasePerTap * weightDifficulty;
        gripBarPosition = Mathf.Clamp01(gripBarPosition);
        
        Debug.Log("Grip tap! Bar position: " + gripBarPosition);
    }
    
    #endregion
    
    #region Phase 2 - Lift
    
    private void TransitionToLiftPhase()
    {
        currentLiftState = LiftState.Lift;
        phaseTimer = 0f;
        powerMeterPosition = 0f;
        powerMeterGoingUp = true;
        hasReleasedInLiftPhase = false;
        isPerfectLift = false;
        
        SwitchCamera(liftCamera);
        SetAllPhasesUIInactive();
        if (liftPhaseUI) liftPhaseUI.SetActive(true);
        
        UpdatePhaseUI("PHASE 2: LIFT", "Tap when the meter hits the green zone!");
        UpdateUI();
    }
    
    private void UpdateLiftPhase()
    {
        phaseTimer += Time.deltaTime;
        
        // Oscillate power meter (gets faster with heavier weights)
        float weightSpeedMultiplier = 1f + (currentWeight / maxWeight); // 1.0 to 2.0
        float currentSpeed = powerMeterSpeed * weightSpeedMultiplier;
        
        if (powerMeterGoingUp)
        {
            powerMeterPosition += currentSpeed * Time.deltaTime;
            if (powerMeterPosition >= 1f)
            {
                powerMeterPosition = 1f;
                powerMeterGoingUp = false;
            }
        }
        else
        {
            powerMeterPosition -= currentSpeed * Time.deltaTime;
            if (powerMeterPosition <= 0f)
            {
                powerMeterPosition = 0f;
                powerMeterGoingUp = true;
            }
        }
        
        // Update UI
        if (powerMeter) powerMeter.value = powerMeterPosition;
        
        // Auto-fail if time runs out without tapping
        if (phaseTimer >= liftPhaseDuration && !hasReleasedInLiftPhase)
        {
            FailLift("Failed to lift in time!");
        }
    }
    
    private void HandleLiftTap()
    {
        if (hasReleasedInLiftPhase) return; // Already tapped
        
        hasReleasedInLiftPhase = true;
        
        // Check if in perfect zone
        if (powerMeterPosition >= liftTargetMin && powerMeterPosition <= liftTargetMax)
        {
            Debug.Log("Perfect lift timing!");
            isPerfectLift = true;
            TransitionToHoldPhase();
        }
        // Check if in acceptable margin
        else if (powerMeterPosition >= (liftTargetMin - acceptableMargin) && 
                 powerMeterPosition <= (liftTargetMax + acceptableMargin))
        {
            Debug.Log("Good lift timing!");
            isPerfectLift = false;
            TransitionToHoldPhase();
        }
        else
        {
            Debug.Log("Poor lift timing!");
            FailLift("Missed the timing window!");
        }
    }
    
    #endregion
    
    #region Phase 3 - Hold
    
    private void TransitionToHoldPhase()
        barTiltAngle = 0f; // Start perfectly balanced (0 degrees)
        tiltingRight = Random.Range(0, 2) == 0; // Randomly pick initial tilt direction
        phaseTimer = 0f;
        tiltChangeTimer = 0f;
        balancePosition = 50f; // Start perfectly balanced in the center
        driftingUp = Random.Range(0, 2) == 0; // Randomly pick initial drift direction
        
        // Set up button listeners
        if (pushLeftButton != null)
        {
            pushLeftButton.onClick.RemoveAllListeners();
            pushLeftButton.onClick.AddListener(OnPushLeftButton);
        }
        
        if (pushRightButton != null)
        {
        // Randomly change tilt direction
        if (tiltChangeTimer >= tiltDirectionChangeInterval)
        }
        
            tiltingRight = Random.Range(0, 2) == 0;
            Debug.Log("Bar tilt direction changed: " + (tiltingRight ? "Right" : "Left"));
        if (holdPhaseUI) holdPhaseUI.SetActive(true);
        
        // Apply tilt based on weight (heavier = faster tilt)
        float weightTiltMultiplier = 1f + (currentWeight / maxWeight); // 1.0 to 2.0
        float currentTiltSpeed = tiltDriftSpeed * weightTiltMultiplier;
    
        // Tilt the bar left or right
        if (tiltingRight)
        phaseTimer += Time.deltaTime;
            barTiltAngle += currentTiltSpeed * Time.deltaTime;
        
        // Randomly change drift direction
        if (tiltChangeTimer >= driftChangeInterval)
            barTiltAngle -= currentTiltSpeed * Time.deltaTime;
            tiltChangeTimer = 0f;
            driftingUp = Random.Range(0, 2) == 0;
        // Clamp the angle
        barTiltAngle = Mathf.Clamp(barTiltAngle, -maxTiltAngle - 10f, maxTiltAngle + 10f);
        }
        // Update the visual bar rotation
        if (barImageTransform != null)
        {
            barImageTransform.localRotation = Quaternion.Euler(0f, 0f, -barTiltAngle); // Negative for correct rotation direction
        }
        float weightDriftMultiplier = 1f + (currentWeight / maxWeight); // 1.0 to 2.0
        // Check for failure (tilted too far)
        if (Mathf.Abs(barTiltAngle) > maxTiltAngle)
        // Drift away from center (50)
            Debug.Log("Balance failed! Tilt angle: " + barTiltAngle);
            FailLift("Lost balance! Bar tilted too far!");
            balancePosition += currentDriftSpeed * Time.deltaTime;
        }
        else
        {
            balancePosition -= currentDriftSpeed * Time.deltaTime;
        }
    // Button press methods for Hold phase
    public void OnPushLeftButton()
        balancePosition = Mathf.Clamp(balancePosition, 0f, 100f);
        if (currentLiftState != LiftState.Hold) return;
        
        // Push bar to the left (decrease angle)
        barTiltAngle -= buttonPushAmount;
        barTiltAngle = Mathf.Clamp(barTiltAngle, -maxTiltAngle - 10f, maxTiltAngle + 10f);
        Debug.Log("Pushed LEFT! Bar angle now: " + barTiltAngle);
    }
    
    public void OnPushRightButton()
    {
        if (currentLiftState != LiftState.Hold) return;
        
        // Push bar to the right (increase angle)
        barTiltAngle += buttonPushAmount;
        barTiltAngle = Mathf.Clamp(barTiltAngle, -maxTiltAngle - 10f, maxTiltAngle + 10f);
        Debug.Log("Pushed RIGHT! Bar angle now: " + barTiltAngle);
        {
            SuccessfulLift();
        }
    }
    
    private void HandleHoldTap()
    {
        // Tap pushes the balance back toward center (50)
        if (balancePosition > 50f)
        {
            // Above center, push it down toward 50
            balancePosition -= tapCorrectionAmount;
        }
        else if (balancePosition < 50f)
        {
            // Below center, push it up toward 50
            balancePosition += tapCorrectionAmount;
        }
        
        balancePosition = Mathf.Clamp(balancePosition, 0f, 100f);
                // Hold phase uses buttons instead of tap input
    }
    
    #endregion
    
    #region Input Handling
    
    private void HandleTapInput()
    {
        switch (currentLiftState)
        {
            case LiftState.Grip:
                HandleGripTap();
                break;
                
            case LiftState.Lift:
                HandleLiftTap();
                break;
                
            case LiftState.Hold:
                HandleHoldTap();
                break;
        }
    }
    
    #endregion
    
    #region Lift Results
    
    private void SuccessfulLift()
    {
        successfulReps++;
        if (isPerfectLift)
        {
            perfectLifts++;
        }
        
        if (currentWeight > maxWeightLifted)
        {
            maxWeightLifted = currentWeight;
        }
        
        // Calculate strength gain
        int strengthGain = CalculateStrengthGain();
        totalStrengthGained += strengthGain;
        
        Debug.Log("Successful lift! Weight: " + currentWeight + "kg, Strength gained: " + strengthGain);
        
        // Increase weight
        currentWeight += weightIncrement;
        currentWeight = Mathf.Min(currentWeight, maxWeight);
        
        // Reset for next lift
        StartCoroutine(ShowSuccessAndContinue());
    }
    
    private void FailLift(string reason)
    {
        failedAttempts++;
        Debug.Log("Lift failed! Reason: " + reason + " (Attempt " + failedAttempts + "/" + maxFailedAttempts + ")");
        
        if (failedAttempts >= maxFailedAttempts)
        {
            EndGame();
        }
        else
        {
            StartCoroutine(ShowFailureAndRetry());
        }
    }
    
    private IEnumerator ShowSuccessAndContinue()
    {
        UpdatePhaseUI("SUCCESS!", isPerfectLift ? "Perfect lift! Weight increased!" : "Good lift! Weight increased!");
        yield return new WaitForSeconds(2f);
        StartNewLift();
    }
    
    private IEnumerator ShowFailureAndRetry()
    {
        UpdatePhaseUI("FAILED!", "Attempts remaining: " + (maxFailedAttempts - failedAttempts));
        yield return new WaitForSeconds(2f);
        
        // Retry same weight
        StartNewLift();
    }
    
    private void EndGame()
    {
        gameActive = false;
        currentLiftState = LiftState.Idle;
        
        SetAllPhasesUIInactive();
        if (resultsUI) resultsUI.SetActive(true);
        
        Debug.Log("=== Training Complete ===");
        Debug.Log("Successful Reps: " + successfulReps);
        Debug.Log("Perfect Lifts: " + perfectLifts);
        Debug.Log("Max Weight: " + maxWeightLifted + "kg");
        Debug.Log("Total Strength Gained: " + totalStrengthGained);
        
        UpdateStatsDisplay();
    }
    
    private int CalculateStrengthGain()
    {
        // Base strength from weight
        int baseStrength = Mathf.RoundToInt(currentWeight / 5f);
        
        // Bonus for perfect lift
        if (isPerfectLift)
        {
            baseStrength = Mathf.RoundToInt(baseStrength * 1.5f);
        }
        
        return baseStrength;
    }
    
    #endregion
    
    #region UI Updates
    
    private void UpdateUI()
    {
        if (weightText)
        {
            weightText.text = "Weight: " + currentWeight + "kg";
        }
    }
    
    private void UpdatePhaseUI(string phase, string instruction)
    {
        if (phaseText)
        {
            phaseText.text = phase;
        }
        
        if (instructionText)
        {
            instructionText.text = instruction;
        }
    }
    
    private void UpdateStatsDisplay()
    {
        if (statsText)
        {
            statsText.text = "Training Complete!\n\n" +
                            "Successful Reps: " + successfulReps + "\n" +
                            "Perfect Lifts: " + perfectLifts + "\n" +
                            "Max Weight: " + maxWeightLifted + "kg\n" +
                            "Strength Gained: +" + totalStrengthGained;
        }
    }
    
    private void SetAllPhasesUIInactive()
    {
        if (gripPhaseUI) gripPhaseUI.SetActive(false);
        if (liftPhaseUI) liftPhaseUI.SetActive(false);
        if (holdPhaseUI) holdPhaseUI.SetActive(false);
        if (resultsUI) resultsUI.SetActive(false);
    }
    
    private void SwitchCamera(Camera targetCamera)
    {
        if (gripCamera) gripCamera.gameObject.SetActive(false);
        if (liftCamera) liftCamera.gameObject.SetActive(false);
        if (holdCamera) holdCamera.gameObject.SetActive(false);
        
        if (targetCamera) targetCamera.gameObject.SetActive(true);
    }
    
    #endregion
    
    public enum LiftState 
    {
        Idle,
        Grip,
        Lift,
        Hold
    }
}

