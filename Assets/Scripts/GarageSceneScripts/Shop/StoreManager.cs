using System;
using System.Collections.Generic;
using UnityEngine;
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
        SpawnItemsInStore();
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
            spawnedItem.InitializeShopItem(itemData);
            spawnedItem.SetStoreManager(this);
            spawnedItems.Add(spawnedItem);
        }
    }
    
    public void UpdateToolTip(string itemName, int itemPrice, string itemDescription,int itemEnergyRegainAmount,int amountOfDaysBeforeExpiry,ShopItemData itemData,Transform itemTransform,bool showToolTip = true)
    { 
        if(storeTooltip != null)
        {
            storeTooltip.UpdateText(itemName, itemPrice, itemDescription,itemEnergyRegainAmount,amountOfDaysBeforeExpiry,itemData,showToolTip);
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
