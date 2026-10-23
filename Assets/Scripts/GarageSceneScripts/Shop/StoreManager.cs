using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class StoreManager : MonoBehaviour
{
    [SerializeField] private List<Transform> itemSpawnLocations;
    [SerializeField] private List<Transform> locationsCurrentlyInUse;
    [SerializeField] private ShopItem itemPrefab;
    [SerializeField] private List<ShopItemData> typesOfItemsWeCanSpawn;
    [SerializeField] private List<ShopItem> spawnedItems;
    [SerializeField] private SpawnedItemsParent spawnedItemsParent;
    [SerializeField] private StoreTooltip storeTooltip;
    [SerializeField] private StoreBuyPanel storeBuyPanel;
    
    [Header("Store Settings")]
    [Tooltip("Minimum number of items to spawn in the store")]
    [SerializeField]private int minItemsToSpawn = 2;
    [Tooltip("Maximum number of items to spawn in the store")]
    [SerializeField] private int maxItemsToSpawn = 5;
    [Tooltip("Maximum number of items of the same type to spawn in the store")]
    [SerializeField] private int maxToSpawnForEachItem = 2;
    [Tooltip("Number of items to spawn in the store in store today// This gets randomized based on the min and max values")]
    [SerializeField] private int maxItemsToSpawnToday;
    [SerializeField] private bool hasSpawnedItems = false;

    private void OnEnable()
    {
        if(spawnedItemsParent != null)
            spawnedItemsParent.SetStoreManager(this);
        if (GameManager.Instance != null)
        {
            hasSpawnedItems = GameManager.Instance.GetHasSpawnedShopItemsToday();
        }
        if(!hasSpawnedItems)
            SpawnItemsInStore();
        else
        {
            RespawnItemsAlreadyGenerated();
        }
    }
    public void SpawnItemsInStore()
    {
        if (hasSpawnedItems || itemSpawnLocations.Count == 0 || typesOfItemsWeCanSpawn.Count == 0)
            return;
        hasSpawnedItems = true;
        for(int i = 0; i < spawnedItems.Count; i++)
        {
            Destroy(spawnedItems[i].gameObject);
        }
        spawnedItems.Clear();
        locationsCurrentlyInUse.Clear();
        
        
        maxItemsToSpawnToday = Random.Range(minItemsToSpawn, maxItemsToSpawn);

        for (int i = 0; i < maxItemsToSpawnToday; i++)
        {
            ShopItemData itemData = typesOfItemsWeCanSpawn[Random.Range(0, typesOfItemsWeCanSpawn.Count)];
            int counterOfThisItemSpawnedToday =0;
            foreach (ShopItem item in spawnedItems)
            {
                if(item.GetItemData()==itemData)
                {
                    counterOfThisItemSpawnedToday++;
                }
            }
            if(counterOfThisItemSpawnedToday>=maxToSpawnForEachItem)
            {
                continue;
            }
            Transform spawnLocation = itemSpawnLocations[Random.Range(0, itemSpawnLocations.Count)];
            if (locationsCurrentlyInUse.Contains(spawnLocation))
            {
                while (locationsCurrentlyInUse.Contains(spawnLocation))
                {
                    spawnLocation = itemSpawnLocations[Random.Range(0, itemSpawnLocations.Count)];
                }
            }
            else
            {
                locationsCurrentlyInUse.Add(spawnLocation);
            }
            ShopItem spawnedItem=Instantiate(itemPrefab, spawnLocation.position, spawnLocation.rotation);
            spawnedItem.transform.SetParent(spawnedItemsParent.transform);
            spawnedItem.InitializeShopItem(itemData,i);
            spawnedItem.SetupItemEventTrigger();
            spawnedItem.SetStoreManager(this);
            if(storeBuyPanel != null)
                spawnedItem.SetShopBuyPanel(storeBuyPanel);
            spawnedItem.SetTransform(spawnedItemsParent.transform);
            spawnedItems.Add(spawnedItem);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetSpawnedItemsToday(spawnedItems);
        }
    }

    public void RespawnItemsAlreadyGenerated()
    {
        if (GameManager.Instance != null)
        {
            for(int i = 0; i < spawnedItems.Count; i++)
            {
                Destroy(spawnedItems[i].gameObject);
            }
            List<SavedStoreItem> savedItems=GameManager.Instance.GetSpawnedItemsToday();
            List<ShopItem> tempSpawnItems = new List<ShopItem>(); 
            for (int i = 0; i < savedItems.Count; i++)
            {
                if(savedItems[i].GetHasBeenPurchased())
                    continue;
                ShopItem spawnedItem=Instantiate(itemPrefab, savedItems[i].GetSavedTransformPosition(), savedItems[i].GetSavedRotation());
                spawnedItem.transform.SetParent(spawnedItemsParent.transform);
                spawnedItem.InitializeShopItem(savedItems[i],i);
                spawnedItem.SetStoreManager(this);
                spawnedItem.SetTransform(spawnedItemsParent.transform);
                spawnedItem.SetupItemEventTrigger();
                spawnedItem.SetStoreManager(this);
                if(storeBuyPanel != null)
                    spawnedItem.SetShopBuyPanel(storeBuyPanel);
               
                tempSpawnItems.Add(spawnedItem);
            }
            
            
            spawnedItems = tempSpawnItems;
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetSpawnedItemsToday(spawnedItems);
            }
        }
    }
    
    public void UpdateToolTip(string itemName, int itemPrice, string itemDescription,int itemEnergyRegainAmount,int amountOfDaysBeforeExpiry,ShopItemData itemData,int itemId,bool showToolTip = true)
    { 
        if(storeTooltip != null)
        {
            storeTooltip.UpdateText(itemName, itemPrice, itemDescription,itemEnergyRegainAmount,amountOfDaysBeforeExpiry,itemData,itemId,showToolTip);
        }
    }
    
    public void BuyItem(int itemId)
    {
        ShopItem selectedItem = spawnedItems.Find(x => x.GetSpawnedItemId() == itemId);
        if (selectedItem != null)
        {
            if(PlayerManager.Instance != null)
            {
                if (!PlayerManager.Instance.IsFridgeFull())
                {
                    bool wasPurchased = PlayerManager.Instance.PurchaseItem(selectedItem.GetItemCost(), PurchaseType.Shop);

                    if (wasPurchased)
                    {
                        PlayerManager.Instance.AddItemToFridge(selectedItem.GetItemData());
                        selectedItem.HasBeenPurchased(true);
                        if(PlayerStatsView.Instance != null)
                            PlayerStatsView.Instance.DisplayInfo($"{selectedItem.GetItemName()} has been added to your fridge");

                    }
                    else
                    {
                        if(PlayerStatsView.Instance != null)
                            PlayerStatsView.Instance.DisplayInfo($"Can't afford to purchase {selectedItem.GetItemName()}");

                    }
                }
                else
                {
                    if(PlayerStatsView.Instance != null)
                        PlayerStatsView.Instance.DisplayInfo("Fridge is full");
                }
            }
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetSpawnedItemsToday(spawnedItems);
        }
    }
    
    
    public void HideToolTip()
    {
        if(storeTooltip != null)
            storeTooltip.HideTooltip();
    }

    public StoreTooltip GetStoreTooltip()
    {
        return storeTooltip;
    }
}

[System.Serializable]
public class SavedStoreItem
{
    
    [SerializeField] private ShopItemData itemData;
    [SerializeField] private bool hasBeenPurchased = false;
    [SerializeField] private Vector3 spawnedTransformInStorePosition;
    [SerializeField] private Quaternion spawnedTransformInStoreRotation;

    public void InitializeShopItem(ShopItem injectedStoreItem)
    {
        itemData = injectedStoreItem.GetItemData();
        if (itemData != null)
        {
            hasBeenPurchased = injectedStoreItem.GetHasBeenPurchased();
            spawnedTransformInStorePosition = injectedStoreItem.GetTransform().position;
            spawnedTransformInStoreRotation = injectedStoreItem.GetTransform().rotation;
        }
    }
    
    
    public ShopItemData GetShopItemData()
    {
        return itemData;
    }


    public Vector3 GetSavedTransformPosition()
    {
        return spawnedTransformInStorePosition;
    }
  
    public bool GetHasBeenPurchased()
    {
        return hasBeenPurchased;
    }

    public Quaternion GetSavedRotation()
    {
        return spawnedTransformInStoreRotation;
    }
}