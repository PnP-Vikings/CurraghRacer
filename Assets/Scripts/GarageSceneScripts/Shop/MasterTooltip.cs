using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MasterTooltip : MonoBehaviour
{
   [Header("Current Clicked Item")]
    private int currentItemPrice;
    private int currentItemEnergyRegainAmount;
    private int currentItemAmount;
    private int currentItemId;
    private FridgeItem currentFridgeItemData;
    private ItemTypes currentItemItemType;
 
    [SerializeField] private MasterTooltip otherTooltip;
    
    [Header("Tooltip UI")]
    [SerializeField] private Button exitButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private List<TMP_Text> listOfTextsToBeShown;
    [SerializeField] private Button interactionButton;
    [SerializeField] private TMP_Text exitButtonText;
    [SerializeField] private float buttonSize = 50f; // Adjust size as desired
    
    
    public GameObject panel;          // Root object with an Image (background)
    public Image panelBackground;          // Background image
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

    public bool blockRaycast = false;
    public bool isItemTooltip = false;
    public bool isFridgeTooltip = false;
    public bool onClickHideOtherTooltip = false;
    public bool isConsumablePopup = false;
    private void OnEnable()
    {
       if(interactionButton != null && isConsumablePopup)
        {
            interactionButton.onClick.AddListener(ConsumeSelectedItem);
        }
    }
    
    private void OnDisable()
    {
        if(interactionButton != null && isConsumablePopup)
        {
            interactionButton.onClick.RemoveListener(ConsumeSelectedItem);
        }
    }
    
    public void UpdatePanel(string popUpTopText, List<string> listOfStringsBeingUpdated,bool showPanel = true)
    {
        if(otherTooltip != null && otherTooltip.gameObject.activeSelf && onClickHideOtherTooltip)
        {
            otherTooltip.gameObject.SetActive(false);
        }
        
        if (titleText == null)
        {
            Debug.Log("One or more UI elements are null. Tooltip update aborted.");
            return;
        }
      
        this.titleText.text = popUpTopText;
        for (int i=0; i < listOfStringsBeingUpdated.Count; i++)
        {
            if (listOfStringsBeingUpdated[i] != null || listOfTextsToBeShown[i] != null)
            {
                listOfTextsToBeShown[i].text = listOfStringsBeingUpdated[i];
            }
        }
        
    
        if (showPanel)
        {
            Invoke(nameof(DisplayPanel), showDelay);
        }
        
    }

    public void UpdateFridgePanel(string popUpTopText, List<string> listOfStringsBeingUpdated,FridgeItem itemData ,int itemId,bool showPanel = true)
    {

        if (isItemTooltip)
        {
            if (isFridgeTooltip && (otherTooltip != null && otherTooltip.GetSelectedShopFridgeItemData() == itemData && otherTooltip.GetSelectedShopItemId() == itemId))
            {
                return;
            }
        }

        if(otherTooltip != null && otherTooltip.gameObject.activeSelf && onClickHideOtherTooltip)
        {
            otherTooltip.gameObject.SetActive(false);
        }
        
        if (titleText == null)
        {
            Debug.Log("One or more UI elements are null. Tooltip update aborted.");
            return;
        }
      
        this.titleText.text = popUpTopText;
        titleText.gameObject.SetActive(true);
        if(isItemTooltip)
        {
            this.currentFridgeItemData = itemData;
            this.currentItemItemType = itemData.itemType;
            this.currentItemId = itemId;
        }
        for (int i=0; i < listOfStringsBeingUpdated.Count; i++)
        {
            if (listOfStringsBeingUpdated[i] != null || listOfTextsToBeShown[i] != null)
            {
                listOfTextsToBeShown[i].text = listOfStringsBeingUpdated[i];
                listOfTextsToBeShown[i].gameObject.SetActive(true);
            }
        }
     
        for (int i=0; i < listOfTextsToBeShown.Count; i++)
        {
            if (listOfTextsToBeShown[i] != null && string.IsNullOrEmpty(listOfTextsToBeShown[i].text))
            {
                listOfTextsToBeShown[i].gameObject.SetActive(false);
            }
        }
        
    
        if (showPanel)
        {
            Invoke(nameof(DisplayPanel), showDelay);
        }
        
    }
    
    public void HideConsumePanel()
  {
      CancelInvoke(nameof(DisplayPanel));

      if (otherTooltip != null && onClickHideOtherTooltip)
          otherTooltip.gameObject.SetActive(false);
  }
  
   private void DisplayPanel()
    {
        if (string.IsNullOrEmpty(titleText.text)) return;
        
        // Setup references
        if (parentCanvas == null) parentCanvas = this.GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;

        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (canvasRect == null || panelRect == null) return;

       
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

        bool hasExitButton = exitButton != null && exitButton.gameObject.activeSelf;
        float availableWidth = Mathf.Min(maxTooltipWidth, canvasSize.x * 0.9f);
        float maxTextPreferredWidth = titleText != null ? titleText.preferredWidth : 0f;
        if (hasExitButton)
        {
            maxTextPreferredWidth += buttonSize + spacing;
        }
        
        for (int i=0; i < listOfTextsToBeShown.Count; i++)
        {
            if (listOfTextsToBeShown[i] != null && !string.IsNullOrEmpty(listOfTextsToBeShown[i].text) && listOfTextsToBeShown[i].gameObject.activeSelf)
            {
                maxTextPreferredWidth = Mathf.Max(maxTextPreferredWidth, listOfTextsToBeShown[i].preferredWidth);
            }
        }
        if(interactionButton != null && interactionButton.gameObject.activeSelf)
            maxTextPreferredWidth = Mathf.Max(maxTextPreferredWidth, interactionButton.GetComponent<RectTransform>().rect.width);

        float optimalWidth = Mathf.Max(
            minTooltipWidth,
            Mathf.Min(availableWidth, maxTextPreferredWidth + paddingHorizontal * 2f)
        );

        float textWidth = optimalWidth - paddingHorizontal * 2f;
        float headerTextWidth = optimalWidth - paddingHorizontal * 2f - (hasExitButton ? (buttonSize + spacing) : 0f);
        
        // Constrain text width to compute preferred height
        if (titleText != null)
        {
            titleText.rectTransform.sizeDelta = new Vector2(headerTextWidth, 0f);
            titleText.ForceMeshUpdate(true, true);
        }

        for (int i = 0; i < listOfTextsToBeShown.Count; i++)
        {
            if (listOfTextsToBeShown[i] != null)
            {
                listOfTextsToBeShown[i].rectTransform.sizeDelta = new Vector2(textWidth, 0f);
                listOfTextsToBeShown[i].ForceMeshUpdate(true, true);
            }
        }
        
        Canvas.ForceUpdateCanvases();

        float currentYOffset = paddingVertical;

        // Layout header: exitButton and titleText on the same line
        float buttonHeight = hasExitButton ? buttonSize : 0f;
        float titleRightPadding = paddingHorizontal + (hasExitButton ? (buttonSize + spacing) : 0f);

        if (hasExitButton)
        {
            var buttonRect = exitButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot     = new Vector2(1f, 1f);
            buttonRect.sizeDelta = new Vector2(buttonSize, buttonSize);
            buttonRect.anchoredPosition = new Vector2(-paddingHorizontal, -currentYOffset);

            if (exitButtonText != null)
            {
                exitButtonText.fontSize = buttonSize;
            }
        }
        
        float titleHeight = 0f;
        if (titleText != null && !string.IsNullOrEmpty(titleText.text))
        {
            var textRect = titleText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot     = new Vector2(0.5f, 1f);
            titleHeight = titleText.preferredHeight;
            textRect.offsetMax = new Vector2(-titleRightPadding, -currentYOffset);
            textRect.offsetMin = new Vector2( paddingHorizontal, -(currentYOffset + titleHeight));
        }

        float headerRowHeight = Mathf.Max(titleHeight, buttonHeight);
        currentYOffset += headerRowHeight;
        
        for (int i = 0; i < listOfTextsToBeShown.Count; i++)
        {
            if (listOfTextsToBeShown[i] != null && listOfTextsToBeShown[i].gameObject.activeSelf && !string.IsNullOrEmpty(listOfTextsToBeShown[i].text))
            {
                if (currentYOffset > paddingVertical) currentYOffset += spacing;
                var textRect = listOfTextsToBeShown[i].rectTransform;
                textRect.anchorMin = new Vector2(0f, 1f);
                textRect.anchorMax = new Vector2(1f, 1f);
                textRect.pivot     = new Vector2(0.5f, 1f);
                float h = listOfTextsToBeShown[i].preferredHeight;
                textRect.offsetMax = new Vector2(-paddingHorizontal, -currentYOffset);
                textRect.offsetMin = new Vector2( paddingHorizontal, -(currentYOffset + h));
                currentYOffset += h;
            }
        }
        
        if (interactionButton != null && interactionButton.gameObject.activeSelf)
        {
            if (currentYOffset > paddingVertical) currentYOffset += spacing;
            var buttonRect = interactionButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot     = new Vector2(0.5f, 1f);
            float h = interactionButton.GetComponent<RectTransform>().rect.height;
            buttonRect.offsetMax = new Vector2(-paddingHorizontal, -currentYOffset);
            buttonRect.offsetMin = new Vector2( paddingHorizontal, -(currentYOffset + h));
            currentYOffset += h;
        }

        float maxHeight = canvasSize.y * 0.9f;                       // allow up to 90% of canvas height
        float totalHeight = Mathf.Clamp(currentYOffset + paddingVertical, 50f, maxHeight);

        // Apply final tooltip size
        Vector2 tooltipSize = new Vector2(optimalWidth, totalHeight);
        panelRect.sizeDelta = tooltipSize;
        
        
        // Background look & make sure it doesn't eat raycasts
        if (panelBackground != null)
        {
            panelBackground.color = backgroundColor;
            panelBackground.raycastTarget = false;
        }

        // ---------- Position ----------
        Vector2 pos = CalculateSmartPosition(mouseLocal, tooltipSize, canvasSize, panelRect);
        panelRect.localPosition = pos;

        // Ensure the panel blocks input
        if (blockRaycast)
        {
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
        }
        panel.SetActive(true);
        if(otherTooltip != null && otherTooltip.gameObject.activeSelf)
        {
            otherTooltip.gameObject.SetActive(false);
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

    
    /*private void BuySelectedItem()
    {
        if (storeManager != null)
        {
            storeManager.BuyItem(GetSelectedShopItemId());
            HideBuyPanel();
            
        }
    }*/

    private void ConsumeSelectedItem()
    {
        if (PlayerManager.Instance != null)
        {
            //Add in stuff for cosuming here
            
        }
    }

    public void CloseConsumePanel()
    {
        if (isConsumablePopup)
        {
            currentFridgeItemData = null;
            currentItemId = 99999999;
           this.gameObject.SetActive(false);
        }
    }
    
    public FridgeItem GetSelectedShopFridgeItemData()
    {
        return currentFridgeItemData;
    }
    
    
    public int GetSelectedShopItemId()
    {
        return currentItemId;
    }
}
