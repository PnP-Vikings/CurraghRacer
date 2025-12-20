using System;
using System.Collections.Generic;
using UnityEngine;

public class WeightSelectionUi : MonoBehaviour
{
    [SerializeField] private WeightLiftingController gameController;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private WeightSelectionButton weightButtonPrefab;
    private List<WeightSelectionButton> weightButtons = new List<WeightSelectionButton>();
    [SerializeField] private List<int> availableWeights;

    public void OnEnable()
    {
        // Clear existing buttons
        foreach (var button in weightButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }
        weightButtons.Clear();

        if (gameController == null)
            gameController = WeightLiftingController.Instance;

        if (gameController == null)
        {
            Debug.LogError("WeightLiftingController instance not found!");
            return;
        }

        availableWeights = gameController.GetAvailableWeights();

        if (availableWeights == null || availableWeights.Count == 0)
        {
            Debug.LogError("Available weights list is empty or null!");
            return;
        }

        Debug.Log($"Creating {availableWeights.Count} weight buttons");

        for (int i = 0; i < availableWeights.Count; i++)
        {
            Debug.Log($"Weight at index {i}: {availableWeights[i]}");
            WeightSelectionButton wb = Instantiate(weightButtonPrefab, buttonContainer);
            wb.SetIntWeight(availableWeights[i]);
            wb.SetWeightSelectionUi(this);
            weightButtons.Add(wb);
            Debug.Log("Created weight button for weight: " + availableWeights[i]);
        }
    }
    
    public void OnSelectWeight(int weight,WeightSelectionButton wb = null)
    {
        if (wb != null)
        {
            foreach (var button in weightButtons)
            {
                button.ResetColor();
            }
            weightButtons[weightButtons.IndexOf(wb)].SetSelectedColor();

        }
        else
        {
            Debug.Log("Weight selected programmatically: " + weight);
        }
        
        gameController.SetSelectedWeight(weight);
    }
    
    public void ConfirmSelection()
    {
       bool selectionMade = gameController.ConfirmWeightSelection();
         if (selectionMade)
         {
              this.gameObject.SetActive(false);
         }
    }
}
