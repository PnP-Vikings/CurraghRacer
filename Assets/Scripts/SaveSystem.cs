using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using League;

[System.Serializable]
public class SaveData
{
    [Header("Save Metadata")]
    public string saveName = "Save Slot";
    public string saveDate;
    public float playTime;
    public int saveVersion = 1;

    [Header("Player Data")]
    public PlayerSaveData playerData;
    
    [Header("League Data")]
    public LeagueSaveData leagueData;
    
    [Header("Game Progress")]
    public GameProgressData gameProgress;
    
    [Header("Calendar Data")]
    public CalendarSaveData calendarData;

    public SaveData()
    {
        saveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        playerData = new PlayerSaveData();
        leagueData = new LeagueSaveData();
        gameProgress = new GameProgressData();
        calendarData = new CalendarSaveData();
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
public class TeamMemberSaveData
{
    public string memberName;
    public int age;
    public string attitude;
    public CharacterStatsSaveData stats;
    public int level;
    public int experience;
    
    public TeamMemberSaveData() { }
    
    public TeamMemberSaveData(TeamMember member)
    {
        memberName = member.memberName;
        age = member.age;
        attitude = member.attitude.ToString();
        stats = new CharacterStatsSaveData(member.characterStats);
        level = member.level;
        experience = member.experience;
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
    public RaceSaveData[] raceSchedule;
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
    public TeamMemberSaveData[] teamMembers;
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

    /// <summary>
    /// Indicates whether the current game session was loaded from a save file
    /// </summary>
    public bool WasLoadedFromSave => _wasLoadedFromSave;

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

            // Set the flag before applying save data
            _wasLoadedFromSave = true;

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
                saveData.playerData.teamMembers = new TeamMemberSaveData[PlayerManager.Instance.team.Length];
                for (int i = 0; i < PlayerManager.Instance.team.Length; i++)
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
                        description = league.description,
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
                for (int i = 0; i < Mathf.Min(saveData.playerData.teamMembers.Length, PlayerManager.Instance.team.Length); i++)
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
                    // Set the time manager to the saved date
                    // Note: You may need to add a SetCurrentDate method to TimeManager
                    Debug.Log($"Restored game date: {savedDate}");
                }
            }
        }

        // Update UI if needed
        if (PlayerManager.Instance?.playerStatsView != null)
        {
            PlayerManager.Instance.playerStatsView.UpdatePlayerStats();
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
            teamSave.teamMembers = new TeamMemberSaveData[team.teamMembers.Length];
            for (int i = 0; i < team.teamMembers.Length; i++)
            {
                teamSave.teamMembers[i] = new TeamMemberSaveData(team.teamMembers[i]);
            }
        }

        return teamSave;
    }

    private void RestoreTeamMember(TeamMember member, TeamMemberSaveData saveData)
    {
        if (member == null || saveData == null) return;

        member.memberName = saveData.memberName;
        member.age = saveData.age;

        if (System.Enum.TryParse<Attitude>(saveData.attitude, out Attitude attitude))
        {
            member.attitude = attitude;
        }

        if (saveData.stats != null)
        {
            member.characterStats = saveData.stats.ToCharacterStats();
        }

        member.level = saveData.level;
        member.experience = saveData.experience;
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
                foundLeague.description = leagueSave.description;
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
            }
        }
    }

    private void RestoreTeamsData(TeamSaveData[] teamsData)
    {
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
                // Restore team data
                foundTeam.teamDescription = teamSave.teamDescription;
                foundTeam.teamQuality = teamSave.teamQuality;
                foundTeam.teamExperience = teamSave.teamExperience;
                foundTeam.currentForm = teamSave.currentForm;
                foundTeam.recentResults = new List<int>(teamSave.recentResults);

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
                    for (int i = 0; i < Mathf.Min(teamSave.teamMembers.Length, foundTeam.teamMembers.Length); i++)
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
            
            // Initialize fresh game state
            InitializeNewGameState();
            
            // Save to the first available slot
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
            DateTime startDate = new DateTime(2024, 1, 1);
            // Note: You may need to add a SetCurrentDate method to TimeManager
            Debug.Log($"New game starting date: {startDate}");
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
        
        // Set fresh game progress
        saveData.gameProgress.gameStarted = true;
        saveData.gameProgress.difficulty = 1f;
        saveData.gameProgress.loadedRaceScene = false;
        saveData.gameProgress.playerIsBusy = false;
        
        // Set fresh calendar data
        saveData.calendarData.currentDate = DateTime.Now.ToString("yyyy-MM-dd");
        
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
