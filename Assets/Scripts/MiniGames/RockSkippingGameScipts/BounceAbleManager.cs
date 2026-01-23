using System.Collections.Generic;
using UnityEngine;

public class BounceAbleManager : MonoBehaviour
{
    public List<RockSkippingObject>  objects;
    public static BounceAbleManager Instance { get; private set; }
    
    public List<Bounceable>  rockSkippingObjectsInstances = new List<Bounceable>();

    [SerializeField] private List<Transform> spawnLocations;
    
    [SerializeField] private int currentSpawnIndex = 0;
    
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
        if(rockSkippingObjectsInstances.Count <= 0) return;
        
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
