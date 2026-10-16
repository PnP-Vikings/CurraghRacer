using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class FoodConsumable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,IPointerClickHandler
{
    public string itemName;
    [Tooltip("The description of the item.")]
    public string itemDescription;
    [Tooltip("The amount of energy this item will regain when used.")]
    public int itemEnergyRegainAmount =0;
    [Tooltip("The amount of days before this item leaves the fridge.")]
    public int itemDaysBeforeExpiry =0;

    public FridgeItem fridgeItem;
    
    [SerializeField] private EventTrigger itemEventTrigger;
    
    public void InitializeFood(string injectedItemName,string injectedItemDescription,int injectedItemEnergyRegainAmount,int amountOfDaysBeforeExpiry,FridgeItem injectedFridgeItem)
    {
        this.itemName = injectedItemName;
        this.itemDescription = injectedItemDescription;
        this.itemEnergyRegainAmount = injectedItemEnergyRegainAmount;
        this.itemDaysBeforeExpiry = amountOfDaysBeforeExpiry;
        fridgeItem = injectedFridgeItem;
      
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Pointer Enter Consumbale");
        if (TooltipManager.Instance != null && fridgeItem != null)
        {
            TooltipManager.Instance.UpdateFridgeToolTip(itemName, itemDescription, itemEnergyRegainAmount, itemDaysBeforeExpiry, fridgeItem, fridgeItem.itemId);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideFridgeTooltip();
        }
    }

    
    
    public void UpdateTooltip(BaseEventData eventData)
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.UpdateFridgeToolTip(itemName, itemDescription, itemEnergyRegainAmount, itemDaysBeforeExpiry, fridgeItem, fridgeItem.itemId);
        }
    }
  
    public void HideTooltip()
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideFridgeTooltip();
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.UpdateConsumeToolTip(itemName, itemDescription, itemEnergyRegainAmount, itemDaysBeforeExpiry, fridgeItem, fridgeItem.itemId);
        }
    }
}
