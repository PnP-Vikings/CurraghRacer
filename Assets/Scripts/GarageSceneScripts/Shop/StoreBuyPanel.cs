using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreBuyPanel : MonoBehaviour
{
    [Header("Current Clicked Item")]
    private int currentItemPrice;
    private int currentItemEnergyRegainAmount;
    private int currentItemAmount;
    private int currentItemId;
    private ShopItemData currentItemData;
    private ItemTypes currentItemItemType;
 
    [SerializeField] private StoreTooltip storeTooltip;
    
    [Header("Tooltip UI")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemPriceText;
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private TMP_Text energyRegainAmountText;
    [SerializeField] private TMP_Text itemDaysBeforeExpiryText;
    [SerializeField] private Button buyButton;
    
    public GameObject storeBuyPanel;          // Root object with an Image (background)
    public Image storeBuyPanelBackground;          // Background image
    public Canvas parentCanvas;              // Canvas that contains this tooltip
  
    [Header("Tooltip Styling")]
    public Color backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
    public Color borderColor = new Color(0.4f, 0.6f, 0.8f, 1f);  // (not used here but kept for future)
    public Color textColor = new Color(0.9f, 0.95f, 1f, 1f);
  
    [Header("Settings")]
    public float showDelay = 0.1f;
    public Vector2 offset = new Vector2(15, 10);
    public float maxTooltipWidth = 400f;
    public float minTooltipWidth = 250f;
    public float paddingHorizontal = 12f;
    public float paddingVertical = 8f;
    public float spacing = 4f;
    
    
    public void UpdateBuyPanel(string itemName, int itemPrice, string itemDescription, int itemEnergyRegainAmount,int amountOfDaysBeforeExpiry,int itemId,ShopItemData itemData,bool showBuyPanel = true)
    {
        if(storeTooltip != null && storeTooltip.gameObject.activeSelf)
        {
           storeTooltip.gameObject.SetActive(false);
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
        this.currentItemId = itemId;
        if (currentItemItemType == ItemTypes.Food)
        {
            this.energyRegainAmountText.text = $"Energy Regain: {itemEnergyRegainAmount}";
            this.itemDaysBeforeExpiryText.text = $"Days Before Expiry: {amountOfDaysBeforeExpiry}";
        }

 
    
        if (showBuyPanel)
        {
            Invoke(nameof(DisplayStoreBuyPanel), showDelay);
        }
        
    }
    
    public void HideBuyPanel()
  {
      CancelInvoke(nameof(DisplayStoreBuyPanel));

      if (storeBuyPanel != null)
          storeBuyPanel.SetActive(false);
  }
  
   private void DisplayStoreBuyPanel()
    {
        if (string.IsNullOrEmpty(itemNameText.text)) return;
        
        // Setup references
        if (parentCanvas == null) parentCanvas = this.GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;

        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        RectTransform buyPanelRect = storeBuyPanel.GetComponent<RectTransform>();
        if (canvasRect == null || buyPanelRect == null) return;

       
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
        float maxTextPreferredWidth = itemNameText.preferredWidth;
        if (itemPriceText != null && !string.IsNullOrEmpty(itemPriceText.text))
            maxTextPreferredWidth = Mathf.Max(maxTextPreferredWidth, itemPriceText.preferredWidth);
        if (itemDescriptionText != null && !string.IsNullOrEmpty(itemDescriptionText.text))
            maxTextPreferredWidth = Mathf.Max(maxTextPreferredWidth, itemDescriptionText.preferredWidth);
        if(energyRegainAmountText != null && !string.IsNullOrEmpty(energyRegainAmountText.text))
            maxTextPreferredWidth = Mathf.Max(maxTextPreferredWidth, energyRegainAmountText.preferredWidth);
        if(itemDaysBeforeExpiryText != null && !string.IsNullOrEmpty(itemDaysBeforeExpiryText.text))
            maxTextPreferredWidth = Mathf.Max(maxTextPreferredWidth, itemDaysBeforeExpiryText.preferredWidth);
        if(buyButton != null)
            maxTextPreferredWidth = Mathf.Max(maxTextPreferredWidth, buyButton.GetComponent<RectTransform>().rect.width);

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
        
        if (buyButton != null )
        {
            if (currentYOffset > paddingVertical) currentYOffset += spacing;
            var textRect = buyButton.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot     = new Vector2(0.5f, 1f);
            float h = buyButton.GetComponent<RectTransform>().rect.height;
            textRect.offsetMax = new Vector2(-paddingHorizontal, -currentYOffset);
            textRect.offsetMin = new Vector2( paddingHorizontal, -(currentYOffset + h));
            currentYOffset += h;
        }

        float maxHeight = canvasSize.y * 0.9f;                       // allow up to 90% of canvas height
        float totalHeight = Mathf.Clamp(currentYOffset + paddingVertical, 50f, maxHeight);

        // Apply final tooltip size
        Vector2 tooltipSize = new Vector2(optimalWidth, totalHeight);
        buyPanelRect.sizeDelta = tooltipSize;
        
        
        // Background look & make sure it doesn't eat raycasts
        if (storeBuyPanelBackground != null)
        {
            storeBuyPanelBackground.color = backgroundColor;
            storeBuyPanelBackground.raycastTarget = false;
        }

        // ---------- Position ----------
        Vector2 pos = CalculateSmartPosition(mouseLocal, tooltipSize, canvasSize, buyPanelRect);
        buyPanelRect.localPosition = pos;

        // Ensure the panel never blocks input
        var cg = storeBuyPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = storeBuyPanel.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

        storeBuyPanel.SetActive(true);
        if(storeTooltip != null && storeTooltip.gameObject.activeSelf)
        {
            storeTooltip.gameObject.SetActive(false);
        }
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

    public ShopItemData GetSelectedShopItemData()
    {
        return currentItemData;
    }
    
    public int GetSelectedShopItemId()
    {
        return currentItemId;
    }
}
