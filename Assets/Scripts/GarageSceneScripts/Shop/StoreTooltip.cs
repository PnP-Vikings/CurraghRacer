using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class StoreTooltip : MonoBehaviour
{
  [Header("Current Hover Item")]
  private int currentItemPrice;
  private int currentItemEnergyRegainAmount;
  private int currentItemAmount;
  private int currentItemId;
  private ShopItemData currentItemData;
  private ItemTypes currentItemItemType;
  [Header("Tooltip UI")]
  [SerializeField] private TMP_Text itemNameText;
  [SerializeField] private TMP_Text itemPriceText;
  [SerializeField] private TMP_Text itemDescriptionText;
  [SerializeField] private TMP_Text energyRegainAmountText;
  [SerializeField] private TMP_Text itemDaysBeforeExpiryText;
  public GameObject tooltipPanel;          // Root object with an Image (background)
  public Image tooltipBackground;          // Background image
  public Canvas parentCanvas;              // Canvas that contains this tooltip
  public StoreBuyPanel storeBuyPanel;
  [Header("Tooltip Styling")]
  public Color backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
  public Color borderColor = new Color(0.4f, 0.6f, 0.8f, 1f);  // (not used here but kept for future)
  public Color textColor = new Color(0.9f, 0.95f, 1f, 1f);
  
  [Header("Settings")]
  public float showDelay = 0.3f;
  public Vector2 offset = new Vector2(15, 10);
  public float maxTooltipWidth = 400f;
  public float minTooltipWidth = 250f;
  public float maxTooltipHeight = 600f;
  public float minTooltipHeight = 50f;
  public float paddingHorizontal = 12f;
  public float paddingVertical = 8f;
  public float spacing = 4f;
  public float minDistanceFromBottom = 20f;
  public float minDistanceFromTop = 20f;
  
  // Internal state
  private bool isHovering = false;
  
  private void Awake()
  {
      DisableWrapping(itemNameText);
      DisableWrapping(itemPriceText);
      DisableWrapping(itemDescriptionText);
      DisableWrapping(energyRegainAmountText);
      DisableWrapping(itemDaysBeforeExpiryText);
  }

  private void DisableWrapping(TMP_Text text)
  {
      if (text == null) return;
#if UNITY_6000_0_OR_NEWER
      text.textWrappingMode = TextWrappingModes.NoWrap;
#else
      text.enableWordWrapping = false;
#endif
      text.overflowMode = TextOverflowModes.Overflow;
  }
  
  public void UpdateText(string itemName, int itemPrice, string itemDescription, int itemEnergyRegainAmount,int amountOfDaysBeforeExpiry,ShopItemData itemData,int itemId,bool showToolTip = true)
  {
      if (storeBuyPanel !=null && storeBuyPanel.GetSelectedShopItemData() == itemData && storeBuyPanel.GetSelectedShopItemId() == itemId)
      {
          return;
      }
      
      if (itemNameText == null || itemPriceText == null || itemDescriptionText == null || energyRegainAmountText == null || itemDaysBeforeExpiryText == null || itemData == null)
      {
          Debug.Log("One or more UI elements are null. Tooltip update aborted.");
          return;
      }
      
    this.itemNameText.text = itemName;
    this.itemPriceText.text = $"€ {itemPrice}";
    this.itemDescriptionText.text = itemDescription;
    this.currentItemData = itemData;
    this.currentItemItemType = itemData.itemType;
    if (currentItemItemType == ItemTypes.Food)
    {
        this.energyRegainAmountText.text = $"Energy Regain: {itemEnergyRegainAmount}";
        this.itemDaysBeforeExpiryText.text = $"Days Before Expiry: {amountOfDaysBeforeExpiry}";
    }

 
    
    
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
        if (!isHovering || string.IsNullOrEmpty(itemNameText.text)) return;
        
        // Setup references
        if (parentCanvas == null) parentCanvas = this.GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;

        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        if (canvasRect == null || tooltipRect == null) return;

        // Ensure text wrapping is disabled
        DisableWrapping(itemNameText);
        DisableWrapping(itemPriceText);
        DisableWrapping(itemDescriptionText);
        DisableWrapping(energyRegainAmountText);
        DisableWrapping(itemDaysBeforeExpiryText);
       
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
        float maxTextPreferredWidth = itemNameText != null ? itemNameText.preferredWidth : 0f;
        if (itemPriceText != null && !string.IsNullOrEmpty(itemPriceText.text))
            maxTextPreferredWidth = Mathf.Max(maxTextPreferredWidth, itemPriceText.preferredWidth);
        if (itemDescriptionText != null && !string.IsNullOrEmpty(itemDescriptionText.text))
            maxTextPreferredWidth = Mathf.Max(maxTextPreferredWidth, itemDescriptionText.preferredWidth);
        if(energyRegainAmountText != null && !string.IsNullOrEmpty(energyRegainAmountText.text) && currentItemItemType == ItemTypes.Food)
            maxTextPreferredWidth = Mathf.Max(maxTextPreferredWidth, energyRegainAmountText.preferredWidth);
        if(itemDaysBeforeExpiryText != null && !string.IsNullOrEmpty(itemDaysBeforeExpiryText.text) && currentItemItemType == ItemTypes.Food)
            maxTextPreferredWidth = Mathf.Max(maxTextPreferredWidth, itemDaysBeforeExpiryText.preferredWidth);

        float optimalWidth = Mathf.Max(
            minTooltipWidth,
            Mathf.Min(availableWidth, maxTextPreferredWidth + paddingHorizontal * 2f)
        );

        float textWidth = optimalWidth - paddingHorizontal * 2f;

        // Constrain text width to compute preferred height
        if (itemNameText != null)
        {
            itemNameText.rectTransform.sizeDelta = new Vector2(textWidth, 0f);
            itemNameText.ForceMeshUpdate(true, true);
        }
        if (itemPriceText != null)
        {
            itemPriceText.rectTransform.sizeDelta = new Vector2(textWidth, 0f);
            itemPriceText.ForceMeshUpdate(true, true);
        }
        if (itemDescriptionText != null)
        {
            itemDescriptionText.rectTransform.sizeDelta = new Vector2(textWidth, 0f);
            itemDescriptionText.ForceMeshUpdate(true, true);
        }
        if(energyRegainAmountText != null && !string.IsNullOrEmpty(energyRegainAmountText.text) && currentItemItemType == ItemTypes.Food)
        {
            energyRegainAmountText.rectTransform.sizeDelta = new Vector2(textWidth, 0f);
            energyRegainAmountText.ForceMeshUpdate(true, true);
        }
        if(itemDaysBeforeExpiryText != null && !string.IsNullOrEmpty(itemDaysBeforeExpiryText.text) && currentItemItemType == ItemTypes.Food)
        {
            itemDaysBeforeExpiryText.rectTransform.sizeDelta = new Vector2(textWidth, 0f);
            itemDaysBeforeExpiryText.ForceMeshUpdate(true, true);
        }
        
        Canvas.ForceUpdateCanvases();

        float currentYOffset = paddingVertical;

        // Layout itemNameText inside (top anchored so it grows downward)
        if (itemNameText != null && !string.IsNullOrEmpty(itemNameText.text))
        {
            var textRect = itemNameText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot     = new Vector2(0.5f, 1f);
            float h = itemNameText.preferredHeight;
            textRect.offsetMax = new Vector2(-paddingHorizontal, -currentYOffset);
            textRect.offsetMin = new Vector2( paddingHorizontal, -(currentYOffset + h));
            currentYOffset += h;
        }

        // Layout itemPriceText inside
        if (itemPriceText != null && !string.IsNullOrEmpty(itemPriceText.text))
        {
            if (currentYOffset > paddingVertical) currentYOffset += spacing;
            var textRect = itemPriceText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot     = new Vector2(0.5f, 1f);
            float h = itemPriceText.preferredHeight;
            textRect.offsetMax = new Vector2(-paddingHorizontal, -currentYOffset);
            textRect.offsetMin = new Vector2( paddingHorizontal, -(currentYOffset + h));
            currentYOffset += h;
        }

        // Layout itemDescriptionText inside
        if (itemDescriptionText != null && !string.IsNullOrEmpty(itemDescriptionText.text))
        {
            if (currentYOffset > paddingVertical) currentYOffset += spacing;
            var textRect = itemDescriptionText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot     = new Vector2(0.5f, 1f);
            float h = itemDescriptionText.preferredHeight;
            textRect.offsetMax = new Vector2(-paddingHorizontal, -currentYOffset);
            textRect.offsetMin = new Vector2( paddingHorizontal, -(currentYOffset + h));
            currentYOffset += h;
        }
        
        if (energyRegainAmountText != null && !string.IsNullOrEmpty(energyRegainAmountText.text))
        {
            if (currentYOffset > paddingVertical) currentYOffset += spacing;
            var textRect = energyRegainAmountText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot     = new Vector2(0.5f, 1f);
            float h = energyRegainAmountText.preferredHeight;
            textRect.offsetMax = new Vector2(-paddingHorizontal, -currentYOffset);
            textRect.offsetMin = new Vector2( paddingHorizontal, -(currentYOffset + h));
            currentYOffset += h;
        }
        
        if (itemDaysBeforeExpiryText != null && !string.IsNullOrEmpty(itemDaysBeforeExpiryText.text))
        {
            if (currentYOffset > paddingVertical) currentYOffset += spacing;
            var textRect = itemDaysBeforeExpiryText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot     = new Vector2(0.5f, 1f);
            float h = itemDaysBeforeExpiryText.preferredHeight;
            textRect.offsetMax = new Vector2(-paddingHorizontal, -currentYOffset);
            textRect.offsetMin = new Vector2( paddingHorizontal, -(currentYOffset + h));
            currentYOffset += h;
        }

        float maxHeight = maxTooltipHeight > 0f ? Mathf.Min(maxTooltipHeight, canvasSize.y * 0.9f) : (canvasSize.y * 0.9f);
        float minHeight = minTooltipHeight > 0f ? minTooltipHeight : 50f;
        float totalHeight = Mathf.Clamp(currentYOffset + paddingVertical, minHeight, maxHeight);

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
        float bottomPad = Mathf.Max(pad, minDistanceFromBottom);
        float topPad = Mathf.Max(pad, minDistanceFromTop);

        float rightRoom  =  half.x - mouseLocal.x;
        float leftRoom   =  half.x + mouseLocal.x;
        float topRoom    =  half.y - mouseLocal.y;
        float bottomRoom =  half.y + mouseLocal.y;

        bool placeRight;
        if (rightRoom >= tooltipSize.x + offset.x + pad)
            placeRight = true;
        else if (leftRoom >= tooltipSize.x + offset.x + pad)
            placeRight = false;
        else
            placeRight = rightRoom >= leftRoom;

        bool placeAbove;
        if (topRoom >= tooltipSize.y + offset.y + topPad)
            placeAbove = true;
        else if (bottomRoom >= tooltipSize.y + offset.y + bottomPad)
            placeAbove = false;
        else
            placeAbove = topRoom >= bottomRoom;

        // pivot matches the corner "touching" the cursor
        tooltipRect.pivot = new Vector2(placeRight ? 0f : 1f, placeAbove ? 0f : 1f);

        Vector2 pos = mouseLocal;
        pos.x += placeRight ?  offset.x : -offset.x;
        pos.y += placeAbove ?  offset.y : -offset.y;

        // clamp fully inside canvas
        float minX = -half.x + pad + tooltipRect.pivot.x * tooltipSize.x;
        float maxX =  half.x - pad - (1f - tooltipRect.pivot.x) * tooltipSize.x;
        float minY = -half.y + bottomPad + tooltipRect.pivot.y * tooltipSize.y;
        float maxY =  half.y - topPad - (1f - tooltipRect.pivot.y) * tooltipSize.y;

        if (minX <= maxX)
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
        else
            pos.x = -half.x + pad + tooltipRect.pivot.x * (canvasSize.x - 2f * pad);

        if (minY <= maxY)
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
        else
            pos.y = -half.y + bottomPad + tooltipRect.pivot.y * (canvasSize.y - bottomPad - topPad);

        return pos;
    }
}
