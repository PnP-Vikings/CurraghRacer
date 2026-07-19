using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class ClockUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDoc;
    
    private Label _dayOfWeekText;
    private VisualElement _minuteHand,
        _hourHand;

    
    
    private void OnEnable()
    {
        
        uiDoc = GetComponent<UIDocument>();
        
       
        var root = uiDoc.rootVisualElement;
        _minuteHand = root.Q<VisualElement>("MinuteHand");
        _hourHand = root.Q<VisualElement>("HourHand");
        _dayOfWeekText = root.Q<Label>("DayOfWeekText");
        UpdateClock();
        if (TimeManager.Instance == null) {Debug.Log("TimeManger Instance is null");return;}
        TimeManager.Instance.onNewDay.AddListener(UpdateClock);
        TimeManager.Instance.timeChangedEvent.AddListener(UpdateClock);
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        
      
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SceneManager.GetActiveScene().name == "Main Menu")
        {
            uiDoc.rootVisualElement.style.display = DisplayStyle.None;
        }
        else
        {
            uiDoc.rootVisualElement.style.display = DisplayStyle.Flex;
        }
    }
    
    private void OnDestroy()
    {
     TimeManager.Instance.onNewDay.RemoveListener(UpdateClock);
        TimeManager.Instance.timeChangedEvent.RemoveListener(UpdateClock);
    }
    
    
    public void UpdateClock()
    {
        if (TimeManager.Instance == null) return;

        float totalHours = TimeManager.Instance.TimeOfDay; // e.g., 14.5 for 2:30 PM
        float hours = totalHours % 12f; // Convert to 12-hour format (includes fractional minutes)
        float minutes = (totalHours % 1f) * 60f; // Extract minutes from decimal portion

        // Calculate angles (0° = 12 o'clock, rotates clockwise)
        // Hour hand: totalHours already includes minutes as decimal, so just multiply by 30
        float hourAngle = hours * 30f; // 30° per hour (e.g., 2.5 hours = 75°)
    
        // Minute hand: moves 360° in 60 minutes = 6° per minute
        float minuteAngle = minutes * 6f;

        // Apply rotations (negative for clockwise)
        if (_hourHand != null)
            _hourHand.transform.rotation = Quaternion.Euler(0, 0, hourAngle);
    
        // Rotate the minute hand based on the minutes 
        if (_minuteHand != null)
            _minuteHand.transform.rotation = Quaternion.Euler(0, 0, minuteAngle);
        
        // Get the current day number from the TimeManager
        int dayNumber = TimeManager.Instance.DaysPassed;
        // Calculate the day of the week using the modulo operator
        string dayOfWeek = TimeManager.Instance.GetCurrentDayOfWeekString();
        // Update the day number text
        //dayNumberText.text = $"Day {dayNumber + 1}";
        // Update the day of the week text
        _dayOfWeekText.text = dayOfWeek;
    }
    

}