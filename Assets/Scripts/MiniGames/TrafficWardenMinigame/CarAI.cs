using UnityEngine;

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
    public bool shouldObey = true; // set by spawner
    public bool isStopped;

    float speed;
    Vector3 dir; // Remove the = Vector3.forward initialization

    bool nearStopLine;
    bool hasCrossedLine;
    StopLine currentStopLine; // Track which specific stop line we're near

    /// <summary>Which lane this car belongs to (-1 = unassigned).</summary>
    [HideInInspector] public int laneIndex = -1;

    /// <summary>When true the car ignores stop lines and floors it.</summary>
    [HideInInspector] public bool isRogue;

    /// <summary>The spawner that created this car (for deregistration).</summary>
    [HideInInspector] public CarSpawner ownerSpawner;

    // Cached for follow behavior
    CarAI carAhead;
    float distToCarAhead;

    void Start()
    {
        speed = Random.Range(maxSpeed * 0.6f, maxSpeed * 1.1f);
        dir = transform.forward; // Use the car's local forward direction at spawn
        
        // Auto-detect car layer if not set
        if (carLayer == 0)
            carLayer = LayerMask.GetMask("Default");
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

            // High anger makes obeying cars more likely to run the stop
            if (shouldObey && !isRogue && anger > 0.7f)
            {
                // 30% chance per frame-check to go rogue at high anger (checked once)
                if (!hasCrossedLine && nearStopLine && Random.value < 0.002f * (anger - 0.7f) / 0.3f)
                {
                    GoRogue();
                    Debug.Log($"Lane {laneIndex + 1} car went rogue from impatience! (anger: {anger:F2})");
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
            }
        }
    }

    void OnDestroy()
    {
        if (ownerSpawner != null)
            ownerSpawner.Unregister(this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("StopLine"))
        {
            nearStopLine = true;
            // Try to get the StopLine component from the collider or its parent
            /*currentStopLine = other.GetComponent<StopLine>();
            if (currentStopLine == null)
                currentStopLine = other.GetComponentInParent<StopLine>();*/
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<CarAI>() != null)
        {
            CarAI car = collision.gameObject.GetComponent<CarAI>();
            if(car.isStopped || isStopped) return;
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

        // Check specific stop line state
        StopLine exitedLine = other.GetComponent<StopLine>();
        if (exitedLine == null)
            exitedLine = other.GetComponentInParent<StopLine>();
        
        bool stopActive = exitedLine != null ? mg.IsStopActive(exitedLine) : mg.IsStopActive();

        // If STOP is active:
        if (stopActive)
        {
            if (shouldObey)
            {
                // Obeying cars should be stopped when they pass the line
                if (!isStopped) mg.Penalize("Car crossed during STOP");
                else mg.AwardCorrect("Stopped correctly",laneIndex);
            }
            else
            {
                // Violator ran the stop
                mg.Penalize("Violator ran STOP");
            }
        }
        else
        {
            // GO is active
            mg.AwardCorrect("Flowed on GO",laneIndex);
        }
        
        // Clear the stop line reference after crossing
        currentStopLine = null;
    }
}
