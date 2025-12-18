using System.Collections.Generic;
using UnityEngine;

public class WeightSpawner : MonoBehaviour
{
    public GameObject weightPrefab;
    public Transform spawnPoint;
    public List<GameObject> spawnedWeights = new List<GameObject>();
    
    [SerializeField] float maxSpawnRangeXDirection = 1;
    [SerializeField] float maxSpawnRangeZDirection = 1;
    
    void Start()
    {
        if (WeightLiftingController.Instance != null)
        {
            
            WeightLiftingController.Instance.onWeightSelectionChanged.AddListener(ManageWeightChange);
        }
    }


    public void ManageWeightChange(int selectedWeight)
    {
        ClearWeights();
        SpawnWeight(selectedWeight);
    }
    
    
    void ClearWeights()
    {
        foreach (var weight in spawnedWeights)
        {
            Destroy(weight);
        }
        spawnedWeights.Clear();
    }
    
    void SpawnWeight(int weight)
    {
        for(int i =0; i< weight; i+=10)
        {
            float x = Random.Range(-maxSpawnRangeXDirection, maxSpawnRangeXDirection);
            float z = Random.Range(-maxSpawnRangeZDirection, maxSpawnRangeZDirection);
            GameObject newWeight = Instantiate(weightPrefab, spawnPoint.position + new Vector3(x,z), weightPrefab.transform.rotation);
            spawnedWeights.Add(newWeight);
        }
        
    }
    
}
