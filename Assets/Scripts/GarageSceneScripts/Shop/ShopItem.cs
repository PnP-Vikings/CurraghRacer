using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour
{
  [SerializeField] string itemNameText;
  [SerializeField] private Image itemIconImage;
  [SerializeField] int itemPrice=0;
  [SerializeField] int itemEnergyRegainAmount=0;
  [SerializeField] int amountOfDaysBeforeExpiry=0;
  [SerializeField] string itemDescriptionText;
  [SerializeField] ShopItemData itemData;
  [SerializeField] EventTrigger itemEventTrigger;
  [SerializeField] StoreManager storeManager;
  
  public void Start()
  {
    itemEventTrigger = GetComponent<EventTrigger>();
    SetupItemEventTrigger();
  }

  public void SetupItemEventTrigger()
  {
    if (itemEventTrigger != null&& storeManager!=null)
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
    storeManager.UpdateToolTip(itemNameText, itemPrice, itemDescriptionText, itemEnergyRegainAmount, amountOfDaysBeforeExpiry,itemData, this.transform);
  }
  
  public void HideTooltip()
  {
    storeManager.HideToolTip();
  }

  public void InitializeShopItem(ShopItemData injectedItemData)
  {
    itemNameText = injectedItemData.itemName;
    itemIconImage.sprite = injectedItemData.itemIcon;
    itemPrice = injectedItemData.itemPrice;
    itemEnergyRegainAmount = injectedItemData.itemEnergyRegainAmount;
    amountOfDaysBeforeExpiry = injectedItemData.itemDaysBeforeExpiry;
    itemDescriptionText = injectedItemData.itemDescription;
    itemData = injectedItemData;
  }
  
  public ShopItemData GetItemData()
  {
    return itemData;
  }
  
  public void SetStoreManager(StoreManager injectedStoreManager)
  {
    storeManager = injectedStoreManager;
  }
}
