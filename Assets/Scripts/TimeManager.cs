using UnityEngine;
// Add this at the beginning of the TimeManager script
using UnityEngine.Events;
using UnityEngine.Serialization;

public class TimeManager : MonoBehaviour
{
    // Singleton instance
    private static TimeManager _instance;
    public static TimeManager Instance { get { return _instance; } }
    internal string[] daysOfWeek = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    

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

    // Properties
    public float TimeOfDay { get => timeOfDay; }
    public float TimeMultiplier { get => timeMultiplier; set => timeMultiplier = Mathf.Max(value, 0f); }
    public int DaysPassed { get => daysPassed; }
    

    // Initialize singleton instance
    private void Awake()
    {
        // If an instance already exists, destroy this object
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
        
        timeOfDay = 6f; // Start at 6 AM
    }

    // Assign the itemSpawner reference and update the time of day every frame
    /*private void FixedUpdate()
    {
        UpdateTime();
    }*/

    // Update the time of day based on the time multiplier
    
    public void SleepTime()
    {
        // Reset time of day to 0 (start of a new day)
        timeOfDay = 6f;
        daysPassed++;
        newItemSpawned = false;
        onNewDay.Invoke(); // Raise the OnNewDay event
        timeChangedEvent.Invoke();
    }
    
    
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


    


