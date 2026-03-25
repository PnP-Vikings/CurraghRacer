using UnityEngine;

/// <summary>
/// Ordinary  – always obeys stop lines, never goes rogue.
/// Impatient – obeys most of the time but can occasionally run the line (anger / random).
/// Violator  – never obeys stop lines.
/// </summary>
public enum CarBehaviourType { Ordinary, Impatient, Violator }

public class CarAI : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 8f;
    public float accel = 10f;
    public float brake = 18f;

    [Header("Follow Distance")]
    public float detectDistance = 5f;      // How far ahead to look
    public float minFollowDistance = 2f;   // Minimum gap to maintain
    public LayerMask carLayer;             // Set this to the car layer in inspector

    [Header("Logic")]
    [Tooltip("Set in inspector or by spawner. Ordinary always stops, Impatient mostly stops, Violator never stops.")]
    public CarBehaviourType behaviourType = CarBehaviourType.Ordinary;
    
    /// <summary>Runtime flag – true when this car should stop at lines right now.</summary>
    public bool shouldObey = true; // derived from behaviourType at spawn
    public bool isStopped;
    
    [Header("Impatient Settings")]
    [Tooltip("Chance per second that an Impatient car decides to run a red (before anger modifier).")]
    public float impatientRunChancePerSec = 0.03f;

    float speed;
    Vector3 dir; // Remove the = Vector3.forward initialization

    bool nearStopLine;
    bool hasCrossedLine;
    StopLine currentStopLine; // Track which specific stop line we're near

    // Snapshot taken when car enters the stop-line trigger
    bool stopWasActiveOnEntry;
    bool shouldObeyOnEntry;
    float speedOnEntry; // speed when entering the stop-line zone

    /// <summary>Which lane this car belongs to (-1 = unassigned).</summary>
    [HideInInspector] public int laneIndex = -1;

    /// <summary>When true the car ignores stop lines and floors it.</summary>
    [HideInInspector] public bool isRogue;

    /// <summary>The spawner that created this car (for deregistration).</summary>
    [HideInInspector] public CarSpawner ownerSpawner;

    // Cached for follow behavior
    CarAI carAhead;
    float distToCarAhead;
    
    // Close-call detection
    [Header("Close Call")]
    public float closeCallThreshold = 1.8f;
    bool closeCallTriggered;

    /// <summary>Set to true when the game ends – car freezes in place.</summary>
    bool gameStopped;

    /// <summary>Grace period after spawn during which collisions are ignored.</summary>
    [Header("Spawn Protection")]
    public float spawnGracePeriod = 0.5f;
    float spawnTime;

    void Start()
    {
        // Derive shouldObey from the behaviour type
        switch (behaviourType)
        {
            case CarBehaviourType.Ordinary:
                shouldObey = true;
                break;
            case CarBehaviourType.Impatient:
                shouldObey = true; // starts obeying, may change at runtime
                break;
            case CarBehaviourType.Violator:
                shouldObey = false;
                break;
        }
        
        speed = Random.Range(maxSpeed * 0.6f, maxSpeed * 1.1f);
        dir = transform.forward; // Use the car's local forward direction at spawn
        spawnTime = Time.time;
        
        // Auto-detect car layer if not set
        if (carLayer == 0)
            carLayer = LayerMask.GetMask("Default");
        
        // Listen for game end
        if (TrafficWardenMinigameController.Instance != null)
            TrafficWardenMinigameController.Instance.onGameEnded.AddListener(OnGameEnded);
    }
    
    public void SetCurrentStopLine(StopLine line)
    {
        currentStopLine = line;
    }

    /// <summary>Force the car to ignore the stop line and accelerate through.</summary>
    public void GoRogue()
    {
        isRogue = true;
        shouldObey = false;
    }

    void Update()
    {
        if (gameStopped) return;
        
        var mg = TrafficWardenMinigameController.Instance;

        // Check specific stop line state
        bool stopActive;
        if (currentStopLine != null)
            stopActive = mg != null && mg.IsStopActive(currentStopLine);
        else
            stopActive = mg != null && mg.IsStopActive();

        float effectiveBrake = brake;
        if (mg != null && mg.activeEvent == TrafficEventType.Rain)
            effectiveBrake *= 0.65f; // slippery

        float target = maxSpeed;

        // ── Anger impatience: cars speed up as their lane gets angrier ──
        if (mg != null && laneIndex >= 0)
        {
            float anger = mg.GetLaneAnger(laneIndex);
            // At 0 anger: normal speed. At 1.0 anger: +40% speed boost
            target *= (1f + anger * 0.4f);

            // Only Impatient cars can go rogue from anger / randomness.
            // Ordinary cars ALWAYS obey. Violators are already non-obeying.
            if (behaviourType == CarBehaviourType.Impatient && shouldObey && !isRogue)
            {
                // Base random chance per second
                float runChance = impatientRunChancePerSec * Time.deltaTime;
                
                // Anger amplifies the chance — at anger > 0.5 it starts climbing fast
                if (anger > 0.5f)
                    runChance += 0.06f * ((anger - 0.5f) / 0.5f) * Time.deltaTime;

                if (!hasCrossedLine && nearStopLine && Random.value < runChance)
                {
                    GoRogue();
                    Debug.Log($"Lane {laneIndex + 1} Impatient car went rogue! (anger: {anger:F2})");
                }
            }
        }

        // Rogue cars ignore stop lines entirely
        if (isRogue)
        {
            target = maxSpeed * 1.5f; // floor it
        }
        // Stop line logic
        else if (nearStopLine && stopActive && shouldObey && !hasCrossedLine)
        {
            target = 0f;
        }

        // Check for car ahead (simple raycast)
        CheckCarAhead();
        
        // Follow distance logic - slow down if too close to car ahead
        if (carAhead != null && !isRogue) // rogue cars don't slow for others
        {
            if (distToCarAhead < minFollowDistance)
            {
                // Too close - match their speed or slower
                target = Mathf.Min(target, carAhead.speed * 0.8f);
            }
            else if (distToCarAhead < detectDistance)
            {
                // Approaching - gradually match speed
                float t = (distToCarAhead - minFollowDistance) / (detectDistance - minFollowDistance);
                float followSpeed = Mathf.Lerp(carAhead.speed, maxSpeed, t);
                target = Mathf.Min(target, followSpeed);
            }
        }

        float rate = (target < speed) ? effectiveBrake : accel;
        speed = Mathf.MoveTowards(speed, target, rate * Time.deltaTime);
        isStopped = speed <= 0.05f;

        transform.position += dir * (speed * Time.deltaTime);
    }

    void CheckCarAhead()
    {
        carAhead = null;
        distToCarAhead = float.MaxValue;
        
        // Raycast forward to detect cars
        if (Physics.Raycast(transform.position, dir, out RaycastHit hit, detectDistance, carLayer))
        {
            CarAI other = hit.collider.GetComponent<CarAI>();
            if (other == null)
                other = hit.collider.GetComponentInParent<CarAI>();
            
            if (other != null && other != this)
            {
                carAhead = other;
                distToCarAhead = hit.distance;
                
                // Close call detection: both cars moving, very close but no crash
                if (!closeCallTriggered && distToCarAhead < closeCallThreshold 
                    && !isStopped && !other.isStopped
                    && speed > maxSpeed * 0.4f)
                {
                    closeCallTriggered = true;
                    if (TrafficWardenMinigameController.Instance != null)
                        TrafficWardenMinigameController.Instance.AwardCloseCall();
                }
                
                // Reset close-call flag once they separate
                if (closeCallTriggered && distToCarAhead > closeCallThreshold * 2f)
                    closeCallTriggered = false;
            }
        }
    }

    void OnGameEnded()
    {
        gameStopped = true;
        speed = 0f;
        isStopped = true;
    }

    void OnDestroy()
    {
        if (TrafficWardenMinigameController.Instance != null)
            TrafficWardenMinigameController.Instance.onGameEnded.RemoveListener(OnGameEnded);
        
        if (ownerSpawner != null)
            ownerSpawner.Unregister(this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("StopLine"))
        {
            nearStopLine = true;
            
            // Snapshot the state at entry so exit evaluation is fair
            shouldObeyOnEntry = shouldObey;
            speedOnEntry = speed;
            
            var mg = TrafficWardenMinigameController.Instance;
            if (mg != null)
            {
                StopLine enteredLine = other.GetComponent<StopLine>();
                if (enteredLine == null)
                    enteredLine = other.GetComponentInParent<StopLine>();
                    
                stopWasActiveOnEntry = enteredLine != null 
                    ? mg.IsStopActive(enteredLine) 
                    : mg.IsStopActive();
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Ignore collisions during spawn grace period
        if (Time.time - spawnTime < spawnGracePeriod) return;

        if (collision.gameObject.GetComponent<CarAI>() != null)
        {
            CarAI car = collision.gameObject.GetComponent<CarAI>();
            // Also check the other car's grace period
            if (car.isStopped || isStopped) return;
            if (Time.time - car.spawnTime < car.spawnGracePeriod) return;
            
            Debug.Log("Car AI Collision");
            
            if(TrafficWardenMinigameController.Instance != null)
                TrafficWardenMinigameController.Instance.onCarCrashed.Invoke();
            
            Destroy(this.gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("StopLine")) return;

        Debug.Log("Collided with " + other.name);
        // Crossed the line
        nearStopLine = false;
        hasCrossedLine = true;

        var mg = TrafficWardenMinigameController.Instance;
        if (mg == null) return;

        // Check specific stop line state at the moment of exit
        StopLine exitedLine = other.GetComponent<StopLine>();
        if (exitedLine == null)
            exitedLine = other.GetComponentInParent<StopLine>();
        
        bool stopActiveNow = exitedLine != null ? mg.IsStopActive(exitedLine) : mg.IsStopActive();

        if (stopActiveNow)
        {
            if (shouldObeyOnEntry)
            {
                // Car was a law-abiding car when it entered
                if (!isStopped)
                {
                    // Only penalize if the stop was already active when the car entered
                    // (don't punish for a light that changed while car was mid-crossing)
                    if (stopWasActiveOnEntry && laneIndex == exitedLine.GetLaneIndex())
                        mg.Penalize("Car crossed during STOP");
                    // else: car entered on green, light changed mid-crossing — no penalty
                }
                else
                {
                    mg.AwardCorrect("Stopped correctly", laneIndex);
                    
                    // Near-miss bonus: entered fast and managed to stop
                    if (speedOnEntry > maxSpeed * 0.7f)
                    {
                        mg.AwardNearMiss(laneIndex);
                    }
                }
            }
            else
            {
               
                if (stopWasActiveOnEntry)
                    Debug.Log($"Violator ran STOP on lane {laneIndex + 1} — no player penalty.");
                
            }
        }
        else
        {
            // GO is active
            mg.AwardCorrect("Flowed on GO", laneIndex);
        }
        
        // Clear the stop line reference after crossing
        currentStopLine = null;
    }
}
