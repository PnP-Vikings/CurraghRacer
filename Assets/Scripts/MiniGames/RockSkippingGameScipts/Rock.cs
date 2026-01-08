using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Rock : MonoBehaviour
{
    
    public float acceleration = 5f;
    public float speed = 0f;
    public float bounceForce = 5f;
    public int maxBounces = 3;
    private int currentBounces = 0;
    private Rigidbody rb;
    public bool isThrown = false;
    public RockType rockType;
    public RockVisual rockVisual;
    
    // Distance tracking
    private Vector3 startPosition;
    private float totalDistanceTraveled = 0f;
    private Vector3 lastPosition;
    
    // Bounce timing system
    private float pendingBounceMultiplier = 1f;
    private bool hasPendingMultiplier = false;
    
    // Events for timing system
    public event Action OnWaterContact;
    public event Action<float> OnRockSunk;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = transform.forward * speed;
        startPosition = transform.position;
        lastPosition = transform.position;
    }
    
    public void Initialize(RockType rockType)
    {
        switch (rockType)
        {
            case RockType.Small:
                acceleration = Random.Range(8f, 15f);
                bounceForce = Random.Range(4f, 6f);
                maxBounces = Random.Range(2,3);
                break;
            case RockType.Medium:
                acceleration = Random.Range(8f, 15f);
                bounceForce = Random.Range(8f, 15f);
                maxBounces = Random.Range(2, 4);
                break;
            case RockType.Large:
                acceleration = Random.Range(7f, 10f);
                bounceForce = Random.Range(4f, 6f);
                maxBounces = Random.Range(4, 8);
                break;
        }
    }
    
    public void SetStats(float acceleration, float bounceForce, int maxBounces)
    {
        this.acceleration = acceleration;
        this.bounceForce = bounceForce;
        this.maxBounces = maxBounces;
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if(!isThrown) return;
        
        if (collision.gameObject.CompareTag("Water"))
        {
            if (currentBounces < maxBounces)
            {
                // Fire event for timing system
                OnWaterContact?.Invoke();
                
                // Apply pending multiplier from timing system
                float effectiveBounceForce = bounceForce;
                if (hasPendingMultiplier)
                {
                    effectiveBounceForce *= pendingBounceMultiplier;
                    hasPendingMultiplier = false;
                    pendingBounceMultiplier = 1f;
                }
                
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, effectiveBounceForce, rb.linearVelocity.z);
                currentBounces++;
            }
            else
            {
                // Rock has sunk - calculate final distance
                float finalDistance = CalculateTotalDistance();
                OnRockSunk?.Invoke(finalDistance);
                Destroy(gameObject, 2f);
            }
        }
        else
        {
            // Hit something other than water
            float finalDistance = CalculateTotalDistance();
            OnRockSunk?.Invoke(finalDistance);
            Destroy(gameObject, 3f);
        }
    }
    
    void Update()
    {
        if (!isThrown) return;
        
        // Track distance traveled
        totalDistanceTraveled += Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;
        
        speed += acceleration * Time.deltaTime;
        rb.linearVelocity = rb.linearVelocity.normalized * speed;
    }
    
    public void ThrowRock()
    {
        isThrown = true;
        startPosition = transform.position;
        lastPosition = transform.position;
        totalDistanceTraveled = 0f;
        rb.linearVelocity = transform.forward * acceleration;
    }
    
    /// <summary>
    /// Apply a multiplier to the next bounce (from timing system)
    /// </summary>
    public void ApplyBounceMultiplier(float multiplier)
    {
        pendingBounceMultiplier = multiplier;
        hasPendingMultiplier = true;
    }
    
    /// <summary>
    /// Calculate total horizontal distance from start position
    /// </summary>
    public float CalculateTotalDistance()
    {
        Vector3 flatStart = new Vector3(startPosition.x, 0, startPosition.z);
        Vector3 flatEnd = new Vector3(transform.position.x, 0, transform.position.z);
        return Vector3.Distance(flatStart, flatEnd);
    }
    
    /// <summary>
    /// Get the current bounce count
    /// </summary>
    public int GetCurrentBounces() => currentBounces;
    
    /// <summary>
    /// Get total distance traveled (including vertical movement)
    /// </summary>
    public float GetTotalDistanceTraveled() => totalDistanceTraveled;
    
   
    
    public enum RockType
    {
        Small,
        Medium,
        Large
    }
}
