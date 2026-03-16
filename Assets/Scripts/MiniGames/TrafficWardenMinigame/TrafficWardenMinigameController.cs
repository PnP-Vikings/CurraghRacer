using UnityEngine;

public enum TrafficEventType { None, Ambulance, Rain, Roadworks }

/// <summary>Spawn patterns the controller can trigger on the managed spawners.</summary>
public enum SpawnPattern { Normal, Burst, RushHour, Convoy, Chaos }

public class TrafficWardenMinigameController : MonoBehaviour
{
    public static TrafficWardenMinigameController I;

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

    [Header("Anger Meter (0..1)")]
    [Range(0, 1)] public float anger;
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
    public float difficultyRampTime = 120f;           // Seconds to reach max difficulty
    public float minIntervalAtMax = 0.5f;             // Fastest spawn interval multiplier
    public float maxSpeedAtMax = 1.4f;                // Car speed multiplier at max difficulty
    public float maxViolatorBonus = 0.20f;            // Extra violator chance at max

    [Header("Spawn Patterns")]
    public float patternMinInterval = 12f;
    public float patternMaxInterval = 25f;

    // ── Private state ────────────────────────────────
    float lastToggleTime;
    float nextEventTime;
    float eventEndTime;
    float gameStartTime;

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
        I = this;
        lastToggleTime = -999f;
        gameStartTime = Time.time;

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
    }

    void Update()
    {
        UpdateDifficulty();
        UpdateAnger();
        UpdateEvents();
        UpdatePatterns();
    }

    // ═══════════════════════════════════════════════════
    //  DIFFICULTY RAMP
    // ═══════════════════════════════════════════════════

    /// <summary>0‒1 representing how far into the difficulty curve we are.</summary>
    public float DifficultyT => Mathf.Clamp01((Time.time - gameStartTime) / difficultyRampTime);

    void UpdateDifficulty()
    {
        float t = DifficultyT;

        // Gradually tighten spawn intervals & boost car speed across ALL spawners
        float intervalMul = Mathf.Lerp(1f, minIntervalAtMax, t);
        float speedMul    = Mathf.Lerp(1f, maxSpeedAtMax, t);
        float violatorAdd = Mathf.Lerp(0f, maxViolatorBonus, t);

        for (int i = 0; i < spawners.Length; i++)
        {
            if (spawners[i] == null) continue;

            spawners[i].intervalMultiplier = intervalMul;
            spawners[i].speedMultiplier    = speedMul;
            spawners[i].violatorBonus      = violatorAdd;

            // Handle roadworks blocking
            if (activeEvent == TrafficEventType.Roadworks && i == roadworksBlockedLane)
                spawners[i].paused = true;
            else if (activePattern != SpawnPattern.Normal)
                ApplyPatternToSpawner(i);
            else
                spawners[i].paused = false;
        }
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
                    car.shouldObey = true; // convoys always obey
                    car.maxSpeed *= 0.85f; // slightly slower, tight pack
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

    /// <summary>Apply per-spawner overrides based on current pattern.</summary>
    void ApplyPatternToSpawner(int i)
    {
        var s = spawners[i];
        switch (activePattern)
        {
            case SpawnPattern.RushHour:
                // Rush-hour lane spawns 3× faster, others are normal
                s.paused = false;
                if (i == rushHourLane)
                    s.intervalMultiplier *= 0.35f;
                break;

            case SpawnPattern.Burst:
                // Burst lane pauses normal spawning (ForceSpawn handles it)
                s.paused = (i == burstLane);
                break;

            case SpawnPattern.Convoy:
                // Convoy lane pauses normal spawning (ForceSpawn handles it)
                s.paused = (i == convoyLane);
                break;

            case SpawnPattern.Chaos:
                // Everything spawns fast
                s.paused = false;
                s.intervalMultiplier *= 0.4f;
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
    //  ANGER SYSTEM
    // ═══════════════════════════════════════════════════

    void UpdateAnger()
    {
        float gain = angerGainPerSecond_Stop;

        if (activeEvent == TrafficEventType.Roadworks)
            gain *= 1.6f;

        // Chaos pattern also raises anger faster
        if (activePattern == SpawnPattern.Chaos)
            gain *= 1.3f;

        bool anyStopActive = IsStopActive();

        if (anyStopActive)
            anger = Mathf.Clamp01(anger + gain * Time.deltaTime);
        else
            anger = Mathf.Clamp01(anger - angerReliefPerSecond_Go * Time.deltaTime);

        if (anger >= 1f)
        {
            AddStrike("Traffic anger overflow");
            anger = 0f;
            ResetCombo();
        }
    }

    // ═══════════════════════════════════════════════════
    //  EVENTS
    // ═══════════════════════════════════════════════════

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
            // Pick a random lane to block
            if (spawners.Length > 0)
                roadworksBlockedLane = Random.Range(0, spawners.Length);
        }
        else
        {
            activeEvent = TrafficEventType.Ambulance;
            // Force-spawn an ambulance on a random lane
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

        // Un-pause roadworks lane
        if (activeEvent == TrafficEventType.Roadworks && roadworksBlockedLane >= 0
            && roadworksBlockedLane < spawners.Length && spawners[roadworksBlockedLane] != null)
        {
            spawners[roadworksBlockedLane].paused = false;
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

        anger = Mathf.Clamp01(anger - 0.04f);
    }

    public void AwardCorrect(string reason)
    {
        AwardCorrect();
        Debug.Log("Correct: " + reason);
    }

    public void Penalize(string reason)
    {
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

        if (strikes >= maxStrikes)
        {
            Debug.Log("Traffic Warden Game Over");
        }
    }
}
