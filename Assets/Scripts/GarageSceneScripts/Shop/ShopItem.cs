using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour
{
  [SerializeField] private string itemNameText;
  [SerializeField] private Image itemIconImage;
  [SerializeField] private int itemPrice=0;
  [SerializeField] private int itemEnergyRegainAmount=0;
  [SerializeField] private int amountOfDaysBeforeExpiry=0;
  [SerializeField] private int spawnedItemId=0;
  [SerializeField] private string itemDescriptionText;
  [SerializeField] private ShopItemData itemData;
  [SerializeField] private EventTrigger itemEventTrigger;
  [SerializeField] private StoreManager storeManager;
  [SerializeField] private StoreBuyPanel storeBuyPanel;
  [SerializeField] private Button shopItemButton;
  
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
    storeManager.UpdateToolTip(itemNameText, itemPrice, itemDescriptionText, itemEnergyRegainAmount, amountOfDaysBeforeExpiry,itemData,spawnedItemId, this.transform);
  }
  
  public void HideTooltip()
  {
    storeManager.HideToolTip();
  }

  public void InitializeShopItem(ShopItemData injectedItemData,int injectedSpawnedItemId)
  {
    itemNameText = injectedItemData.itemName;
    itemIconImage.sprite = injectedItemData.itemIcon;
    itemPrice = injectedItemData.itemPrice;
    itemEnergyRegainAmount = injectedItemData.itemEnergyRegainAmount;
    amountOfDaysBeforeExpiry = injectedItemData.itemDaysBeforeExpiry;
    itemDescriptionText = injectedItemData.itemDescription;
    itemData = injectedItemData;
    spawnedItemId = injectedSpawnedItemId;
    
    /*if(storeBuyPanel != null && shopItemButton != null)
    {
      shopItemButton.onClick.AddListener(OnShopItemButtonClicked);
    }*/
  }
  
  public ShopItemData GetItemData()
  {
    return itemData;
  }
  
  public int GetSpawnedItemId()
  {
    return spawnedItemId;
  }

  public ShopItemData GetShopItemData()
  {
    return itemData;
  }

  public void SetStoreManager(StoreManager injectedStoreManager)
  {
    storeManager = injectedStoreManager;
  }
  public void SetShopBuyPanel(StoreBuyPanel injectedShopBuyPanel)
  {
    storeBuyPanel = injectedShopBuyPanel;
  }

  public void OnShopItemButtonClicked()
  {
    Debug.Log("ShopItemButtonClicked");
    if (storeBuyPanel != null)
    {
      storeBuyPanel.UpdateBuyPanel(itemNameText,itemPrice,itemDescriptionText,itemEnergyRegainAmount,amountOfDaysBeforeExpiry,spawnedItemId,itemData);
    }
  }
}
