using UnityEngine;
// Add this at the beginning of the TimeManager script
using UnityEngine.Events;
using UnityEngine.Serialization;
using System;

public class TimeManager : MonoBehaviour
{
    // Singleton instance
    private static TimeManager _instance;
    public static TimeManager Instance { get { return _instance; } }
    internal string[] daysOfWeek = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
    internal string[] monthNames = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
    internal int[] daysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
   
    

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

    // Calendar variables
    [SerializeField] private int currentDay = 1;
    [SerializeField] private int currentMonth = 0; // 0-based index (0 = January)
    [SerializeField] private int currentYear = 2008; // Starting year
    [SerializeField] private int currentDayOfWeek = 0; // 0-based index (0 = Sunday)
    [SerializeField] internal int daysInCurrentMonth = 31; // Default to 31 days for January
    
    [Serializable]
    public class DateChangedEvent : UnityEvent<int, int, int> { } // day, month, year
    
    public DateChangedEvent onDateChanged;

    // Properties
    public float TimeOfDay { get => timeOfDay; }
    public float TimeMultiplier { get => timeMultiplier; set => timeMultiplier = Mathf.Max(value, 0f); }
    public int DaysPassed { get => daysPassed; }
    

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
        
        timeOfDay = 6f; // Start at 6 AM
        daysPassed = 0;
        
        daysInCurrentMonth = GetDaysInCurrentMonth();
        
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
        // Trigger the date changed event
        if (onDateChanged != null)
            onDateChanged.Invoke(currentDay, currentMonth, currentYear);
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
        // Calculate the day of the week using Zeller's Congruence
        int k = day;
        int m = month + 1; // January and February are treated as months 13 and 14 of the previous year
        if (m < 3)
        {
            m += 12;
            year--;
        }
        int D = year % 100;
        int C = year / 100;
        
        int f = k + (13 * m + 1) / 5 + D + D / 4 + C / 4 - 2 * C;
        f = f % 7; // Result is in the range [0, 6]
        
        return daysOfWeek[(f + 5) % 7]; // Adjust to match Sunday = 0
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
    
    
    public void UpdateTime()
    {
        float previousTimeOfDay = timeOfDay;
        /*timeOfDay += Time.deltaTime * timeMultiplier;*/
        timeOfDay += 3f;
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

}
