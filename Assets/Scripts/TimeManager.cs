using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using System;
using System.Collections.Generic;
using Calendar;
using UnityEngine.Localization;

public class TimeManager : MonoBehaviour
{
    // Singleton instance
    private static TimeManager _instance;
    
    public int startYear = 2008;
    [Tooltip("1-12 (January = 1)")] public int startMonth = 1;
    [Tooltip("1-based day of month")] public int startDay = 5;

    private void OnValidate()
    {
        if (startMonth < 1) startMonth = 1;
        if (startMonth > 12) startMonth = 12;
        int maxDay = DateTime.DaysInMonth(Mathf.Clamp(startYear, 1, 9999), startMonth);
        if (startDay < 1) startDay = 1;
        if (startDay > maxDay) startDay = maxDay;
    }
    
    public DateTime StartDate = new DateTime(2008, 1, 5);
    public static TimeManager Instance { get { return _instance; } }
    string[] daysOfWeek = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
    internal string[] monthNames = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
    internal int[] daysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
    public CalendarEvents calendarEvents;
    
    
    
    //Real-time duration of an in-game day
    [SerializeField] private bool useRealTimeDayDuration = true;
    [SerializeField] private float dayDurationInMinutes = 15F; // Real-time minutes for a full in-game day

    [SerializeField] internal UnityEvent timeChangedEvent;

    // Add this variable to the TimeManager class
    public UnityEvent onNewDay;
   public UnityEvent onNightStart;
  

    // Private constructor to enforce singleton pattern
    private TimeManager() { }

    // Time variables
    [SerializeField, Range(0, 24)] private float timeOfDay;
    [SerializeField, Range(0f, 1f)] private float timeMultiplier = 1f;
    private int daysPassed = 0;
    private bool newItemSpawned = false;
    [SerializeField] private bool isTimePaused = false;
    
    
    // Calendar variables
    [SerializeField] private int currentDay = 1;
    [SerializeField] private int currentMonth = 0; // 0-based index (0 = January)
    [SerializeField] private int currentYear = 2008; // Starting year
    [SerializeField] private int currentDayOfWeek = 2; // 0-based index (0 = Sunday)
    [SerializeField] internal int daysInCurrentMonth = 31; // Default to 31 days for January
    
    [Serializable]
    public class DateChangedEvent : UnityEvent<int, int, int> { } // day, month, year
    
 
    
    public DateChangedEvent onDateChanged;
    
    public class TodaysEvents : UnityEvent<List<DayEventType>> { } // List of events for today
    [SerializeField] public TodaysEvents todaysEvents = new TodaysEvents();
    
    // Properties
    public float TimeOfDay { get => timeOfDay; }
    public float TimeMultiplier { get => timeMultiplier; set => timeMultiplier = Mathf.Max(value, 0f); }
    public int DaysPassed { get => daysPassed; }
    
    
    
    [Header("Localization")]
    internal LocalizedString[] _localizedDays = {new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Sunday" },
        new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Monday" },
        new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Tuesday" },
        new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Wednesday" },
        new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Thursday" },
        new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Friday" },
        new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Saturday" }};
    
    internal LocalizedString[] _localizedMonth = {new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Month.January" },
        new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Month.February" },
        new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Month.March" },
        new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Month.April" },
        new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Month.May" },
        new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Month.June" },
        new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Month.July" },
        new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Month.August" },
        new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Month.September" },
        new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Month.October" },
        new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Month.November" },
        new LocalizedString { TableReference = "TimeManager", TableEntryReference = "TimeManager.Month.December" }};
    // Initialize singleton instance
    private void Awake()
    {
        // If an instance already exists, destroy this object
        if (Instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        // Initialize starting date
        StartDate = new DateTime(startYear, startMonth, startDay);
        
        timeOfDay = 6f; // Start at 6 AM
        daysPassed = 0;
        
        // Calculate the correct day of the week for the starting date (January 1st, 2008 = Tuesday)
        int calculatedDayOfWeek = GetDayOfWeekIndex(currentDay, currentMonth, currentYear);
     
        currentDayOfWeek = calculatedDayOfWeek;
        
        daysInCurrentMonth = GetDaysInCurrentMonth();
        HasEventToday();
        
        onNewDay.Invoke(); // Raise the OnNewDay event
        timeChangedEvent.Invoke(); // Raise the time changed event
        
        if(RaceManager.Instance != null)
            todaysEvents.AddListener(RaceManager.Instance.CheckForRaceDay);
        
        if(BillsController.Instance != null)
        {
            onNewDay.AddListener(BillsController.Instance.HandleNewDay);
        }
    }
    
    public void RecheckIfRaceDay()
    {
        if(RaceManager.Instance != null)
            RaceManager.Instance.CheckForRaceDay(GetEventsToday());
    }
    
    public void AdvanceTimeByHours(float hours)
    {
        if (hours < 0) throw new ArgumentException("Hours to advance must be non-negative");
        
        float previousTimeOfDay = timeOfDay;
        timeOfDay += hours * timeMultiplier;
        timeOfDay %= 24f; // Clamp to 0-24

        // Check if a new day has started
        if (previousTimeOfDay > timeOfDay)
        {
            daysPassed++;
            newItemSpawned = false;
          //  onNewDay.Invoke(); // Raise the OnNewDay event
        }

        // Call SpawnItems method at the beginning of a new day
        if (!newItemSpawned && timeOfDay >= 0 && timeOfDay <= 1)
        {
            Debug.Log("New day has started");
           // onNewDay.Invoke(); // Raise the OnNewDay event
            newItemSpawned = true;
        }

        if (IsNight())
        {
            onNightStart.Invoke(); // Raise the OnNightStart event
        }
        
        timeChangedEvent.Invoke();
    }
    
    // Update the time of day based on the time multiplier
    public void SleepTime()
    {
        // Reset time of day to 0 (start of a new day)
        timeOfDay = 6f;
        daysPassed++;
        newItemSpawned = false;
        
        // Advance the calendar by one day
        AdvanceCalendar(1);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerAutoSave();
        }
        onNewDay.Invoke(); // Raise the OnNewDay event
        timeChangedEvent.Invoke();
    }
    
    // Calendar-related methods
    private void AdvanceCalendar(int daysToAdvance)
    {
        for (int i = 0; i < daysToAdvance; i++)
        {
            // Advance day of week (cycle through 0-6)
            currentDayOfWeek = (currentDayOfWeek + 1) % 7;
            
            // Advance day
            currentDay++;
            
            // Check if we need to move to the next month
            if (currentDay > GetDaysInCurrentMonth())
            {
                currentDay = 1;
                currentMonth++;
                daysInCurrentMonth = GetDaysInCurrentMonth(); // Update days in current month
                // Check if we need to move to the next year
                if (currentMonth >= 12)
                {
                    currentMonth = 0;
                    currentYear++;
                }
            }
        }

        HasEventToday();
        // Trigger the date changed event
        if (onDateChanged != null)
            onDateChanged.Invoke(currentDay, currentMonth, currentYear);
            
        // Check if the new date is a race day
        RecheckIfRaceDay();
    }
    
    private int GetDaysInCurrentMonth()
    {
        // Handle leap years for February
        if (currentMonth == 1 && IsLeapYear(currentYear))
            return 29;
            
        return daysInMonth[currentMonth];
    }
    
    public string GetDayOfWeek(int day, int month, int year)
    {
        // For Zeller's Congruence, January and February are counted as months 13 and 14 of the previous year
        int adjustedMonth = month + 1; // Convert from 0-based to 1-based
        int adjustedYear = year;
        
        if (adjustedMonth < 3)
        {
            adjustedMonth += 12;
            adjustedYear--;
        }
        
        // Zeller's Congruence formula
        int q = day;
        int m = adjustedMonth;
        int k = adjustedYear % 100;
        int j = adjustedYear / 100;
        
        int h = (q + (13 * (m + 1)) / 5 + k + k / 4 + j / 4 - 2 * j) % 7;
        
        // Convert Zeller's result (Saturday=0) to our format (Sunday=0)
        // Zeller: Sat=0, Sun=1, Mon=2, Tue=3, Wed=4, Thu=5, Fri=6
        // Ours:   Sun=0, Mon=1, Tue=2, Wed=3, Thu=4, Fri=5, Sat=6
        int dayOfWeekIndex = (h + 6) % 7;
        
        bool isLocalizationAvailable = _localizedDays != null && _localizedDays.Length == 7;
        foreach (var localizedDay in _localizedDays)
        {
          if (localizedDay == null || localizedDay.IsEmpty)
          {
              isLocalizationAvailable = false;
              break;
          }
        }
        
        
        if (isLocalizationAvailable)
        {
            return _localizedDays[dayOfWeekIndex].GetLocalizedString();
        }
        else
        {
            return daysOfWeek[dayOfWeekIndex];
        }
    }
    
    public string GetDayOfWeekString(int dayOfWeekIndex)
    {
        if (dayOfWeekIndex < 0 || dayOfWeekIndex > 6)
        {
            Debug.LogWarning($"Invalid day of week index: {dayOfWeekIndex}. Returning empty string.");
            return string.Empty;
        }
        
        bool isLocalizationAvailable = _localizedDays != null && _localizedDays.Length == 7;
        foreach (var localizedDay in _localizedDays)
        {
            if (localizedDay == null || localizedDay.IsEmpty)
            {
                isLocalizationAvailable = false;
                break;
            }
        }
        
        if (isLocalizationAvailable)
        {
            return _localizedDays[dayOfWeekIndex].GetLocalizedString();
        }
        else
        {
            return daysOfWeek[dayOfWeekIndex];
        }
    }
    
    public string GetMonthName(int month)
    {
        if (month < 0 || month > 11)
        {
            Debug.LogWarning($"Invalid month index: {month}. Returning empty string.");
            return string.Empty;
        }
        
        bool isLocalizationAvailable = _localizedMonth != null && _localizedMonth.Length == 12;
        foreach (var localizedMonth in _localizedMonth)
        {
            if (localizedMonth == null || localizedMonth.IsEmpty)
            {
                isLocalizationAvailable = false;
                break;
            }
        }
        
        if (isLocalizationAvailable)
        {
            return _localizedMonth[month].GetLocalizedString();
        }
        else
        {
            Debug.LogWarning("Month names are not properly initialized. Returning empty string.");
            return string.Empty;
        }
    }
    
    public int GetDayOfWeekIndex(int day, int month, int year)
    {
        // For Zeller's Congruence, January and February are counted as months 13 and 14 of the previous year
        int adjustedMonth = month + 1; // Convert from 0-based to 1-based
        int adjustedYear = year;
        
        if (adjustedMonth < 3)
        {
            adjustedMonth += 12;
            adjustedYear--;
        }
        
        // Zeller's Congruence formula
        int q = day;
        int m = adjustedMonth;
        int k = adjustedYear % 100;
        int j = adjustedYear / 100;
        
        int h = (q + (13 * (m + 1)) / 5 + k + k / 4 + j / 4 - 2 * j) % 7;
        
        // Convert Zeller's result (Saturday=0) to our format (Sunday=0)
        // Zeller: Sat=0, Sun=1, Mon=2, Tue=3, Wed=4, Thu=5, Fri=6
        // Ours:   Sun=0, Mon=1, Tue=2, Wed=3, Thu=4, Fri=5, Sat=6
        int dayOfWeek = (h + 6) % 7;
        
       
        return dayOfWeek;
    }
    
    internal int GetDaysInMonth(int month, int year)
    {
        // Handle leap years for February
        if (month == 1 && IsLeapYear(year))
            return 29;
        
        return daysInMonth[month];
    }
    
    private bool IsLeapYear(int year)
    {
        // Leap year calculation
        return (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);
    }
    
    // Utility methods for date formatting
    public string GetCurrentDateFormatted()
    {
        return string.Format("{0} {1}, {2}", monthNames[currentMonth], currentDay, currentYear);
    }
    
    public string GetCurrentDayOfWeekString()
    {
        return GetDayOfWeek(currentDay, currentMonth, currentYear);
    }
    
    public string GetFullDateFormatted()
    {
        return string.Format("{0}, {1} {2}, {3}", 
            daysOfWeek[currentDayOfWeek], 
            monthNames[currentMonth], 
            currentDay, 
            currentYear);
    }
    
    // Getters for calendar variables
    public int GetCurrentDay() { return currentDay; }
    public int GetCurrentMonth() { return currentMonth; }
    public int GetCurrentYear() { return currentYear; }
    
 
    public int GetCurrentDayOfWeek() { return currentDayOfWeek; }

    /// <summary>
    /// Gets the current date as a System.DateTime
    /// </summary>
    public DateTime GetCurrentDate()
    {
        // Validate parameters before creating DateTime
        if (currentYear < 1 || currentYear > 9999)
        {
            Debug.LogError($"Invalid year: {currentYear}. Using default year 2008.");
            currentYear = 2008;
        }
        
        if (currentMonth < 0 || currentMonth > 11)
        {
            Debug.LogError($"Invalid month: {currentMonth}. Using default month 0 (January).");
            currentMonth = 0;
        }
        
        int daysInThisMonth = GetDaysInCurrentMonth();
        if (currentDay < 1 || currentDay > daysInThisMonth)
        {
            Debug.LogError($"Invalid day: {currentDay} for month {currentMonth + 1}/{currentYear}. Days in month: {daysInThisMonth}. Using day 1.");
            currentDay = 1;
        }
        
        // currentMonth is 0-based (0 = January), DateTime expects 1-12
        return new DateTime(currentYear, currentMonth + 1, currentDay);
    }
    
    public void Update()
    {
        if (GameManager.Instance.GameStarted && useRealTimeDayDuration && !isTimePaused)
        {
            UpdateTimeRealTime(Time.deltaTime);
        }
    }

    public void UpdateTimeRealTime(float deltaTime)
    {
        // Stop clock at 23:30 (23.5 hours)
        if(timeOfDay >= 23.99f) return;
    
        float previousTimeOfDay = timeOfDay;
    
        // Calculate time multiplier: 15 minutes real time = 24 hours game time
        // 15 minutes = 900 seconds
        // 24 hours = 86400 seconds in-game
        // Multiplier = 86400 / 900 = 96
        float calculatedMultiplier = (24f * 60f * 60f) / (dayDurationInMinutes * 60f);
    
        timeOfDay += (deltaTime / 3600f) * calculatedMultiplier;
    
        // Clamp to prevent going past 23:30
        timeOfDay = Mathf.Min(timeOfDay, 23.99f);

        // Call SpawnItems method at the beginning of a new day
        if (!newItemSpawned && timeOfDay >= 0 && timeOfDay <= 0.1f)
        {
            Debug.Log("New day has started");
            onNewDay.Invoke();
            newItemSpawned = true;
        }

        if (IsNight())
        {
            onNightStart.Invoke();
        }
    
        timeChangedEvent.Invoke();
    }

    

    public void UpdateTime()
    {
        float previousTimeOfDay = timeOfDay;
        /*timeOfDay += Time.deltaTime * timeMultiplier;*/
        if (!useRealTimeDayDuration)
        {
            if (timeOfDay < 20f)
            {
                timeOfDay += 3f;
            }
            else
            {
                timeOfDay += 1f;
            }
            
            if (timeOfDay > 23f)
            {
                timeOfDay = 23.99f; // Stop at 23:59 to avoid skipping night events
            }
        }

        timeOfDay %= 24f; // Clamp to 0-24

        // Check if a new day has started
        if (previousTimeOfDay > timeOfDay)
        {
            daysPassed++;
            newItemSpawned = false;
        }

        // Call SpawnItems method at the beginning of a new day
        if (!newItemSpawned && timeOfDay >= 0 && timeOfDay <= 1)
        {
            Debug.Log("New day has started");
            onNewDay.Invoke(); // Raise the OnNewDay event
            newItemSpawned = true;
        }

        if (IsNight())
        {
            onNightStart.Invoke(); // Raise the OnNightStart event
        }
        
        timeChangedEvent.Invoke();
    }

    public bool IsNight()
    {
        return timeOfDay >= 19 && timeOfDay <= 24;
    }
    
    public bool IsTooLateForActivities()
    {
        return timeOfDay >= 22.5 && timeOfDay <= 24;
    }
    
    /// <summary>
    /// Returns all calendar events occurring on the current date
    /// </summary>
    public List<DayEventType> GetEventsToday()
    {
       var events = calendarEvents.GetEventsOnDate(GetCurrentDate());
       todaysEvents?.Invoke(events);
       return events;
    }

    /// <summary>
    /// Returns true if there is at least one event today
    /// </summary>
    public bool HasEventToday()
    {
        Debug.Log("Checking for events today"+ " " + GetCurrentDate() + " " + GetEventsToday().Count);
        return GetEventsToday().Count > 0;
    }
    
    public DateTime[] ReturnAllSundaysDuringTournament(DateTime startDate, int numOfWeeks)
    {
        List<DateTime> sundays = new List<DateTime>();
        
        // Find the first Sunday from the start date
        DateTime currentDate = startDate;
        
        // If start date is not Sunday, find the next Sunday
        while (currentDate.DayOfWeek != System.DayOfWeek.Sunday)
        {
            currentDate = currentDate.AddDays(1);
        }
        
        // Add Sundays for the specified number of weeks
        for (int week = 0; week < numOfWeeks; week++)
        {
            sundays.Add(currentDate);
            currentDate = currentDate.AddDays(7); // Move to next Sunday
        }
        
        return sundays.ToArray();
    }

    public void SetCurrentDate(DateTime newDate)
    {
        currentDay = newDate.Day;
        currentMonth = newDate.Month - 1; // Convert to 0-based index
        currentYear = newDate.Year;
        currentDayOfWeek = (int)newDate.DayOfWeek; // Sunday=0, Monday=1, ..., Saturday=6
        daysInCurrentMonth = GetDaysInCurrentMonth();
        HasEventToday();
        
        // Trigger the date changed event
        if (onDateChanged != null)
            onDateChanged.Invoke(currentDay, currentMonth, currentYear);
        
        RecheckIfRaceDay();
    }

    public float GetTimeOfDay()
    {
        return TimeOfDay;
    }
    
    public void SetTimeOfDay(float newTimeOfDay)
    {
        timeOfDay = Mathf.Clamp(newTimeOfDay, 0f, 23.99f);
        timeChangedEvent.Invoke();
    }
    
    public void AdjustTimeOfDay(float adjustment)
    {
        if(adjustment == 0f) return;
        if (adjustment < -24f || adjustment > 24f)
        {
            Debug.LogWarning("Adjustment out of bounds (-24 to 24). No adjustment made.");
            return;
        }
        if(timeOfDay >=23 && adjustment > 0f)
        {
            Debug.Log("Time is already at or past 23:00, cannot adjust further forward.");
            return;
        }
        
        timeOfDay += adjustment;
        timeOfDay = Mathf.Clamp(timeOfDay, 0f, 23.99f);
        timeChangedEvent.Invoke();
    }
    
    public bool RealtimeDayDurationEnabled()
    {
        return useRealTimeDayDuration;
    }
    
    public void SetTimePauseState(bool paused)
    {
        isTimePaused = paused;
    }
    
    public bool GetIsTimePaused()
    {
        return isTimePaused;
    }
    /// <summary>
    /// Alternative method using current date as start date
    /// </summary>
    public DateTime[] ReturnAllSundaysDuringTournament(int numOfWeeks)
    {
        return ReturnAllSundaysDuringTournament(GetCurrentDate(), numOfWeeks);
    }
}

