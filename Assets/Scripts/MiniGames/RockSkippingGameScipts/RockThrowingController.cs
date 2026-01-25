using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// Handles rock throwing mechanics with three different modes:
/// A: Flick-based (swipe gesture)
/// B: Rhythm-based (throw and bounce on beat)
/// C: Oscillator-based (side-to-side aiming + power charge)
/// </summary>
public class RockThrowingController : MonoBehaviour
{
    public static RockThrowingController Instance { get; private set; }
    
    public enum ThrowingMode
    {
        Flick,      // Option A: Swipe gesture determines angle/power
        Rhythm,     // Option B: Throw on beat, bounce on beat
        Oscillator  // Option C: Side-to-side + timing (recommended)
    }
    
    [Header("Mode Selection")]
    public ThrowingMode currentMode = ThrowingMode.Oscillator;
    
    [Header("References")]
    public Rock currentRock;
    public Transform rockSpawnPoint;
    public Camera mainCamera;
    public RockThrowingUI throwingUI;
    [SerializeField] Camera followCamera;
    public CameraSmoothlyFollowGameObject cameraFollower;
    
    [Header("Camera Settings")]
    public Vector3 rockFollowOffset = new Vector3(0, 5f, 10f); // Offset when following rock
    private Vector3 originalCameraPosition;
    private bool wasFollowingRock = false;
    
    [Header("Throwing Settings")]
    public float minThrowPower = 15f;   // Balanced - enough to reach water
    public float maxThrowPower = 35f;  // Balanced - good distance but not too fast
    public float minThrowAngle = -15f; // Slight angle variance
    public float maxThrowAngle = 15f;  
    public float throwArcHeight = 5f;  // Nice arc to see the rock fly
    
   
    [Tooltip("Base direction for throwing. Set to (0,0,-1) if water is in -Z direction")]
    public Vector3 baseThrowDirection = new Vector3(0, 0, -1f); // Default to -Z (towards water)
    
    [Header("Oscillator Mode Settings (Option C)")]
    public float oscillatorSpeed = 2.5f;
    public float powerChargeSpeed = 1.2f;
    public float maxChargeTime = 2f;
    private float currentOscillatorValue = 0f;
    private float currentPowerCharge = 0f;
    private bool isCharging = false;
    private bool oscillatorActive = false;
    
    [Header("Preview Rock")]
    public Vector3 previewRockOffset = new Vector3(0, 1f, 1f); // Offset from spawn point for preview
    private Rock previewRock; // Rock shown while aiming/charging
    
    [Header("Flick Mode Settings (Option A)")]
    public float flickMinDistance = 50f;
    public float flickMaxTime = 0.5f;
    public float flickPowerMultiplier = 0.15f;
    private Vector2 flickStartPos;
    private float flickStartTime;
    private bool isFlicking = false;
    
    [Header("Rhythm Mode Settings (Option B)")]
    public float beatInterval = 0.75f;
    public float perfectTimingWindow = 0.08f;
    public float goodTimingWindow = 0.2f;
    private float rhythmTimer = 0f;
    private bool rhythmModeActive = false;
    private bool waitingForThrowBeat = false;
    
    [Header("Bounce Timing Settings")]
    public float baseBounceTimingWindow = 0.4f;
    public float timingWindowShrinkPerBounce = 0.05f;
    public float minTimingWindow = 0.15f;
    public float perfectBounceMultiplier = 1.5f;
    public float goodBounceMultiplier = 1.2f;
    public float missBounceMultiplier = 0.7f;
    private float currentTimingWindow;
    private int consecutivePerfectBounces = 0;
    
    [Header("Screen Shake Settings")]
    public float perfectShakeStrength = 0.3f;
    public float perfectShakeDuration = 0.2f;
    public float goodShakeStrength = 0.15f;
    public float goodShakeDuration = 0.15f;
    public float missShakeStrength = 0.4f;
    public float missShakeDuration = 0.3f;
    
    //Events    
    public event Action<float, float> OnThrowReady;
    public event Action<float> OnPowerChargeChanged;
    public event Action<float> OnAngleChanged;
    public event Action OnThrowExecuted;
    public event Action<BounceResult> OnBounceResult;
    public event Action<float> OnRockLanded;
    public event Action OnBeatTick;
    public event Action OnBounceWindowStart;
    public event Action OnBounceWindowEnd;
    
    [Header("State")]
    [SerializeField] private bool canThrow;
    [SerializeField] private bool isRockInFlight;
    [SerializeField] private bool randomBounceMode;
    private Rock activeRock;
    private Vector3 rockStartPosition;
    [SerializeField] private bool bounceInputEnabled;
    private Coroutine bounceTimingCoroutine;
    
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
        
        // Find or setup camera follower
        if (cameraFollower == null && mainCamera != null)
        {
            cameraFollower = mainCamera.GetComponent<CameraSmoothlyFollowGameObject>();
            if (cameraFollower == null)
            {
                cameraFollower = mainCamera.gameObject.AddComponent<CameraSmoothlyFollowGameObject>();
                Debug.Log("Added CameraSmoothlyFollowGameObject to main camera");
            }
        }
        
        // Save original camera position
        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.position;
        }
        
        if(cameraFollower != null)
        {
            cameraFollower.enabled = false; // Start disabled
            followCamera = cameraFollower.GetComponent<Camera>();
        }
        
        // Try to find throwing UI if not assigned
        if (throwingUI == null)
        {
            throwingUI = FindFirstObjectByType<RockThrowingUI>();
            if (throwingUI != null)
            {
                Debug.Log("Found RockThrowingUI in scene");
            }
            else
            {
                Debug.LogWarning("RockThrowingUI not found - power bar will not update!");
            }
        }
            
        currentTimingWindow = baseBounceTimingWindow;
        
       
        
        Debug.Log($"RockThrowingController initialized: Power {minThrowPower}-{maxThrowPower}, Angle {minThrowAngle}-{maxThrowAngle}");
    }
    
    private void Update()
    {
        if (!canThrow && !isRockInFlight) return;
        
        if (canThrow)
        {
            switch (currentMode)
            {
                case ThrowingMode.Oscillator:
                    UpdateOscillatorMode();
                    break;
                case ThrowingMode.Flick:
                    UpdateFlickMode();
                    break;
                case ThrowingMode.Rhythm:
                    UpdateRhythmMode();
                    break;
            }
        }
    }
    
    public void SwapCameras()
    {
        if(mainCamera != null && followCamera != null)
        {
            bool mainActive = mainCamera.gameObject.activeSelf;
            mainCamera.gameObject.SetActive(!mainActive);

            if (cameraFollower.gameObject.activeSelf)
            {
                cameraFollower.gameObject.transform.position = mainCamera.transform.position;
            }
            followCamera.gameObject.SetActive(mainActive);
        }
    }
    
    #region Public Methods
    
    public void PrepareThrow(Rock rock)
    {
        mainCamera.gameObject.SetActive(true);
        followCamera.gameObject.SetActive(false);
        
        currentRock = rock;
        canThrow = true;
        isRockInFlight = false;
        currentPowerCharge = 0f;
        currentOscillatorValue = 0f;
        consecutivePerfectBounces = 0;
        currentTimingWindow = baseBounceTimingWindow;
        isCharging = false;
        
        // Spawn preview rock that floats while aiming
        SpawnPreviewRock();
        
        switch (currentMode)
        {
            case ThrowingMode.Oscillator:
                oscillatorActive = true;
                break;
            case ThrowingMode.Rhythm:
                rhythmModeActive = true;
                waitingForThrowBeat = true;
                rhythmTimer = 0f;
                break;
            case ThrowingMode.Flick:
                break;
        }
        
        if (throwingUI != null)
        {
            throwingUI.ShowThrowingUI(currentMode);
            if(bounceInputEnabled == false || randomBounceMode == true)
            {
                throwingUI.HideBounceUI();
            }
        }
        
        Debug.Log($"Throw prepared with mode: {currentMode}");
    }
    
    private void SpawnPreviewRock()
    {
        // Clean up any existing preview
        if (previewRock != null)
        {
            Destroy(previewRock.gameObject);
        }
        
        if (currentRock == null || rockSpawnPoint == null) return;
        
        // Calculate preview position - offset in the throw direction (towards water)
        Vector3 offsetDirection = baseThrowDirection.normalized;
        Vector3 previewPos = rockSpawnPoint.position + 
                            offsetDirection * previewRockOffset.z + 
                            Vector3.up * previewRockOffset.y;
        
        // Instantiate preview rock
        previewRock = Instantiate(currentRock, previewPos, rockSpawnPoint.rotation);
        previewRock.gameObject.SetActive(true);
        previewRock.StartPreviewMode(previewPos);
        
        Debug.Log("Preview rock spawned and floating");
    }
    
    private void DestroyPreviewRock()
    {
        if (previewRock != null)
        {
            previewRock.StopPreviewMode();
            Destroy(previewRock.gameObject);
            previewRock = null;
        }
    }
    
    public void CancelThrow()
    {
        canThrow = false;
        oscillatorActive = false;
        rhythmModeActive = false;
        isCharging = false;
        currentPowerCharge = 0f;
        
        DestroyPreviewRock();
        
        if (throwingUI != null)
        {
            throwingUI.HideThrowingUI();
        }
    }
    
    public void SetThrowingMode(ThrowingMode mode)
    {
        currentMode = mode;
        Debug.Log($"Throwing mode set to: {mode}");
    }
    
    public float GetCurrentPower() => currentPowerCharge;
    public float GetCurrentAngle() => currentOscillatorValue;
    public bool IsCharging() => isCharging;
    public bool IsRockInFlight() => isRockInFlight;
    public Rock GetActiveRock() => activeRock;
    public float GetCurrentTimingWindow() => currentTimingWindow;
    public int GetConsecutivePerfects() => consecutivePerfectBounces;
    
    public void ResetForNewThrow()
    {
        currentTimingWindow = baseBounceTimingWindow;
        consecutivePerfectBounces = 0;
        isRockInFlight = false;
        canThrow = false;
        bounceInputEnabled = false;
    }
    
    #endregion
    
    #region Oscillator Mode (Option C)
    
    private void UpdateOscillatorMode()
    {
        if (!oscillatorActive) return;
        
        // Oscillate side to side using sine wave
        currentOscillatorValue = Mathf.Sin(Time.time * oscillatorSpeed);
        OnAngleChanged?.Invoke(currentOscillatorValue);
        
        if (throwingUI != null)
        {
            throwingUI.UpdateAngleIndicator(currentOscillatorValue);
            throwingUI.UpdatePowerBar(currentPowerCharge);
        }
        
        // Handle charging
        bool buttonHeld = IsThrowButtonHeld();
        
        if (buttonHeld)
        {
            if (!isCharging)
            {
                isCharging = true;
                Debug.Log($"Started charging... (UI connected: {throwingUI != null})");
            }
            
            currentPowerCharge += (powerChargeSpeed / maxChargeTime) * Time.deltaTime;
            currentPowerCharge = Mathf.Clamp01(currentPowerCharge);
            OnPowerChargeChanged?.Invoke(currentPowerCharge);
            
            if (throwingUI != null)
            {
                throwingUI.UpdatePowerBar(currentPowerCharge);
            }
            
            // Debug every 0.5 seconds while charging
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"Charging: {currentPowerCharge:P0}");
            }
        }
        else if (isCharging)
        {
            // Released - attempt to throw
            Debug.Log($"Released at power: {currentPowerCharge:P0}");
            bool throwSucceeded = ExecuteThrow(currentPowerCharge, currentOscillatorValue);
            
            if (throwSucceeded)
            {
                isCharging = false;
                oscillatorActive = false;
            }
            else
            {
                // Throw failed (not enough power), reset for another attempt
                isCharging = false;
                // oscillatorActive stays true so player can try again
            }
        }
    }
    
    #endregion
    
    #region Flick Mode (Option A)
    
    private void UpdateFlickMode()
    {
        Vector2 currentPos = Vector2.zero;
        bool pressed = false;
        bool justPressed = false;
        bool justReleased = false;
        
        // Check touch first, then mouse
        var touch = Touchscreen.current?.primaryTouch;
        var mouse = Mouse.current;
        
        if (touch != null && touch.press.isPressed)
        {
            currentPos = touch.position.ReadValue();
            pressed = true;
            justPressed = touch.press.wasPressedThisFrame;
            justReleased = touch.press.wasReleasedThisFrame;
        }
        else if (mouse != null)
        {
            currentPos = mouse.position.ReadValue();
            pressed = mouse.leftButton.isPressed;
            justPressed = mouse.leftButton.wasPressedThisFrame;
            justReleased = mouse.leftButton.wasReleasedThisFrame;
        }
        
        if (justPressed)
        {
            flickStartPos = currentPos;
            flickStartTime = Time.time;
            isFlicking = true;
        }
        else if (justReleased && isFlicking)
        {
            isFlicking = false;
            ProcessFlick(currentPos);
        }
        
        // Visual feedback while dragging
        if (pressed && isFlicking)
        {
            Vector2 delta = currentPos - flickStartPos;
            float power = Mathf.Clamp01(delta.magnitude * flickPowerMultiplier / 100f);
            float angle = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
            float normalizedAngle = Mathf.Clamp(angle / maxThrowAngle, -1f, 1f);
            
            OnPowerChargeChanged?.Invoke(power);
            OnAngleChanged?.Invoke(normalizedAngle);
            
            if (throwingUI != null)
            {
                throwingUI.UpdatePowerBar(power);
                throwingUI.UpdateAngleIndicator(normalizedAngle);
            }
        }
    }
    
    private void ProcessFlick(Vector2 endPos)
    {
        float flickTime = Time.time - flickStartTime;
        Vector2 delta = endPos - flickStartPos;
        
        if (flickTime <= flickMaxTime && delta.magnitude >= flickMinDistance)
        {
            float power = Mathf.Clamp01(delta.magnitude * flickPowerMultiplier / 100f);
            float angle = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
            float normalizedAngle = Mathf.Clamp(angle / maxThrowAngle, -1f, 1f);
            
            ExecuteThrow(power, normalizedAngle);
        }
        else
        {
            Debug.Log("Flick too slow or short - try again!");
            if (throwingUI != null)
            {
                throwingUI.ShowMessage("Flick harder!", 1f);
            }
        }
    }
    
    #endregion
    
    #region Rhythm Mode (Option B)
    
    private void UpdateRhythmMode()
    {
        if (!rhythmModeActive) return;
        
        rhythmTimer += Time.deltaTime;
        
        // Check for beat
        if (rhythmTimer >= beatInterval)
        {
            rhythmTimer -= beatInterval;
            OnBeatTick?.Invoke();
            
            if (throwingUI != null)
            {
                throwingUI.PulseBeat();
            }
        }
        
        // Oscillator follows beat timing
        float beatProgress = rhythmTimer / beatInterval;
        currentOscillatorValue = Mathf.Sin(beatProgress * Mathf.PI * 2f);
        OnAngleChanged?.Invoke(currentOscillatorValue);
        
        // Power visualization based on beat
        float powerVisual = 1f - Mathf.Abs(beatProgress - 0.5f) * 2f;
        OnPowerChargeChanged?.Invoke(powerVisual);
        
        if (throwingUI != null)
        {
            throwingUI.UpdateAngleIndicator(currentOscillatorValue);
            throwingUI.UpdatePowerBar(powerVisual);
        }
        
        if (waitingForThrowBeat && IsThrowButtonPressed())
        {
            // Calculate timing accuracy relative to beat
            float timingOffset = rhythmTimer;
            if (timingOffset > beatInterval / 2f)
                timingOffset = beatInterval - timingOffset;
            
            float power;
            string message;
            if (timingOffset <= perfectTimingWindow)
            {
                power = 1f;
                message = "PERFECT!";
                DoScreenShake(perfectShakeStrength, perfectShakeDuration);
            }
            else if (timingOffset <= goodTimingWindow)
            {
                power = 0.75f;
                message = "GOOD!";
                DoScreenShake(goodShakeStrength, goodShakeDuration);
            }
            else
            {
                power = 0.5f;
                message = "OK";
            }
            
            if (throwingUI != null)
            {
                throwingUI.ShowTimingResult(message);
            }
            
            ExecuteThrow(power, currentOscillatorValue);
            waitingForThrowBeat = false;
            rhythmModeActive = false;
        }
    }
    
    #endregion
    
    #region Throw Execution
    
    /// <summary>
    /// Execute the throw with given power and angle.
    /// Returns true if throw succeeded, false if rejected (e.g., not enough power)
    /// </summary>
    private bool ExecuteThrow(float powerNormalized, float angleNormalized)
    {
        if (!canThrow || currentRock == null) return false;

        UiUpdatePlayerThrowing(0 + 1);
        
        // Require minimum power to throw
        if (powerNormalized < 0.1f)
        {
            Debug.Log("Not enough power to throw! Hold longer to charge.");
            if (throwingUI != null)
            {
                throwingUI.ShowMessage("Hold to charge power!", 1f);
            }
            
            // Reset charging state so player can try again
            currentPowerCharge = 0f;
            isCharging = false;
            // Keep oscillatorActive true so the mode keeps running
            // Keep canThrow true so another attempt is allowed
            return false;
        }
        
        canThrow = false;
        oscillatorActive = false;
        rhythmModeActive = false;
        
        // Calculate actual throw parameters
        float power = Mathf.Lerp(minThrowPower, maxThrowPower, powerNormalized);
        float angle = Mathf.Lerp(minThrowAngle, maxThrowAngle, (angleNormalized + 1f) / 2f);
        
        // Use the preview rock if it exists, otherwise spawn a new one
        if (previewRock != null)
        {
            activeRock = previewRock;
            previewRock = null; // Clear reference so we don't destroy it
            
            // Move to spawn point for throw
            activeRock.transform.position = rockSpawnPoint.position;
            activeRock.transform.rotation = rockSpawnPoint.rotation;
        }
        else
        {
            activeRock = Instantiate(currentRock, rockSpawnPoint.position, rockSpawnPoint.rotation);
            activeRock.gameObject.SetActive(true);
        }
        
        rockStartPosition = rockSpawnPoint.position;
        
        // Setup bounce timing callbacks
        activeRock.OnWaterContact += HandleWaterContact;
        activeRock.OnRockSunk += HandleRockSunk;
        
        // Calculate throw direction using base direction (default -Z towards water)
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        Vector3 throwDirection = rotation * baseThrowDirection.normalized;
        
        // Calculate initial velocity
        Vector3 velocity = throwDirection * power;
        // Ensure minimum Y velocity to clear dock - at least 4 even at low power
        velocity.y = Mathf.Max(2f, throwArcHeight * Mathf.Max(0.5f, powerNormalized));
        
        // Throw with calculated velocity
        activeRock.ThrowRock(velocity);
        
        isRockInFlight = true;
        bounceInputEnabled = true;
        wasFollowingRock = true;
        
        if(cameraFollower != null)
        {
            // Start camera following the rock
            StartCameraFollow(activeRock.transform);
        }
       
        
        if (throwingUI != null)
        {
            throwingUI.HideThrowingUI();
            
            if(bounceInputEnabled == true && randomBounceMode == false)
                throwingUI.ShowBounceUI();
        }
        
        OnThrowExecuted?.Invoke();
        Debug.Log($"Rock thrown! Power: {power:F1}, Angle: {angle:F1}°, Velocity: {velocity}");

        if(AudioManager.instance != null)
        {
            AudioManager.instance.rockThrow.start();
        }
        
        return true;
    }
    
    #endregion
    
    #region Bounce Timing
    
    private void HandleWaterContact()
    {
        if (!isRockInFlight || activeRock == null) return;
        
        OnBounceWindowStart?.Invoke();
        
        
        if (bounceInputEnabled == true && randomBounceMode == false)
        {
            if (bounceTimingCoroutine != null)
            {
                StopCoroutine(bounceTimingCoroutine);
            }
            bounceTimingCoroutine = StartCoroutine(BounceTimingWindowCoroutine());
        }
        else
        {
            if (throwingUI != null)
            {
               throwingUI.HideBounceUI();
            }
        }
        
        if (randomBounceMode == true)
        {
            DoRandomBounce();
        }
    }
    
    private IEnumerator BounceTimingWindowCoroutine()
    {
        
        if(bounceInputEnabled == false || randomBounceMode == true)
        {
            if (throwingUI != null)
            {
                
               throwingUI.HideBounceUi();
            }
            yield break;
        }
        float elapsed = 0f;
        bool inputReceived = false;
        BounceResult result = BounceResult.Miss;
        
        if (throwingUI != null)
        {
            throwingUI.StartBounceTimingCircle(currentTimingWindow);
        }
        
        while (elapsed < currentTimingWindow && !inputReceived)
        {
            elapsed += Time.deltaTime;
            
            if (IsThrowButtonPressed())
            {
                inputReceived = true;
                
                // Center of window is perfect (0.5 progress)
                float timingProgress = elapsed / currentTimingWindow;
                float centerOffset = Mathf.Abs(timingProgress - 0.5f) * 2f;
                
                if (centerOffset <= 0.25f)
                {
                    result = BounceResult.Perfect;
                    consecutivePerfectBounces++;
                    DoScreenShake(perfectShakeStrength, perfectShakeDuration);
                }
                else if (centerOffset <= 0.5f)
                {
                    result = BounceResult.Good;
                    consecutivePerfectBounces = 0;
                    DoScreenShake(goodShakeStrength, goodShakeDuration);
                }
                else
                {
                    result = BounceResult.Okay;
                    consecutivePerfectBounces = 0;
                }
            }
            
            yield return null;
        }
        
        if (!inputReceived)
        {
            result = BounceResult.Miss;
            consecutivePerfectBounces = 0;
            DoScreenShake(missShakeStrength, missShakeDuration);
        }
        
        OnBounceWindowEnd?.Invoke();
        
        // Apply result to rock
        if (activeRock != null)
        {
            float multiplier = result switch
            {
                BounceResult.Perfect => perfectBounceMultiplier,
                BounceResult.Good => goodBounceMultiplier,
                BounceResult.Okay => 1f,
                BounceResult.Miss => missBounceMultiplier,
                _ => 1f
            };
            
            activeRock.ApplyBounceMultiplier(multiplier);
            
            // Shrink window for next bounce
            currentTimingWindow = Mathf.Max(minTimingWindow, 
                currentTimingWindow - timingWindowShrinkPerBounce);
        }
        
        if (throwingUI != null)
        {
            throwingUI.ShowBounceResult(result, consecutivePerfectBounces);
        }
        
        OnBounceResult?.Invoke(result);
        Debug.Log($"Bounce: {result} | Combo: {consecutivePerfectBounces} | Next window: {currentTimingWindow:F2}s");
    }

    private void DoRandomBounce()
    {
        // Apply result to rock
        if (activeRock != null)
        {
            float roll = UnityEngine.Random.value;
            BounceResult result = roll switch
            {
                <= 0.10f => BounceResult.Perfect,  // 10%
                <= 0.55f => BounceResult.Good,     // 45%
                <= 0.85f => BounceResult.Okay,     // 30%
                _ => BounceResult.Miss             // 15% (0.85 to 1.0)
            };
            
            if(activeRock.currentBounces == 0)
            {
                if(result == BounceResult.Miss)
                { //This Should Reduce the chance of getting a miss on the first bounce
                    roll = UnityEngine.Random.value;
                    result = roll switch
                  {
                      <= 0.20f => BounceResult.Good, // 20%
                      <= 0.60f => BounceResult.Okay, // 40%
                      _ => BounceResult.Miss    // 40% (0.60 to 1.0)
                  };
                }
            }
            
           
            
            float multiplier = result switch
            {
                BounceResult.Perfect => perfectBounceMultiplier,
                BounceResult.Good => goodBounceMultiplier,
                BounceResult.Okay => 1f,
                BounceResult.Miss => missBounceMultiplier,
                _ => 1f
            };
            
            activeRock.ApplyBounceMultiplier(multiplier);
        }
    }
    
    private void HandleRockSunk(float totalDistance)
    {
        isRockInFlight = false;
        bounceInputEnabled = false;
        
        if (bounceTimingCoroutine != null)
        {
            StopCoroutine(bounceTimingCoroutine);
            bounceTimingCoroutine = null;
        }
        
        
        // Stop camera following and return to original position
        StopCameraFollow();
        
        CleanupActiveRock();
        
        if (throwingUI != null)
        {
            throwingUI.HideBounceUI();
            throwingUI.HideThrowingUI();
            throwingUI.ShowDistanceResult(totalDistance);
        }
        
        OnRockLanded?.Invoke(totalDistance);
    }
    
    private void CleanupActiveRock()
    {
        if (activeRock != null)
        {
            activeRock.OnWaterContact -= HandleWaterContact;
            activeRock.OnRockSunk -= HandleRockSunk;
            activeRock = null;
        }
    }
    
    #endregion
    
    #region Screen Shake
    
    private void DoScreenShake(float strength, float duration)
    {
        if (mainCamera != null)
        {
            mainCamera.transform.DOShakePosition(duration, strength, 10, 90f, false, true)
                .SetUpdate(true);
        }
        else if(followCamera != null)
        {
            followCamera.transform.DOShakePosition(duration, strength, 10, 90f, false, true)
                .SetUpdate(true);
        }
    }
    
    #endregion
    
    #region Input Helpers
    
    private bool IsThrowButtonHeld()
    {
        var mouse = Mouse.current;
        var touch = Touchscreen.current?.primaryTouch;
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;
        
        if (touch != null && touch.press.isPressed) return true;
        if (mouse != null && mouse.leftButton.isPressed) return true;
        if (keyboard != null && keyboard.spaceKey.isPressed) return true;
        if (gamepad != null && gamepad.aButton.isPressed) return true;
        
        return false;
    }
    
    private bool IsThrowButtonPressed()
    {
        
        var mouse = Mouse.current;
        var touch = Touchscreen.current?.primaryTouch;
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;
        
        if (touch != null && touch.press.wasPressedThisFrame) return true;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            Debug.Log("Mouse left button pressed this frame");
            return true;
        }
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) return true;
        if (gamepad != null && gamepad.aButton.wasPressedThisFrame) return true;
        
        return false;
    }
    
    #endregion
    
    #region Camera Follow
    
    public void StartCameraFollow(Transform target)
    {
        SwapCameras();
        if (cameraFollower != null && target != null)
        {
            cameraFollower.SetTarget(target);
            cameraFollower.SetOffset(rockFollowOffset);
            cameraFollower.enabled = true;
            Debug.Log("Camera now following rock");
        }
    }
    
    public void StopCameraFollow()
    {
        SwapCameras();
        if (cameraFollower != null)
        {
            cameraFollower.SetTarget(null);
            cameraFollower.enabled = false;
        }
        
        // Smoothly return camera to original position
        if (mainCamera != null && wasFollowingRock)
        {
            mainCamera.transform.DOMove(originalCameraPosition, 1f).SetEase(Ease.OutQuad);
            wasFollowingRock = false;
            Debug.Log("Camera returning to original position");
        }
        
        
    }
    
    #endregion
    
    public void UiUpdatePlayerThrowing(int playerId)
    {
        // Placeholder for updating UI with player info
        Debug.Log($"UI updated for player {playerId} throwing");
        
        string playerName = $"Player {playerId}";
        if (throwingUI != null)
        {
            throwingUI.UpdateInstructionText(playerName);
            
        }
       
    }
    
    public void UiUpdateAiDistance(float distance)
    {
        // Placeholder for updating UI with AI distance info
        Debug.Log($"UI updated with AI distance: {distance:F1} meters");
        
        if (throwingUI != null)
        {
            throwingUI.ShowAIDistanceResult(distance);
        }
    }
    
    private void OnDestroy()
    {
        CleanupActiveRock();
        DestroyPreviewRock();
        StopCameraFollow();
        
        if (Instance == this)
            Instance = null;
    }
}

public enum BounceResult
{
    Perfect,
    Good,
    Okay,
    Miss
}
