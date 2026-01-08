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
    
    [Header("Throwing Settings")]
    public float minThrowPower = 8f;
    public float maxThrowPower = 25f;
    public float minThrowAngle = -30f;
    public float maxThrowAngle = 30f;
    public float throwArcHeight = 8f;
    
    [Header("Oscillator Mode Settings (Option C)")]
    public float oscillatorSpeed = 2.5f;
    public float powerChargeSpeed = 1.2f;
    public float maxChargeTime = 2f;
    private float currentOscillatorValue = 0f;
    private float currentPowerCharge = 0f;
    private bool isCharging = false;
    private bool oscillatorActive = false;
    
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
    private Rock activeRock;
    private Vector3 rockStartPosition;
    private bool bounceInputEnabled;
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
            
        currentTimingWindow = baseBounceTimingWindow;
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
    
    #region Public Methods
    
    public void PrepareThrow(Rock rock)
    {
        currentRock = rock;
        canThrow = true;
        isRockInFlight = false;
        currentPowerCharge = 0f;
        currentOscillatorValue = 0f;
        consecutivePerfectBounces = 0;
        currentTimingWindow = baseBounceTimingWindow;
        isCharging = false;
        
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
        }
        
        Debug.Log($"Throw prepared with mode: {currentMode}");
    }
    
    public void CancelThrow()
    {
        canThrow = false;
        oscillatorActive = false;
        rhythmModeActive = false;
        isCharging = false;
        currentPowerCharge = 0f;
        
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
        }
        
        // Handle charging
        if (IsThrowButtonHeld())
        {
            if (!isCharging)
            {
                isCharging = true;
            }
            
            currentPowerCharge += (powerChargeSpeed / maxChargeTime) * Time.deltaTime;
            currentPowerCharge = Mathf.Clamp01(currentPowerCharge);
            OnPowerChargeChanged?.Invoke(currentPowerCharge);
            
            if (throwingUI != null)
            {
                throwingUI.UpdatePowerBar(currentPowerCharge);
            }
        }
        else if (isCharging)
        {
            // Released - execute throw
            ExecuteThrow(currentPowerCharge, currentOscillatorValue);
            isCharging = false;
            oscillatorActive = false;
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
    
    private void ExecuteThrow(float powerNormalized, float angleNormalized)
    {
        if (!canThrow || currentRock == null) return;
        
        canThrow = false;
        oscillatorActive = false;
        rhythmModeActive = false;
        
        // Calculate actual throw parameters
        float power = Mathf.Lerp(minThrowPower, maxThrowPower, powerNormalized);
        float angle = Mathf.Lerp(minThrowAngle, maxThrowAngle, (angleNormalized + 1f) / 2f);
        
        // Spawn the rock
        activeRock = Instantiate(currentRock, rockSpawnPoint.position, rockSpawnPoint.rotation);
        activeRock.gameObject.SetActive(true);
        rockStartPosition = rockSpawnPoint.position;
        
        // Setup bounce timing callbacks
        activeRock.OnWaterContact += HandleWaterContact;
        activeRock.OnRockSunk += HandleRockSunk;
        
        // Calculate throw direction
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        Vector3 throwDirection = rotation * rockSpawnPoint.forward;
        
        // Apply initial velocity
        Rigidbody rb = activeRock.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 velocity = throwDirection * power;
            velocity.y = throwArcHeight * Mathf.Max(0.5f, powerNormalized);
            rb.linearVelocity = velocity;
        }
        
        activeRock.ThrowRock();
        isRockInFlight = true;
        bounceInputEnabled = true;
        
        if (throwingUI != null)
        {
            throwingUI.HideThrowingUI();
            throwingUI.ShowBounceUI();
        }
        
        OnThrowExecuted?.Invoke();
        Debug.Log($"Rock thrown! Power: {power:F1}, Angle: {angle:F1}°");
    }
    
    /// <summary>
    /// Direct throw for AI use - no player input needed
    /// </summary>
    public void ExecuteAIThrow(Rock rock, float power, float angle, Action<float> onComplete)
    {
        // Spawn the rock
        activeRock = Instantiate(rock, rockSpawnPoint.position, rockSpawnPoint.rotation);
        activeRock.gameObject.SetActive(true);
        rockStartPosition = rockSpawnPoint.position;
        
        // Setup callbacks
        activeRock.OnRockSunk += (distance) => {
            CleanupActiveRock();
            onComplete?.Invoke(distance);
        };
        
        // Calculate throw direction
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        Vector3 throwDirection = rotation * rockSpawnPoint.forward;
        
        // Apply velocity
        Rigidbody rb = activeRock.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 velocity = throwDirection * power;
            velocity.y = throwArcHeight * (power / maxThrowPower);
            rb.linearVelocity = velocity;
        }
        
        activeRock.ThrowRock();
        isRockInFlight = true;
        bounceInputEnabled = false; // AI doesn't use input
        
        Debug.Log($"AI Rock thrown! Power: {power:F1}, Angle: {angle:F1}°");
    }
    
    #endregion
    
    #region Bounce Timing
    
    private void HandleWaterContact()
    {
        if (!isRockInFlight || activeRock == null) return;
        
        OnBounceWindowStart?.Invoke();
        
        if (bounceInputEnabled)
        {
            if (bounceTimingCoroutine != null)
            {
                StopCoroutine(bounceTimingCoroutine);
            }
            bounceTimingCoroutine = StartCoroutine(BounceTimingWindowCoroutine());
        }
    }
    
    private IEnumerator BounceTimingWindowCoroutine()
    {
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
    
    private void HandleRockSunk(float totalDistance)
    {
        isRockInFlight = false;
        bounceInputEnabled = false;
        
        if (bounceTimingCoroutine != null)
        {
            StopCoroutine(bounceTimingCoroutine);
            bounceTimingCoroutine = null;
        }
        
        CleanupActiveRock();
        
        if (throwingUI != null)
        {
            throwingUI.HideBounceUI();
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
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) return true;
        if (gamepad != null && gamepad.aButton.wasPressedThisFrame) return true;
        
        return false;
    }
    
    #endregion
    
    private void OnDestroy()
    {
        CleanupActiveRock();
        
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
