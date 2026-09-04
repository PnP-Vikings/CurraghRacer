using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.Localization;

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
    [SerializeField] private TextMeshProUGUI rockSelectionText;
    
    
    [Header("Settings")]
    [SerializeField] private Vector2 offset = new Vector2(10, 10);
    
    private RectTransform rectTransform;
    private Canvas canvas;
    
    [Header(("Localization"))]
    [SerializeField] private LocalizedString rockTypeLocalizedString = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.RockSkippingGame.InfoPanel.RockTypeText" };
    [SerializeField] private LocalizedString accelerationLocalizedString = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.RockSkippingGame.InfoPanel.Acceleration" };
    [SerializeField] private LocalizedString bounceForceLocalizedString = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.RockSkippingGame.InfoPanel.BounceForce" };
    [SerializeField] private LocalizedString maxBouncesLocalizedString = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.RockSkippingGame.InfoPanel.MaxBounces" };
    [SerializeField] private LocalizedString selectThisRockLocalizedString = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.RockSkippingGame.InfoPanel.SelectThisRockToUseText" };
    [SerializeField] private LocalizedString rockSelectedLocalizedString = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.RockSkippingGame.InfoPanel.RockSelected" };
    
    private void Awake()
    {
        rectTransform = infoPanel.GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        rockSelectionText.gameObject.SetActive(false);
        HideInfo();
    }
    
    
    public void ShowRockInfo(Rock rock)
    {
        if (rock == null) return;
        
        infoPanel.SetActive(true);
        
        // Update text with rock stats
        if (rockTypeText != null)
        {
            if(rockTypeLocalizedString != null &&!rockTypeLocalizedString.IsEmpty)
            {
                rockTypeText.text = rockTypeLocalizedString.GetLocalizedString(rock.GetLocalizedRockType());
            }
            else
            {
                rockTypeText.text = $"Type: {rock.GetLocalizedRockType()}";
            }
        }
        if (accelerationText != null)
        {
            if(accelerationLocalizedString != null && !accelerationLocalizedString.IsEmpty)
            {
                accelerationText.text = accelerationLocalizedString.GetLocalizedString(rock.acceleration.ToString("F1"));
            }
            else
            {
                accelerationText.text = $"Acceleration: {rock.acceleration:F1}";
            }
        }
        
        if (bounceForceText != null)
        {
            if(bounceForceLocalizedString != null && !bounceForceLocalizedString.IsEmpty)
            {
                bounceForceText.text = bounceForceLocalizedString.GetLocalizedString(rock.bounceForce.ToString("F1"));
            }
            else
            {
                bounceForceText.text = $"Bounce Force: {rock.bounceForce:F1}";
            }
        }
        if (maxBouncesText != null)
        {
            if(maxBouncesLocalizedString != null && !maxBouncesLocalizedString.IsEmpty)
            {
                maxBouncesText.text = maxBouncesLocalizedString.GetLocalizedString(rock.maxBounces.ToString());
            }
            else
            {
                maxBouncesText.text = $"Max Bounces: {rock.maxBounces}";
            }
        }

        if (rockSelectionText != null)
        {
            rockSelectionText.gameObject.SetActive(true);
            if(selectThisRockLocalizedString != null && !selectThisRockLocalizedString.IsEmpty)
            {
                rockSelectionText.text = selectThisRockLocalizedString.GetLocalizedString();
            }
            else
            {
                rockSelectionText.text = $"Select this rock to use it!";
            }
        }
        //FollowMousePosition();
    }
    
    public void RockSelected(Rock rock)
    {
        if (rockSelectionText != null)
        {
            if(rockSelectedLocalizedString != null && !rockSelectedLocalizedString.IsEmpty)
            {
                rockSelectionText.text = rockSelectedLocalizedString.GetLocalizedString(rock.GetLocalizedRockType());
            }
            else
            {
                rockSelectionText.text = $"{rock.GetLocalizedRockType()} rock selected!";
            }
        }
    }
    
    public void HideInfo()
    {
        rockSelectionText.gameObject.SetActive(false);
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

