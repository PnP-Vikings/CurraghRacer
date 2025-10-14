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
        
        // If ShipStartLocations hasn't initialized yet, wait for it
        if (ShipStartLocations.Instance == null)
        {
            Debug.Log("RaceManager: ShipStartLocations.Instance is NULL - waiting for initialization");
            StartCoroutine(WaitForShipStartLocationsAndSpawn());
            return;
        }
        
        // Always try to get fresh positions from ShipStartLocations if available
        Debug.Log($"RaceManager: ShipStartLocations.Instance is {(ShipStartLocations.Instance == null ? "NULL" : "not null")}");
        if (ShipStartLocations.Instance != null && ShipStartLocations.Instance.raceStartPositions != null)
        {
            Debug.Log($"RaceManager: ShipStartLocations.Instance.raceStartPositions is not null");
            raceStartPositions = ShipStartLocations.Instance.raceStartPositions;
            Debug.Log($"Got positions from ShipStartLocations: {raceStartPositions.Count} positions");
            
            // Debug each transform immediately after assignment
            for (int i = 0; i < raceStartPositions.Count; i++)
            {
                Transform t = raceStartPositions[i];
                Debug.Log($"RaceManager: Position {i} from ShipStartLocations is {(t == null ? "NULL" : t.name)}");
            }
        }
        else if (raceStartPositions == null || raceStartPositions.Count == 0)
        {
            Debug.Log("ShipStartLocations not available, trying fallback methods");
        }

        if (raceStartPositions == null || raceStartPositions.Count == 0)
        {
            // Fallback to finding by tag, but filter out null transforms
            var foundObjects = GameObject.FindGameObjectsWithTag("RaceStart");
            raceStartPositions = foundObjects
                .Where(go => go != null && go.transform != null)
                .Select(go => go.transform)
                .Where(t => t != null)
                .ToList();
            Debug.Log($"Searched by tag and found {raceStartPositions.Count} valid start positions");
        }
        
        
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

        Debug.Log($"Main SpawnShips: raceStartPositions list has {raceStartPositions.Count} items");
        
        // Count how many are actually null
        int nullCount = raceStartPositions.Count(t => t == null);
        int validCount = raceStartPositions.Count - nullCount;
        Debug.Log($"Main SpawnShips: Of {raceStartPositions.Count} positions: {validCount} valid, {nullCount} null");

        if (shipPrefab == null)
        {
            Debug.LogError("Ship prefab is null! Cannot spawn ships. Please assign the ship prefab in the RaceManager inspector.");
            return;
        }
       
        for (int i = 0; i < shipsToSpawn; i++)
        {
            Transform racepos = raceStartPositions[i];
            Team team = raceTeams[i];
            
            if (racepos == null)
            {
                Debug.LogWarning($"Race start position {i} is null - skipping team {team.teamName}");
                continue;
            }

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
                if (PlayerManager.Instance != null)
                {
                    movement.stats = PlayerManager.Instance.GetPlayerStats();
                }
                else
                {
                    Debug.LogWarning("PlayerManager.Instance is null, using fallback player stats");
                    movement.stats = new CharacterStats(
                        strength: 12f,
                        stamina: 12f,
                        technique: 10f,
                        teamWork: 10f
                    );
                }
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
            if (PlayerManager.Instance != null)
            {
                lastMovement.stats = PlayerManager.Instance.GetPlayerStats();
            }
            else
            {
                Debug.LogWarning("PlayerManager.Instance is null in SpawnShips, using fallback player stats");
                // Create fallback player stats
                lastMovement.stats = new CharacterStats(
                    strength: 12f,
                    stamina: 12f,
                    technique: 10f,
                    teamWork: 10f
                );
            }
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
        // If ShipStartLocations hasn't initialized yet, wait for it
        if (ShipStartLocations.Instance == null)
        {
            Debug.Log("Fallback: ShipStartLocations.Instance is NULL - waiting for initialization");
            StartCoroutine(WaitForShipStartLocationsAndSpawnFallback());
            return;
        }
        
        // Always try to get fresh positions from ShipStartLocations if available
        Debug.Log($"Fallback: ShipStartLocations.Instance is {(ShipStartLocations.Instance == null ? "NULL" : "not null")}");
        if (ShipStartLocations.Instance != null && ShipStartLocations.Instance.raceStartPositions != null)
        {
            Debug.Log($"Fallback: ShipStartLocations.Instance.raceStartPositions is not null");
            raceStartPositions = ShipStartLocations.Instance.raceStartPositions;
            Debug.Log($"Fallback: Got positions from ShipStartLocations: {raceStartPositions.Count} positions");
            
            // Debug each transform immediately after assignment
            for (int i = 0; i < raceStartPositions.Count; i++)
            {
                Transform t = raceStartPositions[i];
                Debug.Log($"Fallback: Position {i} from ShipStartLocations is {(t == null ? "NULL" : t.name)}");
            }
        }
        else if (raceStartPositions == null || raceStartPositions.Count == 0)
        {
            Debug.Log("Fallback: ShipStartLocations not available, trying tag-based fallback");
        }
        
        if(raceStartPositions == null || raceStartPositions.Count == 0)
        {
            // Fallback to finding by tag, but filter out null transforms
            var foundObjects = GameObject.FindGameObjectsWithTag("RaceStart");
            raceStartPositions = foundObjects
                .Where(go => go != null && go.transform != null)
                .Select(go => go.transform)
                .Where(t => t != null)
                .ToList();
            Debug.Log($"Searched by tag and found {raceStartPositions.Count} valid start positions");
        }

        if (raceStartPositions != null && raceStartPositions.Count != 0)
        {
            Debug.Log($"raceStartPositions list has {raceStartPositions.Count} items");
            
            // Count how many are actually null
            int nullCount = raceStartPositions.Count(t => t == null);
            int validCount = raceStartPositions.Count - nullCount;
            Debug.Log($"Of {raceStartPositions.Count} positions: {validCount} valid, {nullCount} null");
            
            if (validCount == 0)
            {
                Debug.LogError("All race start positions are null! Cannot spawn ships.");
            }
            else
            {
                if (raceStartPositions == null || raceStartPositions.Count == 0)
                {
                    // Try ShipStartLocations first, but with null check
                    if (ShipStartLocations.Instance != null && ShipStartLocations.Instance.raceStartPositions != null)
                    {
                        raceStartPositions = ShipStartLocations.Instance.raceStartPositions;
                        Debug.Log($"Got positions from ShipStartLocations: {raceStartPositions.Count} positions");
                    }
                }

                if (raceStartPositions == null || raceStartPositions.Count == 0)
                {
                    // Fallback to finding by tag, but filter out null transforms
                    var foundObjects = GameObject.FindGameObjectsWithTag("RaceStart");
                    raceStartPositions = foundObjects
                        .Where(go => go != null && go.transform != null)
                        .Select(go => go.transform)
                        .Where(t => t != null)
                        .ToList();
                    Debug.Log($"Searched by tag and found {raceStartPositions.Count} valid start positions");
                }
            }

            if (shipPrefab == null)
            {
                Debug.LogError("Ship prefab is null! Cannot spawn ships. Please assign the ship prefab in the RaceManager inspector.");
                return;
            }


            // Get teams from league system for realistic fallback
            Team[] fallbackTeams = GetFallbackTeams(raceStartPositions.Count);
            
            int teamIndex = 0;
            foreach (Transform racepos in raceStartPositions)
            {
                if (racepos == null)
                {
                    Debug.LogWarning("Found null Transform in raceStartPositions - skipping this spawn position");
                    continue;
                }
                
                Debug.Log("Spawning ship at position: " + racepos.position);
                GameObject ship = Instantiate(shipPrefab, racepos.position, shipPrefab.transform.rotation);
                var movement = ship.GetComponent<ShipMovement>();
                
                // Use real team name and stats instead of generic "Ship X"
                if (teamIndex < fallbackTeams.Length)
                {
                    Team currentTeam = fallbackTeams[teamIndex];
                    movement.shipName = currentTeam.teamName;
                    ship.name = currentTeam.teamName + "_Ship";
                    movement.stats = currentTeam.GetTeamStats();
                    Debug.Log($"Fallback: Using team {currentTeam.teamName} with stats from league system");
                }
                else
                {
                    // Ultimate fallback if we somehow don't have enough teams
                    movement.shipName = "Ship " + (ships.Count + 1);
                    movement.stats = new CharacterStats(
                        strength: Random.Range(8f, 12f) * difficulty,
                        stamina: Random.Range(8f, 12f) * difficulty,
                        technique: Random.Range(5f, 10f) * difficulty,
                        teamWork: Random.Range(5f, 10f) * difficulty
                    );
                    Debug.LogWarning($"Using ultimate fallback for ship {movement.shipName}");
                }

                ships.Add(ship);
                teamIndex++;
            }

            // Check if we actually spawned any ships
            if (ships.Count == 0)
            {
                Debug.LogError("No ships were spawned! All race start positions were null. Cannot start race.");
                return;
            }

            // Mark one as "player" for fallback
            var playerGO = ships[ships.Count - 1];
            var playerMove = playerGO.GetComponent<ShipMovement>();
            
            // Check if PlayerManager is available, otherwise use fallback stats
            if (PlayerManager.Instance != null)
            {
                playerMove.stats = PlayerManager.Instance.GetPlayerStats();
            }
            else
            {
                Debug.LogWarning("PlayerManager.Instance is null, using fallback player stats");
                // Create fallback player stats that are slightly better than AI
                playerMove.stats = new CharacterStats(
                    strength: 12f,
                    stamina: 12f,
                    technique: 10f,
                    teamWork: 10f
                );
            }
            
            playerMove.isPlayerShip = true;
            playerMove.shipName = "Player Ship";
            playerGO.name = "PlayerShip";
            playerShip = playerMove;

            StartCoroutine(StartShips());
        }
        else
        {
            Debug.LogError("No race start positions found - cannot spawn ships!");
        }
    }

    /// <summary>
    /// Waits for ShipStartLocations to initialize, then spawns ships
    /// </summary>
    IEnumerator WaitForShipStartLocationsAndSpawn()
    {
        Debug.Log("Waiting for ShipStartLocations to initialize...");
        float timeout = 5f; // 5 second timeout
        float elapsed = 0f;
        
        // Wait for ShipStartLocations.Instance to become available
        while (ShipStartLocations.Instance == null && elapsed < timeout)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
        }
        
        if (ShipStartLocations.Instance == null)
        {
            Debug.LogWarning("ShipStartLocations failed to initialize within timeout - using fallback spawning");
            SpawnShipsWithFallback();
        }
        else
        {
            Debug.Log("ShipStartLocations initialized - proceeding with normal spawning");
            SpawnShips(); // Call again now that ShipStartLocations is available
        }
    }

    /// <summary>
    /// Waits for ShipStartLocations to initialize, then spawns ships with fallback method
    /// </summary>
    IEnumerator WaitForShipStartLocationsAndSpawnFallback()
    {
        Debug.Log("Fallback: Waiting for ShipStartLocations to initialize...");
        float timeout = 5f; // 5 second timeout
        float elapsed = 0f;
        
        // Wait for ShipStartLocations.Instance to become available
        while (ShipStartLocations.Instance == null && elapsed < timeout)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
        }
        
        if (ShipStartLocations.Instance == null)
        {
            Debug.LogError("ShipStartLocations failed to initialize within timeout - cannot spawn ships!");
        }
        else
        {
            Debug.Log("Fallback: ShipStartLocations initialized - proceeding with fallback spawning");
            SpawnShipsWithFallback(); // Call again now that ShipStartLocations is available
        }
    }

    IEnumerator StartShips()
    {
        if (PlayerStatsView.Instance != null)
        {
            PlayerStatsView.Instance.ClearInfo();
        }
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
        if (finishLineTransform == null)
        {
            Debug.LogWarning("Finish line transform not set - attempting to find by tag");
            finishLineTransform = GameObject.FindGameObjectWithTag("Finish")?.transform;
            if (finishLineTransform == null)
            {
                Debug.LogError("Could not find finish line! Cannot calculate positions.");
                return;
            }
        }
        
        // Sort ships by forward progress toward finish line (ascending - less distance remaining = better position)
        var sortedShips = ships.OrderBy(ship =>
        {
            var movement = ship.GetComponent<ShipMovement>();
            if (movement != null)
            {
                // Calculate forward progress along the race direction (Z-axis)
                // Ships move forward in positive Z direction, so closer to finish line Z position = better
                float distanceToFinish = finishLineTransform.position.z - movement.transform.position.z;
                Debug.Log(movement.shipName + " distance to finish (Z-axis): " + distanceToFinish + " (Ship Z: " + movement.transform.position.z + ", Finish Z: " + finishLineTransform.position.z + ")");
                return distanceToFinish;
            }
            return float.MaxValue;
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

            if (PlayerManager.Instance != null)
            {
                if (isRaceDay)
                {
                    PlayerManager.Instance.ModifyPlayerEnergy(-50);
                }
                else
                {
                    PlayerManager.Instance.ModifyPlayerEnergy(-25);
                }
            }
            else
            {
                Debug.LogWarning("PlayerManager.Instance is null - cannot modify player energy");
            }


            if (RaceMovementPositions[0].isPlayerShip)
            {
                Debug.Log("Player finished first!");
                if (LeagueController.Instance != null && LeagueController.Instance.currentLeague != null && LeagueController.Instance.currentLeague.isFinished)
                {
                    finishMenu.UpdatePlayerMessage(true, "You are the champion!");
                }
                else
                {
                    finishMenu.UpdatePlayerMessage(true, "You have won the race!");
                }
               
                if (isRaceDay)
                {
                    if (PlayerManager.Instance != null)
                    {
                        PlayerManager.Instance.ModifyPlayerCoins(125f); // Reward player with coins
                    }
                    else
                    {
                        Debug.LogWarning("PlayerManager.Instance is null - cannot modify player coins");
                    }
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerAutoSave();
        }
        
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
    /// Gets teams for fallback spawning from the league system
    /// </summary>
    private Team[] GetFallbackTeams(int numberOfTeams)
    {
        List<Team> availableTeams = new List<Team>();
        
        // First, try to get teams from the current active league
        if (LeagueController.Instance?.currentLeague != null)
        {
            var currentLeague = LeagueController.Instance.currentLeague;
            Debug.Log($"Fallback: Using teams from current league: {currentLeague.leagueName}");
            
            // Get all teams from current league (excluding player team for now)
            foreach (var team in currentLeague.teams)
            {
                if (team.teamType != TeamType.Player) // We'll add player team separately
                {
                    availableTeams.Add(team);
                }
            }
        }
        
        // If current league doesn't have enough teams, try other leagues
        if (availableTeams.Count < numberOfTeams - 1) // -1 because we'll add player team
        {
            Debug.Log("Fallback: Current league doesn't have enough teams, searching other leagues");
            
            if (LeagueController.Instance?.leagues != null)
            {
                foreach (var league in LeagueController.Instance.leagues)
                {
                    if (league != LeagueController.Instance.currentLeague) // Skip current league (already processed)
                    {
                        foreach (var team in league.teams)
                        {
                            if (team.teamType != TeamType.Player && !availableTeams.Contains(team))
                            {
                                availableTeams.Add(team);
                                if (availableTeams.Count >= numberOfTeams - 1) break;
                            }
                        }
                        if (availableTeams.Count >= numberOfTeams - 1) break;
                    }
                }
            }
        }
        
        // Shuffle the teams for variety
        for (int i = 0; i < availableTeams.Count; i++)
        {
            Team temp = availableTeams[i];
            int randomIndex = Random.Range(i, availableTeams.Count);
            availableTeams[i] = availableTeams[randomIndex];
            availableTeams[randomIndex] = temp;
        }
        
        // Take only the number of teams we need
        List<Team> selectedTeams = availableTeams.Take(numberOfTeams - 1).ToList();
        
        // Add player team at the end (will be in last boat position)
        if (LeagueController.Instance?.currentLeague != null)
        {
            var playerTeam = LeagueController.Instance.currentLeague.teams.FirstOrDefault(t => t.teamType == TeamType.Player);
            if (playerTeam != null)
            {
                selectedTeams.Add(playerTeam);
                Debug.Log("Fallback: Added player team to race");
            }
            else
            {
                Debug.LogWarning("Fallback: No player team found, creating fallback player team");
                // Create a fallback player team
                var fallbackPlayerTeam = ScriptableObject.CreateInstance<Team>();
                fallbackPlayerTeam.teamName = "Player Team";
                fallbackPlayerTeam.teamType = TeamType.Player;
                selectedTeams.Add(fallbackPlayerTeam);
            }
        }
        else
        {
            Debug.LogWarning("Fallback: No league controller available, creating generic player team");
            var fallbackPlayerTeam = ScriptableObject.CreateInstance<Team>();
            fallbackPlayerTeam.teamName = "Player Team";
            fallbackPlayerTeam.teamType = TeamType.Player;
            selectedTeams.Add(fallbackPlayerTeam);
        }
        
        Debug.Log($"Fallback: Selected {selectedTeams.Count} teams for race");
        foreach (var team in selectedTeams)
        {
            Debug.Log($"  - {team.teamName} ({team.teamType})");
        }
        
        return selectedTeams.ToArray();
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
