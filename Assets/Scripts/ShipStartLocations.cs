using System.Collections.Generic;
using UnityEngine;

public class ShipStartLocations : MonoBehaviour
{
    public List<Transform> raceStartPositions;

    private void Awake()
    {
        if(RaceManager.Instance != null)
        {
            RaceManager.Instance.raceStartPositions = raceStartPositions;
        }
    }
}
