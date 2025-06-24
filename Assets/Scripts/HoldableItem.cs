using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class HoldableItem : MonoBehaviour, Holdable
{
    [Header("Holdable Settings")]
    [SerializeField] private bool canBeHeld = true;
    [SerializeField]
    internal float followSpeed = 10f;
    [SerializeField] private float maxHoldDistance = 5f;
    
    [Header("Physics Settings")]
    [SerializeField]
    internal bool usePhysicsWhenHeld = true;
    [SerializeField] private float heldDrag = 5f;
    [SerializeField] private float heldAngularDrag = 10f;
    
    [Header("Interaction Settings")]
    [SerializeField]
    internal LayerMask interactionLayers = -1;
    [SerializeField] private bool triggerInteractionsWhenHeld = true;
    
    [Header("Events")]
    public UnityEvent OnPickedUp;
    public UnityEvent OnReleased;
    public UnityEvent<Collider> OnInteractWithCollider;
    
    private Rigidbody rb;
    private Collider col;
    private Vector3 targetPosition;
    private bool isBeingHeld;
    
    // Store original physics values
    private float originalDrag;
    private float originalAngularDrag;
    private bool originalUseGravity;
    
    public bool CanBeHeld => canBeHeld;
    public bool IsBeingHeld => isBeingHeld;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        
        // Store original physics settings
        originalDrag = rb.linearDamping;
        originalAngularDrag = rb.angularDamping;
        originalUseGravity = rb.useGravity;
    }
    
    public void SetPositionOnHold(Vector3 position)
    {
        if (!isBeingHeld) return;
        
        targetPosition = position;
        
        if (usePhysicsWhenHeld)
        {
            // Use physics-based movement for more realistic interactions
            Vector3 direction = (targetPosition - transform.position);
            
            // Limit distance if needed
            if (direction.magnitude > maxHoldDistance)
            {
                direction = direction.normalized * maxHoldDistance;
                targetPosition = transform.position + direction;
            }
            
            // Use velocity for smoother movement
            Vector3 velocity = direction * followSpeed;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, velocity, Time.deltaTime * followSpeed);
        }
        else
        {
            // Direct position setting for precise control
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }
    }
    public Vector3 GetPositionOnHold(out Vector3 position)
    {
        throw new System.NotImplementedException();
    }

    public void OnPickup()
    {
        if (!canBeHeld) return;
        
        isBeingHeld = true;
        
        if (usePhysicsWhenHeld)
        {
            rb.linearDamping = heldDrag;
            rb.angularDamping = heldAngularDrag;
            rb.useGravity = false;
        }
        else
        {
            rb.isKinematic = true;
        }
        
        OnPickedUp.Invoke();
    }
    
    public void OnRelease()
    {
        isBeingHeld = false;
        
        // Restore original physics settings
        rb.linearDamping = originalDrag;
        rb.angularDamping = originalAngularDrag;
        rb.useGravity = originalUseGravity;
        rb.isKinematic = false;
        
        OnReleased.Invoke();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (isBeingHeld && triggerInteractionsWhenHeld && IsInInteractionLayer(other.gameObject.layer))
        {
            HandleInteraction(other);
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (isBeingHeld && triggerInteractionsWhenHeld && IsInInteractionLayer(collision.gameObject.layer))
        {
            HandleInteraction(collision.collider);
        }
    }
    
    private bool IsInInteractionLayer(int layer)
    {
        return (interactionLayers.value & (1 << layer)) != 0;
    }
    
    private void HandleInteraction(Collider otherCollider)
    {
        // Check for specific interaction components
        var interactable = otherCollider.GetComponent<IInteractable>();
        if (interactable != null)
        {
            interactable.OnInteractWithHeldItem(this);
        }
        
        // Trigger the general interaction event
        OnInteractWithCollider.Invoke(otherCollider);
        
        Debug.Log($"Held item {gameObject.name} interacted with {otherCollider.name}");
    }
    
    public void SetCanBeHeld(bool canHold)
    {
        canBeHeld = canHold;
    }
}