using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.InputSystem;
using Sequence = DG.Tweening.Sequence;

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
    public bool weightSelected = false;
    public GameObject bar,armModel;
    [SerializeField]private Transform barBellPosition,barObjectInitialPosition;
    private Vector3 armInitialPosition,barBellInitialPosition,barObjectInitialPositionVector;
    private Quaternion armInitialRotation,barObjectInitialRotation;
    
    
    [Header("Phase 0 - Weight Selection Settings")]
    public List<int> availableWeights = new List<int>() {20, 40, 60, 80, 100,120};
    public int selectedWeightIndex = 0;
    public float selectedWeight
    {
        get { return availableWeights[selectedWeightIndex]; }
    }
    public List<GameObject> weightModels; // Visual representations of weights
    
    public class WeightSelectionChangedEvent : UnityEngine.Events.UnityEvent<int>{};
    public WeightSelectionChangedEvent onWeightSelectionChanged = new WeightSelectionChangedEvent();
    
    
    
    
    [Header("Phase 1 - Grip Settings")]
    public float gripPhaseDuration = 2f;
    public float gripCountdownDuration = 1f; // Countdown before accepting taps
    public float goMessageDuration = 2f; // How long to show "GO!" message
    public int gripTapsRequired = 10; // Base taps needed
    public float gripBarPosition; // 0 to 1
    public float gripBarTargetMin = 0.7f; // Green zone min
    public float gripBarTargetMax = 0.9f; // Green zone max
    public float gripBarIncreasePerTap = 0.1f;
    public float gripBarDecaySpeed = 0.15f; // How fast bar falls
    private bool gripPhaseReady; // True when countdown is complete
    private bool gripPhaseCompleted; // True when grip phase is done
    private float goMessageTimer; // Tracks time since GO message started
    Quaternion armStartRotation = Quaternion.Euler(225, 180, 0);
    Quaternion armTargetRotation = Quaternion.Euler(180, 180, 0);
    
    
    [Header("Phase 2 - Lift Settings")]
    public float liftPhaseDuration = 4f;
    public float liftCountdownDuration = 1f; // Countdown before accepting taps
    public float powerMeterSpeed = 0.5f; // Speed of oscillation
    public float powerMeterPosition; // 0 to 1
    public float liftTargetMin = 0.65f; // Green zone min
    public float liftTargetMax = 0.85f; // Green zone max
    public bool powerMeterGoingUp = true;
    public float acceptableMargin = 0.15f; // Margin for "good" lift
    private bool liftPhaseReady; // True when countdown is complete
    private bool liftPhaseCompleted; // True when lift phase is done
    
    [Header("Phase 3 - Hold Settings")]
    public float holdPhaseDuration = 3f;
    public float holdCountdownDuration = 1f; // Countdown before starting hold
    public float barTiltAngle; // -45 to +45 degrees (left to right)
    public float tiltDriftSpeed = 20f; // How fast bar tilts in random direction
    public float buttonPushAmount = 30f; // How much each button press corrects
    public float maxTiltAngle = 45f; // Max tilt before failure
    public float tiltDirectionChangeInterval = 1f; // How often tilt direction changes
    private bool holdPhaseReady; // True when countdown is complete
    private bool tiltingRight = true; // Which direction the bar is tilting
    
    [Header("Hold Phase UI")]
    public Button pushLeftButton;
    public Button pushRightButton;
    public RectTransform barImageTransform; // The bar image that rotates
    
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
    public TMP_Text phaseText;
    public TMP_Text weightText;
    public TMP_Text instructionText;
    public TMP_Text statsText;
    public TMP_Text liftsRemainingTxt;
    public GameObject gripPhaseUI;
    public GameObject liftPhaseUI;
    public GameObject holdPhaseUI;
    public GameObject resultsUI;
    
    [Header("Cameras")]
    public Camera gripCamera;
    public Camera liftCamera;
    public Camera holdCamera;
    public Camera weightSelectionCamera;
    public Camera phaseTransitionCamera;

    [Header("Transitions and Effects")]
    public float transitionDuration = 1f;
    public Transform lookAtBenchLocation;
    
    
    [Header("Timers")]
    // Timers
    private float phaseTimer;
    private float tiltChangeTimer;
    private bool hasReleasedInLiftPhase;
    private bool isPerfectLift;
    private bool isProcessingPhaseTransition; // Prevent multiple transitions

    private Tween currentDelayTween;
    
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
        armInitialPosition = armModel.transform.position;
        armInitialRotation = armModel.transform.rotation;
        barObjectInitialPosition = bar.transform;
        barObjectInitialPositionVector = bar.transform.position;
        barObjectInitialRotation = bar.transform.rotation;
        barBellInitialPosition = barBellPosition.position;
    }
    
    private void Update()
    {
        if (!gameActive) return;
        
        switch (currentLiftState)
        {
            case LiftState.WeightSelection:
                // Currently not implemented
                break;
            
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
        
        bool inputDetected = false;
        Vector2 inputPosition = Vector2.zero;

        // Check for touch input first (mobile)
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
        {
            inputDetected = true;
            inputPosition = ts.primaryTouch.position.ReadValue();
        }
        // Fall back to mouse input (desktop/editor)
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            inputDetected = true;
            inputPosition = Mouse.current.position.ReadValue();
        }
        
        // Handle tap input (only for Grip and Lift phases)
        if (inputDetected)
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
        isProcessingPhaseTransition = false;
        //StartNewLift();
        TransitionToWeightSelectionPhase();
    }
    public void ResetForNewLift()
    {
        ResetBarPosition();
        ResetBarRotation();
        SetArmRotationForGrip(); // Set to grip starting rotation (225, 180, 0) instead of target
        MoveArmToInitialPosition(.0f);
        gripPhaseReady = false;
        liftPhaseReady = false;
        holdPhaseReady = false;
        hasReleasedInLiftPhase = false;
        isPerfectLift = false;
        phaseTimer = 0f;
        goMessageTimer = 0f;
        isProcessingPhaseTransition = false;
    }
    public void StartNewLift()
    {
        ResetForNewLift();
        liftsRemainingTxt .text = "Attempts Left: " + (maxFailedAttempts - failedAttempts);
        if (failedAttempts >= maxFailedAttempts)
        {
            UpdatePhaseUI("FAILED!", "You have Failed\nAttempts remaining: " + (maxFailedAttempts - failedAttempts));

            EndGame();
            return;
        }
        
        // Start with Grip phase
        TransitionToGripPhase();
    }
    
    #region Phase 0 - WeightSelection
    // Currently not implemented, but could be added for player to choose starting weight
    private void TransitionToWeightSelectionPhase()
    {
        currentLiftState = LiftState.WeightSelection;
        phaseTimer = 0f;
       
        SwitchCamera(weightSelectionCamera);
        SetAllPhasesUIInactive();
        ClearAllOtherUI();
        HideArmModel();
        UpdatePhaseUI("PHASE 0: Weight Selection", "Choose your starting weight.");
    }
    
    public List<int> GetAvailableWeights()
    {
        return availableWeights;
    }
    
    public void SetSelectedWeight(int weight)
    {
        if (availableWeights.Contains(weight))
        {
            selectedWeightIndex = availableWeights.IndexOf(weight);
            currentWeight = weight;
            Debug.Log("Selected starting weight: " + currentWeight + "kg");
            weightSelected = true;
            onWeightSelectionChanged.Invoke(weight);
            UpdateUI();
           // StartNewLift();
        }
        else
        {
            Debug.LogWarning("Selected weight not available: " + weight + "kg");
        }
    }
    
    public bool ConfirmWeightSelection()
    {
        if (weightSelected)
        {
            // Kill any existing delay
            currentDelayTween?.Kill();

            // Update UI BEFORE the delay
            
            Debug.Log("Confirmed starting weight: " + currentWeight + "kg");

            // Then wait and start the lift
            /*currentDelayTween = DOVirtual.DelayedCall(2f, () =>
            {
                StartNewLift();
            });*/
            
            phaseTransitionCamera.transform.position = weightSelectionCamera.transform.position;
            SwitchCamera(phaseTransitionCamera);
            Sequence transitionSequence = DOTween.Sequence();
            
            transitionSequence.AppendCallback(() => UpdatePhaseUI("GET READY", "You selected " + currentWeight + "kg\nStarting in 3 seconds..."))
                .Append(DOVirtual.Float(3, 0, 3f, (countdown) =>
                {
                    int seconds = Mathf.CeilToInt(countdown);
                    UpdatePhaseUI("GET READY", "You selected " + currentWeight + "kg\nStarting in " + seconds + " seconds...");
                }))
                .Join(phaseTransitionCamera.transform.DOMove(lookAtBenchLocation.transform.position + new Vector3(0, 1, 0), 3f))
                .Join(phaseTransitionCamera.transform.DOLookAt(lookAtBenchLocation.position, 3f))
                .AppendCallback(() => UpdatePhaseUI("GO!", "Lift that weight!"))
                .Append(phaseTransitionCamera.transform.DOMove(gripCamera.transform.position , 1.5f))
                .Join(phaseTransitionCamera.transform.DORotate(gripCamera.transform.rotation.eulerAngles, 3f))
                .AppendInterval(1f)
                .AppendCallback(() => StartNewLift());
            
        
            return true;
        }
        else
        {
            Debug.LogWarning("No weight selected to confirm.");
            return false;
        }
    }
    
    
    #endregion
    
    #region Phase 1 - Grip
    
    private void TransitionToGripPhase()
    {
        currentLiftState = LiftState.Grip;
        phaseTimer = 0f;
        gripBarPosition = 0f;
        gripPhaseReady = false;
        gripPhaseCompleted = false;
        
        SwitchCamera(gripCamera);
        ShowArmModel();
        MoveArmToInitialPosition(.0f);
        MoveArmToWardsPosition(armInitialPosition + new Vector3(0, 0.24f, 0),.3f);
        SetArmRotationForGrip();
        SetAllPhasesUIInactive();
        if (gripPhaseUI) gripPhaseUI.SetActive(true);
        
        UpdatePhaseUI("PHASE 1: GRIP", "Get ready...");
        UpdateUI();
    }
    
    private void UpdateGripPhase()
    {
        phaseTimer += Time.deltaTime;
        
        // Handle countdown at start of phase
        if (!gripPhaseReady)
        {
            if (phaseTimer > gripCountdownDuration)
            {
                gripPhaseReady = true;
                goMessageTimer = 0f; // Start GO message timer
                UpdatePhaseUI("PHASE 1: GRIP", "GO!\nTap rapidly to grip the bar!");
                phaseTimer = 0f; // Reset timer for grip duration
            }
            else
            {
                // Show countdown
                int countdown = Mathf.CeilToInt(gripCountdownDuration - phaseTimer);
                UpdatePhaseUI("PHASE 1: GRIP", countdown.ToString()+"\n Tap rapidly to grip the bar!");
            }
            return;
        }
        
        // Update GO message timer and show instructions after GO message ends
        if (goMessageTimer < goMessageDuration)
        {
            goMessageTimer += Time.deltaTime;
            if (goMessageTimer >= goMessageDuration)
            {
                UpdatePhaseUI("PHASE 1: GRIP", "Tap rapidly to grip the bar!");
            }
        }
        
        // Decay grip bar over time
        if(!gripPhaseCompleted)
        {
            gripBarPosition -= gripBarDecaySpeed * Time.deltaTime;
            gripBarPosition = Mathf.Clamp01(gripBarPosition);
    
            // Interpolate arm rotation based on grip bar position
            // Use LerpUnclamped so arm continues rotating beyond target when gripBarPosition > gripBarTargetMin
            float normalizedPosition = gripBarPosition / gripBarTargetMin;
            armModel.transform.rotation = Quaternion.LerpUnclamped(armStartRotation, armTargetRotation, normalizedPosition);
        }
        // Update UI
        if (gripBar) gripBar.value = gripBarPosition;
        
        // Check if time is up
        if (phaseTimer >= gripPhaseDuration)
        {
            if (isProcessingPhaseTransition) return;
            isProcessingPhaseTransition = true;
            
            // Check if grip is successful
            if (gripBarPosition >= gripBarTargetMin && gripBarPosition <= gripBarTargetMax)
            {
                gripPhaseCompleted = true;
                UpdatePhaseUI("PHASE 1: GRIP", "Grip successful!\nPreparing to lift...");
                
                /*currentDelayTween?.Kill();*/
                
                /*currentDelayTween = DOVirtual.DelayedCall(2f, () =>
                {
                    
                });*/
                
                
                Sequence transitionSequence = DOTween.Sequence();
                transitionSequence
                    .Append(bar.transform.DOMove(bar.transform.position + new Vector3(0f, 0f, -0.10f), 1f).SetRelative(false))
                    .AppendCallback(() => { }) // Force evaluation
                    .Append(bar.transform.DOMove(new Vector3(0f, -0.10f, 0), 1f).SetRelative(true))
                    .Append(bar.transform.DOMove(new Vector3(0f, 0f, 0.10f), 1f).SetRelative(true))
                    .AppendInterval(1f)
                    .AppendCallback(() => TransitionToLiftPhase());
                
                Debug.Log("Grip successful!");
                

                if (AudioManager.instance != null)
                {
                    AudioManager.instance.barGrip.start();
                }
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
        if (!gripPhaseReady || gripPhaseCompleted) return;
        
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
        liftPhaseReady = false; // Start with countdown
        hasReleasedInLiftPhase = false;
        isPerfectLift = false;
        isProcessingPhaseTransition = false;
        liftPhaseCompleted = false;
        SwitchCamera(liftCamera);
        SetAllPhasesUIInactive();
        if (liftPhaseUI) liftPhaseUI.SetActive(true);
        
        UpdatePhaseUI("PHASE 2: LIFT", "Get ready...");
        UpdateUI();
    }
    
    private void UpdateLiftPhase()
    {
        phaseTimer += Time.deltaTime;
        
        // Handle countdown at start of phase
        if (!liftPhaseReady)
        {
            if (phaseTimer > liftCountdownDuration)
            {
                liftPhaseReady = true;
                goMessageTimer = 0f; // Start GO message timer
                UpdatePhaseUI("PHASE 2: LIFT", "GO!\nTap when the meter hits the green zone!");
                phaseTimer = 0f; // Reset timer for lift duration
            }
            else
            {
                // Show countdown
                int countdown = Mathf.CeilToInt(liftCountdownDuration - phaseTimer);
                UpdatePhaseUI("PHASE 2: LIFT", countdown.ToString() +"\n Tap when the meter hits the green zone!");
            }
            return;
        }
        
        // Update GO message timer and show instructions after GO message ends
        if (goMessageTimer < goMessageDuration)
        {
            goMessageTimer += Time.deltaTime;
            if (goMessageTimer >= goMessageDuration)
            {
                UpdatePhaseUI("PHASE 2: LIFT", "Tap when the meter hits the green zone!");
            }
        }
        
        // Oscillate power meter (gets faster with heavier weights)
        float weightSpeedMultiplier = 1f + (currentWeight / maxWeight); // 1.0 to 2.0
        float currentSpeed = powerMeterSpeed * weightSpeedMultiplier;
        
        if (powerMeterGoingUp)
        {
            if(liftPhaseCompleted) return;
            powerMeterPosition += currentSpeed * Time.deltaTime;
            if (powerMeterPosition >= 1f)
            {
                powerMeterPosition = 1f;
                powerMeterGoingUp = false;
            }
        }
        else
        {
            if(liftPhaseCompleted) return;
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
            if (isProcessingPhaseTransition) return;
            isProcessingPhaseTransition = true;
            FailLift("Failed to lift in time!");
        }
    }
    
    private void HandleLiftTap()
    {
        if (hasReleasedInLiftPhase || !liftPhaseReady || isProcessingPhaseTransition || liftPhaseCompleted) return;
        
        hasReleasedInLiftPhase = true;
        isProcessingPhaseTransition = true;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.grunt.start();
        }

        // Check if in perfect zone
        if (powerMeterPosition >= liftTargetMin && powerMeterPosition <= liftTargetMax)
        {
            Debug.Log("Perfect lift timing!");
            isPerfectLift = true;
            liftPhaseCompleted = true;
            UpdatePhaseUI("PHASE 2: LIFT", "Perfect lift!\nPreparing to hold...");
            /*currentDelayTween?.Kill();
            
            currentDelayTween = DOVirtual.DelayedCall(1f, () =>
            {
                TransitionToHoldPhase();
            });*/
            
            DoLiftThenProgressToHold();
            
            
        }
        // Check if in acceptable margin
        else if (powerMeterPosition >= (liftTargetMin - acceptableMargin) && 
                 powerMeterPosition <= (liftTargetMax + acceptableMargin))
        {
            Debug.Log("Good lift timing!");
            isPerfectLift = false;
            liftPhaseCompleted = true;
            UpdatePhaseUI("PHASE 2: LIFT", "Good Enough!\nPreparing to hold...");
            DoLiftThenProgressToHold();
        }
        else
        {
            Debug.Log("Poor lift timing!");
            FailLift("Missed the timing window!");
        }
    }
    
    public void DoLiftThenProgressToHold()
    {
        Sequence transitionSequence = DOTween.Sequence();
            
        transitionSequence
            .Append(bar.transform.DOMove(bar.transform.position + new Vector3(0f,-0.10f, 0f), 1f).SetRelative(false))
            .AppendCallback(() => { }) // Force evaluation
            .Append(bar.transform.DOMove(new Vector3(0f, 0.10f, 0), 1f).SetRelative(true))
            .AppendInterval(1f)
            .AppendCallback(() => TransitionToHoldPhase());
    }
    
    #endregion
    
    #region Phase 3 - Hold
    
    private void TransitionToHoldPhase()
    {
        currentLiftState = LiftState.Hold;
        phaseTimer = 0f;
        tiltChangeTimer = 0f;
        barTiltAngle = 0f;
        holdPhaseReady = false;
        tiltingRight = Random.Range(0, 2) == 0;
        isProcessingPhaseTransition = false;
        
        SwitchCamera(holdCamera);
        SetAllPhasesUIInactive();
        if (holdPhaseUI) holdPhaseUI.SetActive(true);
        
        // Set up button listeners
        if (pushLeftButton != null)
        {
            pushLeftButton.onClick.RemoveAllListeners();
            pushLeftButton.onClick.AddListener(OnPushLeftButton);
        }
        
        if (pushRightButton != null)
        {
            pushRightButton.onClick.RemoveAllListeners();
            pushRightButton.onClick.AddListener(OnPushRightButton);
        }
        
        UpdatePhaseUI("PHASE 3: HOLD", "Get ready...");
        UpdateUI();
    }
    
    private void UpdateHoldPhase()
    {
        if (!gameActive) return;
        
        phaseTimer += Time.deltaTime;
        
        // Handle countdown at start of phase
        if (!holdPhaseReady)
        {
            if (phaseTimer > holdCountdownDuration)
            {
                holdPhaseReady = true;
                goMessageTimer = 0f; // Start GO message timer
                UpdatePhaseUI("PHASE 3: HOLD", "GO!\nUse buttons to keep the bar balanced!");
                phaseTimer = 0f; // Reset timer for hold duration
            }
            else
            {
                // Show countdown
                int countdown = Mathf.CeilToInt(holdCountdownDuration - phaseTimer);
                UpdatePhaseUI("PHASE 3: HOLD", countdown.ToString());
            }
            return;
        }
        
        // Update GO message timer and show instructions after GO message ends
        if (goMessageTimer < goMessageDuration)
        {
            goMessageTimer += Time.deltaTime;
            if (goMessageTimer >= goMessageDuration)
            {
                UpdatePhaseUI("PHASE 3: HOLD", "Use buttons to keep the bar balanced!");
            }
        }
        
        tiltChangeTimer += Time.deltaTime;
        
        // Randomly change tilt direction
        if (tiltChangeTimer >= tiltDirectionChangeInterval)
        {
            tiltChangeTimer = 0f;
            tiltingRight = Random.Range(0, 2) == 0;
            Debug.Log("Bar tilt direction changed: " + (tiltingRight ? "Right" : "Left"));
        }
        
        // Apply tilt based on weight (heavier = faster tilt)
        float weightTiltMultiplier = 1f + (currentWeight / maxWeight);
        float currentTiltSpeed = tiltDriftSpeed * weightTiltMultiplier;
        
        // Tilt the bar left or right
        if (tiltingRight)
        {
            barTiltAngle += currentTiltSpeed * Time.deltaTime;
            //isDumbellSlideAudioPlaying = true;
            PlayBarSlideAudioFunction();
        }
        else
        {
            barTiltAngle -= currentTiltSpeed * Time.deltaTime;
            //isDumbellSlideAudioPlaying = true;
            PlayBarSlideAudioFunction();
        }
        
        // Clamp the angle
        barTiltAngle = Mathf.Clamp(barTiltAngle, -maxTiltAngle - 10f, maxTiltAngle + 10f);
        
        // Update the visual bar rotation
        if (barImageTransform != null)
        {
            barImageTransform.localRotation = Quaternion.Euler(0f, 0f, -barTiltAngle);
            bar.transform.localRotation = Quaternion.Euler(0f, 0f, barTiltAngle);
        }
        
        // Check for failure (tilted too far)
        if (Mathf.Abs(barTiltAngle) > maxTiltAngle)
        {
            if (isProcessingPhaseTransition) return;
            isProcessingPhaseTransition = true;
            
            Debug.Log("Balance failed! Tilt angle: " + barTiltAngle);
            currentLiftState = LiftState.Idle;
            FailLift("Lost balance! Bar tilted too far!");
            return;
        }
        
        // Check for success (survived the duration)
        if (phaseTimer >= holdPhaseDuration)
        {
            if (isProcessingPhaseTransition) return;
            isProcessingPhaseTransition = true;
            
            currentLiftState = LiftState.Idle;
            SuccessfulAnimationComplete();
        }
    }
    
    public void SuccessfulAnimationComplete()
    {
        Sequence transitionSequence = DOTween.Sequence();
        
        transitionSequence
            .Append(bar.transform.DORotate(new Vector3(0f, 0f, 0f), 1f).SetRelative(false))
            .Append(bar.transform.DOMove(bar.transform.position + new Vector3(0f,-0.10f, 0f), 1f).SetRelative(false))
            .AppendCallback(() => { }) // Force evaluation
            .Append(bar.transform.DOMove(new Vector3(0f, 0.10f, 0), 1f).SetRelative(true))
            .Append(bar.transform.DOMove(new Vector3(0f, 0f, -0.10f), 1f).SetRelative(true))
            .Append(bar.transform.DOMove(new Vector3(0f, 0.10f, 0), 1f).SetRelative(true))
            .Append(bar.transform.DOMove(new Vector3(0f, 0f, 0.10f), 1f).SetRelative(true))
            .AppendInterval(1f)
            .AppendCallback(() => SuccessfulLift());
    }
    
    public void OnPushLeftButton()
    {
        if (!gameActive || currentLiftState != LiftState.Hold || !holdPhaseReady) return;
        
        barTiltAngle -= buttonPushAmount;
        barTiltAngle = Mathf.Clamp(barTiltAngle, -maxTiltAngle - 10f, maxTiltAngle + 10f);
        Debug.Log("Pushed LEFT! Bar angle now: " + barTiltAngle);
    }
    
    public void OnPushRightButton()
    {
        if (!gameActive || currentLiftState != LiftState.Hold || !holdPhaseReady) return;
        
        barTiltAngle += buttonPushAmount;
        barTiltAngle = Mathf.Clamp(barTiltAngle, -maxTiltAngle - 10f, maxTiltAngle + 10f);
        Debug.Log("Pushed RIGHT! Bar angle now: " + barTiltAngle);
    }

    private void PlayBarSlideAudioFunction()
    {
        if (barTiltAngle > 10 | barTiltAngle < -10)
        {
            Debug.Log("Bar is sliding");

            if (AudioManager.instance != null)
            {
                AudioManager.instance.dumbbellSlide.start();
            }
        }
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
                // Hold phase uses buttons instead of tap input
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
            StartCoroutine(ShowFailureAndRetry(reason));
        }
    }
    
    private IEnumerator ShowSuccessAndContinue()
    {
        UpdatePhaseUI("SUCCESS!", isPerfectLift ? "Perfect lift! Weight increased!" : "Good lift! Weight increased!");
        yield return new WaitForSeconds(2f);
        StartNewLift();
    }
    
    private IEnumerator ShowFailureAndRetry(string reason)
    {
        UpdatePhaseUI("FAILED!", $"You have {reason}\nAttempts remaining: " + (maxFailedAttempts - failedAttempts));
        yield return new WaitForSeconds(2f);
        
        // Retry same weight
        StartNewLift();
    }
    
    private void EndGame()
    {
        gameActive = false;
        currentLiftState = LiftState.Idle;
        
        // Remove button listeners
        if (pushLeftButton != null)
        {
            pushLeftButton.onClick.RemoveAllListeners();
        }
        if (pushRightButton != null)
        {
            pushRightButton.onClick.RemoveAllListeners();
        }
        
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

    #region Arm Stuff
    
    public void ResetBarPosition()
    {
        bar.transform.position = barObjectInitialPositionVector;
    }
    public void ResetBarRotation()
    {
        bar.transform.rotation = barObjectInitialRotation;
    }
    
    public void HideArmModel()
    {
        armModel.SetActive(false);
    }
    
    public void ShowArmModel()
    {
        armModel.SetActive(true);
    }
    
    public void SetArmRotationToDefault()
    {
        armModel.transform.rotation = armInitialRotation;
    }
    
    public void SetArmRotationForGrip()
    {
        armModel.transform.rotation = armStartRotation;
    }
    
    public void MoveArmToWardsPosition(Vector3 targetPosition, float duration)
    {
        armModel.transform.DOMove(targetPosition, duration);
    }
    public void MoveArmToInitialPosition(float duration)
    {
        armModel.transform.DOMove(armInitialPosition, duration);
    }
    
    
    
    #endregion
    
    #region UI Updates
    
    private void UpdateUI()
    {
        if (weightText)
        {
            if (currentLiftState == LiftState.WeightSelection)
            {
                weightText.text = "Weight Selected " + selectedWeight + "kg";
            }
            else
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
    
    private void ClearAllOtherUI()
    {
        if (phaseText) phaseText.text = "";
        if (instructionText) instructionText.text = "";
        if (statsText) statsText.text = "";
        if( weightText) weightText.text = "";
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
        if (weightSelectionCamera) weightSelectionCamera.gameObject.SetActive(false);
        if (phaseTransitionCamera) phaseTransitionCamera.gameObject.SetActive(false);
        
        if (targetCamera) targetCamera.gameObject.SetActive(true);
    }
    
    #endregion
    
    public enum LiftState 
    {
        Idle,
        WeightSelection,
        Grip,
        Lift,
        Hold
    }
}

