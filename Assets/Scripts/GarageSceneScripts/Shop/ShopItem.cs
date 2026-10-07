using UnityEngine;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour
{
  [SerializeField] string itemNameText;
  [SerializeField] private Image itemIconImage;
  [SerializeField] int itemPrice;
  [SerializeField] string itemDescriptionText;
  [SerializeField] ShopItemData itemData;
  
  public void InitializeShopItem(ShopItemData injectedItemData)
  {
    itemNameText = injectedItemData.itemName;
    itemIconImage.sprite = injectedItemData.itemIcon;
    itemPrice = injectedItemData.itemPrice;
    itemDescriptionText = injectedItemData.itemDescription;
    itemData = injectedItemData;
  }
}
