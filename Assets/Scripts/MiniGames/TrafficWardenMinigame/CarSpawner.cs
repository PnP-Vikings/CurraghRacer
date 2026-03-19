using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public CarAI carPrefab;
    public CarAI ambulancePrefab;

    [Header("Spawn Setup")]
    public Transform spawnPoint;
    public StopLine spawnLine;
    public Image laneAngerIndicator; // Optional UI element to show lane status

    [Header("Base Values (read by controller)")]
    public float minInterval = 1.2f;
    public float maxInterval = 2f;
    public float violatorChance = 0.15f;

    [Header("Cleanup")]
    public float despawnDistance = 80f;   // destroy cars this far from spawn point

    [Header("Controller Overrides")]
    [HideInInspector] public float speedMultiplier = 1f;
    [HideInInspector] public float violatorBonus = 0f;
    [HideInInspector] public int laneIndex = -1;

    /// <summary>All living cars spawned by this lane.</summary>
    public readonly List<CarAI> activeCars = new List<CarAI>();

    /// <summary>Spawn a car right now. Returns the CarAI instance.</summary>
    public CarAI ForceSpawn(bool forceAmbulance = false)
    {
        return SpawnCar(forceAmbulance);
    }

    CarAI SpawnCar(bool forceAmbulance)
    {
        var mg = TrafficWardenMinigameController.Instance;

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

            if (spawnLine != null)
                ai.SetCurrentStopLine(spawnLine);
            ai.laneIndex = laneIndex;
            ai.ownerSpawner = this;

            activeCars.Add(ai);
        }

        return ai;
    }

    /// <summary>Remove a car from tracking (called by CarAI.OnDestroy).</summary>
    public void Unregister(CarAI car)
    {
        activeCars.Remove(car);
    }

    /// <summary>Destroy cars that have travelled too far from the spawn point.</summary>
    public void CleanupDistant()
    {
        for (int i = activeCars.Count - 1; i >= 0; i--)
        {
            var car = activeCars[i];
            if (car == null)
            {
                activeCars.RemoveAt(i);
                continue;
            }

            float dist = Vector3.Distance(car.transform.position, spawnPoint.position);
            if (dist > despawnDistance)
            {
                Destroy(car.gameObject);
                activeCars.RemoveAt(i);
            }
        }
    }

    /// <summary>Tell every active car on this lane to go rogue.</summary>
    public void UnleashAll()
    {
        // Clean nulls first
        activeCars.RemoveAll(c => c == null);

        int count = 0;
        foreach (var car in activeCars)
        {
            car.GoRogue();
            count++;
        }
        Debug.Log($"Lane {laneIndex + 1} unleashed! {count} cars going rogue.");
    }
}