using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    [SerializeField]
    private MasterTooltip fridgeToolTip;
    [SerializeField]
    private MasterTooltip consumePanel;
    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            /*DontDestroyOnLoad(gameObject);*/
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    
    
    public void UpdateFridgeToolTip(string itemName, string itemDescription,int itemEnergyRegainAmount,int amountOfDaysBeforeExpiry,FridgeItem itemData,int itemId,bool showToolTip = true)
    { 
        if(fridgeToolTip != null)
        {
            Debug.Log("Updating Tooltip");
            List<string> listOfStringsToBePassed = new List<string>();
            
            listOfStringsToBePassed.Add(itemDescription);
            listOfStringsToBePassed.Add($"Energy Regain Amount: {itemEnergyRegainAmount.ToString()}");
            listOfStringsToBePassed.Add($"Days Before Expiry: {amountOfDaysBeforeExpiry.ToString()}");
            
            fridgeToolTip.UpdateFridgePanel(itemName, listOfStringsToBePassed,itemData,itemId,showToolTip);
        }
    }
    public void UpdateConsumeToolTip(string itemName, string itemDescription,int itemEnergyRegainAmount,int amountOfDaysBeforeExpiry,FridgeItem itemData,int itemId,bool showToolTip = true)
    { 
        if(consumePanel != null)
        {
            List<string> listOfStringsToBePassed = new List<string>();
            
            listOfStringsToBePassed.Add(itemDescription);
            listOfStringsToBePassed.Add($"Energy Regain Amount: {itemEnergyRegainAmount.ToString()}");
            listOfStringsToBePassed.Add($"Days Before Expiry: {amountOfDaysBeforeExpiry.ToString()}");
            
            consumePanel.UpdateFridgePanel(itemName, listOfStringsToBePassed,itemData,itemId,showToolTip);
        }
    }

    public void HideFridgeTooltip()
    {
        fridgeToolTip.gameObject.SetActive(false);
    }

    public void HideBothFridgePopups()
    {
        fridgeToolTip.gameObject.SetActive(false);
        consumePanel.gameObject.SetActive(false);
        fridgeToolTip.ResetFridgePopups();
        consumePanel.ResetFridgePopups();
    }
}
