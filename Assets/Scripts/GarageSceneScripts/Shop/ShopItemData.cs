using UnityEngine;

[CreateAssetMenu(menuName = "Shop/ShopItemData", fileName = "NewShopItemData")]
public class ShopItemData : ScriptableObject
{
    [Tooltip("The name of the item.")]
    public string itemName;
    [Tooltip("The icon representing the item.")]
    public Sprite itemIcon;
    [Tooltip("The price of the item in in-game currency.")]
    public int itemPrice;
    [Tooltip("The description of the item.")]
    public string itemDescription;
    [Tooltip("The amount of energy this item will regain when used.")]
    public int itemEnergyRegainAmount;
    
}
