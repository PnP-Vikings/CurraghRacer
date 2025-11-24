using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BeerPourLocation : MonoBehaviour
{
  public BeerEnterBoxCollider beerEnterBoxCollider;
  public Button interactButton;
  public bool isAvailable = true;
  public bool isPouringAutomatically = false;
   
    private void Start()
    { 
        if (beerEnterBoxCollider == null) 
        {beerEnterBoxCollider = GetComponentInChildren<BeerEnterBoxCollider>();}
    }

    public bool IsAvailable()
    {
        if (beerEnterBoxCollider != null)
        {
            return beerEnterBoxCollider.currentBeerShaderPour == null;
        }
        else
        {
            return false;
        }
    }

    /*public void TestPourBeer()
    {
        Debug.Log("TestPourBeer");
    }
    */

    public void Reset()
    {
        beerEnterBoxCollider.ClearBeerShaderPour();
        beerEnterBoxCollider.currentBeerShaderPour = null;
        isAvailable = true; // Mark pour point as available again
        isPouringAutomatically = false;
    }

    public void PourBeer()
    {
        if (beerEnterBoxCollider != null)
        {
            if(beerEnterBoxCollider.currentBeerShaderPour != null && !beerEnterBoxCollider.currentBeerShaderPour.isLocked)
            {
                Debug.Log("Pouring beer");
                beerEnterBoxCollider.currentBeerShaderPour.StartPouring();
            }
        }
    }
    
    public void StopPouringBeer()
    {
        Debug.Log("Stop pouring beer");
        if (beerEnterBoxCollider != null && beerEnterBoxCollider.currentBeerShaderPour != null)
        {
            if (!beerEnterBoxCollider.currentBeerShaderPour.isLocked)
            {
                beerEnterBoxCollider.currentBeerShaderPour.StopPouring();
                StartCoroutine(LockAndCompleteBeer());
            }
        }
    }

    private IEnumerator LockAndCompleteBeer()
    {
        if (beerEnterBoxCollider.currentBeerShaderPour != null)
        {
            beerEnterBoxCollider.currentBeerShaderPour.LockPourAndCalculateQuality();
            
            // Wait for foam animation
            yield return new WaitForSeconds(0.5f);
            Debug.Log("LockAndCompleteBeer - Triggering completion event");
            // Trigger completion event
            beerEnterBoxCollider.CheckBeerCompletion();
        }
    }
}
