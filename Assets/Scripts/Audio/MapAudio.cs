using FMODUnity;
using System.Collections;
using UnityEngine;

public class MapAudio : MonoBehaviour
{
    //[SerializeField] RadioManager radioManager;
    [SerializeField] StudioEventEmitter garageAmbienceEmitter;
    public bool hasJustTravelledByTaxi = false;
    private GameObject shopCanvas;
    private MapSpline mapSplineScript;
    private float startTime;
    private float playerMoveSpeed;
    private float completion;
    private bool isPlayerMoving;


    private void Update()
    {
        //float completion = Mathf.Clamp01((Time.time - mapSplineScript.startTime) / mapSplineScript.playerMoveSpeed);
        shopCanvas = GameObject.Find("ShopCanvas");

        if (shopCanvas != null)
        {
            //Debug.Log("shopCanvas found - AudioDebug");
            FindMapSplineScriptAndGetData();
        }

    }

    private void FindMapSplineScriptAndGetData()
    {
        mapSplineScript = shopCanvas.GetComponent<MapSpline>();
        if (mapSplineScript != null)
        {
            //Debug.Log("mapSplineScript found - AudioDebug");
            startTime = mapSplineScript.ReturnStartTime();
            playerMoveSpeed = mapSplineScript.ReturnPlayerMoveSpeed();
            isPlayerMoving = mapSplineScript.IsPlayerMoving();
            CheckIfTaxiJourneyIsEnding();
        }
    }

    private void CheckIfTaxiJourneyIsEnding()
    {
        completion = Mathf.Clamp01((Time.time - startTime) / playerMoveSpeed);
        //Debug.Log($"{completion} - AudioDebug"); 

        if(completion > 0.77 && completion < 0.99 && isPlayerMoving == true) 
        {
            if (AudioManager.instance != null)
            {
                //Debug.Log("Taxi Journey completion is between 0.77 & 0.99 - AudioDebug");
                AudioManager.instance.payTaxi.start();
            }
        }
    }



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

    public void CallPlayTaxiSoundIEnum()
    {
        StartCoroutine(PlayTaxiSound());
    }

    private IEnumerator PlayTaxiSound()
    {
        if (AudioManager.instance != null)
        {
            if (PlayerManager.Instance != null)
            {
                float coins = PlayerManager.Instance.GetPlayerCoins();
                if (coins >= 40)
                {
                    //Debug.Log($"Player has {coins} and can afford a taxi - AudioDebug");
                    AudioManager.instance.taxi.start();
                    hasJustTravelledByTaxi = true;

                }
                else
                {
                    //Debug.Log($"Player has {coins} and can't afford a taxi - AudioDebug");
                }
            }

            yield return null;

            AudioManager.instance.coins.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
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
