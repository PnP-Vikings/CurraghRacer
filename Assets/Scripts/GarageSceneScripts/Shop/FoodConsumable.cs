using UnityEngine;

public class FoodConsumable : MonoBehaviour
{
    public string itemName;
    [Tooltip("The description of the item.")]
    public string itemDescription;
    [Tooltip("The amount of energy this item will regain when used.")]
    public int itemEnergyRegainAmount =0;
    [Tooltip("The amount of days before this item leaves the fridge.")]
    public int itemDaysBeforeExpiry =0;
    
    
    public void InitializeFood(string itemName,string itemDescription,int itemEnergyRegainAmount,int amountOfDaysBeforeExpiry)
    {
        this.itemName = itemName;
        this.itemDescription = itemDescription;
        this.itemEnergyRegainAmount = itemEnergyRegainAmount;
        this.itemDaysBeforeExpiry = amountOfDaysBeforeExpiry;
    }
}
