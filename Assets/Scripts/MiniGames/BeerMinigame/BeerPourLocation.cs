using System;
using UnityEngine;
using UnityEngine.UI;

public class BeerPourLocation : MonoBehaviour
{
  public BeerEnterBoxCollider beerEnterBoxCollider;
  public Button interactButton;
  public bool isAvailable = true;
   
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
    }

    public void PourBeer()
    {
        if (beerEnterBoxCollider != null)
        {
            if(beerEnterBoxCollider.currentBeerShaderPour != null)
            {
                Debug.Log("Pouring beer");
                beerEnterBoxCollider.currentBeerShaderPour.StartPouring();
                beerEnterBoxCollider.CheckBeerCompletion();
            }
        }
    }

    public void StopPouringBeer()
    {
        Debug.Log("Stop pouring beer");
        if (beerEnterBoxCollider != null && beerEnterBoxCollider.currentBeerShaderPour != null)
        {
            beerEnterBoxCollider.currentBeerShaderPour.StopPouring();
            beerEnterBoxCollider.CheckBeerCompletion();
        }
    }
}
