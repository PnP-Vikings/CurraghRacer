using UnityEngine;

public class SetUiActiveOnGameStart : MonoBehaviour
{
    public GameObject[] uiElementsToActivate;
   

    void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStarted.AddListener(ActivateUiElements);
            
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.GetGameStarted())
                {
                    ActivateUiElements();
                }
            }
        }
    }    
    
    void OnDisable()
    {
        if(GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStarted.RemoveListener(ActivateUiElements);
        }
    }
    
    public void ActivateUiElements()
    {
        foreach (var uiElement in uiElementsToActivate)
        {
            if (uiElement != null)
            {
                uiElement.SetActive(true);
            }
            else
            {
                Debug.LogWarning("One of the UI elements to activate is not assigned in the inspector.");
            }
        }
    }

   
}
