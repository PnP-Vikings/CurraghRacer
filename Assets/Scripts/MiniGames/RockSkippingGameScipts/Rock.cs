using System;
using UnityEngine;
using DG.Tweening;
using Random = UnityEngine.Random;

public class Rock : MonoBehaviour
{
    [Header("Rock Stats")]
    public float dragAmount = 0.995f;  // Very slight drag
    public float bounceForce = 4f;     
    public int maxBounces = 3;
    private int currentBounces = 0;
    private Rigidbody rb;
    private Collider rockCollider;
    public bool isThrown = false;
    public RockType rockType;
    public RockVisual rockVisual;
    
    // Collision settings
    private float throwTime = 0f;
    private float collisionGracePeriod = 0.5f; // Longer grace period to clear obstacles
    private float maxFlightTime = 15f; // Max time before forcing sink
    private bool collisionsEnabled = false;
    private bool hasHitWater = false; // Track if we've hit water at least once
    
    // These are kept for compatibility but not used for acceleration anymore
    [HideInInspector] public float acceleration = 1f;
    [HideInInspector] public float speed = 0f;
    
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
    
    // Floating animation for preview
    private Tweener floatTweener;
    private bool isPreviewMode = false;
    
    void Awake()
    {
        EnsureRigidbody();
    }
    
    void Start()
    {
        EnsureRigidbody();
        startPosition = transform.position;
        lastPosition = transform.position;
    }
    
    private void EnsureRigidbody()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            Debug.Log("Rock: Added missing Rigidbody component");
        }
        
        // Configure rigidbody for skipping rock physics
        rb.mass = 0.5f;           // Light rock
        rb.linearDamping = 0.2f;  // Some air resistance
        rb.angularDamping = 0.5f;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // Get collider reference
        if (rockCollider == null)
            rockCollider = GetComponent<Collider>();
    }
    
    /// <summary>
    /// Start floating animation for preview mode while charging
    /// </summary>
    public void StartPreviewMode(Vector3 position)
    {
        EnsureRigidbody();
        
        isPreviewMode = true;
        isThrown = false;
        transform.position = position;
        
        // Disable physics while in preview mode
        rb.isKinematic = true;
        rb.useGravity = false;
        
        // Start floating animation
        StopFloating();
        floatTweener = transform.DOMoveY(position.y + 0.3f, 0.8f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
        
        // Also rotate slowly
        transform.DORotate(new Vector3(0, 360, 0), 4f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }
    
    /// <summary>
    /// Stop preview mode and prepare for throwing
    /// </summary>
    public void StopPreviewMode()
    {
        isPreviewMode = false;
        StopFloating();
        
        // Re-enable physics
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }
    
    private void StopFloating()
    {
        if (floatTweener != null && floatTweener.IsActive())
        {
            floatTweener.Kill();
        }
        transform.DOKill();
    }
    
    public void Initialize(RockType rockType)
    {
        this.rockType = rockType;
        
        switch (rockType)
        {
            case RockType.Small:
                // Small rocks: light, bounce high but fewer times
                bounceForce = Random.Range(3f, 4.5f);
                maxBounces = Random.Range(2, 4);
                dragAmount = 0.997f; // Less drag, travels further
                break;
            case RockType.Medium:
                // Medium rocks: balanced
                bounceForce = Random.Range(3.5f, 5f);
                maxBounces = Random.Range(3, 5);
                dragAmount = 0.995f;
                break;
            case RockType.Large:
                // Large rocks: heavier, more bounces but lower
                bounceForce = Random.Range(3f, 4f);
                maxBounces = Random.Range(4, 7);
                dragAmount = 0.993f; // Slightly more drag
                break;
        }
    }
    
    public void SetStats(float newAcceleration, float newBounceForce, int newMaxBounces)
    {
        this.acceleration = newAcceleration;
        this.bounceForce = newBounceForce;
        this.maxBounces = newMaxBounces;
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if(!isThrown) return;
        
        if (collision.gameObject.CompareTag("Water"))
        {
            hasHitWater = true;
            HandleWaterContact();
        }
        else
        {
            // Hit something other than water
            Debug.Log($"Hit object: {collision.gameObject.name}");
            
            // Only end throw if we've hit water at least once
            // This prevents ending when brushing past obstacles on the way to water
            if (hasHitWater)
            {
                float finalDistance = CalculateTotalDistance();
                OnRockSunk?.Invoke(finalDistance);
                isThrown = false; // Stop processing
                Destroy(gameObject, 2f);
            }
            else
            {
                Debug.Log($"Ignoring collision with {collision.gameObject.name} - haven't hit water yet");
            }
        }
    }
    
    void Update()
    {
        if (!isThrown) return;
        if (rb == null) return;
        
        // Re-enable collisions after grace period
        if (!collisionsEnabled && Time.time - throwTime > collisionGracePeriod)
        {
            collisionsEnabled = true;
            if (rockCollider != null)
            {
                rockCollider.enabled = true;
                Debug.Log("Rock collisions enabled");
            }
        }
        
        // Track distance traveled
        totalDistanceTraveled += Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;
        
        // Apply slight drag to horizontal velocity (makes rock slow down naturally)
        Vector3 vel = rb.linearVelocity;
        vel.x *= dragAmount;
        vel.z *= dragAmount;
        rb.linearVelocity = vel;
        
        // Track current speed for reference
        speed = new Vector3(vel.x, 0, vel.z).magnitude;
        
        // If rock is moving too slow horizontally after bouncing, it sinks
        if (speed < 0.3f && currentBounces > 0)
        {
            Debug.Log("Rock lost momentum and sank");
            float finalDistance = CalculateTotalDistance();
            OnRockSunk?.Invoke(finalDistance);
            Destroy(gameObject, 2f);
        }
        
        // Safety: if rock falls below certain Y level, it sank
        if (transform.position.y < -10f)
        {
            Debug.Log("Rock fell too far - ending throw");
            float finalDistance = CalculateTotalDistance();
            OnRockSunk?.Invoke(finalDistance);
            Destroy(gameObject, 1f);
        }
        
        // Timeout: end throw after max flight time
        if (Time.time - throwTime > maxFlightTime)
        {
            Debug.Log("Rock flight timeout - ending throw");
            float finalDistance = CalculateTotalDistance();
            OnRockSunk?.Invoke(finalDistance);
            Destroy(gameObject, 1f);
        }
    }
    
    // Use trigger for water to get more reliable bouncing
    void OnTriggerEnter(Collider other)
    {
        if (!isThrown) return;
        
        if (other.CompareTag("Water"))
        {
            hasHitWater = true;
            HandleWaterContact();
        }
    }
    
    private void HandleWaterContact()
    {
        Debug.Log($"Water contact! Bounce {currentBounces + 1}/{maxBounces}");
        
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
            
            // Apply bounce - keep horizontal velocity, add upward force
            if (rb != null)
            {
                Vector3 currentVel = rb.linearVelocity;
                rb.linearVelocity = new Vector3(currentVel.x, effectiveBounceForce, currentVel.z);
            }
            
            currentBounces++;
            Debug.Log($"Bounced! New velocity: {rb.linearVelocity}");
        }
        else
        {
            // Rock has sunk - calculate final distance
            float finalDistance = CalculateTotalDistance();
            Debug.Log($"Rock sunk at distance: {finalDistance}m");
            OnRockSunk?.Invoke(finalDistance);
            Destroy(gameObject, 2f);
        }
    }
    
    public void ThrowRock(Vector3? initialVelocity = null)
    {
        // Stop preview mode if active
        if (isPreviewMode)
        {
            StopPreviewMode();
        }
        
        // Ensure rigidbody is initialized
        EnsureRigidbody();
        
        isThrown = true;
        throwTime = Time.time;
        collisionsEnabled = false;
        hasHitWater = false;
        startPosition = transform.position;
        lastPosition = transform.position;
        totalDistanceTraveled = 0f;
        currentBounces = 0;
        
        // Temporarily disable collider so rock can clear spawn area
        if (rockCollider != null)
        {
            rockCollider.enabled = false;
        }
        
        // Make sure physics is enabled
        rb.isKinematic = false;
        rb.useGravity = true;
        
        // Use provided velocity or default to forward * acceleration
        if (initialVelocity.HasValue)
        {
            rb.linearVelocity = initialVelocity.Value;
            speed = initialVelocity.Value.magnitude;
        }
        else
        {
            rb.linearVelocity = transform.forward * acceleration;
            speed = acceleration;
        }
        
        Debug.Log($"Rock thrown with velocity: {rb.linearVelocity}, speed: {speed}");
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
    
    private void OnDestroy()
    {
        StopFloating();
    }
    
    public enum RockType
    {
        Small,
        Medium,
        Large
    }
}
