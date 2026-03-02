using UnityEngine;

public enum TrafficEventType { None, Ambulance, Rain, Roadworks }

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

    float lastToggleTime;
    float nextEventTime;
    float eventEndTime;
    
    [Header("Stop Lines")]
    public StopLine[] stopLines;

    void Awake()
    {
        I = this;
        lastToggleTime = -999f;
        playerInputs = new PlayerInputs();
        playerInputs.Enable();
        playerInputs.TrafficWardenGame.Enable();
        playerInputs.TrafficWardenGame.ToggleLane1.performed += ctx => ToggleLane1();
        playerInputs.TrafficWardenGame.ToggleLane2.performed += ctx => ToggleLane2();
        playerInputs.TrafficWardenGame.ToggleLane3.performed += ctx => ToggleLane3();
        playerInputs.TrafficWardenGame.ToggleLane4.performed += ctx => ToggleLane4();
        playerInputs.TrafficWardenGame.ToggleAllLanes.performed += ctx => Toggle();
        ScheduleNextEvent();
    }

    void Update()
    {
        UpdateAnger();
        UpdateEvents();
    }

    // -----------------------------
    // STATE TOGGLE
    // -----------------------------


    public void ToggleLane1()
    {
        if (stopLines[0] != null)
        {
          
            if (activeEvent == TrafficEventType.Ambulance && stopLines[0].GetState() == CrossingState.Go)
            {
                AddStrike("Stopped during Ambulance event");
                ResetCombo();
                return;
            }

            stopLines[0].ChangeState();
            Debug.Log($"Toggling lane 1 and to state  {stopLines[0].GetState()}");
            lastToggleTime = Time.time;
        }
    }
    
    public void ToggleLane2()
    {
        if (stopLines[1] != null)
        {
            if (activeEvent == TrafficEventType.Ambulance && stopLines[1].GetState() == CrossingState.Go)
            {
                AddStrike("Stopped during Ambulance event");
                ResetCombo();
                return;
            }

            stopLines[1].ChangeState();
            Debug.Log($"Toggling lane 2 and to state  {stopLines[1].GetState()}");
            lastToggleTime = Time.time;
        }
    }
    
    public void ToggleLane3()
    {
        if (stopLines[2] != null)
        {
            if (activeEvent == TrafficEventType.Ambulance && stopLines[2].GetState() == CrossingState.Go)
            {
                AddStrike("Stopped during Ambulance event");
                ResetCombo();
                return;
            }

            stopLines[2].ChangeState();
            Debug.Log($"Toggling lane 3 and to state  {stopLines[2].GetState()}");
            lastToggleTime = Time.time;
        }
    }
    
    public void ToggleLane4()
    {
        if (stopLines[3] != null)
        {
            if (activeEvent == TrafficEventType.Ambulance && stopLines[3].GetState() == CrossingState.Go)
            {
                AddStrike("Stopped during Ambulance event");
                ResetCombo();
                return;
            }

            stopLines[3].ChangeState();
            Debug.Log($"Toggling lane 4 and to state  {stopLines[3].GetState()}");
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

    // -----------------------------
    // ANGER SYSTEM
    // -----------------------------

    void UpdateAnger()
    {
        float gain = angerGainPerSecond_Stop;

        if (activeEvent == TrafficEventType.Roadworks)
            gain *= 1.6f;

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

    // -----------------------------
    // EVENTS
    // -----------------------------

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

        if (r < 0.4f) activeEvent = TrafficEventType.Rain;
        else if (r < 0.7f) activeEvent = TrafficEventType.Roadworks;
        else activeEvent = TrafficEventType.Ambulance;

        eventEndTime = Time.time + Random.Range(eventDurationMin, eventDurationMax);

        Debug.Log("Event started: " + activeEvent);
    }

    void EndEvent()
    {
        Debug.Log("Event ended: " + activeEvent);
        activeEvent = TrafficEventType.None;
        ScheduleNextEvent();
    }

    // -----------------------------
    // SCORING
    // -----------------------------

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
