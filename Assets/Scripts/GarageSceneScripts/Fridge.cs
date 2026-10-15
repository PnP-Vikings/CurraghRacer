using System;
using System.Collections.Generic;
using UnityEngine;

public class Fridge : MonoBehaviour
{
    [SerializeField] private List<Transform> listOfPossibleItemsTransforms;
    [SerializeField] private int numberOfSlotsInFridge;
    [SerializeField] private List<FridgeItem> fridgeItems;
    public void OnEnable()
    {
        if (PlayerManager.Instance != null)
        {
            numberOfSlotsInFridge=PlayerManager.Instance.GetFridgeMaxSlots();
            for (int i = 0; i < numberOfSlotsInFridge; i++)
            {
                if (PlayerManager.Instance.GetItemFromFridge(i) != null)
                {
                    PlayerManager.Instance.GetItemFromFridge(i)
                    FridgeItem spawnedFridgeItem = Instantiate()
                }
            }
        }
    }

}
