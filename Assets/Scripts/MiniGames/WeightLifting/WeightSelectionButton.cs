using System;
using UnityEngine;
using UnityEngine.UI;

public class WeightSelectionButton : MonoBehaviour
{
    [SerializeField] private int weight;
    [SerializeField] private WeightSelectionUi weightSelectionUi;
    [SerializeField] private TMPro.TextMeshProUGUI weightText;
    Image image;
    private Color normalColor = Color.white;
    private Color selectedColor = Color.yellow;

    private void OnEnable()
    {
        image = GetComponent<Image>();
        normalColor = image != null ? image.color : Color.white;
        
    }
    
    public void ResetColor()
    {
        if (image != null)
        {
            image.color = normalColor;
        }
    }
    public void SetSelectedColor()
    {
        if (image != null)
        {
            image.color = selectedColor;
        }
    }


    public void SetIntWeight(int w)
    {
        weight = w;
        if (weightText != null)
        {
            weightText.text = weight.ToString() + " kg";
        }
    }
    
    public void SetWeightSelectionUi(WeightSelectionUi ui)
    {
        weightSelectionUi = ui;
    }

    public void OnButtonPressed()
    {
        weightSelectionUi.OnSelectWeight(weight,this);
    }
  
}
