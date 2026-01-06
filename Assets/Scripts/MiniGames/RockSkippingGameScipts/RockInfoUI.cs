using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Optional UI component to display rock statistics on hover
/// </summary>
public class RockInfoUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TextMeshProUGUI rockTypeText;
    [SerializeField] private TextMeshProUGUI accelerationText;
    [SerializeField] private TextMeshProUGUI bounceForceText;
    [SerializeField] private TextMeshProUGUI maxBouncesText;
    
    [Header("Settings")]
    [SerializeField] private Vector2 offset = new Vector2(10, 10);
    [SerializeField] private bool followMouse = true;
    
    private RectTransform rectTransform;
    private Canvas canvas;
    
    private void Awake()
    {
        rectTransform = infoPanel.GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        HideInfo();
    }
    
    /*private void Update()
    {
        if (followMouse && infoPanel.activeSelf)
        {
            FollowMousePosition();
        }
    }*/
    
    public void ShowRockInfo(Rock rock)
    {
        if (rock == null) return;
        
        infoPanel.SetActive(true);
        
        // Update text with rock stats
        if (rockTypeText != null)
            rockTypeText.text = $"Type: {rock.rockType}";
        
        if (accelerationText != null)
            accelerationText.text = $"Acceleration: {rock.acceleration:F1}";
        
        if (bounceForceText != null)
            bounceForceText.text = $"Bounce Force: {rock.bounceForce:F1}";
        
        if (maxBouncesText != null)
            maxBouncesText.text = $"Max Bounces: {rock.maxBounces}";
        
        //FollowMousePosition();
    }
    
    public void HideInfo()
    {
        infoPanel.SetActive(false);
    }
    
    private void FollowMousePosition()
    {
        if (canvas == null || rectTransform == null) return;
        
        // Get pointer position using new Input System
        Vector2 pointerPosition;
        if (!InputHelpers.TryGetPrimaryPointerPosition(out pointerPosition))
        {
            return; // No valid input
        }
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            pointerPosition,
            canvas.worldCamera,
            out localPoint
        );
        
        rectTransform.localPosition = localPoint + offset;
        
        // Keep UI within screen bounds
        ClampToScreen();
    }
    
    private void ClampToScreen()
    {
        Vector3 pos = rectTransform.localPosition;
        Vector2 sizeDelta = rectTransform.sizeDelta;
        
        RectTransform canvasRect = canvas.transform as RectTransform;
        Vector2 canvasSize = canvasRect.sizeDelta;
        
        // Clamp X
        if (pos.x + sizeDelta.x > canvasSize.x / 2)
            pos.x = canvasSize.x / 2 - sizeDelta.x;
        if (pos.x < -canvasSize.x / 2)
            pos.x = -canvasSize.x / 2;
        
        // Clamp Y
        if (pos.y + sizeDelta.y > canvasSize.y / 2)
            pos.y = canvasSize.y / 2 - sizeDelta.y;
        if (pos.y < -canvasSize.y / 2)
            pos.y = -canvasSize.y / 2;
        
        rectTransform.localPosition = pos;
    }
}

