using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;
using Calendar;

namespace Calendar
{
    [Serializable]
    public class PersistentCompletedRacesData
    {
        public List<CompletedRaceData> completedRaces = new List<CompletedRaceData>();
    }

    public class CompletedRacesManager : MonoBehaviour
    {
        [Header("Configuration")]
        public CalendarEvents calendarEvents;
        
        [Header("Persistence Settings")]
        public bool enablePersistence = true;
        public string saveFileName = "completed_races.json";
        
        [Header("Runtime Data")]
        public List<CompletedRaces> completedRaceObjects = new List<CompletedRaces>();
        
        private PersistentCompletedRacesData persistentData;
        private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);
        
        public static CompletedRacesManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeManager()
        {
            persistentData = new PersistentCompletedRacesData();
            LoadCompletedRaces();
            CreateCompletedRaceObjects();
            IntegrateWithCalendar();
        }
        
        /// <summary>
        /// Add a new completed race to the persistent collection
        /// </summary>
        public void AddCompletedRace(string leagueName, string raceName, DateTime raceDate, 
                                    int playerPosition, int totalParticipants, string trackName, 
                                    float raceTime, int pointsEarned, string[] participantNames)
        {
            var raceData = new CompletedRaceData(leagueName, raceName, raceDate, playerPosition, 
                                               totalParticipants, trackName, raceTime, pointsEarned, participantNames);
            
            // Check if this race already exists (prevent duplicates)
            bool alreadyExists = persistentData.completedRaces.Exists(r => 
                r.leagueName == leagueName && 
                r.raceName == raceName && 
                r.raceDate.Date == raceDate.Date);
                
            if (!alreadyExists)
            {
                persistentData.completedRaces.Add(raceData);
                CreateCompletedRaceObject(raceData);
                SaveCompletedRaces();
                IntegrateWithCalendar();
                
                Debug.Log($"Added completed race: {leagueName} - {raceName} ({raceData.GetPositionText()})");
            }
        }
        
        /// <summary>
        /// Remove a completed race (useful for testing or corrections)
        /// </summary>
        public void RemoveCompletedRace(string leagueName, string raceName, DateTime raceDate)
        {
            var raceToRemove = persistentData.completedRaces.Find(r => 
                r.leagueName == leagueName && 
                r.raceName == raceName && 
                r.raceDate.Date == raceDate.Date);
                
            if (raceToRemove != null)
            {
                persistentData.completedRaces.Remove(raceToRemove);
                
                // Remove corresponding completed race object
                var objToRemove = completedRaceObjects.Find(obj => 
                    obj.raceData != null && 
                    obj.raceData.leagueName == leagueName && 
                    obj.raceData.raceName == raceName && 
                    obj.raceData.raceDate.Date == raceDate.Date);
                    
                if (objToRemove != null)
                {
                    completedRaceObjects.Remove(objToRemove);
                    if (calendarEvents != null)
                    {
                        calendarEvents.completedRaces.Remove(objToRemove);
                    }
                    Destroy(objToRemove.gameObject);
                }
                
                SaveCompletedRaces();
                Debug.Log($"Removed completed race: {leagueName} - {raceName}");
            }
        }
        
        /// <summary>
        /// Get all completed races for a specific league
        /// </summary>
        public List<CompletedRaceData> GetCompletedRacesForLeague(string leagueName)
        {
            return persistentData.completedRaces.FindAll(r => r.leagueName == leagueName);
        }
        
        /// <summary>
        /// Get completed races within a date range
        /// </summary>
        public List<CompletedRaceData> GetCompletedRacesInDateRange(DateTime startDate, DateTime endDate)
        {
            return persistentData.completedRaces.FindAll(r => 
                r.raceDate.Date >= startDate.Date && r.raceDate.Date <= endDate.Date);
        }
        
        /// <summary>
        /// Get race statistics
        /// </summary>
        public RaceStatistics GetRaceStatistics()
        {
            var stats = new RaceStatistics();
            stats.totalRaces = persistentData.completedRaces.Count;
            stats.wins = persistentData.completedRaces.FindAll(r => r.playerWon).Count;
            stats.podiumFinishes = persistentData.completedRaces.FindAll(r => r.playerPosition <= 3).Count;
            stats.totalPointsEarned = 0;
            
            foreach (var race in persistentData.completedRaces)
            {
                stats.totalPointsEarned += race.pointsEarned;
            }
            
            if (stats.totalRaces > 0)
            {
                stats.winPercentage = (float)stats.wins / stats.totalRaces * 100f;
                stats.podiumPercentage = (float)stats.podiumFinishes / stats.totalRaces * 100f;
            }
            
            return stats;
        }
        
        /// <summary>
        /// Create completed race objects from persistent data
        /// </summary>
        private void CreateCompletedRaceObjects()
        {
            // Clear existing objects
            foreach (var obj in completedRaceObjects)
            {
                if (obj != null) Destroy(obj.gameObject);
            }
            completedRaceObjects.Clear();
            
            // Create new objects from persistent data
            foreach (var raceData in persistentData.completedRaces)
            {
                CreateCompletedRaceObject(raceData);
            }
        }
        
        /// <summary>
        /// Create a single completed race object
        /// </summary>
        private void CreateCompletedRaceObject(CompletedRaceData raceData)
        {
            var raceObj = new GameObject($"CompletedRace_{raceData.leagueName}_{raceData.raceName}");
            raceObj.transform.SetParent(transform);
            
            var completedRace = raceObj.AddComponent<CompletedRaces>();
            completedRace.Initialize(raceData);
            
            completedRaceObjects.Add(completedRace);
        }
        
        /// <summary>
        /// Integrate completed races with the calendar system
        /// </summary>
        private void IntegrateWithCalendar()
        {
            if (calendarEvents == null) return;
            
            // Clear existing completed races from calendar
            calendarEvents.completedRaces.Clear();
            
            // Add all current completed race objects to calendar
            foreach (var completedRace in completedRaceObjects)
            {
                if (completedRace != null)
                {
                    calendarEvents.completedRaces.Add(completedRace);
                }
            }
            
            Debug.Log($"Integrated {completedRaceObjects.Count} completed races with calendar system");
        }
        
        /// <summary>
        /// Save completed races to persistent storage
        /// </summary>
        private void SaveCompletedRaces()
        {
            if (!enablePersistence) return;
            
            try
            {
                string json = JsonUtility.ToJson(persistentData, true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"Saved {persistentData.completedRaces.Count} completed races to {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save completed races: {e.Message}");
            }
        }
        
        /// <summary>
        /// Load completed races from persistent storage
        /// </summary>
        private void LoadCompletedRaces()
        {
            if (!enablePersistence || !File.Exists(SavePath))
            {
                persistentData = new PersistentCompletedRacesData();
                return;
            }
            
            try
            {
                string json = File.ReadAllText(SavePath);
                persistentData = JsonUtility.FromJson<PersistentCompletedRacesData>(json);
                Debug.Log($"Loaded {persistentData.completedRaces.Count} completed races from {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load completed races: {e.Message}");
                persistentData = new PersistentCompletedRacesData();
            }
        }
        
        /// <summary>
        /// Clear all completed races (useful for testing)
        /// </summary>
        [ContextMenu("Clear All Completed Races")]
        public void ClearAllCompletedRaces()
        {
            persistentData.completedRaces.Clear();
            
            foreach (var obj in completedRaceObjects)
            {
                if (obj != null) Destroy(obj.gameObject);
            }
            completedRaceObjects.Clear();
            
            if (calendarEvents != null)
            {
                calendarEvents.completedRaces.Clear();
            }
            
            SaveCompletedRaces();
            Debug.Log("Cleared all completed races");
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveCompletedRaces();
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) SaveCompletedRaces();
        }
    }

    [Serializable]
    public class RaceStatistics
    {
        public int totalRaces;
        public int wins;
        public int podiumFinishes;
        public int totalPointsEarned;
        public float winPercentage;
        public float podiumPercentage;
    }
}
