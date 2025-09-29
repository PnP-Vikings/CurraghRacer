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
using NUnit.Framework.Interfaces;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

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
    [FormerlySerializedAs("LoadedRaceScene")]
    public bool loadedRaceScene = false; // Flag to check
    public int raceStartDelaySeconds = 5; // Delay


    public GameObject shipPrefab;
    
    
    public List<GameObject> ships = new List<GameObject>();

    public List<ShipMovement> RaceMovementPositions;

    public List<ShipMovement> currentRaceMovementPositions;
    public Transform finishLineTransform;
    public ShipMovement playerShip;
    
    [SerializeField]
    private FinishMenu finishMenu;
    
    
    [SerializeField]
    public bool waitingForAd = false; // Flag to check if we are waiting for an ad to show

    public FMOD.Studio.EventInstance GarageAmbience;
    FMOD.Studio.EventInstance RaceAmbience;
    public FMOD.Studio.EventInstance RaceWin;
    FMOD.Studio.EventInstance RaceLose;
    public FMOD.Studio.EventInstance Radio;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void OnEnable()
    {
      
    }

    private void Start()
    {
        GarageAmbience = FMODUnity.RuntimeManager.CreateInstance("event:/Garage/Garage Ambience");
        GarageAmbience.start();
        Radio = FMODUnity.RuntimeManager.CreateInstance("event:/Garage/Radio");
        Radio.start();
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
                Debug.Log("Checking today's events for Race Day..." + isRaceDay + " Event: " + eventType.eventName);
            });
        }
        else
        {
            isRaceDay = false;
            Debug.Log("No events today - not Race Day");
        }
    }

    public void StartRace()
    {
        if (isRaceStarted) return; // Prevent starting multiple times

        RaceDetails[] raceTracks = LeagueController.Instance?.currentLeague.raceTracks;
        if (raceTracks != null && raceTracks.Length > 0 && !loadedRaceScene)
        {
            Debug.Log("Starting Race with " + raceTracks.Length + " tracks available.");
            SceneManager.LoadScene(raceTracks[Random.Range(0, raceTracks.Length)].raceSceneName ?? "DefaultRaceScene");
            loadedRaceScene = true; // Set flag to
            GameManager.Instance.SetPlayerBusy(true);
        }
        else if (loadedRaceScene)
        {
            SpawnShips();
            Debug.Log("Spawning In Ships on New Scene");
        }
        else
        {
            if (LeagueController.Instance != null)
            {
                Debug.LogWarning("No Race Tracks found in current league -");
            }
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
        var raceTeamsList = raceTeams.ToList();
        // Ensure we don't spawn more ships than we have start positions
        int shipsToSpawn = Mathf.Min(raceTeams.Length, raceStartPositions.Count);
        
        Team playerTeamTemp = null; 
        
        for (int i = 0; i < raceTeams.Length; i++)
        {
            
            if (raceTeams[i].teamType == TeamType.Player)
            {
                // If player team is in
                playerTeamTemp = raceTeamsList[i];
                raceTeamsList.RemoveAt(i);
            }
        }
            
        raceTeamsList.Add(playerTeamTemp);
            
            
            
        raceTeams = raceTeamsList.ToArray();

        currentRace.teams = raceTeams;
       
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
            
            movement.shipName = raceTeams[i].teamName;
            ship.name = raceTeams[i].teamName;
            
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
            playerShip = lastMovement;
        }
        foreach (var go in ships)
        {
            currentRaceMovementPositions.Add(go.GetComponent<ShipMovement>());
        }
        
        finishLineTransform = GameObject.FindGameObjectWithTag("Finish").transform;
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
        yield return new WaitForSeconds(raceStartDelaySeconds);
        startRace.Invoke();

        RaceAmbience = FMODUnity.RuntimeManager.CreateInstance("event:/Race/Race Ambience");
        RaceAmbience.start();
        GarageAmbience.setParameterByName("Mute Garage Ambience", 0f);
        //GarageAmbience.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        Radio.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

        foreach (var go in ships)
            go.GetComponent<ShipMovement>().SetRaceStarted(true);
        
        StartCoroutine(CheckShipPositionsRoutine());
    }
    
     IEnumerator CheckShipPositionsRoutine()
    {
        var wait = new WaitForSeconds(.02f); // Instantiate once outside the loop
       
        while (!RaceFinished())
        {
            yield return wait;
            CalculateShipPositions();
        }
    }
    
    public void CalculateShipPositions()
    {
        // Sort ships by forward progress (descending - most progress = best position)
        var sortedShips = ships.OrderByDescending(ship =>
        {
            var movement = ship.GetComponent<ShipMovement>();
            if (movement != null)
            {
                // Calculate forward progress by measuring Z-axis movement from starting position
                // Assumes ships move in the positive Z direction (forward)
                return movement.transform.position.z;
            }
            return float.MinValue;
        }).ToList();

        // Update RaceMovementPositions based on sorted order
        currentRaceMovementPositions.Clear();
        foreach (var ship in sortedShips)
        {
            var movement = ship.GetComponent<ShipMovement>();
            if (movement != null)
            {
                currentRaceMovementPositions.Add(movement);
            }
        }
        
        for (int i = 0; i < currentRaceMovementPositions.Count; i++)
        {
            currentRaceMovementPositions[i].SetShipPositionText(i + 1);
        }
        
        Debug.Log($"Updated positions for {currentRaceMovementPositions.Count} ships. Player position: {GetCurrentPlayerPosition()}");
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
            
            Debug.Log(firstPlaceShip + " finished first!" + secondPlaceShip + " finished second!" + thirdPlaceShip + " finished third!" + forthPlaceShip + " finished forth!"); 
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

                RaceAmbience.setParameterByName("Mute Encouragement", 0f);
                RaceAmbience.setParameterByName("Mute Rowing", 0f);
                RaceWin = FMODUnity.RuntimeManager.CreateInstance("event:/Race/Race Win");
                RaceWin.start();
            }
            else
            {
                Debug.Log("Player did not finish first.");
                finishMenu.UpdatePlayerMessage(false, "Better luck next time!");
                if(!isRaceDay) return; // No coins deducted

                RaceAmbience.setParameterByName("Mute Encouragement", 0f);
                RaceAmbience.setParameterByName("Mute Rowing", 0f);
                RaceLose = FMODUnity.RuntimeManager.CreateInstance("event:/Race/Race Lose");
                RaceLose.start();
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
        loadedRaceScene = false; // Reset flag when ending
        
        mainCamera.transform.position = GameManager.Instance.cameraStartPosition.position;
        mainCamera.transform.rotation = GameManager.Instance.cameraStartPosition.rotation;

        RaceAmbience.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        SceneManager.LoadScene(GameManager.Instance.mainSceneName);
        GameManager.Instance.SetPlayerBusy(false);
    }
    
    IEnumerator ShowAd()
    {
        waitingForAd = true;
        yield return new WaitForSeconds(5f);
        AdsManager.Instance.interstitialAds.ShowInterstitialAd();
    }
    
    public bool RaceFinished()
    {
        // Race is finished when all ships have crossed the finish line
        // RaceMovementPositions contains ships that have finished
        return ships.Count > 0 && RaceMovementPositions.Count >= ships.Count;
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
        if(!isRaceDay) return; // Only record results
        
        
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

        for (int i = 0; i < raceTeams.Length; i++)
        {
          Debug.Log("Race Teams Records" +  raceTeams[i].teamName);  
        }
        
        int[] allPositions = new int[raceTeams.Length];
        
        // Map ship finishing positions to team positions by matching ship names to team names
        for (int i = 0; i < RaceMovementPositions.Count; i++)
        {
            ShipMovement ship = RaceMovementPositions[i];
            int finishingPosition = i + 1; // Position is 1-based (1st, 2nd, 3rd, etc.)
            
            // Find the corresponding team index for this ship
            for (int j = 0; j < raceTeams.Length; j++)
            {
                bool isMatch = false;
                
                if (ship.isPlayerShip && raceTeams[j].teamType == TeamType.Player)
                {
                    isMatch = true;
                }
                else if (!ship.isPlayerShip && raceTeams[j].teamType != TeamType.Player && raceTeams[j].teamName == ship.shipName)
                {
                    isMatch = true;
                }
                
                if (isMatch)
                {
                    allPositions[j] = finishingPosition;
                    Debug.Log($"Team {raceTeams[j].teamName} finished in position {finishingPosition}");
                    break;
                }
            }
        }
        
        // Verify all teams have been assigned positions
        for (int i = 0; i < allPositions.Length; i++)
        {
            if (allPositions[i] == 0)
            {
                Debug.LogError($"Team {raceTeams[i].teamName} was not assigned a finishing position!");
                // Assign remaining position as fallback
                for (int pos = 1; pos <= raceTeams.Length; pos++)
                {
                    bool positionTaken = false;
                    for (int j = 0; j < allPositions.Length; j++)
                    {
                        if (allPositions[j] == pos)
                        {
                            positionTaken = true;
                            break;
                        }
                    }
                    if (!positionTaken)
                    {
                        allPositions[i] = pos;
                        Debug.Log($"Fallback: Assigned position {pos} to team {raceTeams[i].teamName}");
                        break;
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
        
        // Add completed race to persistent tracking system
        if (Calendar.CompletedRacesManager.Instance != null)
        {
            string leagueName = LeagueController.Instance.currentLeague.leagueName;
            string raceName = $"Race Day {(TimeManager.Instance?.GetCurrentDate().DayOfYear ?? System.DateTime.Now.DayOfYear)}";
            DateTime raceDate = TimeManager.Instance?.GetCurrentDate() ?? DateTime.Now;
            string trackName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            int totalParticipants = raceTeams.Length;
            float playerRaceTime = Time.time; // Simple fallback - can be enhanced later
            string[] participantNames = new string[RaceMovementPositions.Count];
            
            // Get participant names in finishing order
            for (int i = 0; i < RaceMovementPositions.Count; i++)
            {
                participantNames[i] = RaceMovementPositions[i].shipName;
            }

            // Calculate points (F1-style scoring)
            int pointsEarned = 0;
            switch (playerPosition)
            {
                case 1: pointsEarned = 25; break;
                case 2: pointsEarned = 18; break;
                case 3: pointsEarned = 15; break;
                case 4: pointsEarned = 12; break;
                case 5: pointsEarned = 10; break;
                case 6: pointsEarned = 8; break;
                case 7: pointsEarned = 6; break;
                case 8: pointsEarned = 4; break;
                case 9: pointsEarned = 2; break;
                case 10: pointsEarned = 1; break;
                default: pointsEarned = 0; break;
            }

            Calendar.CompletedRacesManager.Instance.AddCompletedRace(
                leagueName,
                raceName,
                raceDate,
                playerPosition,
                totalParticipants,
                trackName,
                playerRaceTime,
                pointsEarned,
                participantNames
            );

            Debug.Log($"Completed race tracked: {leagueName} - {raceName} (Position: {playerPosition})");
        }
        else
        {
            Debug.LogWarning("CompletedRacesManager not found - race not tracked for calendar!");
        }
        
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
    
    /// <summary>
    /// Helper method to get the current player position during the race
    /// </summary>
    private int GetCurrentPlayerPosition()
    {
        for (int i = 0; i < currentRaceMovementPositions.Count; i++)
        {
            if (currentRaceMovementPositions[i].isPlayerShip)
            {
                return i + 1;
            }
        }
        return -1; // Player not found
    }
}
