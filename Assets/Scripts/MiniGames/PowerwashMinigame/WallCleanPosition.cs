using System;
using UnityEngine;
using UnityEngine.Events;

public class WallCleanPosition : MonoBehaviour
{
    public WallLogic wallLogic; // Reference to the WallLogic component
    public UnityEvent onWallCleaned; // Event to trigger when the Wall is cleaned
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if the collider belongs to a Wall
        if (other.GetComponent<WallLogic>() != null)
        {
            wallLogic = other.GetComponent<WallLogic>(); // Get the WallLogic component from the Wall
            Debug.Log("Wall entered clean position: " + wallLogic.name);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.GetComponent<WallLogic>() != null)
        {
            wallLogic = other.GetComponent<WallLogic>();
            // Check if the Wall is clean
            if (wallLogic == null)
            {
                Debug.LogWarning("WallLogic is not assigned or found in the collider.");
                return; // Exit if WallLogic is not set
            }
            if (wallLogic.IsWallClean())
            {
                Debug.Log("Wall is clean and ready to be placed in the rack.");
                onWallCleaned.Invoke(); // Trigger the event when the Wall is clean
            }
            else
            {
                Debug.Log("Wall is not clean, cannot be placed in the rack.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the collider belongs to a Wall
        if (other.GetComponent<WallLogic>() != null)
        {
            Debug.Log("Wall exited clean position: " + other.name);
            wallLogic = null; // Clear the reference to the WallLogic component
            // Additional logic for when the Wall exits the clean position can be added here
        }
        
        if(other.GetComponent<WallLogic>() == null)
        {
            wallLogic = null;
        }
    }
    
}
