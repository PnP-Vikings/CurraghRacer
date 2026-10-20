using FMODUnity;
using System.Collections;
using UnityEngine;
using static MapController;

public class MapAudio : MonoBehaviour
{
    //[SerializeField] RadioManager radioManager;
    [SerializeField] StudioEventEmitter garageAmbienceEmitter;
    public bool hasJustTravelledByTaxi = false;
    private GameObject shopCanvas;
    public GameObject shopPanel;
    private MapSpline mapSplineScript;
    private MapController mapControllerScript;

    // Below are for returned variables from MapSpline 
    private float startTime;
    private float playerMoveSpeed;
    private float completion;
    private bool isPlayerMoving;


    private void Update()
    {
        shopCanvas = GameObject.Find("ShopCanvas");
        shopPanel = GameObject.Find("ShopPanel");

        // Mutes radio if the map is open & the shoppanel is not, unmutes radio if shoppanel is open
        if (shopCanvas != null && shopPanel == null)
        {
            //Debug.Log("shopCanvas found - AudioDebug");
            FindMapSplineScriptAndGetData();
            FindMapControllerScript();

            if (RadioManager.instance != null)
            {
                RadioManager.instance.MuteRadio();
            }
        }
        else
        {
            if (RadioManager.instance != null)
            {
                RadioManager.instance.UnMuteRadio();
            }
        }

        
    }

    void FindMapControllerScript()
    {
        mapControllerScript = shopCanvas.GetComponent<MapController>();
        if (mapControllerScript != null)
        {
            //Debug.Log("mapControllerScript found - AudioDebug");
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

        if(completion > 0.77 && completion < 0.99 && isPlayerMoving == true && hasJustTravelledByTaxi == true)
        {
            if (AudioManager.instance != null)
            {
                //Debug.Log("Taxi Journey completion is between 0.77 & 0.99 - AudioDebug");
                AudioManager.instance.payAndExitTaxi.start();
            }
        }
    }

    public void MuteGarageAudio()
    {
        //Debug.Log("Garage Ambience was muted because the map was opened - AudioDebug");

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
            garageAmbienceEmitter.SetParameter("Garage Ambience Volume", 0f, false);
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.countrysideAmbience.start();
        }
    }

    public void UnmuteGarageAudio()
    {
        //Debug.Log("Garage Ambience was unmuted because the map was closed - AudioDebug");

        //if (radioManager != null)
        //{
        //    Debug.Log("radioManager found - AudioDebug");
        //    radioManager.MuteRadio();
        //    garageAmbienceEmitter.SetParameter("Mute Garage Ambience", 0f, false);
        //}

        //if (RadioManager.instance != null)
        //{
        //    //Debug.Log("RadioManager found - AudioDebug");
        //    //RadioManager.instance.UnMuteRadio();
        //    garageAmbienceEmitter.SetParameter("Mute Garage Ambience", 1f, false);
        //}
        

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

        if(mapControllerScript != null)
        {
            if (mapControllerScript.GetCurrentPlayerLocation() == MapController.PlayerLocation.Home)
            {
                if (RadioManager.instance != null)
                {
                    //Debug.Log("RadioManager found - AudioDebug");
                    //RadioManager.instance.UnMuteRadio();
                    Debug.Log("Player is Home, Unmuting Garage Ambience - AudioDebug");
                    garageAmbienceEmitter.SetParameter("Garage Ambience Volume", 1f, false);
                    if (AudioManager.instance != null)
                    {
                        AudioManager.instance.countrysideAmbience.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    }
                }
            }
            else
            {
                Debug.Log("Player is not Home, Cannot unmute Garage Ambience - AudioDebug");
            }
        }
    }

    // Called by the Already Here Btn
    public void OutsideToShopAudioTransition()
    {
        if (shopPanel != null)
        {
            if (AudioManager.instance != null)
            {
                //Debug.Log("Muting countryside Ambience because the player is at the shop - AudioDebug");
                AudioManager.instance.shopRoomTone.start();
                AudioManager.instance.countrysideAmbience.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                
            }
        }
    }
    // Called by the Exit Shop Btn
    public void ShopToOutsideAudioTransition()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.countrysideAmbience.start();
            AudioManager.instance.shopRoomTone.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }

    public void CallPlayEnterTaxiSoundIEnum()
    {
        StartCoroutine(PlayEnterTaxiSound());
    }

    private IEnumerator PlayEnterTaxiSound()
    {
        if (AudioManager.instance != null)
        {
            if (PlayerManager.Instance != null)
            {
                float coins = PlayerManager.Instance.GetPlayerCoins();
                if (coins >= 40)
                {
                    //Debug.Log($"Player has {coins} and can afford a taxi - AudioDebug");
                    AudioManager.instance.enterTaxi.start();
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

    public void PlayOpenDoorSound()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.openDoor.start();
        }
    }
}
