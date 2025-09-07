using UnityEngine;

public class SetUiActiveOnGameStart : MonoBehaviour
{
    public GameObject[] uiElementsToActivate;
    void Start()
    {
        GameManager.Instance.OnGameStarted.AddListener(ActivateUiElements);
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
