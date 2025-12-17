using UnityEngine;

public class WeightSelectionButton : MonoBehaviour
{
    [SerializeField] private int weight;
    [SerializeField] private WeightSelectionUi weightSelectionUi;
    [SerializeField] private TMPro.TextMeshProUGUI weightText;
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
        weightSelectionUi.OnSelectWeight(weight);
    }
  
}
