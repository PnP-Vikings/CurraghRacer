using System;
using TMPro;
using UnityEditor.UI;
using UnityEngine;

public class TravelPanel : MonoBehaviour
{
   
    [SerializeField] private MapController mapController;
    [SerializeField] private bool isTravelingToShop = false;
    [SerializeField] private TMP_Text travelingToText;
   
    
    public void TravelingToShop()
    {
        isTravelingToShop = true;
        CheckIfPlayerIsMoving();




    }
    public void NotTravelingToShop()
    {
        
        isTravelingToShop = false;
        CheckIfPlayerIsMoving();    
    }
    
    public void CheckIfPlayerIsMoving()
    {
        if (mapController != null)
        {
            if (!mapController.IsPlayerMoving())
            {
                mapController.SetAlreadyMovingTextActive(false);
                this.gameObject.SetActive(true);
            }
            else
            {
                mapController.SetAlreadyMovingTextActive(true);
                this.gameObject.SetActive(false);
            }
        }
    }

    private void OnEnable()
    {
        travelingToText.text = isTravelingToShop ? "Traveling to Shop" : "Traveling to Home";
    }

    public void Travel()
    {
        if (isTravelingToShop)
        {
            TravelToShop();
        }
        else
        {
            TravelToHome();
        }
    }
    
    private void TravelToShop()
    {
        if (mapController == null)
        {
            mapController = GetComponent<MapController>();
        }
        gameObject.SetActive(false);
        mapController.StartMovingPlayerTowardsShop();
    }
    
    private void TravelToHome()
    {
        if (mapController == null)
        {
            mapController = GetComponent<MapController>();
        }
        gameObject.SetActive(false);
        mapController.StartMovingPlayerTowardsHome();
    }
    
}
