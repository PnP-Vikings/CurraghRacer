using UnityEngine;

public class ShowUiOnClick : MonoBehaviour
{
    public GameObject[] uiElement;
    
    public void ShowUi()
    {
      if(uiElement != null && uiElement.Length > 0)
      {
          foreach (GameObject ui in uiElement)
          {
              if (ui != null)
              {
                  ui.SetActive(true);
              }
              else
              {
                  Debug.LogWarning("ShowUiOnClick: One of the UI elements is null.");
              }
          }
      }
      else
      {
          Debug.LogWarning("ShowUiOnClick: No UI elements assigned to show.");
      }
    }
}
