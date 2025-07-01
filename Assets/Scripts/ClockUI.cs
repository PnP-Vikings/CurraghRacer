using System;
using TMPro;
using UnityEngine;
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
       TimeManager.Instance.onNewDay.AddListener(UpdateClock);
       TimeManager.Instance.timeChangedEvent.AddListener(UpdateClock);
       
    }

    private void OnDestroy()
    {
     TimeManager.Instance.onNewDay.RemoveListener(UpdateClock);
        TimeManager.Instance.timeChangedEvent.RemoveListener(UpdateClock);
    }

    private void UpdateClock()
    {
        // Get the current time from the TimeManager in hours
        float hours = TimeManager.Instance.TimeOfDay;
        // Calculate the minutes based on the fraction of the hour
        float minutes = (hours % 1) * 60;

        // Rotate the hour hand based on the hours (30 degrees per hour) and subtract 30 degrees from the initial rotation
        _hourHand.transform.rotation = Quaternion.Euler(0, 0, -hours * 90f);
        // Rotate the minute hand based on the minutes (6 degrees per minute)
        _minuteHand.transform.rotation = Quaternion.Euler(0, 0, -minutes * 90f);

        // Get the current day number from the TimeManager
        int dayNumber = TimeManager.Instance.DaysPassed;
        // Calculate the day of the week using the modulo operator
        string dayOfWeek = TimeManager.Instance.daysOfWeek[dayNumber % 7];
        // Update the day number text
        //dayNumberText.text = $"Day {dayNumber + 1}";
        // Update the day of the week text
        _dayOfWeekText.text = dayOfWeek;
    }


}