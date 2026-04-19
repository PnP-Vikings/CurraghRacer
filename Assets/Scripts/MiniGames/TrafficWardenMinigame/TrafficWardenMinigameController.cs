using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum TrafficEventType { None, Ambulance, Rain, Roadworks, OldPerson }

/// <summary>Spawn patterns the controller can trigger on the managed spawners.</summary>
public enum SpawnPattern { Normal, Burst, RushHour, Convoy, Chaos }

public class TrafficWardenMinigameController : MonoBehaviour
{
    public static TrafficWardenMinigameController Instance;

    public PlayerInputs playerInputs;

    [Header("Timing")]
    public float minStopTime = 1.5f;
    public float minGoTime = 1.0f;

    [Header("Score / Combo")]
    public int score;
    public int combo;
    public int bestCombo;
    public int basePoints = 1;

    [Header("Strikes")]
    public int strikes;
    public int maxStrikes = 3;
    [Tooltip("Cooldown (seconds) between penalties so a single crash doesn't count twice.")]
    public float penaltyCooldown = 0.5f;
    float lastPenaltyTime = -999f;
    
    [Tooltip("Seconds before an individual strike expires")]
    public float strikeDecayTime = 2f;
    private System.Collections.Generic.List<float> strikeTimestamps = new System.Collections.Generic.List<float>();
    private Coroutine strikeReasonCoroutine;

    public UnityEvent onCarCrashed;
    public UnityEvent onGameEnded;
    
    [Header("Game Timer & Win Condition")]
    public float gameDuration = 120f; // 2 minutes
    public float timeRemaining;
    public bool gameEnded;
    
    [Header("Crash Tracking & Bonuses")]
    public int totalCrashes;
    public int crashFreeBonus = 500;
    public int lowCrashBonus = 250; // Bonus if 1-2 crashes
    public int perfectComboBonus = 300; // Bonus for best combo >= 10
    
    [Header("Combo Multiplier")]
    [Tooltip("Combo thresholds → multiplier: 5→2x, 10→3x, 20→5x")]
    public int comboTier1 = 5;
    public int comboTier2 = 10;
    public int comboTier3 = 20;
    
    [Header("Combo Milestones")]
    public int milestone1Bonus = 50;
    public int milestone2Bonus = 100;
    public int milestone3Bonus = 200;
    public int milestone4Bonus = 500;
    private int lastMilestoneHit;
    
    [Header("Time Pressure Bonus")]
    [Tooltip("Seconds remaining at which time-pressure multiplier kicks in")]
    public float timePressureThreshold = 30f;
    public float timePressureMultiplier = 2f;
    
    [Header("Close Call Bonus")]
    public int closeCallBonus = 25;
    public float closeCallCooldown = 2f;
    float lastCloseCallTime = -999f;
    
    [Header("Quick Toggle Bonus")]
    [Tooltip("How many distinct lanes toggled within the window to trigger the bonus")]
    public int quickToggleRequirement = 3;
    public float quickToggleWindow = 1.5f;
    public int quickToggleBonus = 75;
    private List<float> recentToggleTimes = new List<float>();
    private List<int> recentToggleLanes = new List<int>();
    
    [Header("Near-Miss Bonus")]
    public int nearMissBonus = 30;
    
    [Header("Event Warnings")]
    public float warningDuration = 2f;
    
    [Header("Anger Meter (per lane, 0..1)")]
    public float[] laneAnger;                                   // one per stop line
    public float angerGainPerSecond_Stop = 0.10f;
    public float angerReliefPerSecond_Go = 0.18f;

    [Header("Random Events")]
    public TrafficEventType activeEvent = TrafficEventType.None;
    public float eventMinInterval = 10f;
    public float eventMaxInterval = 20f;
    public float eventDurationMin = 6f;
    public float eventDurationMax = 12f;

    [Header("Stop Lines")]
    public StopLine[] stopLines;

    // ── Spawner Control ──────────────────────────────
    [Header("Spawner Control")]
    public CarSpawner[] spawners;                     // Assign in inspector

    [Header("Initial Spawn")]
    [Tooltip("Seconds before the very first car appears. Set to 0 for instant spawns.")]
    public float initialSpawnDelay = 0.5f;

    [Header("Difficulty Ramp")]
    public float difficultyRampTime = 120f;
    public float minIntervalAtMax = 0.5f;
    public float maxSpeedAtMax = 1.4f;
    public float maxViolatorBonus = 0.20f;

    [Header("Spawn Patterns")]
    public float patternMinInterval = 12f;
    public float patternMaxInterval = 25f;
    
    [Header("UI")]
    [SerializeField] MinigameCanvasUI  minigameCanvasUI;
    public TMPro.TMP_Text gameOverText;
    public GameObject testDemoButtons;
    public bool showRestartButtons;
    public Image eventIcon;
    public Image eventIconBackground;
    public List<Sprite> eventIcons;
    
    
    // ── Private state ────────────────────────────────
    float lastToggleTime;
    float nextEventTime;
    float eventEndTime;
    float gameStartTime;

    // Per-lane spawn timers (owned by controller)
    float[] laneTimers;
    float[] laneIntervals;
    bool[]  lanePaused;

    // Pattern state
    float nextPatternTime;
    SpawnPattern activePattern = SpawnPattern.Normal;
    float patternEndTime;
    int rushHourLane = -1;

    // Burst coroutine tracking
    int burstRemaining;
    float burstTimer;
    float burstInterval;
    int burstLane;

    // Convoy tracking
    int convoyRemaining;
    float convoyTimer;
    float convoyInterval;
    int convoyLane;
    int spawnedConvoyCars; // To track how many cars we've spawned in the current convoy pattern

    // Roadworks blocked lane
    int roadworksBlockedLane = -1;

    // Old Person event
    [Header("Old Person Event")]
    [Tooltip("Speed multiplier applied to cars on the affected lane (e.g. 0.35 = 35% speed).")]
    public float oldPersonSpeedFactor = 0.35f;
    [Tooltip("Spawn interval multiplier – higher means bigger gaps between slow cars.")]
    public float oldPersonIntervalFactor = 2.5f;
    int oldPersonLane = -1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        lastToggleTime = -999f;
        gameStartTime = Time.time;
        timeRemaining = gameDuration;
        gameEnded = false;
        totalCrashes = 0;
        lastMilestoneHit = 0;

        // Initialise per-lane timers
        int count = spawners != null ? spawners.Length : 0;
        laneTimers    = new float[count];
        laneIntervals = new float[count];
        lanePaused    = new bool[count];
        for (int i = 0; i < count; i++)
        {
            RollLaneInterval(i);
            // Pre-fill timer so the first car only waits 'initialSpawnDelay' seconds
            laneTimers[i] = Mathf.Max(0f, laneIntervals[i] - initialSpawnDelay);
            if (spawners[i] != null)
                spawners[i].laneIndex = i;
            
            if(stopLines != null && i < stopLines.Length && stopLines[i] != null)
                stopLines[i].SetLaneIndex(i);
        }

        // Initialise per-lane anger
        int laneCount = stopLines != null ? stopLines.Length : 0;
        laneAnger = new float[laneCount];

        playerInputs = new PlayerInputs();
        playerInputs.Enable();
        playerInputs.TrafficWardenGame.Enable();
        playerInputs.TrafficWardenGame.ToggleLane1.performed += ctx => ToggleLane1();
        playerInputs.TrafficWardenGame.ToggleLane2.performed += ctx => ToggleLane2();
        playerInputs.TrafficWardenGame.ToggleLane3.performed += ctx => ToggleLane3();
        playerInputs.TrafficWardenGame.ToggleLane4.performed += ctx => ToggleLane4();
        playerInputs.TrafficWardenGame.ToggleAllLanes.performed += ctx => Toggle();

        ScheduleNextEvent();
        ScheduleNextPattern();
        SetupUi();
        onCarCrashed.AddListener(() => { 
            Penalize("Car crashed");
            totalCrashes++;
        });
    }

    void Update()
    {
        if (gameEnded) return;
        
        UpdateGameTimer();
        UpdateStrikeDecay();
        UpdateDifficulty();
        UpdateSpawning();
        CleanupLanes();
        UpdateAnger();
        UpdateEvents();
        UpdatePatterns();
    }
    
    // ═══════════════════════════════════════════════════
    //  GAME TIMER & WIN/LOSE CONDITIONS
    // ═══════════════════════════════════════════════════

    void UpdateGameTimer()
    {
        timeRemaining -= Time.deltaTime;
        
        if (minigameCanvasUI != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            
            string timerPrefix = timeRemaining <= timePressureThreshold ? "<color=#FF4444>! </color>" : "";
            minigameCanvasUI.UpdateTimer($"{timerPrefix}Time: {minutes}:{seconds:00}");
        }

        if (timeRemaining <= 0f && !gameEnded)
        {
            timeRemaining = 0f;
            EndGame(true); // Won by surviving the timer
        }
    }

    void EndGame(bool won)
    {
        gameEnded = true;
        onGameEnded?.Invoke();
        
        if (won)
        {
            // Calculate final score with bonuses
            int finalScore = score;
            string bonusSummary = "";
            
            // Crash-free bonus
            if (totalCrashes == 0)
            {
                finalScore += crashFreeBonus;
                bonusSummary += $"\n<color=#44AAFF>[SHIELD]</color> No Crashes: +{crashFreeBonus}";
                Debug.Log($"Perfect! No crashes! Bonus: +{crashFreeBonus}");
            }
            else if (totalCrashes <= 2)
            {
                finalScore += lowCrashBonus;
                bonusSummary += $"\n<color=#44AAFF>[SHIELD]</color> Low Crashes: +{lowCrashBonus}";
                Debug.Log($"Good job! Low crashes. Bonus: +{lowCrashBonus}");
            }
            
            // Best combo bonus
            if (bestCombo >= 10)
            {
                int comboBonus = perfectComboBonus + (bestCombo - 10) * 20;
                finalScore += comboBonus;
                bonusSummary += $"\n<color=#FF6600>[FIRE]</color> Best Combo x{bestCombo}: +{comboBonus}";
                Debug.Log($"Amazing combo streak of {bestCombo}! Bonus: +{comboBonus}");
            }
            
            score = finalScore;
            
            if (minigameCanvasUI != null)
            {
                minigameCanvasUI.UpdateScore(finalScore);
                minigameCanvasUI.ShowVictory();
                minigameCanvasUI.HideMultiplier();
            }
            
            if (gameOverText != null)
            {
                gameOverText.gameObject.SetActive(true);
                gameOverText.text = $"<color=#FFD700>[TROPHY]</color> Victory!\n\nFinal Score: {finalScore}\nBest Combo: x{bestCombo}\nCrashes: {totalCrashes}{bonusSummary}";
            }
            
            if (showRestartButtons && testDemoButtons != null)
            {
                testDemoButtons.SetActive(true);
            }
            
            Debug.Log($"Traffic Warden Victory! Final Score: {finalScore}, Crashes: {totalCrashes}, Best Combo: {bestCombo}");
        }
        else
        {
            Debug.Log("Traffic Warden Game Over - Too many strikes");
            if (minigameCanvasUI != null)
            {
                minigameCanvasUI.ShowGameOver();
                minigameCanvasUI.HideMultiplier();
            }
            
            if (showRestartButtons && testDemoButtons != null)
            {
                testDemoButtons.SetActive(true);
            }
        }
    }
 
    

    /// <summary>Tell each spawner to destroy cars that have gone too far.</summary>
    void CleanupLanes()
    {
        for (int i = 0; i < spawners.Length; i++)
        {
            if (spawners[i] != null)
                spawners[i].CleanupDistant();
        }
    }

    // ═══════════════════════════════════════════════════
    //  DIFFICULTY RAMP
    // ═══════════════════════════════════════════════════

    /// <summary>0‒1 representing how far into the difficulty curve we are.</summary>
    public float DifficultyT => Mathf.Clamp01((Time.time - gameStartTime) / difficultyRampTime);

    void UpdateDifficulty()
    {
        float t = DifficultyT;

        float speedMul    = Mathf.Lerp(1f, maxSpeedAtMax, t);
        float violatorAdd = Mathf.Lerp(0f, maxViolatorBonus, t);

        for (int i = 0; i < spawners.Length; i++)
        {
            if (spawners[i] == null) continue;

            spawners[i].speedMultiplier = speedMul;
            spawners[i].violatorBonus   = violatorAdd;

            // Old Person event: slow down the affected lane
            if (activeEvent == TrafficEventType.OldPerson && i == oldPersonLane)
                spawners[i].speedMultiplier = speedMul * oldPersonSpeedFactor;

            // Determine if this lane is paused
            if (activeEvent == TrafficEventType.Roadworks && i == roadworksBlockedLane)
                lanePaused[i] = true;
            else if (activePattern != SpawnPattern.Normal)
                ApplyPatternToLane(i);
            else
                lanePaused[i] = false;
        }
    }

    // ═══════════════════════════════════════════════════
    //  CENTRALIZED SPAWNING (replaces CarSpawner.Update)
    // ═══════════════════════════════════════════════════

    void UpdateSpawning()
    {
        float t = DifficultyT;
        float intervalMul = Mathf.Lerp(1f, minIntervalAtMax, t);

        for (int i = 0; i < spawners.Length; i++)
        {
            if (spawners[i] == null || lanePaused[i]) continue;

            laneTimers[i] += Time.deltaTime;

            // Apply pattern speed-up at check time instead of mutating the stored interval
            float effectiveInterval = laneIntervals[i];
            if (activePattern == SpawnPattern.RushHour && i == rushHourLane)
                effectiveInterval *= 0.35f;
            else if (activePattern == SpawnPattern.Chaos)
                effectiveInterval *= 0.4f;

            // Old Person event: widen the gap between spawns on the slow lane
            if (activeEvent == TrafficEventType.OldPerson && i == oldPersonLane)
                effectiveInterval *= oldPersonIntervalFactor;

            // Safety: never let effective interval drop below a hard floor
            effectiveInterval = Mathf.Max(effectiveInterval, 0.3f);

            if (laneTimers[i] >= effectiveInterval)
            {
                laneTimers[i] = 0f;
                spawners[i].ForceSpawn();
                RollLaneInterval(i, intervalMul);
            }
        }
    }

    /// <summary>Roll a new random interval for the given lane, applying the difficulty multiplier.</summary>
    void RollLaneInterval(int lane, float intervalMul = 1f)
    {
        if (spawners[lane] == null) return;
        var s = spawners[lane];
        laneIntervals[lane] = Random.Range(s.minInterval, s.maxInterval) * intervalMul;
    }

    // ═══════════════════════════════════════════════════
    //  SPAWN PATTERNS
    // ═══════════════════════════════════════════════════

    void ScheduleNextPattern()
    {
        // Scale: patterns come more frequently as difficulty rises
        float mul = Mathf.Lerp(1f, 0.6f, DifficultyT);
        nextPatternTime = Time.time + Random.Range(patternMinInterval, patternMaxInterval) * mul;
    }

    void UpdatePatterns()
    {
        // Tick active burst / convoy
        TickBurst();
        TickConvoy();

        if (activePattern != SpawnPattern.Normal)
        {
            if (Time.time >= patternEndTime)
                EndPattern();
            return;
        }

        if (Time.time >= nextPatternTime)
            StartRandomPattern();
    }

    void StartRandomPattern()
    {
        // Prevent re-entry: block UpdatePatterns from calling this again during the warning delay
        nextPatternTime = float.MaxValue;

        // Pick a weighted random pattern; heavier ones appear later
        float r = Random.value;
        float t = DifficultyT;

        SpawnPattern chosenPattern;
        string warningMsg;
        
        // Pre-pick the target lane so we can show the orientation in the warning
        int chosenLane = Random.Range(0, spawners.Length);
        string laneDir = (stopLines != null && chosenLane < stopLines.Length && stopLines[chosenLane] != null)
            ? stopLines[chosenLane].laneOrientation
            : $"Lane {chosenLane + 1}";
        
        if (r < 0.30f)
        {
            chosenPattern = SpawnPattern.Burst;
            warningMsg = $"<color=#FF4400>* BURST!</color> Rapid cars from the <color=#FFFFFF>{laneDir}</color>!";
        }
        else if (r < 0.55f)
        {
            chosenPattern = SpawnPattern.RushHour;
            warningMsg = $"<color=#FFAA00>RUSH HOUR!</color> Heavy traffic from the <color=#FFFFFF>{laneDir}</color>!";
        }
        else if (r < 0.80f)
        {
            chosenPattern = SpawnPattern.Convoy;
            warningMsg = $"<color=#FFAA00>CONVOY!</color> Tight formation from the <color=#FFFFFF>{laneDir}</color>!";
        }
        else if (t > 0.5f) // Chaos only after halfway through difficulty curve
        {
            chosenPattern = SpawnPattern.Chaos;
            warningMsg = "<color=#FF0000>!! CHAOS !!</color> ALL lanes flooding!";
        }
        else
        {
            chosenPattern = SpawnPattern.Burst;
            warningMsg = $"<color=#FF4400>* BURST!</color> Rapid cars from the <color=#FFFFFF>{laneDir}</color>!";
        }

        // Show warning, then activate after a short heads-up
        float patternWarning = 1.5f; // shorter warning for patterns
        if (minigameCanvasUI != null)
            minigameCanvasUI.ShowWarning(warningMsg, patternWarning);
        
        StartCoroutine(ActivatePatternDelayed(chosenPattern, patternWarning, chosenLane));
    }
    
    System.Collections.IEnumerator ActivatePatternDelayed(SpawnPattern pattern, float delay, int lane)
    {
        yield return new WaitForSeconds(delay);
        if (gameEnded) yield break;
        
        switch (pattern)
        {
            case SpawnPattern.Burst:   StartBurst(lane);     break;
            case SpawnPattern.RushHour: StartRushHour(lane);  break;
            case SpawnPattern.Convoy:  StartConvoy(lane);    break;
            case SpawnPattern.Chaos:   StartChaos();     break;
        }
        Debug.Log($"Spawn pattern started: {activePattern}");
    }

    void EndPattern()
    {
        Debug.Log($"Spawn pattern ended: {activePattern}");
        activePattern = SpawnPattern.Normal;
        rushHourLane = -1;
        ScheduleNextPattern();
    }

    // ── Burst: rapid-fire 3-5 cars on a random lane ──
    void StartBurst(int lane)
    {
        activePattern = SpawnPattern.Burst;
        patternEndTime = Time.time + 5f;
        burstLane = lane;
        burstRemaining = Random.Range(3, 6);
        burstInterval = 0.4f;
        burstTimer = 0f;
    }

    void TickBurst()
    {
        if (activePattern != SpawnPattern.Burst || burstRemaining <= 0) return;

        burstTimer += Time.deltaTime;
        if (burstTimer >= burstInterval)
        {
            burstTimer = 0f;
            if (burstLane >= 0 && burstLane < spawners.Length && spawners[burstLane] != null)
                spawners[burstLane].ForceSpawn();
            burstRemaining--;
        }
    }

    // ── Rush Hour: one lane spawns much faster ──
    void StartRushHour(int lane)
    {
        activePattern = SpawnPattern.RushHour;
        patternEndTime = Time.time + Random.Range(6f, 10f);
        rushHourLane = lane;
    }

    // ── Convoy: cars spawn in a tight single-file ──
    void StartConvoy(int lane)
    {
        activePattern = SpawnPattern.Convoy;
        patternEndTime = Time.time + 6f;
        convoyLane = lane;
        convoyRemaining = Random.Range(4, 7);
        convoyInterval = 0.8f;
        convoyTimer = 0f;
        spawnedConvoyCars = 0;
    }

    void TickConvoy()
    {
        if (activePattern != SpawnPattern.Convoy || convoyRemaining <= 0) return;

        convoyTimer += Time.deltaTime;
        if (convoyTimer >= convoyInterval)
        {
            convoyTimer = 0f;
            if (convoyLane >= 0 && convoyLane < spawners.Length && spawners[convoyLane] != null)
            {
                var car = spawners[convoyLane].ForceSpawn();
                if (car != null)
                {
                    car.shouldObey = true;
                    car.maxSpeed *= 0.85f;

                    if (spawnedConvoyCars <= 4)
                    {
                        car.maxSpeed *= 1.5f;
                        
                    }
                }
                spawnedConvoyCars++;
            }
            convoyRemaining--;
        }
    }

    // ── Chaos: ALL lanes spawn fast simultaneously ──
    void StartChaos()
    {
        activePattern = SpawnPattern.Chaos;
        patternEndTime = Time.time + Random.Range(4f, 7f);
    }

    /// <summary>Apply per-lane overrides based on current pattern (sets lanePaused only — interval speed-ups are handled in UpdateSpawning).</summary>
    void ApplyPatternToLane(int i)
    {
        switch (activePattern)
        {
            case SpawnPattern.RushHour:
                lanePaused[i] = false;
                // Rush-hour lane spawns ~3× faster — applied in UpdateSpawning
                break;

            case SpawnPattern.Burst:
                // Burst lane pauses normal spawning (TickBurst handles ForceSpawn)
                lanePaused[i] = (i == burstLane);
                break;

            case SpawnPattern.Convoy:
                // Convoy lane pauses normal spawning (TickConvoy handles ForceSpawn)
                lanePaused[i] = (i == convoyLane);
                break;

            case SpawnPattern.Chaos:
                lanePaused[i] = false;
                // All lanes spawn faster — applied in UpdateSpawning
                break;
        }
    }

    // ═══════════════════════════════════════════════════
    //  STATE TOGGLE
    // ═══════════════════════════════════════════════════

    public void ToggleLane1()
    {
        if (stopLines.Length > 0 && stopLines[0] != null)
        {
            if (activeEvent == TrafficEventType.Ambulance && stopLines[0].GetState() == CrossingState.Go)
            {
                AddStrike("Stopped during Ambulance event");
                ResetCombo();
                return;
            }

            stopLines[0].ChangeState();
            Debug.Log($"Toggling lane 1 to state {stopLines[0].GetState()}");
            lastToggleTime = Time.time;
            RecordToggle(0);
        }
    }

    public void ToggleLane2()
    {
        if (stopLines.Length > 1 && stopLines[1] != null)
        {
            if (activeEvent == TrafficEventType.Ambulance && stopLines[1].GetState() == CrossingState.Go)
            {
                AddStrike("Stopped during Ambulance event");
                ResetCombo();
                return;
            }

            stopLines[1].ChangeState();
            Debug.Log($"Toggling lane 2 to state {stopLines[1].GetState()}");
            lastToggleTime = Time.time;
            RecordToggle(1);
        }
    }

    public void ToggleLane3()
    {
        if (stopLines.Length > 2 && stopLines[2] != null)
        {
            if (activeEvent == TrafficEventType.Ambulance && stopLines[2].GetState() == CrossingState.Go)
            {
                AddStrike("Stopped during Ambulance event");
                ResetCombo();
                return;
            }

            stopLines[2].ChangeState();
            Debug.Log($"Toggling lane 3 to state {stopLines[2].GetState()}");
            lastToggleTime = Time.time;
            RecordToggle(2);
        }
    }

    public void ToggleLane4()
    {
        if (stopLines.Length > 3 && stopLines[3] != null)
        {
            if (activeEvent == TrafficEventType.Ambulance && stopLines[3].GetState() == CrossingState.Go)
            {
                AddStrike("Stopped during Ambulance event");
                ResetCombo();
                return;
            }

            stopLines[3].ChangeState();
            Debug.Log($"Toggling lane 4 to state {stopLines[3].GetState()}");
            lastToggleTime = Time.time;
            RecordToggle(3);
        }
    }

    public void Toggle()
    {
        if (!CanToggle()) return;
        foreach (StopLine stopLine in stopLines)
        {
            if (activeEvent == TrafficEventType.Ambulance && stopLine.GetState() == CrossingState.Go)
            {
                AddStrike("Stopped during Ambulance event");
                ResetCombo();
                return;
            }

            stopLine.ChangeState();
            lastToggleTime = Time.time;
        }
    }

    // Returns true if ANY stop line is in Stop state
    public bool IsStopActive()
    {
        foreach (StopLine stopLine in stopLines)
        {
            if (stopLine != null && stopLine.GetState() == CrossingState.Stop)
                return true;
        }
        return false;
    }

    // Check if a specific stop line is in Stop state
    public bool IsStopActive(StopLine specificLine)
    {
        return specificLine != null && specificLine.GetState() == CrossingState.Stop;
    }

    bool CanToggle()
    {
        float t = Time.time - lastToggleTime;
        bool anyStopActive = IsStopActive();
        return anyStopActive ? t >= minStopTime : t >= minGoTime;
    }

    // ═══════════════════════════════════════════════════
    //  ANGER SYSTEM  (per-lane)
    // ═══════════════════════════════════════════════════

    /// <summary>Highest anger value across all lanes (handy for UI).</summary>
    public float MaxAnger
    {
        get
        {
            float max = 0f;
            for (int i = 0; i < laneAnger.Length; i++)
                if (laneAnger[i] > max) max = laneAnger[i];
            return max;
        }
    }

    /// <summary>Get anger for a specific lane.</summary>
    public float GetLaneAnger(int lane)
    {
        if (lane >= 0 && lane < laneAnger.Length)
            return laneAnger[lane];
        return 0f;
    }

    void UpdateAnger()
    {
        float baseGain = angerGainPerSecond_Stop;

        // Global modifiers
        if (activeEvent == TrafficEventType.Roadworks)
            baseGain *= 1.6f;
        if (activePattern == SpawnPattern.Chaos)
            baseGain *= 1.3f;

        for (int i = 0; i < stopLines.Length && i < laneAnger.Length; i++)
        {
            if (stopLines[i] == null) continue;

            bool laneStopped = stopLines[i].GetState() == CrossingState.Stop;

            float gain = baseGain;

            // Rush-hour lane builds anger faster
            if (activePattern == SpawnPattern.RushHour && i == rushHourLane)
                gain *= 1.5f;

            // Old Person lane builds anger a bit faster (cars crawl, drivers get impatient)
            if (activeEvent == TrafficEventType.OldPerson && i == oldPersonLane)
                gain *= 1.3f;

            // Roadworks-blocked lane doesn't build anger (no cars coming)
            if (activeEvent == TrafficEventType.Roadworks && i == roadworksBlockedLane)
            {
                laneAnger[i] = Mathf.Clamp01(laneAnger[i] - angerReliefPerSecond_Go * Time.deltaTime);
                continue;
            }

            if (laneStopped)
                laneAnger[i] = Mathf.Clamp01(laneAnger[i] + gain * Time.deltaTime);
            else
                laneAnger[i] = Mathf.Clamp01(laneAnger[i] - angerReliefPerSecond_Go * Time.deltaTime);

            if (laneAnger[i] >= 1f)
            {
                // Unleash all cars on this lane — they floor it through the stop
                UnleashLane(i);
                AddStrike($"{stopLines[i].laneOrientation} Lane anger overflow!");
                laneAnger[i] = 0f;
                ResetCombo();
            }

            // Push anger value to the stop line so it can swap mood sprites
            stopLines[i].SetAnger(laneAnger[i]);
        }
    }

    // ═══════════════════════════════════════════════════
    //  EVENTS
    // ═══════════════════════════════════════════════════

    /// <summary>Make every car on the given lane go rogue (ignore stops, floor it).</summary>
    void UnleashLane(int lane)
    {
        if (lane >= 0 && lane < spawners.Length && spawners[lane] != null)
            spawners[lane].UnleashAll();
    }

    void UpdateEvents()
    {
        if (activeEvent == TrafficEventType.None)
        {
            if (Time.time >= nextEventTime)
                StartRandomEvent();
        }
        else
        {
            if (Time.time >= eventEndTime)
                EndEvent();
        }
    }

    void ScheduleNextEvent()
    {
        nextEventTime = Time.time + Random.Range(eventMinInterval, eventMaxInterval);
    }

    void StartRandomEvent()
    {
        // Prevent re-entry: block UpdateEvents from calling this again during the warning delay
        nextEventTime = float.MaxValue;

        float r = Random.value;

        TrafficEventType chosenEvent;
        string warningMsg;
        
        if (r < 0.30f)
        {
            chosenEvent = TrafficEventType.Rain;
            warningMsg = "<color=#4488FF>RAIN INCOMING!</color> Cars will slide!";
        }
        else if (r < 0.55f)
        {
            chosenEvent = TrafficEventType.Roadworks;
            warningMsg = "<color=#FFAA00>ROADWORKS!</color> A lane will be blocked!";
            if (stopLines[roadworksBlockedLane] != null)
            {
                warningMsg = $"<color=#AAAAFF>OLD PERSON CROSSING!</color> The {stopLines[roadworksBlockedLane].laneOrientation} lane is crawling!";
            }
        }
        else if (r < 0.75f)
        {
            chosenEvent = TrafficEventType.OldPerson;
            warningMsg = "<color=#AAAAFF>OLD PERSON CROSSING!</color> One lane is crawling!";
            if (stopLines[oldPersonLane] != null)
            {
                warningMsg = $"<color=#AAAAFF>OLD PERSON CROSSING!</color> The {stopLines[oldPersonLane].laneOrientation} lane is crawling!";
            }
        }
        else
        {
            chosenEvent = TrafficEventType.Ambulance;
            warningMsg = "<color=#FF4444>AMBULANCE!</color> Keep lanes open!";
        }
        
        ShowEventIcon(chosenEvent);
        
        // Show warning, then activate after delay
        if (minigameCanvasUI != null)
            minigameCanvasUI.ShowWarning(warningMsg, warningDuration);
        
        StartCoroutine(ActivateEventDelayed(chosenEvent, warningDuration));
    }
    
    System.Collections.IEnumerator ActivateEventDelayed(TrafficEventType eventType, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (gameEnded) yield break;
        
        activeEvent = eventType;
        
        if (eventType == TrafficEventType.Roadworks && spawners.Length > 0)
            roadworksBlockedLane = Random.Range(0, spawners.Length);
        else if (eventType == TrafficEventType.OldPerson && spawners.Length > 0)
            oldPersonLane = Random.Range(0, spawners.Length);
        else if (eventType == TrafficEventType.Ambulance && spawners.Length > 0)
        {
            int lane = Random.Range(0, spawners.Length);
            if (spawners[lane] != null)
                spawners[lane].ForceSpawn(forceAmbulance: true);
        }

        eventEndTime = Time.time + Random.Range(eventDurationMin, eventDurationMax);
        Debug.Log("Event started: " + activeEvent);
    }

    void EndEvent()
    {
        Debug.Log("Event ended: " + activeEvent);
        HideEventIcon();
        if (activeEvent == TrafficEventType.Roadworks && roadworksBlockedLane >= 0
            && roadworksBlockedLane < spawners.Length)
        {
            lanePaused[roadworksBlockedLane] = false;
        }

        roadworksBlockedLane = -1;
        oldPersonLane = -1;
        activeEvent = TrafficEventType.None;
        ScheduleNextEvent();
    }
    
    private void ShowEventIcon(TrafficEventType eventType)
    {
        if (eventIcon == null || eventIcons == null || eventIcons.Count == 0) return;
        
        Sprite iconSprite = null;
        switch (eventType)
        {
            case TrafficEventType.Rain:
                iconSprite = eventIcons.Find(s => s.name == "RainIcon");
                break;
            case TrafficEventType.Roadworks:
                iconSprite = eventIcons.Find(s => s.name == "RoadworksIcon");
                break;
            case TrafficEventType.OldPerson:
                iconSprite = eventIcons.Find(s => s.name == "OldPersonIcon");
                break;
            case TrafficEventType.Ambulance:
                iconSprite = eventIcons.Find(s => s.name == "AmbulanceIcon");
                break;
        }

        if (iconSprite != null)
        {
            eventIcon.sprite = iconSprite;
            eventIcon.gameObject.SetActive(true);
        }
        
        /*if(eventIconBackground != null)
            eventIconBackground.gameObject.SetActive(true);*/

        if (iconSprite == null)
        {
            Debug.LogWarning($"No icon found for event type {eventType}");
            eventIcon.gameObject.SetActive(false);
            if (eventIconBackground != null)
                eventIconBackground.gameObject.SetActive(false);
        }

    }
    
    private void HideEventIcon()
    {
        if (eventIcon != null)
            eventIcon.gameObject.SetActive(false);
        if (eventIconBackground != null)
            eventIconBackground.gameObject.SetActive(false);
    }

    // ═══════════════════════════════════════════════════
    //  SCORING
    // ═══════════════════════════════════════════════════

    /// <summary>Returns the combo-based score multiplier (1x → 2x → 3x → 5x).</summary>
    public int GetComboMultiplier()
    {
        if (combo >= comboTier3) return 5;
        if (combo >= comboTier2) return 3;
        if (combo >= comboTier1) return 2;
        return 1;
    }
    
    /// <summary>Returns the combined multiplier including time pressure.</summary>
    float GetTotalMultiplier()
    {
        float mul = GetComboMultiplier();
        if (timeRemaining <= timePressureThreshold)
            mul *= timePressureMultiplier;
        return mul;
    }

    public void AwardCorrect()
    {
        combo++;
        if (combo > bestCombo) bestCombo = combo;

        int gained = basePoints + Mathf.FloorToInt(combo * 0.25f);
        gained = Mathf.FloorToInt(gained * GetTotalMultiplier());
        score += gained;
        
        // Check combo milestones
        CheckComboMilestones();
        
        UpdateUi();
        UpdateMultiplierDisplay();
    }

    public void AwardCorrect(string reason)
    {
        AwardCorrect();
        Debug.Log("Correct: " + reason);
    }

    /// <summary>Award points and relieve anger on a specific lane.</summary>
    public void AwardCorrect(string reason, int lane)
    {
        AwardCorrect();
        Debug.Log("Correct: " + reason);

        // Relieve anger on the lane that earned the point
        if (lane >= 0 && lane < laneAnger.Length)
            laneAnger[lane] = Mathf.Clamp01(laneAnger[lane] - 0.04f);
    }
    
    // ── Combo Milestones ────────────────────────────
    
    void CheckComboMilestones()
    {
        int milestone = 0;
        int bonus = 0;
        string label = "";
        
        if (combo >= comboTier3 && lastMilestoneHit < comboTier3)
        {
            milestone = comboTier3;
            bonus = milestone4Bonus;
            label = "<color=#FF2200>*** UNSTOPPABLE</color>";
        }
        else if (combo >= 15 && lastMilestoneHit < 15)
        {
            milestone = 15;
            bonus = milestone3Bonus;
            label = "<color=#FF4400>** ON FIRE</color>";
        }
        else if (combo >= comboTier2 && lastMilestoneHit < comboTier2)
        {
            milestone = comboTier2;
            bonus = milestone2Bonus;
            label = "<color=#FF6600>* GREAT STREAK</color>";
        }
        else if (combo >= comboTier1 && lastMilestoneHit < comboTier1)
        {
            milestone = comboTier1;
            bonus = milestone1Bonus;
            label = "<color=#FFDD00>* NICE COMBO</color>";
        }
        
        if (milestone > 0)
        {
            lastMilestoneHit = milestone;
            score += bonus;
            Debug.Log($"Combo milestone {milestone}! Bonus: +{bonus}");
            if (minigameCanvasUI != null)
                minigameCanvasUI.ShowBonusFlash($"{label}! x{combo} Combo! +{bonus}");
        }
    }
    
    // ── Near-Miss Bonus ─────────────────────────────
    
    /// <summary>Called by CarAI when a car stops very close to the line at high speed.</summary>
    public void AwardNearMiss(int lane)
    {
        score += nearMissBonus;
        combo++;
        if (combo > bestCombo) bestCombo = combo;
        Debug.Log($"NEAR MISS on lane {lane + 1}! +{nearMissBonus}");
        if (minigameCanvasUI != null)
            minigameCanvasUI.ShowBonusFlash($"<color=#00FF88>NEAR MISS!</color> +{nearMissBonus}");
        UpdateUi();
    }
    
    // ── Close Call Bonus ────────────────────────────
    
    /// <summary>Called by CarAI when two cars come very close but don't crash.</summary>
    public void AwardCloseCall()
    {
        if (Time.time - lastCloseCallTime < closeCallCooldown) return;
        lastCloseCallTime = Time.time;
        
        score += closeCallBonus;
        Debug.Log($"CLOSE CALL! +{closeCallBonus}");
        if (minigameCanvasUI != null)
            minigameCanvasUI.ShowBonusFlash($"<color=#FF44FF>CLOSE CALL!</color> +{closeCallBonus}");
        UpdateUi();
    }
    
    // ── Quick Toggle Bonus ──────────────────────────
    
    void RecordToggle(int laneIndex)
    {
        float now = Time.time;
        
        // Purge old entries outside the window
        while (recentToggleTimes.Count > 0 && now - recentToggleTimes[0] > quickToggleWindow)
        {
            recentToggleTimes.RemoveAt(0);
            recentToggleLanes.RemoveAt(0);
        }
        
        // Only count distinct lanes
        if (!recentToggleLanes.Contains(laneIndex))
        {
            recentToggleTimes.Add(now);
            recentToggleLanes.Add(laneIndex);
        }
        
        if (recentToggleLanes.Count >= quickToggleRequirement)
        {
            score += quickToggleBonus;
            Debug.Log($"QUICK TOGGLE! {recentToggleLanes.Count} lanes in {quickToggleWindow}s! +{quickToggleBonus}");
            if (minigameCanvasUI != null)
                minigameCanvasUI.ShowBonusFlash($"<color=#00DDFF>QUICK TOGGLE!</color> +{quickToggleBonus}");
            recentToggleTimes.Clear();
            recentToggleLanes.Clear();
            UpdateUi();
        }
    }
    
    // ── Multiplier Display ──────────────────────────
    
    void UpdateMultiplierDisplay()
    {
        if (minigameCanvasUI == null) return;
        
        float mul = GetTotalMultiplier();
        if (mul > 1f)
            minigameCanvasUI.UpdateMultiplier($"x{mul:F0} MULTIPLIER");
        else
            minigameCanvasUI.HideMultiplier();
    }

    public void Penalize(string reason)
    {
        // Cooldown: ignore rapid-fire penalties (e.g. two cars in the same crash)
        if (Time.time - lastPenaltyTime < penaltyCooldown) return;
        lastPenaltyTime = Time.time;

        AddStrike(reason);
        ResetCombo();
    }

    void ResetCombo()
    {
        combo = 0;
        lastMilestoneHit = 0;
        UpdateMultiplierDisplay();
    }

    void AddStrike(string reason)
    {
        strikes++;
        strikeTimestamps.Add(Time.time);
        Debug.Log($"Strike: {reason} ({strikes}/{maxStrikes})");
        UpdateUi();
        
        // Show strike reason on screen
        if (minigameCanvasUI != null)
        {
            if (strikeReasonCoroutine != null) StopCoroutine(strikeReasonCoroutine);
            strikeReasonCoroutine = StartCoroutine(ShowStrikeReason(reason));
        }
        
        if (strikes >= maxStrikes)
        {
            EndGame(false); // Lost by too many strikes
        }
    }
    
    // ═══════════════════════════════════════════════════
    //  STRIKE DECAY
    // ═══════════════════════════════════════════════════

    void UpdateStrikeDecay()
    {
        if (strikeTimestamps.Count == 0) return;

        bool changed = false;
        for (int i = strikeTimestamps.Count - 1; i >= 0; i--)
        {
            if (Time.time - strikeTimestamps[i] >= strikeDecayTime)
            {
                strikeTimestamps.RemoveAt(i);
                strikes = Mathf.Max(0, strikes - 1);
                changed = true;
                Debug.Log($"Strike decayed! ({strikes}/{maxStrikes})");
            }
        }

        if (changed)
        {
            UpdateUi();
            if (minigameCanvasUI != null)
            {
                if (strikeReasonCoroutine != null) StopCoroutine(strikeReasonCoroutine);
                strikeReasonCoroutine = StartCoroutine(ShowStrikeRecovery());
            }
        }
    }

    System.Collections.IEnumerator ShowStrikeRecovery()
    {
        if(AudioManager.instance != null)
        {
            AudioManager.instance.scribble.start();
        }

        minigameCanvasUI.ShowAdditionalInfo();
        minigameCanvasUI.UpdateAdditionalInfo("<color=#44FF44>Strike Removed!</color>");
        yield return new WaitForSeconds(1.5f);
        minigameCanvasUI.HideAdditionalInfo();
    }

    System.Collections.IEnumerator ShowStrikeReason(string reason)
    {
        minigameCanvasUI.ShowAdditionalInfo();
        minigameCanvasUI.UpdateAdditionalInfo($"<color=#FF4444>STRIKE:</color> {reason}");
        yield return new WaitForSeconds(2f);
        minigameCanvasUI.HideAdditionalInfo();
    }


    // ═══════════════════════════════════════════════════
    //  UI
    // ═══════════════════════════════════════════════════

    private void SetupUi()
    {
        if (minigameCanvasUI != null)
        {
            minigameCanvasUI.SetUpUI(true, true, true, false, true); // Enable timer & additional info
            
            minigameCanvasUI.UpdateScore(score);
            minigameCanvasUI.UpdatePlayerLives($"Strikes: {strikes}/{maxStrikes}");
            minigameCanvasUI.UpdateTimer("Time: 2:00");
            minigameCanvasUI.HideAdditionalInfo(); // Start hidden
        }
    }

    private void UpdateUi()
    {
        if (minigameCanvasUI != null)
        {
            minigameCanvasUI.UpdateScore(combo > 0 
                ? $"Score: {score}  |  Combo: x{combo}" 
                : $"Score: {score}");
            minigameCanvasUI.UpdatePlayerLives($"Strikes: {strikes}/{maxStrikes}");
        }
    }
}
