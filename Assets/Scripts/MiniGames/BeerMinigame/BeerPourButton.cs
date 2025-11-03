using UnityEngine;
using UnityEngine.EventSystems;

public class BeerPourButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public BeerPourLocation pourLocation;

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("Button pressed down");
        if (pourLocation != null)
        {
            pourLocation.PourBeer();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("Button released");
        if (pourLocation != null)
        {
            pourLocation.StopPouringBeer();
        }
    }
}

