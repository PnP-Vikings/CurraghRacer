using System;
using UnityEngine;
using UnityEngine.Events;

public class PlateCleanPosition : MonoBehaviour
{
    public PlateLogic plateLogic; // Reference to the PlateLogic component
    public UnityEvent onPlateCleaned; // Event to trigger when the plate is cleaned
    //private FMOD.Studio.EventInstance spongeAudio;
    private bool _hasInvokedEvent = false;
    /*private void OnTriggerEnter(Collider other)
    {
        // Check if the collider belongs to a plate
        if (other.GetComponent<PlateLogic>() != null)
        {
            plateLogic = other.GetComponent<PlateLogic>(); // Get the PlateLogic component from the plate
            Debug.Log("Plate entered clean position: " + plateLogic.name);
            //spongeAudio = FMODUnity.RuntimeManager.CreateInstance("event:/Kitchen/Sponge");
            //spongeAudio.start();
        }
    }*/

    private void OnTriggerStay(Collider other)
    {
        if (other.GetComponent<PlateLogic>() != null)
        {
            PlateLogic plate = other.GetComponent<PlateLogic>();
            if (plate != null && plate.IsPlateClean() && plateLogic == null)
            {
                plateLogic = plate;
                Debug.Log("Plate is clean and ready to be placed in the rack.");
                _hasInvokedEvent = true; // Set flag before invoking
                onPlateCleaned?.Invoke();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the collider belongs to a plate
        if (other.GetComponent<PlateLogic>() != null)
        {
            Debug.Log("Plate exited clean position: " + other.gameObject.name);
            _hasInvokedEvent = false; // Reset flag when plate exits
        }
        
        if(other.GetComponent<PlateLogic>() == null)
        {
            plateLogic = null;
        }
    }
    
}
