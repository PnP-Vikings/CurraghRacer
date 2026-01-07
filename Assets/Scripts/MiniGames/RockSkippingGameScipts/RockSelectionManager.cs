using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles rock selection using Unity's Input System with raycasting.
/// Works with both mouse and touch input.
/// </summary>
public class RockSelectionManager : MonoBehaviour
{
    
    public static RockSelectionManager Instance { get; private set; }
    [SerializeField] private Camera raycastCamera;
    [SerializeField] private LayerMask rockLayer;
    [SerializeField] private string rockTag = "Rock";
    
    [SerializeField] private RockVisual currentHoveredRock;
    [SerializeField] private RockVisual lastHoveredRock;
    
    [SerializeField] private bool isEnabled = true;
    
    private void Awake()
    {
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Update()
    {
        if (!isEnabled) return;
        
        HandleRockInteraction();
    }
    
    private void HandleRockInteraction()
    {
        bool hasInput = false;
        Vector2 inputPosition = Vector2.zero;
        
        // Get pointer position using InputHelpers
        if (InputHelpers.TryGetPrimaryPointerPosition(out inputPosition))
        {
            hasInput = true;
        }
        
        if (!hasInput) return;
        
        // Check if pointer is over UI
        if (InputHelpers.IsPointerOverUI(inputPosition))
        {
            // Clear hover if over UI
            if (currentHoveredRock != null)
            {
                currentHoveredRock.OnPointerExit();
                lastHoveredRock = currentHoveredRock;
                currentHoveredRock = null;
            }
            return;
        }
        
        // Perform raycast from camera through pointer position
        Ray ray = raycastCamera.ScreenPointToRay(inputPosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 100f, rockLayer))
        {
            // Check if we hit a rock
            RockVisual rock = hit.collider.GetComponent<RockVisual>();
            
            if (rock != null)
            {
                // Handle hover
                if (currentHoveredRock != rock)
                {
                    // Exit previous rock
                    if (currentHoveredRock != null)
                    {
                        currentHoveredRock.OnPointerExit();
                    }
                    
                    // Enter new rock
                    currentHoveredRock = rock;
                    currentHoveredRock.OnPointerEnter();
                    lastHoveredRock = rock;
                }
                
                // Handle click/tap
                if (WasPointerPressedThisFrame())
                {
                    rock.OnPointerClick();
                    Debug.Log($"Rock clicked: {rock.gameObject.name}");
                }
            }
            else
            {
                // Hit something else, clear hover
                ClearHover();
            }
        }
        else
        {
            // No hit, clear hover
            ClearHover();
        }
    }
    
    private void ClearHover()
    {
        if (currentHoveredRock != null)
        {
            currentHoveredRock.OnPointerExit();
            lastHoveredRock = currentHoveredRock;
            currentHoveredRock = null;
        }
    }
    
    private bool WasPointerPressedThisFrame()
    {
        // Check touch
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }
        
        // Check mouse
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
        
        return false;
    }
    
    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
        
        if (!enabled)
        {
            ClearHover();
        }
    }

    public RockVisual GetCurrentHoveredRock()
    {
        return currentHoveredRock;
    }
    
    
    private void OnDisable()
    {
        ClearHover();
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

