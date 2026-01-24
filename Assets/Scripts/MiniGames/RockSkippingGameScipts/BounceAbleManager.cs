using System.Collections.Generic;
using DG.Tweening;
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
    
    
    private void Update()
    {
     if(!managerOn || rockSkippingObjectsInstances.Count <=0) return; 
     
     foreach(var obj in rockSkippingObjectsInstances)
     {
         if(obj == null) continue;
         
         Transform rockSkippingObj = ((MonoBehaviour)obj).transform;
         if (rockSkippingObj.position.x < -750f)
         {
                rockSkippingObj.position = spawnLocations[currentSpawnIndex].position;
                currentSpawnIndex++;
                if (currentSpawnIndex >= spawnLocations.Count)
                    currentSpawnIndex = 0;
         }
         else
         {
             float movementSpeed = 1f;
             if (obj is RockSkippingBounceGameObject rockSkippingBounceGameObject)
             {
                  movementSpeed = rockSkippingBounceGameObject.movementSpeed;
             }
             rockSkippingObj.Translate(Vector3.left * movementSpeed * Time.deltaTime );
         }
     }
     
     
    }
    
    private void InstantiateObjectPools()
    {
        if (objects == null || objects.Count <= 0 || managerOn == false) return;
        
        foreach (var obj in objects)
        {
            for (int i = 0; i < 5; i++)
            {
                DOVirtual.DelayedCall(i * 5f, () =>
                {

                    rockSkippingObjectsInstances.Add(obj.CreateInstance(spawnLocations[currentSpawnIndex]));
                    currentSpawnIndex++;
                    if (currentSpawnIndex >= spawnLocations.Count)
                        currentSpawnIndex = 0;
                });
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
