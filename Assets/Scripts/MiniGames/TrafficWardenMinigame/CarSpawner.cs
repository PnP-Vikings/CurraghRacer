using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    public CarAI carPrefab;
    public CarAI ambulancePrefab; // optional; can reuse carPrefab with different visuals
    public Transform spawnPoint;
    public StopLine spawnLine;
    public float minInterval = 1.2f,maxInterval = 2f;
    public float interval = 1.2f;
    public float violatorChance = 0.15f;

    float t;

    void Start()
    {
        interval = Random.Range(minInterval, maxInterval);
    }
    
    void Update()
    {
        t += Time.deltaTime;
        if (t >= interval)
        {
            t = 0f;
            Spawn();
        }
    }

    void Spawn()
    {
        var mg = TrafficWardenMinigameController.I;

        bool spawnAmbulance =
            (mg != null && mg.activeEvent == TrafficEventType.Ambulance && Random.value < 0.35f);

        GameObject prefab = spawnAmbulance && ambulancePrefab.gameObject != null ? ambulancePrefab.gameObject : carPrefab.gameObject;
        var go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        var ai = go.GetComponent<CarAI>();
        if (ai != null)
        {
            if (spawnAmbulance)
            {
                ai.shouldObey = false;
                ai.maxSpeed *= 1.2f;
            }
            else
            {
                ai.shouldObey = Random.value > violatorChance;
            }
        }

        if (spawnLine != null)
        {
           ai.SetCurrentStopLine(spawnLine); // will check any stop line
        }
    }

}