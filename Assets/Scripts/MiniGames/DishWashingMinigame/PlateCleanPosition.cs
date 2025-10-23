using System;
using UnityEngine;
using UnityEngine.Events;

public class PlateCleanPosition : MonoBehaviour
{
    public PlateLogic plateLogic; // Reference to the PlateLogic component
    public UnityEvent onPlateCleaned; // Event to trigger when the plate is cleaned
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if the collider belongs to a plate
        if (other.GetComponent<PlateLogic>() != null)
        {
            plateLogic = other.GetComponent<PlateLogic>(); // Get the PlateLogic component from the plate
            Debug.Log("Plate entered clean position: " + plateLogic.name);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.GetComponent<PlateLogic>() != null)
        {
            plateLogic = other.GetComponent<PlateLogic>();
            // Check if the plate is clean
            if (plateLogic == null)
            {
                Debug.LogWarning("PlateLogic is not assigned or found in the collider.");
                return; // Exit if plateLogic is not set
            }
            if (plateLogic.IsPlateClean())
            {
                Debug.Log("Plate is clean and ready to be placed in the rack.");
                onPlateCleaned.Invoke(); // Trigger the event when the plate is clean
            }
            else
            {
                Debug.Log("Plate is not clean, cannot be placed in the rack.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the collider belongs to a plate
        if (other.GetComponent<PlateLogic>() != null)
        {
            Debug.Log("Plate exited clean position: " + other.name);
            plateLogic = null; // Clear the reference to the PlateLogic component
            // Additional logic for when the plate exits the clean position can be added here
        }
        
        if(other.GetComponent<PlateLogic>() == null)
        {
            plateLogic = null;
        }
    }
    
}
