using UnityEngine;
using UnityEngine.Events;

public class BeerEnterBoxCollider : MonoBehaviour
{
    
    public UnityEvent onBeerCompleted;
    public BeerShaderPour beerShaderPour;
    public BeerShaderPour currentBeerShaderPour; // Reference to the current BeerShaderPour component
    
    public static BeerEnterBoxCollider Instance { get; private set; }
    public void Start()
    {
     //   BeerGameController.Instance.beerEnterBoxCollider = this; // Set the reference in the game controller
    }
  
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Player entered beer pouring area");
        if (other.GetComponent<BeerShaderPour>() != null)
        {
            currentBeerShaderPour = other.GetComponent<BeerShaderPour>();
            currentBeerShaderPour.isActive = true; // Start pouring when player enters
        }  
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<BeerShaderPour>() != null)
        {
            currentBeerShaderPour = other.GetComponent<BeerShaderPour>();
            currentBeerShaderPour.isActive = false; 
        }
    }
    
    public void CheckBeerCompletion()
    {
        if (currentBeerShaderPour != null && currentBeerShaderPour.BeerComplete())
        {
            onBeerCompleted.Invoke(); // Trigger the event when beer is complete
            Debug.Log("Beer is complete!");
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (other.GetComponent<BeerShaderPour>() != null)
        {
            currentBeerShaderPour = other.GetComponent<BeerShaderPour>();
            if (currentBeerShaderPour.BeerComplete())
            {
                onBeerCompleted.Invoke(); // Trigger the event when beer is complete
                Debug.Log("Beer is complete!");
            }
        }
    }
    
    public void ClearBeerShaderPour()
    {
        if (currentBeerShaderPour != null && beerShaderPour != null)
        {
            beerShaderPour.isActive = false; // Stop pouring
            beerShaderPour = null; // Clear the reference when needed
        }
    }  
}
