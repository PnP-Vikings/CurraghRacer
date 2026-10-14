using FMODUnity;
using UnityEngine;

public class MapAudio : MonoBehaviour
{
    //[SerializeField] RadioManager radioManager;
    [SerializeField] StudioEventEmitter garageAmbienceEmitter;
    public void MuteGarageAudio()
    {
        Debug.Log("Garage Ambience was muted because the map was opened - AudioDebug");

        //if (radioManager != null)
        //{
        //    Debug.Log("radioManager found - AudioDebug");
        //    radioManager.MuteRadio();
        //    garageAmbienceEmitter.SetParameter("Mute Garage Ambience", 0f, false);
        //}

        if (RadioManager.instance != null)
        {
            //Debug.Log("RadioManager found - AudioDebug");
            //RadioManager.instance.MuteRadio();
            garageAmbienceEmitter.SetParameter("Mute Garage Ambience", 0f, false);
        }
    }

    public void UnmuteGarageAudio()
    {
        Debug.Log("Garage Ambience was unmuted because the map was closed - AudioDebug");

        //if (radioManager != null)
        //{
        //    Debug.Log("radioManager found - AudioDebug");
        //    radioManager.MuteRadio();
        //    garageAmbienceEmitter.SetParameter("Mute Garage Ambience", 0f, false);
        //}

        if (RadioManager.instance != null)
        {
            //Debug.Log("RadioManager found - AudioDebug");
            //RadioManager.instance.UnMuteRadio();
            garageAmbienceEmitter.SetParameter("Mute Garage Ambience", 1f, false);
        }
    }
}
