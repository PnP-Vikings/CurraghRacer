using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TravelButton : MonoBehaviour
{
  [SerializeField] private Image euroIcon;
  [SerializeField] private TMP_Text euroText;
  [SerializeField] private Image timeIcon;
  [SerializeField] private TMP_Text timeText;
  [SerializeField] private TravelCosts currenTravelCosts;
  [SerializeField] private TravelPanel currentTravelPanel;

  public void ToggleTimeUi(bool isActive)
  {
    timeIcon.gameObject.SetActive(isActive);
    timeText.gameObject.SetActive(isActive);
  }
  
  
  public void ToggleEuroUi(bool isActive)
  {
    euroIcon.gameObject.SetActive(isActive);
    euroText.gameObject.SetActive(isActive);
  }


  public void UpdateUi(int euroCost, int timeCost,TravelCosts injectedTravelCosts)
  {
    currenTravelCosts = injectedTravelCosts;
    if (euroCost == 0)
    {
      ToggleEuroUi(false);
    }
    else
    {
      ToggleEuroUi(true);
    }

    if (timeCost == 0)
    {
      ToggleTimeUi(false);
    }
    else
    {
      ToggleTimeUi(true);
    }
    euroText.text = euroCost.ToString();
    timeText.text = timeCost.ToString();
  }

  public void TravelButtonPressed()
  {
    if (currentTravelPanel != null)
    {
      currentTravelPanel.Travel(currenTravelCosts);
    }
  }
  public void SetTravelPanel(TravelPanel injectedTravelPanel)
  {
    currentTravelPanel = injectedTravelPanel;
  }
}

