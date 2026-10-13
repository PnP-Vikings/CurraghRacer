using UnityEngine;

[CreateAssetMenu(menuName = "Shop/ShopItemData", fileName = "NewShopItemData")]
public class ShopItemData : ScriptableObject
{
    [Tooltip("The name of the item.")]
    public string itemName;
    [Tooltip("The icon representing the item.")]
    public Sprite itemIcon;
    [Tooltip("The price of the item in in-game currency.")]
    public int itemPrice=0;
    [Tooltip("The description of the item.")]
    public string itemDescription;
    [Tooltip("The amount of energy this item will regain when used.")]
    public int itemEnergyRegainAmount =0;
    [Tooltip("The amount of days before this item leaves the fridge.")]
    public int itemDaysBeforeExpiry =0;
    
    public ItemTypes itemType = ItemTypes.Food;
    
}

public enum ItemTypes
{
   Food,
   Upgrade
}
