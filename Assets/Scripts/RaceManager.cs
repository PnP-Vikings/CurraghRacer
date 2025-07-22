using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Threading;
using Calendar;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using EventType = UnityEngine.EventType;
using Random = UnityEngine.Random;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }
    public Camera mainCamera;
    public bool isRaceStarted = false;
    [Header("Spawn & Difficulty")]
    [Range(0.5f, 100f)]
    public float difficulty = 1f; 
    // 0.5 = easy crews, 1 = normal, 2 = monster crews
    public List<Transform> raceStartPositions;
    public UnityEvent startRace;
    public bool isRaceDay;


    public GameObject shipPrefab;
    
    
    public List<GameObject> ships = new List<GameObject>();

    public List<ShipMovement> RaceMovementPositions;
    
    public ShipMovement playerShip;
    
    [SerializeField]
    private FinishMenu finishMenu;
    
    
    [SerializeField]
    public bool waitingForAd = false; // Flag to check if we are waiting for an ad to show

    public FMOD.Studio.EventInstance GarageAmbience;
    FMOD.Studio.EventInstance RaceAmbience;
    public FMOD.Studio.EventInstance CheeringAndClapping;
    FMOD.Studio.EventInstance NegativeEncouragement;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            GarageAmbience = FMODUnity.RuntimeManager.CreateInstance("event:/Garage/Garage Ambience");
            GarageAmbience.start();
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void OnEnable()
    {
        TimeManager.Instance.todaysEvents.AddListener(CheckForRaceDay);
    }

    
    // Listener receives today's events list
    public void CheckForRaceDay(List<DayEventType> todaysEvents)
    {

        if (todaysEvents != null && todaysEvents.Count > 0)
        {
            todaysEvents.ForEach(eventType =>
            {
                if (eventType.OccasionType == Calendar.OccasionType.Race)
                {
                    isRaceDay = true;
                }
                else
                {
                   isRaceDay = false;
                }
                
            });
        }
    }

    public void SpawnShips()
    {
        if (!GameManager.Instance.GetGameStarted()) return;
        
        
        foreach (Transform racepos in raceStartPositions)
        {
            Debug.Log("Spawning ship at position: " + racepos.position);
            GameObject ship = Instantiate(shipPrefab, racepos.position, shipPrefab.transform.rotation);
            var movement = ship.GetComponent<ShipMovement>();
            movement.shipName = "Ship " + (ships.Count + 1);
            
            // 1) Generate AI stats
            var aiStats = new CharacterStats(
                strength : Random.Range(8f, 12f)  * difficulty,
                stamina  : Random.Range(8f, 12f)  * difficulty,
                technique: Random.Range(5f, 10f)  * difficulty,
                teamWork : Random.Range(5f, 10f)  * difficulty
            );
            movement.stats = aiStats;
            
            ships.Add(ship);
            
        }
        
        // 2) Mark one as “player”
        var playerGO = ships[ships.Count - 1];
        var playerMove = playerGO.GetComponent<ShipMovement>();
        playerMove.stats      = PlayerManager.Instance.GetPlayerStats();
        playerMove.isPlayerShip = true; // Set this ship as the player's ship
        playerMove.shipName   = "Player Ship";
        playerGO.name         = "PlayerShip";
        playerShip = playerMove; // Store reference to player ship
        StartCoroutine(StartShips());
    }

    IEnumerator StartShips()
    {
        yield return new WaitForSeconds(1f);
        startRace.Invoke();

        RaceAmbience = FMODUnity.RuntimeManager.CreateInstance("event:/Race/Race Ambience");
        RaceAmbience.start();
        GarageAmbience.setParameterByName("Mute Garage Ambience", 0f);
        //GarageAmbience.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

        foreach (var go in ships)
            go.GetComponent<ShipMovement>().SetRaceStarted(true);
    }

    public void ShipFinished(ShipMovement ship)
    {
        RaceMovementPositions.Add(ship);
        
        bool raceisFinished = RaceFinished();

        if (ship.isPlayerShip)
        {
            foreach(GameObject shipGO in ships)
            {
                ShipMovement shipMovement = shipGO.GetComponent<ShipMovement>();
                if(shipMovement!=null)
                {
                   shipMovement.SetAiStatsAfterPlayerFinished(10f); // Speed up AI ships after player finishes
                }
            }
        }
        
        if (raceisFinished)
        {
            foreach (ShipMovement shipMovement in RaceMovementPositions)
            {
                Debug.Log("Ship finished: " + shipMovement.shipName + " at position: " + (RaceMovementPositions.IndexOf(shipMovement) + 1));
            }
            
            if(GameManager.Instance.CanShowAd())
            {
                StartCoroutine(ShowAd());
                GameManager.Instance.HideBannerAd();
            }
            
            
        }
        
     
    }
    
    public void IsRaceFinished()
    {
        if (RaceFinished())
        {
            if(finishMenu == null)
            {
                finishMenu = FindObjectOfType<FinishLine>().finishMenu;
            }
            
            
            finishMenu.gameObject.SetActive(true);
            
            string firstPlaceShip = RaceMovementPositions[0].shipName;
            string secondPlaceShip = RaceMovementPositions[1].shipName;
            string thirdPlaceShip = RaceMovementPositions[2].shipName;
            string forthPlaceShip = RaceMovementPositions.Count > 3 ? RaceMovementPositions[3].shipName : "N/A";
            finishMenu.UpdatePositions( firstPlaceShip, secondPlaceShip, thirdPlaceShip, forthPlaceShip);
            
            Transform cameraStartPosition = GameManager.Instance.GetCameraStartPosition();
            
            mainCamera.transform.position = cameraStartPosition.position;
            mainCamera.transform.rotation = cameraStartPosition.rotation;


            if (isRaceDay)
            {
                PlayerManager.Instance.ModifyPlayerEnergy(-50);
            }
            else
            {
                PlayerManager.Instance.ModifyPlayerEnergy(-25);
            }


            if (RaceMovementPositions[0].isPlayerShip)
            {
                Debug.Log("Player finished first!");
                finishMenu.UpdatePlayerMessage(true, "You are the champion!");
                if (isRaceDay)
                {
                    PlayerManager.Instance.ModifyPlayerCoins(125f); // Reward player with coins
                    difficulty += .3f;
                }
                CheeringAndClapping = FMODUnity.RuntimeManager.CreateInstance("event:/Race/Cheering and Clapping");
                CheeringAndClapping.start();
                RaceAmbience.setParameterByName("Mute Positive Encouragement", 0f);
                RaceAmbience.setParameterByName("Mute Rowing", 0f);


            }
            else
            {
                Debug.Log("Player did not finish first.");
                finishMenu.UpdatePlayerMessage(false, "Better luck next time!");
                if(!isRaceDay) return; // No coins deducted
                
                if( PlayerManager.Instance.GetPlayerCoins () < 50f)
                {
                    PlayerManager.Instance.ModifyPlayerCoins(0f);
                }
                PlayerManager.Instance.ModifyPlayerCoins(-50f); // Deduct coins for not winning

                RaceAmbience.setParameterByName("Mute Rowing", 0f);
                RaceAmbience.setParameterByName("Mute Positive Encouragement", 0f);
                NegativeEncouragement = FMODUnity.RuntimeManager.CreateInstance("event:/Race/Negative Encouragement");
                NegativeEncouragement.start();
            }
          
            
            
            
            
            
            
        }
    }

    public void EndRace()
    {
        RaceMovementPositions.Clear();
        foreach (GameObject ship in ships)
        {
            Destroy(ship);
        }
        ships.Clear();
        isRaceStarted = false;
        
        mainCamera.transform.position = GameManager.Instance.cameraStartPosition.position;
        mainCamera.transform.rotation = GameManager.Instance.cameraStartPosition.rotation;

        RaceAmbience.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }
    
    IEnumerator ShowAd()
    {
        waitingForAd = true;
        yield return new WaitForSeconds(5f);
        AdsManager.Instance.interstitialAds.ShowInterstitialAd();
    }
    
    public bool RaceFinished()
    {
        if(ships.Count != RaceMovementPositions.Count)
        {
            return false;
        }
      return true;
    }

    public void OnDisable()
    {
        TimeManager.Instance.todaysEvents.RemoveListener(CheckForRaceDay);
    }



}
