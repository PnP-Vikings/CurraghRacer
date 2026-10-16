using UnityEngine;
using UnityEngine.EventSystems;

public class FoodConsumable : MonoBehaviour
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
        SetupItemEventTrigger();
    }
    
    public void SetupItemEventTrigger()
    {
        if (itemEventTrigger != null)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerEnter;
            entry.callback.AddListener((data) => { UpdateTooltip((PointerEventData)data); });
            itemEventTrigger.triggers.Add(entry);
      
            EventTrigger.Entry entry2 = new EventTrigger.Entry();
            entry2.eventID = EventTriggerType.PointerExit;
            entry2.callback.AddListener((data) => { HideTooltip(); });
            itemEventTrigger.triggers.Add(entry2);
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
}
