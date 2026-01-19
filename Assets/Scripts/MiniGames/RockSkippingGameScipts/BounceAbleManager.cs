using System.Collections.Generic;
using UnityEngine;

public class BounceAbleManager : MonoBehaviour
{
    public List<RockSkippingObject>  objects;
    public static BounceAbleManager Instance { get; private set; }
    
    public List<RockSkippingObject>  rockSkippinginstances = new List<RockSkippingObject>();
    
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
    }
    
    private void InstantiateObjectPools()
    {
        foreach (var obj in objects)
        {
            for (int i = 0; i < 5; i++)
            {
                rockSkippinginstances.Add(obj);
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
