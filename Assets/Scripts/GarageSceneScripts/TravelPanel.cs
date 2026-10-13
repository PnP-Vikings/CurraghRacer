using System;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

public class TravelPanel : MonoBehaviour
{
   
    [SerializeField] private MapController mapController;
    [SerializeField] private bool isTravelingToShop = false;
    [SerializeField] private TMP_Text travelingToText;
    [Header("Already Here Panel")]
    [SerializeField] private GameObject alreadyHerePanel;
    [SerializeField] private TMP_Text alreadyHereBtnText;
    [SerializeField] private Button alreadyHereBtn;
    [SerializeField] private TravelButton byFootBtn;
    [SerializeField] private TravelButton byTaxiBtn;


    private void OnEnable()
    {
        travelingToText.text = isTravelingToShop ? "Traveling to Shop" : "Traveling to Home";

        if (byFootBtn != null)
        {
            byFootBtn.SetTravelPanel(this);
        }
        
        if (byTaxiBtn != null)
        {
            byTaxiBtn.SetTravelPanel(this);
        }
    }
    
    public void TravelingToShop()
    {
        if(mapController == null)
            return;
        
        if(mapController.IsPlayerMoving())
            return;
        
        if (mapController.GetCurrentPlayerLocation() == MapController.PlayerLocation.Shop)
        {
            if (alreadyHerePanel != null)
            {
                this.gameObject.SetActive(false);
                alreadyHerePanel.SetActive(true);
                alreadyHereBtnText.text = "Enter Shop";
                alreadyHereBtn.onClick.RemoveAllListeners();
                alreadyHereBtn.onClick.AddListener(EnableStorePanel);
            }
         
        }
        else
        {    
            UpdateTravelButtons(MapController.PlayerLocation.Shop, mapController.GetCurrentPlayerLocation());
            isTravelingToShop = true;
            CheckIfPlayerIsMoving();
           
        }
    }
    public void NotTravelingToShop()
    {
        if(mapController == null)
            return;
        if(mapController.IsPlayerMoving())
            return;
        if (mapController.GetCurrentPlayerLocation() == MapController.PlayerLocation.Home)
        {
            if(alreadyHerePanel != null)
            {
                this.gameObject.SetActive(false);
                alreadyHerePanel.SetActive(true);
                alreadyHereBtnText.text = "Enter Home";
                alreadyHereBtn.onClick.RemoveAllListeners();
                alreadyHereBtn.onClick.AddListener(DisableMapPanel);
            }
        }
        else
        {
            UpdateTravelButtons(MapController.PlayerLocation.Home, mapController.GetCurrentPlayerLocation());
            isTravelingToShop = false;
            CheckIfPlayerIsMoving();    

        }
    }

    public void UpdateTravelButtons(MapController.PlayerLocation clickedOnLocation, MapController.PlayerLocation currentLocation)
    {

        if (byFootBtn != null)
        {
          TravelCosts byFootTravelCosts = mapController.GetTravelCost(clickedOnLocation,currentLocation,TravelCosts.TravelType.Walking);
          if(byFootTravelCosts !=null)  
              PassDataToTravelButtons(byFootBtn, byFootTravelCosts);
        }
        
        if (byTaxiBtn != null)
        {
            TravelCosts byTaxiTravelCosts = mapController.GetTravelCost(clickedOnLocation,currentLocation,TravelCosts.TravelType.Taxi);
            if(byTaxiTravelCosts !=null)  
                PassDataToTravelButtons(byTaxiBtn, byTaxiTravelCosts);
        }
    }
    
    
    public void EnableStorePanel()
    {
        
        mapController.SetShopPanelActive(true);
        if (alreadyHerePanel != null)
        {
            alreadyHerePanel.SetActive(false);
        }
    }
    public void DisableMapPanel()
    {
        mapController.LeaveMapPanel();
        if (alreadyHerePanel != null)
        {
            alreadyHerePanel.SetActive(false);
        }
    }
    
    public void CheckIfPlayerIsMoving()
    {
        if (mapController != null)
        {
            if (!mapController.IsPlayerMoving())
            {
                mapController.SetAlreadyMovingTextActive(false);
                alreadyHerePanel.SetActive(false);
                this.gameObject.SetActive(true);
            }
            else
            {
                mapController.SetAlreadyMovingTextActive(true);
                this.gameObject.SetActive(false);
            }
        }
    }
    
    public void Travel(TravelCosts currentCosts)
    {
        if (PlayerManager.Instance != null && TimeManager.Instance != null)
        {
            bool wasPurchased = false;
            if (currentCosts.MoneyCost ==0)
            {
                wasPurchased = true;
            }
            else
            {
                wasPurchased = PlayerManager.Instance.PurchaseItem(currentCosts.MoneyCost, PurchaseType.Travel);
            }
            if (wasPurchased)
           {
               TimeManager.Instance.AdvanceTimeByHours(currentCosts.TimeCost);
               if (isTravelingToShop)
               {
                   TravelToShop();
               }
               else
               {
                   TravelToHome();
               }
           }
        }
        else
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
    }
    
    private void TravelToShop()
    {
        if (mapController == null)
        {
           Debug.LogError("MapController is null");
           return;
        }
        gameObject.SetActive(false);
        mapController.StartMovingPlayerTowardsShop();
    }
    
    private void TravelToHome()
    {
        if (mapController == null)
        {
           Debug.LogError("MapController is null");
           return;
        }
        gameObject.SetActive(false);
        mapController.StartMovingPlayerTowardsHome();
    }

    private void PassDataToTravelButtons(TravelButton travelButton,TravelCosts injectedTravelCosts)
    {
        if (travelButton != null)
        {
            travelButton.UpdateUi(injectedTravelCosts.MoneyCost,injectedTravelCosts.TimeCost,injectedTravelCosts);
        }
    }
    
    public void OnDisable()
    {
        this.gameObject.SetActive(false);
        if (alreadyHerePanel != null)
        {
            alreadyHerePanel.SetActive(false);
        }
    }

}
