using System.Collections.Generic;
using UnityEngine;

public class BounceAbleManager : MonoBehaviour
{
    public List<RockSkippingObject>  objects;
    public static BounceAbleManager Instance { get; private set; }
    
    public List<Bounceable>  rockSkippingObjectsInstances = new List<Bounceable>();

    [SerializeField] private List<Transform> spawnLocations;
    
    [SerializeField] private int currentSpawnIndex = 0;

    [SerializeField] private bool managerOn = true;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        InstantiateObjectPools();
    }
    
    private void InstantiateObjectPools()
    {
        if (objects == null || objects.Count <= 0 || managerOn == false) return;
        
        foreach (var obj in objects)
        {
            for (int i = 0; i < 5; i++)
            {
                rockSkippingObjectsInstances.Add(obj.CreateInstance(spawnLocations[currentSpawnIndex]));
                currentSpawnIndex++;
                if (currentSpawnIndex >= spawnLocations.Count)
                    currentSpawnIndex = 0;
            }
        }
    }
    
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    
}
