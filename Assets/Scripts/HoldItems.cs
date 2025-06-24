using UnityEngine;
using UnityEngine.InputSystem;

public class HoldItems : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;

    private InputAction clickAction;
    [SerializeField] private Holdable grabbed;
    [SerializeField] private bool   isHolding;
    [Tooltip("Which layers count as 'Interactable' for holding items.")]
    public LayerMask interactableLayerMask = 1 << 7; // Default to layer 7 (Interactable)
    [Tooltip("Maximum distance an object can be held from the camera")]
    [SerializeField] private float maxHoldDistance = 5f;
    [Tooltip("Initial position of grab")]
    private Vector3 initialGrabPosition;
    
    // New field to store distance from camera when grabbed
    private float grabDistance;

    void Awake()
    {
        clickAction = playerInput.actions["Click"];
        clickAction.started  += OnClickStarted;
        clickAction.canceled += OnClickCanceled;
    }

    void OnEnable()  => clickAction.Enable();
    void OnDisable() => clickAction.Disable();

    private void OnClickStarted(InputAction.CallbackContext ctx)
    {
        // raycast and grab
        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        
        // Try to hit objects in the interactableLayerMask first
        if (Physics.Raycast(ray, out var hit, 100f))
        {
            grabbed = hit.collider.GetComponent<Holdable>();
            if (grabbed != null)
            {
                Debug.Log($"Hit interactable object: {hit.collider.name}");
                isHolding = true;
                initialGrabPosition = hit.point;
                grabDistance = hit.distance;
            }
        }
        // If we didn't find an interactable or it wasn't holdable, try a general raycast
        else if (Physics.Raycast(ray, out hit, 100f))
        {
            grabbed = hit.collider.GetComponent<Holdable>();
            if (grabbed != null)
            {
                Debug.Log($"Hit general object: {hit.collider.name}");
                isHolding = true;
                initialGrabPosition = hit.point;
                grabDistance = hit.distance;
                
                // Log warning if object isn't on interactable layer
                if ((interactableLayerMask.value & (1 << hit.collider.gameObject.layer)) == 0)
                {
                    Debug.LogWarning($"Object {hit.collider.name} is holdable but not on the interactable layer.");
                }
            }
        }
    }

    private void OnClickCanceled(InputAction.CallbackContext ctx)
    {
        // release
        isHolding = false;
        grabbed    = null;
    }

    void Update()
    {
        if (isHolding && grabbed != null)
        {
            // Cast ray from camera through mouse position
            var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;
            
            // Check if ray hits an object on the interactableLayerMask
            if (Physics.Raycast(ray, out hit, 100f, interactableLayerMask))
            {
                // Move the object to follow the hit point on the interactable surface
                grabbed.SetPositionOnHold(hit.point);
            }
            else
            {
                // Fallback to horizontal plane if no interactable surface is hit
                Plane floor = new Plane(Vector3.up, Vector3.up * 1f);
                if (floor.Raycast(ray, out float enter))
                {
                    // Limit distance from camera
                    Vector3 hitPoint = ray.GetPoint(enter);
                    Vector3 cameraToPoint = hitPoint - Camera.main.transform.position;
                    
                    // If distance is greater than max hold distance, clamp it
                    if (cameraToPoint.magnitude > maxHoldDistance)
                    {
                        hitPoint = Camera.main.transform.position + (cameraToPoint.normalized * maxHoldDistance);
                    }
                    
                    grabbed.SetPositionOnHold(hitPoint);
                }
            }
        }
    }
}