using UnityEngine;

public class HideUiOnDisable : MonoBehaviour
{
    public GameObject[] uiElement;
    public bool hideOnDisable = true; // If true, hide UI elements when this component is disabled
    public void HideUiOnClick()
    {
        if (uiElement != null)
        {
            foreach (var element in uiElement)
            {
                if (element != null)
                {
                    element.SetActive(false);
                }
            }
        }
    }
    
    private void OnDisable()
    {
        if (uiElement != null && hideOnDisable)
        {
            foreach (var element in uiElement)
            {
                if (element != null)
                {
                    element.SetActive(false);
                }
            }
        }
    }
}
