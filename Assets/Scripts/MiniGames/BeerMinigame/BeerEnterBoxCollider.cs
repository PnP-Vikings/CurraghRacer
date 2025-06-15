using UnityEngine;
using UnityEngine.Events;

public class BeerEnterBoxCollider : MonoBehaviour
{
    
    public UnityEvent onBeerCompleted;
    public BeerShaderPour beerShaderPour;
    
    public static BeerEnterBoxCollider Instance { get; private set; }
    public void Start()
    {
        BeerGameController.Instance.beerEnterBoxCollider = this; // Set the reference in the game controller
    }
  
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Player entered beer pouring area");
        if (other.GetComponent<BeerShaderPour>() != null)
        {
            beerShaderPour = other.GetComponent<BeerShaderPour>();
            beerShaderPour.isActive = true; // Start pouring when player enters
        }
        
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<BeerShaderPour>() != null)
        {
            beerShaderPour = other.GetComponent<BeerShaderPour>();
            beerShaderPour.isActive = false; // Stop pouring when player exits
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (other.GetComponent<BeerShaderPour>() != null)
        {
            beerShaderPour = other.GetComponent<BeerShaderPour>();
            if (beerShaderPour.BeerComplete())
            {
                onBeerCompleted.Invoke(); // Trigger the event when beer is complete
                Debug.Log("Beer is complete!");
            }
        }
    }
  
    
    public void ClearBeerShaderPour()
    {
        beerShaderPour.isActive = false; // Stop pouring
        beerShaderPour = null; // Clear the reference when needed
    }
    
}
