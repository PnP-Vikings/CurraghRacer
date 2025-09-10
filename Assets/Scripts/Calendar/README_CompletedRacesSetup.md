# Persistent Completed Races System - Setup Guide

## Overview
This system allows you to track completed races even after leagues are reset, with detailed tooltips on hover in the calendar UI.

## Components Created

### 1. CompletedRaceData (in CompletedRaces.cs)
- Stores all race details: league name, race name, date, position, time, points, participants
- Provides formatted display methods for position text and race time

### 2. CompletedRaces (in CompletedRaces.cs)
- MonoBehaviour that wraps CompletedRaceData
- Creates DayEventType for calendar integration
- Provides detailed tooltips with race information
- Color-codes events based on performance (gold for wins, silver for podium, blue for participation)

### 3. CompletedRacesManager (in CompletedRacesManager.cs)
- Singleton manager that handles persistence using JSON files
- Automatically saves/loads completed races on app start/pause/focus
- Integrates with your existing CalendarEvents ScriptableObject
- Provides statistics (total races, wins, podium finishes, etc.)

### 4. Enhanced CalendarEvents (updated CalendarEvents.cs)
- New methods to get detailed tooltips for dates
- Integration with persistent completed races
- Support for finding completed races by date

### 5. CalendarTooltip (new UI component)
- Handles hover tooltips on calendar dates
- Shows detailed race information when hovering over completed race dates
- Smooth fade in/out animations
- Smart positioning to stay within screen bounds

### 6. RaceCompletionTracker (helper script)
- Automatically captures race completions and adds them to the persistent system
- Includes test methods for adding sample data

## Setup Instructions

### Step 1: Create CompletedRacesManager GameObject
1. Create an empty GameObject in your scene
2. Name it "CompletedRacesManager"
3. Add the CompletedRacesManager component
4. Assign your CalendarEvents ScriptableObject to the "Calendar Events" field

### Step 2: Update Calendar UI for Tooltips
1. Find your calendar date buttons in the UI
2. Add the CalendarTooltip component to each calendar date button
3. Setup the tooltip UI:
   - Create a Panel for the tooltip (initially inactive)
   - Add a TextMeshProUGUI component for the tooltip text
   - Add a CanvasGroup for smooth fading
   - Assign these references in the CalendarTooltip component

### Step 3: Integrate with Race Completion
1. Add the RaceCompletionTracker to your race manager or league controller
2. Modify the OnRaceCompleted method to work with your existing race data structure
3. Call CompletedRacesManager.Instance.AddCompletedRace() when races finish

### Step 4: Test the System
1. Use the "Add Test Completed Race" context menu on RaceCompletionTracker
2. Check the calendar to see if completed races appear
3. Hover over dates with completed races to see detailed tooltips
4. Use "Clear All Completed Races" context menu on CompletedRacesManager to reset during testing

## Key Features

### Persistence
- Data is saved to JSON files in Application.persistentDataPath
- Survives league resets and app restarts
- Automatic save on app pause/focus loss

### Visual Feedback
- Gold color for race wins
- Silver color for podium finishes (top 3)
- Light blue for other participations
- Detailed tooltips with race statistics

### Statistics
- Track total races, wins, podium finishes
- Calculate win percentage and podium percentage
- Total points earned across all races

### Integration
- Seamlessly integrates with existing CalendarEvents system
- Maintains compatibility with current calendar UI
- No breaking changes to existing functionality

## Customization Options

### Colors
Modify colors in CompletedRaces component:
- winColor: Color for 1st place finishes
- podiumColor: Color for 2nd-3rd place
- participatedColor: Color for other finishes

### Tooltip Appearance
Modify CalendarTooltip component:
- showDelay: Time before tooltip appears
- fadeInDuration: Animation speed
- offset: Tooltip position relative to mouse

### Persistence
Toggle persistence on/off in CompletedRacesManager:
- enablePersistence: Enable/disable JSON saving
- saveFileName: Custom filename for save data

## Integration with Your Existing Race System

To fully integrate this with your existing race completion system, you'll need to:

1. Find where races finish in your code
2. Extract the race data (position, time, participants, etc.)
3. Call CompletedRacesManager.Instance.AddCompletedRace() with that data

Example integration:
```csharp
// When a race finishes
public void OnRaceFinished(RaceResult result)
{
    if (CompletedRacesManager.Instance != null)
    {
        CompletedRacesManager.Instance.AddCompletedRace(
            currentLeague.leagueName,
            result.raceName,
            currentDate,
            result.playerPosition,
            result.totalParticipants,
            result.trackName,
            result.playerTime,
            result.pointsEarned,
            result.participantNames
        );
    }
}
```

## File Locations
- Save data: Application.persistentDataPath/completed_races.json
- Scripts: Assets/Scripts/Calendar/
- All components use the Calendar namespace for organization

The system is now ready to use and will automatically track all completed races, displaying them in your calendar UI with rich tooltips on hover!
