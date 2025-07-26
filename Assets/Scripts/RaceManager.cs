using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Threading;
using Calendar;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using EventType = UnityEngine.EventType;
using Random = UnityEngine.Random;
using League;

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
        
        // Get the teams scheduled for this race from the league
        var currentRace = LeagueController.Instance?.GetCurrentPlayerRace();
        if (currentRace == null)
        {
            Debug.LogWarning("No current race found in league - using fallback ship spawning");
            SpawnShipsWithFallback();
            return;
        }

        var raceTeams = currentRace.teams;
        
        // Ensure we don't spawn more ships than we have start positions
        int shipsToSpawn = Mathf.Min(raceTeams.Length, raceStartPositions.Count);
        
        for (int i = 0; i < shipsToSpawn; i++)
        {
            Transform racepos = raceStartPositions[i];
            Team team = raceTeams[i];
            
            Debug.Log($"Spawning ship for team: {team.teamName} at position: {racepos.position}");
            
            GameObject ship = Instantiate(shipPrefab, racepos.position, shipPrefab.transform.rotation);
            var movement = ship.GetComponent<ShipMovement>();
            
            // Assign team data to ship
            movement.shipName = team.teamName;
            ship.name = team.teamName + "_Ship";
            
            // Check if this is the last boat position (closest to camera) and if player team is in this race
            bool isLastBoat = (i == shipsToSpawn - 1);
            bool playerInRace = raceTeams.Any(t => t.teamType == TeamType.Player);
            
            if (isLastBoat && playerInRace)
            {
                // Set up player ship on the last (closest to camera) position
                movement.stats = PlayerManager.Instance.GetPlayerStats();
                movement.isPlayerShip = true;
                movement.shipName = "Player Ship";
                ship.name = "PlayerShip";
                playerShip = movement;
            }
            else if (team.teamType == TeamType.Player)
            {
                // If player team is not in last position, swap it to last position
                // This ensures player is always in the last boat regardless of team order
                movement.stats = team.GetTeamStats();
                movement.isPlayerShip = false;
                // We'll handle the player assignment after the loop
            }
            else
            {
                // Set up AI ship with team stats
                movement.stats = team.GetTeamStats();
                movement.isPlayerShip = false;
            }
            
            ships.Add(ship);
        }
        
        // Ensure player is always in the last boat (closest to camera)
        if (raceTeams.Any(t => t.teamType == TeamType.Player) && ships.Count > 0)
        {
            var lastShip = ships[ships.Count - 1];
            var lastMovement = lastShip.GetComponent<ShipMovement>();
            
            // Set up as player ship
            lastMovement.stats = PlayerManager.Instance.GetPlayerStats();
            lastMovement.isPlayerShip = true;
            lastMovement.shipName = "Player Ship";
            lastShip.name = "PlayerShip";
            playerShip = lastMovement;
        }
        
        StartCoroutine(StartShips());
    }

    /// <summary>
    /// Fallback method for spawning ships when no league race is available
    /// </summary>
    private void SpawnShipsWithFallback()
    {
        foreach (Transform racepos in raceStartPositions)
        {
            Debug.Log("Spawning ship at position: " + racepos.position);
            GameObject ship = Instantiate(shipPrefab, racepos.position, shipPrefab.transform.rotation);
            var movement = ship.GetComponent<ShipMovement>();
            movement.shipName = "Ship " + (ships.Count + 1);
            
            // Generate AI stats as fallback
            var aiStats = new CharacterStats(
                strength : Random.Range(8f, 12f)  * difficulty,
                stamina  : Random.Range(8f, 12f)  * difficulty,
                technique: Random.Range(5f, 10f)  * difficulty,
                teamWork : Random.Range(5f, 10f)  * difficulty
            );
            movement.stats = aiStats;
            
            ships.Add(ship);
        }
        
        // Mark one as "player" for fallback
        var playerGO = ships[ships.Count - 1];
        var playerMove = playerGO.GetComponent<ShipMovement>();
        playerMove.stats = PlayerManager.Instance.GetPlayerStats();
        playerMove.isPlayerShip = true;
        playerMove.shipName = "Player Ship";
        playerGO.name = "PlayerShip";
        playerShip = playerMove;
        
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
                finishMenu = FindFirstObjectByType<FinishLine>().finishMenu;
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

            // Record race results in League system
            RecordRaceResults();

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

    /// <summary>
    /// Records the race results in the League system when a player race is completed.
    /// This method converts the RaceManager's ship results to the League system format.
    /// </summary>
    private void RecordRaceResults()
    {
        if (LeagueController.Instance?.currentLeague == null)
        {
            Debug.LogWarning("No active league found - race results not recorded");
            return;
        }

        // Get current player race from league
        var currentRace = LeagueController.Instance.GetCurrentPlayerRace();
        if (currentRace == null)
        {
            Debug.LogWarning("No current player race found in league");
            return;
        }

        var race = currentRace;
        
        // Create arrays to store teams and their positions
        Team[] raceTeams = race.teams;
        int[] allPositions = new int[raceTeams.Length];
        
        // Map ship names to team positions
        // For now, we'll use a simple mapping based on ship names
        // This assumes the ships were spawned in the same order as the teams in the race
        for (int i = 0; i < RaceMovementPositions.Count && i < raceTeams.Length; i++)
        {
            ShipMovement ship = RaceMovementPositions[i];
            int position = i + 1; // Position is 1-based (1st, 2nd, 3rd, etc.)
            
            // Find the corresponding team for this ship
            // If it's the player ship, find the player team
            if (ship.isPlayerShip)
            {
                for (int j = 0; j < raceTeams.Length; j++)
                {
                    if (raceTeams[j].teamType == TeamType.Player)
                    {
                        allPositions[j] = position;
                        break;
                    }
                }
            }
            else
            {
                // For AI ships, map them to AI teams in order
                // This is a simplified mapping - in a more complex system you'd want
                // to properly track which ship corresponds to which team
                int aiTeamIndex = 0;
                for (int j = 0; j < raceTeams.Length; j++)
                {
                    if (raceTeams[j].teamType != TeamType.Player)
                    {
                        if (aiTeamIndex == (i - (ship.isPlayerShip ? 0 : GetPlayerShipIndex())))
                        {
                            allPositions[j] = position;
                            break;
                        }
                        aiTeamIndex++;
                    }
                }
            }
        }

        // Find player's finishing position
        int playerPosition = 0;
        for (int i = 0; i < RaceMovementPositions.Count; i++)
        {
            if (RaceMovementPositions[i].isPlayerShip)
            {
                playerPosition = i + 1;
                break;
            }
        }

        // Record the results in the league system
        LeagueController.Instance.RecordPlayerRaceResult(playerPosition, raceTeams, allPositions);
        
        Debug.Log($"Race results recorded - Player finished {playerPosition}");
    }

    /// <summary>
    /// Helper method to find the index of the player ship in the finishing order
    /// </summary>
    private int GetPlayerShipIndex()
    {
        for (int i = 0; i < RaceMovementPositions.Count; i++)
        {
            if (RaceMovementPositions[i].isPlayerShip)
                return i;
        }
        return -1;
    }
}
