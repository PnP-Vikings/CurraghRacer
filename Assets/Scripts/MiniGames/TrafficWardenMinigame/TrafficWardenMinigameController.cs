using UnityEngine;

public enum CrossingState { Go, Stop }
public enum TrafficEventType { None, Ambulance, Rain, Roadworks }

public class TrafficWardenMinigameController : MonoBehaviour
{
    public static TrafficWardenMinigameController I;

    [Header("State")]
    public CrossingState state = CrossingState.Go;

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

    void Awake()
    {
        I = this;
        lastToggleTime = -999f;
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

    public void Toggle()
    {
        if (!CanToggle()) return;

        if (activeEvent == TrafficEventType.Ambulance && state == CrossingState.Go)
        {
            AddStrike("Stopped during Ambulance event");
            ResetCombo();
            return;
        }

        state = (state == CrossingState.Go) ? CrossingState.Stop : CrossingState.Go;
        lastToggleTime = Time.time;
    }

    public bool IsStopActive() => state == CrossingState.Stop;

    bool CanToggle()
    {
        float t = Time.time - lastToggleTime;
        return state == CrossingState.Stop ? t >= minStopTime : t >= minGoTime;
    }

    // -----------------------------
    // ANGER SYSTEM
    // -----------------------------

    void UpdateAnger()
    {
        float gain = angerGainPerSecond_Stop;

        if (activeEvent == TrafficEventType.Roadworks)
            gain *= 1.6f;

        if (state == CrossingState.Stop)
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
