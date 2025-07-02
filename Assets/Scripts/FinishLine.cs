using UnityEngine;

public class FinishLine : MonoBehaviour
{
    public FinishMenu finishMenu;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<ShipMovement>() != null)
        {
            ShipMovement ship = other.GetComponent<ShipMovement>();
            RaceManager.Instance.ShipFinished(ship);
            ship.SetRaceStarted(false);
            RaceManager.Instance.IsRaceFinished();
        }
        
    }
}
