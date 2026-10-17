using FMODUnity;
using UnityEngine;

public class MapAudio : MonoBehaviour
{
    //[SerializeField] RadioManager radioManager;
    [SerializeField] StudioEventEmitter garageAmbienceEmitter;
    //[SerializeField] MapSpline mapSpline;
    public bool hasJustTravelledByTaxi = false;
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

        if (AudioManager.instance != null)
        {
            AudioManager.instance.countrysideAmbience.start();
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

        if (AudioManager.instance != null)
        {
            AudioManager.instance.countrysideAmbience.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }

        if (LeagueInviteCardsUi.Instance != null)
        {
            if(LeagueInviteCardsUi.Instance.LeagueInviteReceivedWhileInMap == true)
            {
                if (AudioManager.instance != null)
                {
                    AudioManager.instance.showInviteAudio.start();
                    LeagueInviteCardsUi.Instance.LeagueInviteReceivedWhileInMap = false;
                }
            }
        }
    }

    public void PlayTaxiSound()
    {
        if (AudioManager.instance != null)
        {
            //AudioManager.instance.coins.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);

            if (PlayerManager.Instance != null)
            {
                float coins = PlayerManager.Instance.GetPlayerCoins();
                if (coins >= 40)
                {
                    Debug.Log($"Player has {coins} and can afford a taxi - AudioDebug");
                    AudioManager.instance.taxi.start();
                    hasJustTravelledByTaxi = true;

                }
                else
                {
                    Debug.Log($"Player has {coins} and can't afford a taxi - AudioDebug");
                }
            }
            //AudioManager.instance.taxi.setParameterByName("Banknote Volume", 0f);
        }
    }

    public void PlayWalkingSound()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.walking.start();
        }
    }
}
