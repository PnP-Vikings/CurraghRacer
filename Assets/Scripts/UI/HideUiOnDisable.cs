using UnityEngine;

public class HideUiOnDisable : MonoBehaviour
{
    public GameObject[] uiElement;

    private void OnDisable()
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
}
