using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Fridge : MonoBehaviour
{
    [SerializeField] private List<Transform> listOfPossibleItemsTransforms;
    [SerializeField] private int numberOfSlotsInFridge;
    [SerializeField] private List<GameObject> spawnedFood;
    [SerializeField] private Transform spawnedParent;
    public void OnEnable()
    {
        UpdateFoodConsumables();

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.onFoodListUpdated.AddListener(UpdateFoodConsumables);
        }
    }

    public void OnDisable()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.onFoodListUpdated.RemoveListener(UpdateFoodConsumables);
        }
    }


    public void UpdateFoodConsumables()
    {
        if (PlayerManager.Instance != null && !PlayerManager.Instance.IsFridgeEmpty())
        {
            for (int i = 0; i < spawnedFood.Count; i++)
            {
                Destroy(spawnedFood[i]);
            }

            numberOfSlotsInFridge=PlayerManager.Instance.GetFridgeMaxSlots();
            int amountOfItemCurrentlyInFridge = PlayerManager.Instance.GetAmountOfItemsInFridge();
            for (int i = 0; i < amountOfItemCurrentlyInFridge; i++)
            {
                if (PlayerManager.Instance.GetItemFromFridge(i) != null)
                {
                    FridgeItem selectedFridgeItem = PlayerManager.Instance.GetItemFromFridge(i);
                    if (listOfPossibleItemsTransforms[i] != null && selectedFridgeItem != null)
                    {
                        GameObject spawnedFridgeItem = Instantiate(selectedFridgeItem.foodPrefab, spawnedParent);
                        spawnedFridgeItem.transform.position = new Vector3(listOfPossibleItemsTransforms[i].transform.position.x,spawnedFridgeItem.transform.position.y,listOfPossibleItemsTransforms[i].transform.position.z) ;
                        if (spawnedFridgeItem.TryGetComponent<FoodConsumable>(out FoodConsumable component))
                        {
                            component.InitializeFood(selectedFridgeItem.itemName, selectedFridgeItem.itemDescription, selectedFridgeItem.itemEnergyRegainAmount, selectedFridgeItem.amountOfDaysBeforeExpiry);
                        }
                        spawnedFood.Add(spawnedFridgeItem);
                    }
                }
                else
                {
                    Debug.Log("Food Item seached was not avaiable");
                }
            }
        }
    }

}
