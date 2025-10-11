using System;
using System.Collections.Generic;
using System.IO;
using Calendar;
using UnityEngine;
using League;

[System.Serializable]
public class SaveData
{
    [Header("Save Metadata")]
    public string saveName = "Save Slot";
    public string saveDate;
    public float playTime =0f;
    public int saveVersion = 1;

    [Header("Player Data")]
    public PlayerSaveData playerData;
    
    [Header("League Data")]
    public LeagueSaveData leagueData;
    
    [Header("Game Progress")]
    public GameProgressData gameProgress;
    
    [Header("Calendar Data")]
    public CalendarSaveData calendarData;
    [Header("Bill Data")]
    public BillSaveData billData;
   

    public SaveData()
    {
        saveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        playerData = new PlayerSaveData();
        leagueData = new LeagueSaveData();
        gameProgress = new GameProgressData();
        calendarData = new CalendarSaveData();
        billData = new BillSaveData();
    }
}

[System.Serializable]
public class PlayerSaveData
{
    public float energy = 100f;
    public float coins = 50f;
    public TeamMemberSaveData[] teamMembers;
    public string currentTeamName;
}

[System.Serializable]
public class BillSaveData
{
    public List<Bill> bills;
    public List<Bill> recurringPaidBills;
}

[System.Serializable]
public class TeamMemberSaveData
{
    public string memberName;
    public int age;
    public string attitude;
    public CharacterStatsSaveData stats;
    public int level;
    public int experience;
    public int racesAvailableFor;
    public TeamMemberFitness fitnessStatus;
    public Happiness happiness;
    
    public TeamMemberSaveData() { }
    
    public TeamMemberSaveData(TeamMember member)
    {
        memberName = member.memberName;
        age = member.age;
        attitude = member.attitude.ToString();
        stats = new CharacterStatsSaveData(member.characterStats);
        level = member.level;
        experience = member.experience;
        racesAvailableFor = member.racesAvailableFor;
        
        // Deep copy fitness status
        if (member.fitness != null)
        {
            fitnessStatus = new TeamMemberFitness
            {
                currentFitness = member.fitness.currentFitness,
                maxFitness = member.fitness.maxFitness,
                recoveryRate = member.fitness.recoveryRate,
                HungerLevel = member.fitness.HungerLevel,
                maxHungerLevel = member.fitness.maxHungerLevel,
                injuryStatus = member.fitness.injuryStatus,
                currentPhysicalState = member.fitness.currentPhysicalState
            };
        }
        
        // Deep copy happiness
        if (member.happiness != null)
        {
            happiness = new Happiness
            {
                currentHappiness = member.happiness.currentHappiness,
                maxHappiness = member.happiness.maxHappiness,
                currentMood = member.happiness.currentMood
            };
        }
    }
}



[System.Serializable]
public class CharacterStatsSaveData
{
    public float strength;
    public float stamina;
    public float technique;
    public float teamWork;
    
    public CharacterStatsSaveData() { }
    
    public CharacterStatsSaveData(CharacterStats stats)
    {
        strength = stats.strength;
        stamina = stats.stamina;
        technique = stats.technique;
        teamWork = stats.teamWork;
    }
    
    public CharacterStats ToCharacterStats()
    {
        return new CharacterStats(strength, stamina, technique, teamWork);
    }
}

[System.Serializable]
public class LeagueSaveData
{
    public string currentLeagueName;
    public bool playerHasJoined;
    public LeagueInfoSaveData[] allLeagues;
    public TeamSaveData[] allTeams;
    public int currentRaceIndex;
}

[System.Serializable]
public class LeagueInfoSaveData
{
    public string leagueName;
    public string description;
    public bool isActive;
    public bool playerHasJoined;
    public int currentRace;
    public int currentSeason;
    public bool isFinished;
    public int maxRaceDays;
    public int leagueRaceEntryCost;
    public bool isPromotionRelegation;
    public int numberOfTeamsToPromoteRelegate;
    public int maxNumberOfBoatsPerRace;
    public int repeatCount;
    public int maxExperienceGivenPerRace;
    public string tournamentStartDate;
    public TeamStandingSaveData[] standings;
    public RaceDayFormationSaveData[] raceSchedule;
}

[System.Serializable]
public class TeamStandingSaveData
{
    public string teamName;
    public int position;
    public int points;
    public int wins;
}

[System.Serializable]
public class RaceDayFormationSaveData
{
    public List<Race> races;
    public bool processed = false;
}

[System.Serializable]
public class TeamSaveData
{
    public string teamName;
    public string teamDescription;
    public string teamType;
    public int teamQuality;
    public int teamExperience;
    public float currentForm;
    public List<int> recentResults;
    public SeasonStatsSaveData currentSeasonStats;
    public SeasonStatsSaveData lifetimeStats;
    public TeamMemberManagerSaveData teamManager;
    public TeamMemberSaveData[] teamMembers;
    public TeamMemberSaveData[] bench;
    public ColorSaveData teamColor;
    
    public TeamSaveData()
    {
        recentResults = new List<int>();
        currentSeasonStats = new SeasonStatsSaveData();
        lifetimeStats = new SeasonStatsSaveData();
        teamColor = new ColorSaveData();
    }
}

[System.Serializable]
public class TeamMemberManagerSaveData : TeamMemberSaveData
{
  
 
}
  

[System.Serializable]
public class ColorSaveData
{
    public float r, g, b, a;
    
    public ColorSaveData() { }
    
    public ColorSaveData(Color color)
    {
        r = color.r;
        g = color.g;
        b = color.b;
        a = color.a;
    }
    
    public Color ToColor()
    {
        return new Color(r, g, b, a);
    }
}

[System.Serializable]
public class SeasonStatsSaveData
{
    public List<int> finishes;
    
    public SeasonStatsSaveData()
    {
        finishes = new List<int>();
    }
}

[System.Serializable]
public class RaceSaveData
{
    public string raceName;
    public string raceSceneName;
    public DateTime raceDate;
    public bool isCompleted;
    public TeamSaveData[] participatingTeams;
    public int[] finalPositions;
}

[System.Serializable]
public class GameProgressData
{
    public bool gameStarted;
    public float difficulty = 1f;
    public bool loadedRaceScene;
    public bool playerIsBusy;
    public Dictionary<string, bool> unlockedFeatures;
    
    public GameProgressData()
    {
        unlockedFeatures = new Dictionary<string, bool>();
    }
}

[System.Serializable]
public class CalendarSaveData
{
    public string currentDate;
    public CompletedRaceSaveData[] completedRaces;
    public DayEventSaveData[] scheduledEvents;
}

[System.Serializable]
public class CompletedRaceSaveData
{
    public string leagueName;
    public string raceName;
    public string raceDate;
    public int playerPosition;
    public int totalParticipants;
    public string trackName;
    public float raceTime;
    public int pointsEarned;
    public string[] participantNames;
}

[System.Serializable]
public class DayEventSaveData
{
    public string eventName;
    public string eventType;
    public string eventDate;
    public bool isCompleted;
}

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    [Header("Save Settings")]
    public int maxSaveSlots = 5;
    public string saveFileName = "CurraghRacerSave";

    [Header("Load State")]
    [SerializeField] private bool _wasLoadedFromSave = false;
    [SerializeField] private bool _isNewGame = false;

    /// <summary>
    /// Indicates whether the current game session was loaded from a save file
    /// </summary>
    public bool WasLoadedFromSave => _wasLoadedFromSave;
    
    public bool IsNewGame => _isNewGame;

    private string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Create save directory if it doesn't exist
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Save game data to a specific slot
    /// </summary>
    public bool SaveGame(int slotIndex, string saveName = "")
    {
        if (slotIndex < 0 || slotIndex >= maxSaveSlots)
        {
            Debug.LogError($"Invalid save slot index: {slotIndex}. Must be between 0 and {maxSaveSlots - 1}");
            return false;
        }

        try
        {
            SaveData saveData = CreateSaveData();

            if (!string.IsNullOrEmpty(saveName))
            {
                saveData.saveName = saveName;
            }

            string filePath = GetSaveFilePath(slotIndex);
            string jsonData = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(filePath, jsonData);

            Debug.Log($"Game saved successfully to slot {slotIndex}: {filePath}");
            return true;
        }
        catch( Exception e )
        {
            Debug.LogError($"Failed to save game to slot {slotIndex}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Load game data from a specific slot
    /// </summary>
    public bool LoadGame(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSaveSlots)
        {
            Debug.LogError($"Invalid save slot index: {slotIndex}. Must be between 0 and {maxSaveSlots - 1}");
            return false;
        }

        string filePath = GetSaveFilePath(slotIndex);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"No save file found at slot {slotIndex}: {filePath}");
            return false;
        }

        try
        {
            string jsonData = File.ReadAllText(filePath);
            SaveData saveData = JsonUtility.FromJson<SaveData>(jsonData);

            // Set the flags before applying save data
            _wasLoadedFromSave = true;
            _isNewGame = false;

            ApplySaveData(saveData);

            Debug.Log($"Game loaded successfully from slot {slotIndex}");
            return true;
        }
        catch( Exception e )
        {
            Debug.LogError($"Failed to load game from slot {slotIndex}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get save data preview for a specific slot without loading it
    /// </summary>
    public SaveData GetSavePreview(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSaveSlots)
        {
            return null;
        }

        string filePath = GetSaveFilePath(slotIndex);

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            string jsonData = File.ReadAllText(filePath);
            return JsonUtility.FromJson<SaveData>(jsonData);
        }
        catch( Exception e )
        {
            Debug.LogError($"Failed to preview save slot {slotIndex}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Delete a save slot
    /// </summary>
    public bool DeleteSave(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSaveSlots)
        {
            Debug.LogError($"Invalid save slot index: {slotIndex}");
            return false;
        }

        string filePath = GetSaveFilePath(slotIndex);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"No save file to delete at slot {slotIndex}");
            return false;
        }

        try
        {
            File.Delete(filePath);
            Debug.Log($"Save slot {slotIndex} deleted successfully");
            return true;
        }
        catch( Exception e )
        {
            Debug.LogError($"Failed to delete save slot {slotIndex}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if a save slot exists
    /// </summary>
    public bool SaveSlotExists(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSaveSlots)
        {
            return false;
        }

        return File.Exists(GetSaveFilePath(slotIndex));
    }

    /// <summary>
    /// Get all available save slots with their preview data
    /// </summary>
    public SaveSlotInfo[] GetAllSaveSlots()
    {
        SaveSlotInfo[] slots = new SaveSlotInfo[maxSaveSlots];

        for (int i = 0; i < maxSaveSlots; i++)
        {
            slots[i] = new SaveSlotInfo
            {
                slotIndex = i,
                exists = SaveSlotExists(i),
                saveData = GetSavePreview(i)
            };
        }

        return slots;
    }

    private string GetSaveFilePath(int slotIndex)
    {
        return Path.Combine(SaveDirectory, $"{saveFileName}_Slot{slotIndex:D2}.json");
    }

    private SaveData CreateSaveData()
    {
        SaveData saveData = new SaveData();

        // Save player data
        if (PlayerManager.Instance != null)
        {
            saveData.playerData.energy = PlayerManager.Instance.energy;
            saveData.playerData.coins = PlayerManager.Instance.coins;

            if (PlayerManager.Instance.team != null)
            {
                saveData.playerData.teamMembers = new TeamMemberSaveData[PlayerManager.Instance.team.Count];
                for (int i = 0; i < PlayerManager.Instance.team.Count; i++)
                {
                    saveData.playerData.teamMembers[i] = new TeamMemberSaveData(PlayerManager.Instance.team[i]);
                }
            }

            if (PlayerManager.Instance.playerTeam != null)
            {
                saveData.playerData.currentTeamName = PlayerManager.Instance.playerTeam.teamName;
            }
        }

        // Save league data
        if (LeagueController.Instance != null)
        {
            if (LeagueController.Instance.currentLeague != null)
            {
                saveData.leagueData.currentLeagueName = LeagueController.Instance.currentLeague.leagueName;
                saveData.leagueData.currentRaceIndex = LeagueController.Instance.currentLeague.currentRace;
                saveData.leagueData.playerHasJoined = LeagueController.Instance.currentLeague.playerHasJoined;
            }

            // Save all leagues with comprehensive data
            if (LeagueController.Instance.leagues != null)
            {
                List<LeagueInfoSaveData> allLeagues = new List<LeagueInfoSaveData>();
                List<TeamSaveData> allTeams = new List<TeamSaveData>();

                foreach (var league in LeagueController.Instance.leagues)
                {
                    // Save league information
                    LeagueInfoSaveData leagueInfo = new LeagueInfoSaveData
                    {
                        leagueName = league.leagueName,
                        isActive = league.isActive,
                        playerHasJoined = league.playerHasJoined,
                        currentRace = league.currentRace,
                        currentSeason = league.currentSeason,
                        isFinished = league.isFinished,
                        maxRaceDays = league.maxRaceDays,
                        leagueRaceEntryCost = league.leagueRaceEntryCost,
                        isPromotionRelegation = league.isPromotionRelegation,
                        numberOfTeamsToPromoteRelegate = league.numberOfTeamsToPromoteRelegate,
                        maxNumberOfBoatsPerRace = league.maxNumberOfBoatsPerRace,
                        repeatCount = league.repeatCount,
                        maxExperienceGivenPerRace = league.maxExperienceGivenPerRace,
                        tournamentStartDate = league.tournamentStartDate.ToString("yyyy-MM-dd HH:mm:ss")
                    };

                    // Save league standings
                    if (league.standings != null)
                    {
                        leagueInfo.standings = new TeamStandingSaveData[league.standings.Length];
                        for (int i = 0; i < league.standings.Length; i++)
                        {
                            leagueInfo.standings[i] = new TeamStandingSaveData
                            {
                                teamName = league.standings[i].team != null ? league.standings[i].team.teamName : "",
                                position = league.standings[i].position,
                                points = league.standings[i].points,
                                wins = league.standings[i].wins
                            };
                        }
                    }
                    
                    //Save Race Schedule
                    if (league.raceDays != null)
                    {
                        leagueInfo.raceSchedule = new RaceDayFormationSaveData[league.raceDays.Length];
                        for (int i = 0; i < league.raceDays.Length; i++)
                        {
                            leagueInfo.raceSchedule[i] = new RaceDayFormationSaveData
                            {
                                races = new List<Race>(league.raceDays[i].races),
                                processed = league.raceDays[i].processed
                            };

                        }
                    }

                    allLeagues.Add(leagueInfo);

                    // Save all teams from this league
                    if (league.teams != null)
                    {
                        foreach (var team in league.teams)
                        {
                            allTeams.Add(CreateTeamSaveData(team));
                        }
                    }
                }

                saveData.leagueData.allLeagues = allLeagues.ToArray();
                saveData.leagueData.allTeams = allTeams.ToArray();
            }
        }

        // Save game progress
        if (GameManager.Instance != null)
        {
            saveData.gameProgress.gameStarted = GameManager.Instance.GetGameStarted();
            saveData.gameProgress.playerIsBusy = GameManager.Instance.playerIsBusy;
        }

        if (RaceManager.Instance != null)
        {
            saveData.gameProgress.difficulty = RaceManager.Instance.difficulty;
            saveData.gameProgress.loadedRaceScene = RaceManager.Instance.loadedRaceScene;
        }

        // Save calendar data
        if (TimeManager.Instance != null)
        {
            saveData.calendarData.currentDate = TimeManager.Instance.GetCurrentDate().ToString("yyyy-MM-dd");
        }
        
        // Save play time
        if (GameManager.Instance != null)
        {
            saveData.playTime = GameManager.Instance.GetTotalPlayTime();
        }
        
        // Save Bills 
        if (BillsController.Instance != null)
        {
            BillSaveData billData = new BillSaveData
            {
                bills = BillsController.Instance.bills != null ? new List<Bill>(BillsController.Instance.bills) : new List<Bill>(),
                recurringPaidBills = BillsController.Instance.recurringPaidBills != null ? new List<Bill>(BillsController.Instance.recurringPaidBills) : new List<Bill>()
            };
            saveData.billData = billData;
        }

        return saveData;
    }

    private void ApplySaveData(SaveData saveData)
    {
        // Apply player data
        if (PlayerManager.Instance != null && saveData.playerData != null)
        {
            PlayerManager.Instance.energy = saveData.playerData.energy;
            PlayerManager.Instance.coins = saveData.playerData.coins;

            // Restore team members
            if (saveData.playerData.teamMembers != null && PlayerManager.Instance.team != null)
            {
                for (int i = 0; i < Mathf.Min(saveData.playerData.teamMembers.Length, PlayerManager.Instance.team.Count); i++)
                {
                    RestoreTeamMember(PlayerManager.Instance.team[i], saveData.playerData.teamMembers[i]);
                }
            }
        }

        // Apply league data
        if (LeagueController.Instance != null && saveData.leagueData != null)
        {
            // Restore all leagues with their complete data
            if (saveData.leagueData.allLeagues != null)
            {
                RestoreLeaguesData(saveData.leagueData.allLeagues);
            }

            // Find and set current league
            if (!string.IsNullOrEmpty(saveData.leagueData.currentLeagueName))
            {
                var league = Array.Find(LeagueController.Instance.leagues,
                    l => l.leagueName == saveData.leagueData.currentLeagueName);
                if (league != null)
                {
                    LeagueController.Instance.currentLeague = league;
                    league.playerHasJoined = saveData.leagueData.playerHasJoined;
                    league.currentRace = saveData.leagueData.currentRaceIndex;
                }
            }

            // Restore teams data
            if (saveData.leagueData.allTeams != null)
            {
                RestoreTeamsData(saveData.leagueData.allTeams);
            }
        }

        // Apply game progress
        if (saveData.gameProgress != null)
        {
            if (GameManager.Instance != null)
            {
                if (saveData.gameProgress.gameStarted)
                {
                    GameManager.Instance.SetGameStarted(saveData.gameProgress.gameStarted);
                }
                GameManager.Instance.SetPlayerBusy(saveData.gameProgress.playerIsBusy);
            }

            if (RaceManager.Instance != null)
            {
                RaceManager.Instance.difficulty = saveData.gameProgress.difficulty;
                RaceManager.Instance.loadedRaceScene = saveData.gameProgress.loadedRaceScene;
            }
        }

        // Apply calendar data
        if (TimeManager.Instance != null && saveData.calendarData != null)
        {
            if (!string.IsNullOrEmpty(saveData.calendarData.currentDate))
            {
                if (DateTime.TryParse(saveData.calendarData.currentDate, out DateTime savedDate))
                {
                    TimeManager.Instance.SetCurrentDate(savedDate);
                    Debug.Log($"Restored game date: {savedDate}");
                }
            }
        }

        // Update UI if needed
        if (PlayerManager.Instance?.playerStatsView != null)
        {
            PlayerManager.Instance.playerStatsView.UpdatePlayerStats();
        }
        
        // Apply Play Time
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetTotalPlayTime(saveData.playTime);
        }
        
        // Apply Bills Data
        
        if (BillsController.Instance != null && saveData.billData != null)
        {
            BillsController.Instance.bills = saveData.billData.bills != null ? new List<Bill>(saveData.billData.bills) : new List<Bill>();
            BillsController.Instance.recurringPaidBills = saveData.billData.recurringPaidBills != null ? new List<Bill>(saveData.billData.recurringPaidBills) : new List<Bill>();
            
            // Generate bills if this is a new game and bills are empty
            if (_isNewGame && BillsController.Instance.bills.Count == 0)
            {
                BillsController.Instance.GenerateBills();
            }
        }
    }

    private TeamSaveData CreateTeamSaveData(Team team)
    {
        TeamSaveData teamSave = new TeamSaveData
        {
            teamName = team.teamName,
            teamDescription = team.teamDescription,
            teamType = team.teamType.ToString(),
            teamQuality = team.teamQuality,
            teamExperience = team.teamExperience,
            currentForm = team.currentForm,
            teamManager =  team.teamManager != null ? new TeamMemberManagerSaveData() 
            { 
                memberName = team.teamManager.memberName,
                age = team.teamManager.age,
                attitude = team.teamManager.attitude.ToString(),
                stats = new CharacterStatsSaveData(team.teamManager.characterStats),
                level = team.teamManager.level,
                experience = team.teamManager.experience,
                racesAvailableFor = team.teamManager.racesAvailableFor,
                fitnessStatus = team.teamManager.fitness != null ? new TeamMemberFitness
                {
                    currentFitness = team.teamManager.fitness.currentFitness,
                    maxFitness = team.teamManager.fitness.maxFitness,
                    recoveryRate = team.teamManager.fitness.recoveryRate,
                    HungerLevel = team.teamManager.fitness.HungerLevel,
                    maxHungerLevel = team.teamManager.fitness.maxHungerLevel,
                    injuryStatus = team.teamManager.fitness.injuryStatus,
                    currentPhysicalState = team.teamManager.fitness.currentPhysicalState
                } : null,
                happiness = team.teamManager.happiness != null ? new Happiness
                {
                    currentHappiness = team.teamManager.happiness.currentHappiness,
                    maxHappiness = team.teamManager.happiness.maxHappiness,
                    currentMood = team.teamManager.happiness.currentMood
                } : null
            } : null,
            bench =  team.bench != null ? new TeamMemberSaveData[team.bench.Count] : null,
            recentResults = new List<int>(team.recentResults),
            teamColor = new ColorSaveData(team.teamColor)
        };

        // Save season stats
        if (team.currentSeasonStats != null)
        {
            teamSave.currentSeasonStats = new SeasonStatsSaveData
            {
                finishes = team.currentSeasonStats.finishes != null ? new List<int>(team.currentSeasonStats.finishes) : new List<int>()
            };
        }

        if (team.lifetimeStats != null)
        {
            teamSave.lifetimeStats = new SeasonStatsSaveData
            {
                finishes = team.lifetimeStats.finishes != null ? new List<int>(team.lifetimeStats.finishes) : new List<int>()
            };
        }

        // Save team members
        if (team.teamMembers != null)
        {
            teamSave.teamMembers = new TeamMemberSaveData[team.teamMembers.Count];
            for (int i = 0; i < team.teamMembers.Count; i++)
            {
                teamSave.teamMembers[i] = new TeamMemberSaveData(team.teamMembers[i]);
            }
        }

        return teamSave;
    }

    private void RestoreTeamMember(TeamMember member, TeamMemberSaveData saveData)
    {
        if (member == null || saveData == null) return;

        // Skip restoring if this is empty/default data (like uninitialized bench members)
        if (string.IsNullOrEmpty(saveData.memberName) && saveData.age == 0)
        {
            Debug.Log("Skipping restoration of empty TeamMember data");
            return;
        }

        member.memberName = saveData.memberName;
        member.age = saveData.age;

        if (!string.IsNullOrEmpty(saveData.attitude) && System.Enum.TryParse<Attitude>(saveData.attitude, out Attitude attitude))
        {
            member.attitude = attitude;
        }

        if (saveData.stats != null)
        {
            member.characterStats = saveData.stats.ToCharacterStats();
        }

        member.level = saveData.level;
        member.experience = saveData.experience;
        member.racesAvailableFor = saveData.racesAvailableFor;
        
        // Restore fitness status
        if (saveData.fitnessStatus != null)
        {
            member.fitness = new TeamMemberFitness
            {
                currentFitness = saveData.fitnessStatus.currentFitness,
                maxFitness = saveData.fitnessStatus.maxFitness,
                recoveryRate = saveData.fitnessStatus.recoveryRate,
                HungerLevel = saveData.fitnessStatus.HungerLevel,
                maxHungerLevel = saveData.fitnessStatus.maxHungerLevel,
                injuryStatus = saveData.fitnessStatus.injuryStatus,
                currentPhysicalState = saveData.fitnessStatus.currentPhysicalState
            };
        }
        
        // Restore happiness
        if (saveData.happiness != null)
        {
            member.happiness = new Happiness
            {
                currentHappiness = saveData.happiness.currentHappiness,
                maxHappiness = saveData.happiness.maxHappiness,
                currentMood = saveData.happiness.currentMood
            };
        }
    }

    private void RestoreLeaguesData(LeagueInfoSaveData[] leaguesData)
    {
        foreach (var leagueSave in leaguesData)
        {
            // Find the corresponding league
            var foundLeague = Array.Find(LeagueController.Instance.leagues,
                l => l.leagueName == leagueSave.leagueName);

            if (foundLeague != null)
            {
                // Restore league data
                foundLeague.isActive = leagueSave.isActive;
                foundLeague.playerHasJoined = leagueSave.playerHasJoined;
                foundLeague.currentRace = leagueSave.currentRace;
                foundLeague.currentSeason = leagueSave.currentSeason;
                foundLeague.isFinished = leagueSave.isFinished;
                foundLeague.maxRaceDays = leagueSave.maxRaceDays;
                foundLeague.leagueRaceEntryCost = leagueSave.leagueRaceEntryCost;
                foundLeague.isPromotionRelegation = leagueSave.isPromotionRelegation;
                foundLeague.numberOfTeamsToPromoteRelegate = leagueSave.numberOfTeamsToPromoteRelegate;
                foundLeague.maxNumberOfBoatsPerRace = leagueSave.maxNumberOfBoatsPerRace;
                foundLeague.repeatCount = leagueSave.repeatCount;
                foundLeague.maxExperienceGivenPerRace = leagueSave.maxExperienceGivenPerRace;
                foundLeague.currentRace = leagueSave.currentRace;

                // Restore tournament start date
                if (DateTime.TryParse(leagueSave.tournamentStartDate, out DateTime startDate))
                {
                    foundLeague.tournamentStartDate = startDate;
                }

                // Restore standings
                if (leagueSave.standings != null && foundLeague.standings != null)
                {
                    for (int i = 0; i < Mathf.Min(leagueSave.standings.Length, foundLeague.standings.Length); i++)
                    {
                        // Find the team reference by name
                        var team = Array.Find(foundLeague.teams, t => t.teamName == leagueSave.standings[i].teamName);
                        if (team != null)
                        {
                            foundLeague.standings[i] = new TeamStanding
                            {
                                team = team,
                                position = leagueSave.standings[i].position,
                                points = leagueSave.standings[i].points,
                                wins = leagueSave.standings[i].wins
                            };
                        }
                    }
                }

                //Restore Race Schedule
                if (leagueSave.raceSchedule != null && foundLeague.raceDays != null)
                {
                    for (int i = 0; i < Mathf.Min(leagueSave.raceSchedule.Length, foundLeague.raceDays.Length); i++)
                    {
                        foundLeague.raceDays[i].races = new List<Race>(leagueSave.raceSchedule[i].races);
                        foundLeague.raceDays[i].processed = leagueSave.raceSchedule[i].processed;
                    }

                }
            }
        }
    }

    private void RestoreTeamsData(TeamSaveData[] teamsData)
    {
        Debug.Log($"Starting RestoreTeamsData with {teamsData.Length} teams");
        
        foreach (var teamSave in teamsData)
        {
            // Find the corresponding team in the leagues
            Team foundTeam = null;
            foreach (var league in LeagueController.Instance.leagues)
            {
                foundTeam = Array.Find(league.teams, t => t.teamName == teamSave.teamName);
                if (foundTeam != null) break;
            }

            if (foundTeam != null)
            {
                Debug.Log($"Restoring data for team: {foundTeam.teamName}");
                
                // Restore team data
                foundTeam.teamDescription = teamSave.teamDescription;
                foundTeam.teamQuality = teamSave.teamQuality;
                foundTeam.teamExperience = teamSave.teamExperience;
                foundTeam.currentForm = teamSave.currentForm;
                foundTeam.recentResults = new List<int>(teamSave.recentResults);
                
                // Restore team manager
                if (teamSave.teamManager != null)
                {
                    Debug.Log($"Team {foundTeam.teamName} has manager data in save: {teamSave.teamManager.memberName}");
                    
                    if (foundTeam.teamManager == null)
                    {
                        // Create a new TeamMember instance for the manager if it doesn't exist
                        foundTeam.teamManager = ScriptableObject.CreateInstance<TeamMember>();
                        Debug.Log($"Created new teamManager instance for team {foundTeam.teamName}");
                    }
                    else
                    {
                        Debug.Log($"Team {foundTeam.teamName} already has a manager instance: {foundTeam.teamManager.memberName}");
                    }
                    
                    Debug.Log($"Restoring manager for team {foundTeam.teamName}: {teamSave.teamManager.memberName}");
                    RestoreTeamMember(foundTeam.teamManager, teamSave.teamManager);
                    Debug.Log($"Manager restored. Current name: {foundTeam.teamManager.memberName}, Age: {foundTeam.teamManager.age}");
                }
                else
                {
                    Debug.LogWarning($"Team {foundTeam.teamName} has NO manager data in save file");
                }
                
                // Restore bench members
                if (teamSave.bench != null && foundTeam.bench != null)
                {
                    for (int i = 0; i < Mathf.Min(teamSave.bench.Length, foundTeam.bench.Count); i++)
                    {
                        if (foundTeam.bench[i] == null)
                        {
                            foundTeam.bench[i] = ScriptableObject.CreateInstance<TeamMember>();
                        }
                        RestoreTeamMember(foundTeam.bench[i], teamSave.bench[i]);
                    }
                }

                // Restore team color
                if (teamSave.teamColor != null)
                {
                    foundTeam.teamColor = teamSave.teamColor.ToColor();
                }

                // Restore season stats
                if (teamSave.currentSeasonStats != null && foundTeam.currentSeasonStats != null)
                {
                    foundTeam.currentSeasonStats.finishes = new List<int>(teamSave.currentSeasonStats.finishes);
                }

                if (teamSave.lifetimeStats != null && foundTeam.lifetimeStats != null)
                {
                    foundTeam.lifetimeStats.finishes = new List<int>(teamSave.lifetimeStats.finishes);
                }

                // Restore team members
                if (teamSave.teamMembers != null && foundTeam.teamMembers != null)
                {
                    for (int i = 0; i < Mathf.Min(teamSave.teamMembers.Length, foundTeam.teamMembers.Count); i++)
                    {
                        RestoreTeamMember(foundTeam.teamMembers[i], teamSave.teamMembers[i]);
                    }
                }
                
                
            }
        }
    }
    
    public bool HasAnySaves()
    {
        for (int i = 0; i < maxSaveSlots; i++)
        {
            if (SaveSlotExists(i))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Start a new game with fresh data
    /// </summary>
    public bool NewGame(string saveName = "New Game")
    {
        try
        {
            // Set flag to false for fresh game
            _wasLoadedFromSave = false;
            _isNewGame = true;
            
            
            
            // Initialize fresh game state
            InitializeNewGameState();
            
            // Save to the first available slot
            int availableSlot = FindFirstAvailableSlot();
            if (availableSlot == -1)
            {
                // If no slots are available, use slot 0
                availableSlot = 0;
            }
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetTotalPlayTime();
            }
            
            // Create and save new game data
            SaveData newGameData = CreateFreshSaveData(saveName);
            ApplySaveData(newGameData);
            
            // Save the new game
            bool saveSuccess = SaveGame(availableSlot, saveName);
            
            if (saveSuccess)
            {
                Debug.Log($"New game started successfully in slot {availableSlot}");
                return true;
            }
            else
            {
                Debug.LogError("Failed to save new game data");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start new game: {e.Message}");
            return false;
        }
    }
    
    public bool NewGame(string saveName = "New Game", int preferredSlot = 0)
    {
        if (preferredSlot < 0)
            throw new ArgumentOutOfRangeException(nameof(preferredSlot));
        try
        {
            // Set flag to false for fresh game
            _wasLoadedFromSave = false;
            _isNewGame = true;
            
            // Initialize fresh game state
            InitializeNewGameState();
            
            // Save to the first available slot
            if (preferredSlot == 0)
            {
                int availableSlot = FindFirstAvailableSlot();
                if (availableSlot == -1)
                {
                    // If no slots are available, use slot 0
                    availableSlot = 0;
                }


                // Create and save new game data
                SaveData newGameData = CreateFreshSaveData(saveName);
                ApplySaveData(newGameData);

                // Save the new game
                bool saveSuccess = SaveGame(availableSlot, saveName);

                if (saveSuccess)
                {
                    Debug.Log($"New game started successfully in slot {availableSlot}");
                    return true;
                }
                else
                {
                    Debug.LogError("Failed to save new game data");
                    return false;
                }
            }
            else
            {
                if (preferredSlot >= maxSaveSlots)
                {
                    Debug.LogError($"Invalid preferred slot index: {preferredSlot}. Must be between 0 and {maxSaveSlots - 1}");
                    return false;
                }

                // Create and save new game data
                SaveData newGameData = CreateFreshSaveData(saveName);
                ApplySaveData(newGameData);

                // Save the new game
                bool saveSuccess = SaveGame(preferredSlot, saveName);

                if (saveSuccess)
                {
                    Debug.Log($"New game started successfully in preferred slot {preferredSlot}");
                    return true;
                }
                else
                {
                    Debug.LogError("Failed to save new game data");
                    return false;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start new game: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Find the first available save slot
    /// </summary>
    public int FindFirstAvailableSlot()
    {
        for (int i = 0; i < maxSaveSlots; i++)
        {
            if (!SaveSlotExists(i))
            {
                return i;
            }
        }
        return -1; // No available slots
    }
    
    /// <summary>
    /// Initialize fresh game state for all managers
    /// </summary>
    private void InitializeNewGameState()
    {
        // Reset PlayerManager to default values
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.energy = 100f;
            PlayerManager.Instance.coins = 50f;
            
        }
        
        // Reset GameManager to initial state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameStarted(true);
            GameManager.Instance.SetPlayerBusy(false);
        }
        
        // Reset RaceManager to default settings
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.difficulty = 1f;
            RaceManager.Instance.loadedRaceScene = false;
        }
        
        // Reset TimeManager to starting date
        if (TimeManager.Instance != null)
        {
            // Set to a default starting date - you may want to adjust this
            
            TimeManager.Instance.SetCurrentDate(TimeManager.Instance.StartDate);
            Debug.Log($"New game starting date: {TimeManager.Instance.StartDate}");
        }
        
        // Reset League data to initial state
        if (LeagueController.Instance != null)
        {
            // Reset all leagues to initial state
            foreach (var league in LeagueController.Instance.leagues)
            {
                if (league != null)
                {
                    league.playerHasJoined = false;
                    league.currentRace = 0;
                    league.currentSeason = 1;
                    league.isFinished = false;
                    league.isActive = true;
                    
                    // Reset all teams in the league
                    if (league.teams != null)
                    {
                        foreach (var team in league.teams)
                        {
                            if (team != null)
                            {
                                team.currentForm = 50f;
                                team.recentResults.Clear();
                                
                                // Reset season stats
                                if (team.currentSeasonStats != null)
                                {
                                    team.currentSeasonStats.finishes.Clear();
                                }
                                
                                // Reset team manager
                                if (team.teamManager != null)
                                {
                                    team.teamManager.ResetAllStats(team.teamQuality);
                                }
                                
                                // Reset team members
                                if (team.teamMembers != null)
                                {
                                    foreach (var member in team.teamMembers)
                                    {
                                        if (member != null)
                                        {
                                            member.ResetAllStats(team.teamQuality);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                    // Reset standings
                    if (league.standings != null)
                    {
                        for (int i = 0; i < league.standings.Length; i++)
                        {
                            league.standings[i] = new TeamStanding
                            {
                                team = league.standings[i].team,
                                position = i + 1,
                                points = 0,
                                wins = 0
                            };
                        }
                    }
                }
            }
            
            // Set the first league as current if available
            if (LeagueController.Instance.leagues != null && LeagueController.Instance.leagues.Length > 0)
            {
                LeagueController.Instance.currentLeague = LeagueController.Instance.leagues[0];
            }
        }
        
        // Clear bills for new game - they will be generated after first save/load
        if (BillsController.Instance != null)
        {
            BillsController.Instance.bills.Clear();
            BillsController.Instance.recurringPaidBills.Clear();
        }
    }
    
    /// <summary>
    /// Create fresh save data for a new game
    /// </summary>
    private SaveData CreateFreshSaveData(string saveName)
    {
        SaveData saveData = new SaveData();
        saveData.saveName = saveName;
        saveData.saveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        saveData.playTime = 0f;
        
        // Set fresh player data
        saveData.playerData.energy = 100f;
        saveData.playerData.coins = 50f;
        
        // Capture current team member data from PlayerManager (preserves attitudes and other ScriptableObject settings)
        if (PlayerManager.Instance != null && PlayerManager.Instance.team != null && PlayerManager.Instance.team.Count > 0)
        {
            saveData.playerData.teamMembers = new TeamMemberSaveData[PlayerManager.Instance.team.Count];
            for (int i = 0; i < PlayerManager.Instance.team.Count; i++)
            {
                if (PlayerManager.Instance.team[i] != null)
                {
                    saveData.playerData.teamMembers[i] = new TeamMemberSaveData(PlayerManager.Instance.team[i]);
                }
            }
        }
        
        // Set fresh game progress
        saveData.gameProgress.gameStarted = true;
        saveData.gameProgress.difficulty = 1f;
        saveData.gameProgress.loadedRaceScene = false;
        saveData.gameProgress.playerIsBusy = false;
        
        // Set fresh calendar data to game's starting date
        if (TimeManager.Instance != null)
        {
            saveData.calendarData.currentDate = TimeManager.Instance.StartDate.ToString("yyyy-MM-dd");
        }
        else
        {
            // Fallback to the default starting date if TimeManager is not available
            saveData.calendarData.currentDate = new DateTime(2008, 1, 1).ToString("yyyy-MM-dd");
        }

        //Set Fresh Bench Data - only team manager if available
        if (TeamManager.Instance != null)
        {
            if (TeamManager.Instance.benchTeamMembers.Count > 1)
            {
                if (TeamManager.Instance.teamManager != null)
                {
                    TeamManager.Instance.benchTeamMembers = new List<TeamMember> { TeamManager.Instance.teamManager };
                }
                else
                {
                    TeamManager.Instance.benchTeamMembers = new List<TeamMember>();
                }
            }
            
            TeamManager.Instance.ResetHireableRacersForHire();
        }
        
        //Clear list
        if(CompletedRacesManager.Instance != null)
        {
            CompletedRacesManager.Instance.ClearAllCompletedRaces();
        }
        
        // Ensure bill data is empty for new game
        saveData.billData = new BillSaveData
        {
            bills = new List<Bill>(),
            recurringPaidBills = new List<Bill>()
        };
        
        return saveData;
    }

  
}

[System.Serializable]
public class SaveSlotInfo
{
    public int slotIndex;
    public bool exists;
    public SaveData saveData;
}
