using FMOD.Studio;
using FMODUnity;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class CarAudio : MonoBehaviour
{
    [SerializeField] GameObject Player; // Assigned at runtime
    [SerializeField] GameObject Car;
    [SerializeField] StudioEventEmitter carEngineEmitter;
    void Start()
    {
        Player = GameObject.FindWithTag("Player");
        
        StartCoroutine(DistanceCalculator());
    }

    IEnumerator DistanceCalculator()
    {
        float distance1 = Vector3.Distance(Player.transform.position, Car.transform.position);

        yield return new WaitForSeconds(0.10f);

        float distance2 = Vector3.Distance(Player.transform.position, Car.transform.position);

        yield return null;

        if (distance1 < 15 & distance1 > distance2)
        {
            carEngineEmitter.SetParameter("Engine Pitch", 1f, false);
            //Debug.Log("Car approaching - AudioDebug");
        }
        else if (distance1 < 15 & distance1 < distance2)
        {
            carEngineEmitter.SetParameter("Engine Pitch", 0f, false);
            //Debug.Log("Car is drving away - AudioDebug");
        }


        if (distance2 == distance1)
        {
            carEngineEmitter.SetParameter("Engine Pitch", 0.5f, false);
            //Debug.Log("Car is stopped - AudioDebug");
        }


        if (distance1 <= 35f)
        {
            carEngineEmitter.SetParameter("Engine Volume", 1f, false);
            //Debug.Log("Engine is unmuted - AudioDebug");
        }
        else
        {
            carEngineEmitter.SetParameter("Engine Volume", 0f, false);
            //Debug.Log("Engine is muted as it is far away - AudioDebug");
        }


        if (distance1 >= 50f)
        {
            carEngineEmitter.Stop();
            //Debug.Log("Engine is stopped as it is far away - AudioDebug");
        }

        //Debug.Log($"Car distance 1 is {distance1}");

        yield return null;

        StartCoroutine(DistanceCalculator());
    }
}
