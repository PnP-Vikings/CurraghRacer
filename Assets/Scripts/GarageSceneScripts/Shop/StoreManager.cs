using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class StoreManager : MonoBehaviour
{
    public List<Transform> itemSpawnLocations;
    public List<Transform> locationsCurrentlyInUse;
    public List<ShopItemData> typesOfItemsWeCanSpawn;
    public ShopItem itemPrefab;
    [SerializeField] List<ShopItem> spawnedItems;
    public SpawnedItemsParent spawnedItemsParent;
    public int maxItemsToSpawn = 5;
    public bool hasSpawnedItems = false;
    [SerializeField] private StoreTooltip storeTooltip;

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
        

        for (int i = 0; i < maxItemsToSpawn; i++)
        {
            ShopItemData itemData = typesOfItemsWeCanSpawn[Random.Range(0, typesOfItemsWeCanSpawn.Count)];
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
    
    public void UpdateToolTip(string itemName, string itemPrice, string itemDescription,Transform itemTransform,bool showToolTip = true)
    { 
        if(storeTooltip != null)
        {
          //  storeTooltip.gameObject.SetActive(showToolTip);
           // storeTooltip.transform.position = itemTransform.position + Vector3.up * 1.5f;
            storeTooltip.UpdateText(itemName, itemPrice, itemDescription,showToolTip);
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
