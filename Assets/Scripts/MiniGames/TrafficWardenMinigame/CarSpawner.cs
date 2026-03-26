using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public CarAI carPrefab;
    public CarAI ambulancePrefab;
    public List<CarAI> carTypes;

    [Header("Spawn Setup")]
    public Transform spawnPoint;
    public StopLine spawnLine;

    [Header("Base Values (read by controller)")]
    public float minInterval = 1.2f;
    public float maxInterval = 2f;
    [Tooltip("Chance that a spawned car is Impatient (mostly obeys but can break).")]
    public float impatientChance = 0.25f;
    [Tooltip("Chance that a spawned car is a full Violator (never obeys). Rolled after impatient check.")]
    public float violatorChance = 0.05f;

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

        GameObject prefab;
        if (spawnAmbulance && ambulancePrefab != null)
        {
            prefab = ambulancePrefab.gameObject;
        }
        else if (carTypes != null && carTypes.Count > 0)
        {
            prefab = carTypes[Random.Range(0, carTypes.Count)].gameObject;
        }
        else
        {
            prefab = carPrefab.gameObject;
        }

        var go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        var ai = go.GetComponent<CarAI>();

        if (ai != null)
        {
            if (spawnAmbulance)
            {
                ai.behaviourType = CarBehaviourType.Violator;
                ai.shouldObey = false;
                ai.maxSpeed *= 1.2f * speedMultiplier;
            }
            else
            {
                // Roll car type: Ordinary → Impatient → Violator
                float roll = Random.value;
                float effectiveImpatient = Mathf.Clamp01(impatientChance + violatorBonus);
                float effectiveViolator  = Mathf.Clamp01(violatorChance + violatorBonus * 0.3f);
                
                if (roll < effectiveViolator)
                {
                    ai.behaviourType = CarBehaviourType.Violator;
                    ai.shouldObey = false;
                }
                else if (roll < effectiveViolator + effectiveImpatient)
                {
                    ai.behaviourType = CarBehaviourType.Impatient;
                    ai.shouldObey = true; // starts obeying, may break at runtime
                }
                else
                {
                    ai.behaviourType = CarBehaviourType.Ordinary;
                    ai.shouldObey = true; // always obeys
                }
                
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

    /// <summary>Tell active non-Ordinary cars on this lane to go rogue. Ordinary cars always obey.</summary>
    public void UnleashAll()
    {
        // Clean nulls first
        activeCars.RemoveAll(c => c == null);

        int count = 0;
        foreach (var car in activeCars)
        {
            // Ordinary cars never go rogue — they always obey
            if (car.behaviourType == CarBehaviourType.Ordinary) continue;
            
            car.GoRogue();
            count++;
        }
        Debug.Log($"Lane {laneIndex + 1} unleashed! {count} cars going rogue (Ordinary cars unaffected).");
    }
}