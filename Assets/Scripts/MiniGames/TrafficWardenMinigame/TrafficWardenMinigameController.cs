using UnityEngine;
using UnityEngine.Events;

public enum TrafficEventType { None, Ambulance, Rain, Roadworks }

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

    public UnityEvent onCarCrashed;
    
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

    // Roadworks blocked lane
    int roadworksBlockedLane = -1;

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

        // Initialise per-lane timers
        int count = spawners != null ? spawners.Length : 0;
        laneTimers    = new float[count];
        laneIntervals = new float[count];
        lanePaused    = new bool[count];
        for (int i = 0; i < count; i++)
        {
            RollLaneInterval(i);
            if (spawners[i] != null)
                spawners[i].laneIndex = i;
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
        onCarCrashed.AddListener(() => Penalize("Car crashed"));
    }

    void Update()
    {
        UpdateDifficulty();
        UpdateSpawning();
        CleanupLanes();
        UpdateAnger();
        UpdateEvents();
        UpdatePatterns();
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
            if (laneTimers[i] >= laneIntervals[i])
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
        // Pick a weighted random pattern; heavier ones appear later
        float r = Random.value;
        float t = DifficultyT;

        if (r < 0.30f)
            StartBurst();
        else if (r < 0.55f)
            StartRushHour();
        else if (r < 0.80f)
            StartConvoy();
        else if (t > 0.5f) // Chaos only after halfway through difficulty curve
            StartChaos();
        else
            StartBurst(); // fallback

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
    void StartBurst()
    {
        activePattern = SpawnPattern.Burst;
        patternEndTime = Time.time + 5f;
        burstLane = Random.Range(0, spawners.Length);
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
    void StartRushHour()
    {
        activePattern = SpawnPattern.RushHour;
        patternEndTime = Time.time + Random.Range(6f, 10f);
        rushHourLane = Random.Range(0, spawners.Length);
    }

    // ── Convoy: cars spawn in a tight single-file ──
    void StartConvoy()
    {
        activePattern = SpawnPattern.Convoy;
        patternEndTime = Time.time + 6f;
        convoyLane = Random.Range(0, spawners.Length);
        convoyRemaining = Random.Range(4, 7);
        convoyInterval = 0.8f;
        convoyTimer = 0f;
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
                }
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

    /// <summary>Apply per-lane overrides based on current pattern (sets lanePaused and tweaks laneIntervals).</summary>
    void ApplyPatternToLane(int i)
    {
        switch (activePattern)
        {
            case SpawnPattern.RushHour:
                lanePaused[i] = false;
                if (i == rushHourLane)
                    laneIntervals[i] *= 0.35f; // rush-hour lane spawns ~3× faster
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
                laneIntervals[i] *= 0.4f; // everything spawns fast
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
                AddStrike($"Lane {i + 1} anger overflow!");
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
        float r = Random.value;

        if (r < 0.4f)
            activeEvent = TrafficEventType.Rain;
        else if (r < 0.7f)
        {
            activeEvent = TrafficEventType.Roadworks;
            if (spawners.Length > 0)
                roadworksBlockedLane = Random.Range(0, spawners.Length);
        }
        else
        {
            activeEvent = TrafficEventType.Ambulance;
            if (spawners.Length > 0)
            {
                int lane = Random.Range(0, spawners.Length);
                if (spawners[lane] != null)
                    spawners[lane].ForceSpawn(forceAmbulance: true);
            }
        }

        eventEndTime = Time.time + Random.Range(eventDurationMin, eventDurationMax);
        Debug.Log("Event started: " + activeEvent);
    }

    void EndEvent()
    {
        Debug.Log("Event ended: " + activeEvent);

        if (activeEvent == TrafficEventType.Roadworks && roadworksBlockedLane >= 0
            && roadworksBlockedLane < spawners.Length)
        {
            lanePaused[roadworksBlockedLane] = false;
        }

        roadworksBlockedLane = -1;
        activeEvent = TrafficEventType.None;
        ScheduleNextEvent();
    }

    // ═══════════════════════════════════════════════════
    //  SCORING
    // ═══════════════════════════════════════════════════

    public void AwardCorrect()
    {
        combo++;
        if (combo > bestCombo) bestCombo = combo;

        int gained = basePoints + Mathf.FloorToInt(combo * 0.25f);
        score += gained;
        UpdateUi();
        // Relieve a little anger on the lane that scored (optional: pass lane index)
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
    }

    void AddStrike(string reason)
    {
        strikes++;
        Debug.Log($"Strike: {reason} ({strikes}/{maxStrikes})");
        UpdateUi();
        if (strikes >= maxStrikes)
        {
            Debug.Log("Traffic Warden Game Over");
            if(minigameCanvasUI != null)
                minigameCanvasUI.ShowGameOver();
        }
    }
    
    
    // ═══════════════════════════════════════════════════
    //  UI
    // ═══════════════════════════════════════════════════

    private void SetupUi()
    {
        if (minigameCanvasUI != null)
        {
            minigameCanvasUI.SetUpUI(true,false,true,false,false);
            
            minigameCanvasUI.UpdateScore(score);
            minigameCanvasUI.UpdatePlayerLives(strikes + "/" + maxStrikes);
        }
    }

    private void UpdateUi()
    {
        if (minigameCanvasUI != null)
        {
            minigameCanvasUI.UpdateScore(score);
            minigameCanvasUI.UpdatePlayerLives(strikes + "/" + maxStrikes);
        }
    }
}
