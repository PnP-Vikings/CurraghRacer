using System.Collections.Generic;
using UnityEngine;

public class ShipStartLocations : MonoBehaviour
{
    public static ShipStartLocations Instance { get; private set; } 
    
    public List<Transform> raceStartPositions;

    private void Awake()
    {
        // Check if Instance is null OR if it's pointing to a destroyed object
        if (Instance == null || Instance.gameObject == null)
        {
            Instance = this;
            
            // Debug the state of our race start positions
            Debug.Log($"ShipStartLocations Awake: raceStartPositions list is {(raceStartPositions == null ? "null" : "not null")}");
            if (raceStartPositions != null)
            {
                Debug.Log($"ShipStartLocations Awake: List has {raceStartPositions.Count} items");
                for (int i = 0; i < raceStartPositions.Count; i++)
                {
                    Transform t = raceStartPositions[i];
                    Debug.Log($"ShipStartLocations Awake: Position {i} is {(t == null ? "NULL" : t.name)}");
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Clear the Instance reference when this object is destroyed
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
