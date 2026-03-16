using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public CarAI carPrefab;
    public CarAI ambulancePrefab;

    [Header("Spawn Setup")]
    public Transform spawnPoint;
    public StopLine spawnLine;

    [Header("Intervals (base values)")]
    public float minInterval = 1.2f;
    public float maxInterval = 2f;
    public float violatorChance = 0.15f;

    [Header("Controller Overrides")]
    [HideInInspector] public float intervalMultiplier = 1f;   // < 1 = faster spawns
    [HideInInspector] public float speedMultiplier = 1f;      // applied to spawned car maxSpeed
    [HideInInspector] public float violatorBonus = 0f;        // added to violatorChance
    [HideInInspector] public bool paused;                     // controller can freeze this lane

    float timer;
    float currentInterval;

    void Start()
    {
        RollInterval();
    }

    void Update()
    {
        if (paused) return;

        timer += Time.deltaTime;
        if (timer >= currentInterval)
        {
            timer = 0f;
            Spawn();
            RollInterval();
        }
    }

    void RollInterval()
    {
        currentInterval = Random.Range(minInterval, maxInterval) * intervalMultiplier;
    }

    /// <summary>Normal timed spawn.</summary>
    void Spawn()
    {
        SpawnCar(false);
    }

    /// <summary>Force-spawn a car right now (used by controller for bursts / waves).</summary>
    public CarAI ForceSpawn(bool forceAmbulance = false)
    {
        return SpawnCar(forceAmbulance);
    }

    CarAI SpawnCar(bool forceAmbulance)
    {
        var mg = TrafficWardenMinigameController.I;

        bool spawnAmbulance = forceAmbulance ||
            (mg != null && mg.activeEvent == TrafficEventType.Ambulance && Random.value < 0.35f);

        GameObject prefab = spawnAmbulance && ambulancePrefab != null
            ? ambulancePrefab.gameObject
            : carPrefab.gameObject;

        var go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        var ai = go.GetComponent<CarAI>();

        if (ai != null)
        {
            if (spawnAmbulance)
            {
                ai.shouldObey = false;
                ai.maxSpeed *= 1.2f * speedMultiplier;
            }
            else
            {
                float effectiveViolator = Mathf.Clamp01(violatorChance + violatorBonus);
                ai.shouldObey = Random.value > effectiveViolator;
                ai.maxSpeed *= speedMultiplier;
            }
        }

        if (spawnLine != null && ai != null)
            ai.SetCurrentStopLine(spawnLine);

        return ai;
    }

}