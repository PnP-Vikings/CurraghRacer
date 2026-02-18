using FMOD.Studio;
using FMODUnity;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class AmbulanceAudio : MonoBehaviour
{
    [SerializeField] GameObject Player; // Assigned at runtime
    [SerializeField] GameObject Ambulance;
    [SerializeField] StudioEventEmitter AmbulanceSirenEmitter;
    void Start()
    {
        Player = GameObject.FindWithTag("Player");

        StartCoroutine(DistanceCalculator());
    }

    IEnumerator DistanceCalculator()
    {
        float distance1 = Vector3.Distance(Player.transform.position, Ambulance.transform.position);

        yield return new WaitForSeconds(0.10f);

        float distance2 = Vector3.Distance(Player.transform.position, Ambulance.transform.position);

        yield return null;

        if (distance1 < 15 & distance1 > distance2)
        {
            AmbulanceSirenEmitter.SetParameter("Siren Pitch", 1f, false);
            Debug.Log("Ambulance approaching - AudioDebug");
        }
        else if (distance1 < 15 & distance1 < distance2)
        {
            AmbulanceSirenEmitter.SetParameter("Siren Pitch", 0f, false);
            Debug.Log("Ambulance is drving away - AudioDebug");
        }


        if (distance2 == distance1)
        {
            AmbulanceSirenEmitter.SetParameter("Siren Pitch", 0.5f, false);
            Debug.Log("Ambulance is stopped - AudioDebug");
        }


        if (distance1 <= 40f)
        {
            AmbulanceSirenEmitter.SetParameter("Siren Volume", 1f, false);
            Debug.Log("Siren is unmuted - AudioDebug");
        }
        else
        {
            AmbulanceSirenEmitter.SetParameter("Siren Volume", 0f, false);
            Debug.Log("Siren is muted as it is far away - AudioDebug");
        }


        if (distance1 >= 50f)
        {
            AmbulanceSirenEmitter.Stop();
            Debug.Log("Siren is stopped as it is far away - AudioDebug");
        }

        Debug.Log($"Ambulance distance 1 is {distance1}");

        yield return null;

        StartCoroutine(DistanceCalculator());
    }
}
