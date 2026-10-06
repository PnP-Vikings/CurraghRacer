
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapController : MonoBehaviour
{
    [SerializeField] private MapSpline mapSpline;
    PlayerLocation currentPlayerLocation = PlayerLocation.Home;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private Button exitMapButton;
    [SerializeField] private TMP_Text returnHomeText;
    [SerializeField] private TMP_Text alreadyMovingText;
    [SerializeField] private TMP_Text alreadyHereText;
    Coroutine alreadyMovingCoroutine;
    Coroutine alreadyHereCoroutine;
    Coroutine returnHomeCoroutine;
    [SerializeField] private float textShowDuration = 2f; // Duration to show the text
    [SerializeField] private GameObject shopPanel;

    
 
    public void StartMovingPlayerTowardsShop()
    {
        HideAllTexts();
        if (currentPlayerLocation == PlayerLocation.Shop)
        {
            Debug.Log("Player is already at the Shop.");
            SetAlreadyHereTextActive(true);
            return;
        }
        if (mapSpline == null)
        {
            mapSpline = GetComponent<MapSpline>();
        }
     
        mapSpline.StartMovingPlayerTowardsShop();
    }
    
    
  
    public void StartMovingPlayerTowardsHome()
    {
        HideAllTexts();
        if (currentPlayerLocation == PlayerLocation.Home)
        {
            Debug.Log("Player is already at the Home.");
            SetAlreadyHereTextActive(true);
            return;
        }
        
        if (mapSpline == null)
        {
            mapSpline = GetComponent<MapSpline>();
        }
     
        mapSpline.StartMovingPlayerTowardsHome();
    }
    
    public void SetAlreadyMovingTextActive(bool isActive)
    {
        if (alreadyMovingText != null)
        {
            alreadyMovingText.gameObject.SetActive(isActive);
            
            if (isActive)
            {
                if (alreadyMovingCoroutine != null)
                {
                    StopCoroutine(alreadyMovingCoroutine);
                }
                alreadyMovingCoroutine = StartCoroutine(HideAlreadyMovingTextAfterDelay());
            }
        }
    }
    
    public void SetAlreadyHereTextActive(bool isActive)
    {
        if (alreadyHereText != null)
        {
            alreadyHereText.gameObject.SetActive(isActive);
            
            if (isActive)
            {
                if (alreadyHereCoroutine != null)
                {
                    StopCoroutine(alreadyHereCoroutine);
                }
                alreadyHereCoroutine = StartCoroutine(HideAlreadyHereTextAfterDelay());
            }
        }
    }
    public void SetReturnHomeTextActive(bool isActive)
    {
        if (returnHomeText != null)
        {
            returnHomeText.gameObject.SetActive(isActive);
            
            if (isActive)
            {
                if (returnHomeCoroutine != null)
                {
                    StopCoroutine(returnHomeCoroutine);
                }
                returnHomeCoroutine = StartCoroutine(HideReturnHomeTextAfterDelay());
            }
        }
    }
    
    public void SetShopPanelActive(bool isActive)
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(isActive);
            SetExitMapButtonActive(!isActive);
        }
    }
    
    public void ShowExitMapButton()
    {
        if (exitMapButton != null)
        {
            SetExitMapButtonActive(true);
        }
    }
    
    public void SetExitMapButtonActive(bool isActive)
    {
        if (exitMapButton != null)
        {
            exitMapButton.gameObject.SetActive(isActive);
        }
    }
    
    
    public void HideAllTexts()
    {
        SetAlreadyMovingTextActive(false);
        SetAlreadyHereTextActive(false);
        SetReturnHomeTextActive(false);
    }
    
    public void LeaveMapPanel()
    {
        HideAllTexts();
        if (mapPanel != null )
        {
            if(currentPlayerLocation == PlayerLocation.Home)
            {
                mapPanel.SetActive(false);
            }
            else
            {
                SetReturnHomeTextActive(true);
            }
        }
       
    }
    
    
    public void UpdatePlayerLocation(MapController.PlayerLocation newLocation)
    {
        currentPlayerLocation = newLocation;
    }
    public bool IsPlayerMoving()
    {
        if (mapSpline == null)
        {
            mapSpline = GetComponent<MapSpline>();
        }
     
        return mapSpline.IsPlayerMoving();
    }

    private IEnumerator HideAlreadyHereTextAfterDelay()
    {
        yield return new WaitForSeconds(textShowDuration);
        SetAlreadyHereTextActive(false);
    }   private IEnumerator HideReturnHomeTextAfterDelay()
    {
        yield return new WaitForSeconds(textShowDuration);
        returnHomeText.gameObject.SetActive(false);
    }
    private IEnumerator HideAlreadyMovingTextAfterDelay()
    {
        yield return new WaitForSeconds(textShowDuration);
        SetAlreadyMovingTextActive(false);
    }
    
    public enum PlayerLocation
    { 
        Home,
        Shop
    }
}
