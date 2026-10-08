using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreTooltip : MonoBehaviour
{
  [SerializeField] private TMP_Text itemName;
  [SerializeField] private TMP_Text itemPrice;
  [SerializeField] private TMP_Text itemDescription;
 
  [Header("Tooltip UI")]
  public GameObject tooltipPanel;          // Root object with an Image (background)
  public Image tooltipBackground;          // Background image
  public Canvas parentCanvas;              // Canvas that contains this tooltip
  
  [Header("Tooltip Styling")]
  public Color backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
  public Color borderColor = new Color(0.4f, 0.6f, 0.8f, 1f);  // (not used here but kept for future)
  public Color textColor = new Color(0.9f, 0.95f, 1f, 1f);
  
  [Header("Settings")]
  public float showDelay = 0.3f;
  public Vector2 offset = new Vector2(15, 10);
  public float maxTooltipWidth = 400f;
  public float minTooltipWidth = 250f;
  public float paddingHorizontal = 12f;
  public float paddingVertical = 8f;
  public float spacing = 4f;
  
  // Internal state
  private bool isHovering = false;
  
  public void UpdateText(string itemName, string itemPrice, string itemDescription,bool showToolTip = true)
  {
    this.itemName.text = itemName;
    this.itemPrice.text = $"€ {itemPrice}";
    this.itemDescription.text = itemDescription;
    
    if(showToolTip)
    {
        isHovering = true;
        Invoke(nameof(DisplayTooltip), showDelay);
    }
    else
    {
        HideTooltip();
    }
  }
  
  public void HideTooltip()
  {
      isHovering = false;
      CancelInvoke(nameof(DisplayTooltip));

      if (tooltipPanel != null)
          tooltipPanel.SetActive(false);
  }
  
   private void DisplayTooltip()
    {
        if (!isHovering || string.IsNullOrEmpty(itemName.text)) return;
        
        // Setup references
        if (parentCanvas == null) parentCanvas = this.GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;

        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        if (canvasRect == null || tooltipRect == null) return;

       
        // Convert mouse to canvas local space
        Vector2 mouse;
        if (!InputHelpers.TryGetPrimaryPointerPosition(out mouse))
            return;
        Vector2 mouseLocal;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, mouse, parentCanvas.worldCamera, out mouseLocal))
            return;

        // ---------- Auto-size ----------
        Vector2 canvasSize = canvasRect.sizeDelta;

        float availableWidth = Mathf.Min(maxTooltipWidth, canvasSize.x * 0.9f);
        float maxTextPreferredWidth = itemName.preferredWidth;
        if (itemPrice != null && !string.IsNullOrEmpty(itemPrice.text))
            maxTextPreferredWidth = Mathf.Max(maxTextPreferredWidth, itemPrice.preferredWidth);
        if (itemDescription != null && !string.IsNullOrEmpty(itemDescription.text))
            maxTextPreferredWidth = Mathf.Max(maxTextPreferredWidth, itemDescription.preferredWidth);

        float optimalWidth = Mathf.Max(
            minTooltipWidth,
            Mathf.Min(availableWidth, maxTextPreferredWidth + paddingHorizontal * 2f)
        );

        float textWidth = optimalWidth - paddingHorizontal * 2f;

        // Constrain text width to compute preferred height
        if (itemName != null)
        {
            itemName.rectTransform.sizeDelta = new Vector2(textWidth, 0f);
            itemName.ForceMeshUpdate(true, true);
        }
        if (itemPrice != null)
        {
            itemPrice.rectTransform.sizeDelta = new Vector2(textWidth, 0f);
            itemPrice.ForceMeshUpdate(true, true);
        }
        if (itemDescription != null)
        {
            itemDescription.rectTransform.sizeDelta = new Vector2(textWidth, 0f);
            itemDescription.ForceMeshUpdate(true, true);
        }
        Canvas.ForceUpdateCanvases();

        float currentYOffset = paddingVertical;

        // Layout itemName inside (top anchored so it grows downward)
        if (itemName != null && !string.IsNullOrEmpty(itemName.text))
        {
            var textRect = itemName.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot     = new Vector2(0.5f, 1f);
            float h = itemName.preferredHeight;
            textRect.offsetMax = new Vector2(-paddingHorizontal, -currentYOffset);
            textRect.offsetMin = new Vector2( paddingHorizontal, -(currentYOffset + h));
            currentYOffset += h;
        }

        // Layout itemPrice inside
        if (itemPrice != null && !string.IsNullOrEmpty(itemPrice.text))
        {
            if (currentYOffset > paddingVertical) currentYOffset += spacing;
            var textRect = itemPrice.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot     = new Vector2(0.5f, 1f);
            float h = itemPrice.preferredHeight;
            textRect.offsetMax = new Vector2(-paddingHorizontal, -currentYOffset);
            textRect.offsetMin = new Vector2( paddingHorizontal, -(currentYOffset + h));
            currentYOffset += h;
        }

        // Layout itemDescription inside
        if (itemDescription != null && !string.IsNullOrEmpty(itemDescription.text))
        {
            if (currentYOffset > paddingVertical) currentYOffset += spacing;
            var textRect = itemDescription.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot     = new Vector2(0.5f, 1f);
            float h = itemDescription.preferredHeight;
            textRect.offsetMax = new Vector2(-paddingHorizontal, -currentYOffset);
            textRect.offsetMin = new Vector2( paddingHorizontal, -(currentYOffset + h));
            currentYOffset += h;
        }

        float maxHeight = canvasSize.y * 0.9f;                       // allow up to 90% of canvas height
        float totalHeight = Mathf.Clamp(currentYOffset + paddingVertical, 50f, maxHeight);

        // Apply final tooltip size
        Vector2 tooltipSize = new Vector2(optimalWidth, totalHeight);
        tooltipRect.sizeDelta = tooltipSize;
        
        
        // Background look & make sure it doesn't eat raycasts
        if (tooltipBackground != null)
        {
            tooltipBackground.color = backgroundColor;
            tooltipBackground.raycastTarget = false;
        }

        // ---------- Position ----------
        Vector2 pos = CalculateSmartPosition(mouseLocal, tooltipSize, canvasSize, tooltipRect);
        tooltipRect.localPosition = pos;

        // Ensure the panel never blocks input
        var cg = tooltipPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = tooltipPanel.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

        tooltipPanel.SetActive(true);
    }

    private Vector2 CalculateSmartPosition(
        Vector2 mouseLocal,
        Vector2 tooltipSize,
        Vector2 canvasSize,
        RectTransform tooltipRect)
    {
        Vector2 half = canvasSize * 0.5f;
        const float pad = 8f;

        float rightRoom  =  half.x - mouseLocal.x;
        float leftRoom   =  half.x + mouseLocal.x;
        float topRoom    =  half.y - mouseLocal.y;
        float bottomRoom =  half.y + mouseLocal.y;

        bool placeRight = rightRoom >= tooltipSize.x + offset.x + pad;
        bool placeAbove = topRoom   >= tooltipSize.y + offset.y + pad;

        // pivot matches the corner "touching" the cursor
        tooltipRect.pivot = new Vector2(placeRight ? 0f : 1f, placeAbove ? 1f : 0f);

        Vector2 pos = mouseLocal;
        pos.x += placeRight ?  offset.x : -offset.x;
        pos.y += placeAbove ?  offset.y : -offset.y;

        if (!placeRight) pos.x -= tooltipSize.x;
        if (!placeAbove) pos.y -= tooltipSize.y;

        // clamp fully inside canvas
        float minX = -half.x + pad;
        float maxX =  half.x - tooltipSize.x - pad;
        float minY = -half.y + pad;
        float maxY =  half.y - tooltipSize.y - pad;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        return pos;
    }
}
