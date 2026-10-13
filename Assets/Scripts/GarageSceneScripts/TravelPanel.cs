using System;
using TMPro;
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
            
            isTravelingToShop = false;
            CheckIfPlayerIsMoving();    

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

    public void OnDisable()
    {
        this.gameObject.SetActive(false);
        if (alreadyHerePanel != null)
        {
            alreadyHerePanel.SetActive(false);
        }
    }

}
