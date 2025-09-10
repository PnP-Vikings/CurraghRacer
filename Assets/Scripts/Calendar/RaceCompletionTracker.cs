using UnityEngine;
using System;
using System.Linq;
using League;
using Calendar;

/// <summary>
/// This script automatically tracks race completions and adds them to the persistent completed races system.
/// Attach this to your race manager or league controller to automatically capture race results.
/// </summary>
public class RaceCompletionTracker : MonoBehaviour
{
    [Header("Auto-Tracking Settings")]
    public bool enableAutoTracking = true;
    public bool debugLogging = true;
    
    private void Start()
    {
        // Subscribe to race completion events if your system has them
        // You may need to modify this based on your existing event system
        if (enableAutoTracking)
        {
            SubscribeToRaceEvents();
        }
    }
    
    private void SubscribeToRaceEvents()
    {
        // This is where you would subscribe to your existing race completion events
        // Example (modify based on your actual event system):
        // RaceManager.OnRaceCompleted += OnRaceCompleted;
        // LeagueController.OnRaceFinished += OnRaceFinished;
        
        if (debugLogging)
            Debug.Log("RaceCompletionTracker: Subscribed to race completion events");
    }
    
    /// <summary>
    /// Call this method when a race is completed to automatically add it to the persistent system
    /// </summary>
    public void OnRaceCompleted(RaceData raceData, int playerPosition, float playerTime)
    {
        if (!enableAutoTracking || CompletedRacesManager.Instance == null)
            return;
            
        try
        {
            // Extract race information
            string leagueName = GetCurrentLeagueName();
            string raceName = GetRaceName(raceData);
            DateTime raceDate = GetRaceDate();
            string trackName = GetTrackName(raceData);
            int totalParticipants = GetTotalParticipants(raceData);
            int pointsEarned = CalculatePointsEarned(playerPosition, totalParticipants);
            string[] participantNames = GetParticipantNames(raceData);
            
            // Add to persistent system
            CompletedRacesManager.Instance.AddCompletedRace(
                leagueName, raceName, raceDate, playerPosition, 
                totalParticipants, trackName, playerTime, 
                pointsEarned, participantNames
            );
            
            if (debugLogging)
                Debug.Log($"RaceCompletionTracker: Added race {raceName} to persistent system (Position: {playerPosition})");
        }
        catch (Exception e)
        {
            Debug.LogError($"RaceCompletionTracker: Failed to track completed race - {e.Message}");
        }
    }
    
    /// <summary>
    /// Manual method to add a completed race (useful for testing or manual entry)
    /// </summary>
    [ContextMenu("Add Test Completed Race")]
    public void AddTestCompletedRace()
    {
        if (CompletedRacesManager.Instance == null)
        {
            Debug.LogWarning("CompletedRacesManager not found!");
            return;
        }
        
        // Create test data
        string[] testParticipants = { "Player", "AI Driver 1", "AI Driver 2", "AI Driver 3" };
        
        CompletedRacesManager.Instance.AddCompletedRace(
            "Test League",
            "Test Race",
            DateTime.Now.AddDays(-1),
            UnityEngine.Random.Range(1, 5),
            4,
            "Test Track",
            120.5f,
            UnityEngine.Random.Range(10, 25),
            testParticipants
        );
        
        Debug.Log("Added test completed race");
    }
    
    private string GetCurrentLeagueName()
    {
        if (LeagueController.Instance != null && LeagueController.Instance.currentLeague != null)
            return LeagueController.Instance.currentLeague.leagueName;
        return "Unknown League";
    }
    
    private string GetRaceName(RaceData raceData)
    {
        // Modify this based on your RaceData structure
        // This is a placeholder - adapt to your actual race data structure
        return raceData != null ? $"Race {DateTime.Now.DayOfYear}" : "Unknown Race";
    }
    
    private DateTime GetRaceDate()
    {
        // You might want to get this from your time manager or race data
        if (TimeManager.Instance != null)
        {
            // Assuming you have a current date property in TimeManager
            // return TimeManager.Instance.currentDate;
        }
        return DateTime.Now;
    }
    
    private string GetTrackName(RaceData raceData)
    {
        // Modify this based on your RaceData structure
        return raceData != null ? "Track Name" : "Unknown Track";
    }
    
    private int GetTotalParticipants(RaceData raceData)
    {
        // Modify this based on your RaceData structure
        // Example: return raceData.participants.Length;
        return 8; // Placeholder
    }
    
    private int CalculatePointsEarned(int position, int totalParticipants)
    {
        // Standard F1-style point system (modify as needed)
        switch (position)
        {
            case 1: return 25;
            case 2: return 18;
            case 3: return 15;
            case 4: return 12;
            case 5: return 10;
            case 6: return 8;
            case 7: return 6;
            case 8: return 4;
            case 9: return 2;
            case 10: return 1;
            default: return 0;
        }
    }
    
    private string[] GetParticipantNames(RaceData raceData)
    {
        // Modify this based on your RaceData structure
        // Example: return raceData.participants.Select(p => p.name).ToArray();
        return new string[] { "Player", "AI Driver 1", "AI Driver 2", "AI Driver 3" }; // Placeholder
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        // Example:
        // RaceManager.OnRaceCompleted -= OnRaceCompleted;
        // LeagueController.OnRaceFinished -= OnRaceFinished;
    }
}

/// <summary>
/// Placeholder for your actual RaceData structure
/// Replace this with your existing race data class
/// </summary>
public class RaceData
{
    // Add your actual race data fields here
    public string raceName;
    public string trackName;
    // etc.
}
