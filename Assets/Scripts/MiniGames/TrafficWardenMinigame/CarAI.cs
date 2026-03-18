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

    void Update()
    {
        var mg = TrafficWardenMinigameController.I;
        
        // Check specific stop line state if we have one, otherwise check any
        bool stopActive;
        if (currentStopLine != null)
            stopActive = mg != null && mg.IsStopActive(currentStopLine);
        else
            stopActive = mg != null && mg.IsStopActive();

        float effectiveBrake = brake;
        if (mg != null && mg.activeEvent == TrafficEventType.Rain)
            effectiveBrake *= 0.65f; // slippery

        float target = maxSpeed;

        // Stop line logic
        if (nearStopLine && stopActive && shouldObey && !hasCrossedLine)
            target = 0f;

        // Check for car ahead (simple raycast)
        CheckCarAhead();
        
        // Follow distance logic - slow down if too close to car ahead
        if (carAhead != null)
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
            Destroy(this.gameObject);

            if (AudioManager.instance != null)
            {
                AudioManager.instance.carCrash.start();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("StopLine")) return;

        Debug.Log("Collided with " + other.name);
        // Crossed the line
        nearStopLine = false;
        hasCrossedLine = true;

        var mg = TrafficWardenMinigameController.I;
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
                else mg.AwardCorrect("Stopped correctly");
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
            mg.AwardCorrect("Flowed on GO");
        }
        
        // Clear the stop line reference after crossing
        currentStopLine = null;
    }
}
