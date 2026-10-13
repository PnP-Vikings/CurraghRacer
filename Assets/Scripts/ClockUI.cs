using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class ClockUI : MonoBehaviour
{
    [SerializeField] private PanelRenderer uiDoc;
    
    private Label _dayOfWeekText;
    private VisualElement _minuteHand,
        _hourHand;
    private VisualElement _clockContainer;

    [SerializeField] private bool showClockInGarage = false;
    
    private void OnEnable()
    {
        if (uiDoc == null)
        {
            uiDoc = GetComponent<PanelRenderer>();
        }

        if (uiDoc != null)
        {
            uiDoc.RegisterUIReloadCallback(OnUIReload);
        }

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.onNewDay.AddListener(UpdateClock);
            TimeManager.Instance.timeChangedEvent.AddListener(UpdateClock);
            showClockInGarage = TimeManager.Instance.GetShowClockInGarage();
        }
        else
        {
            Debug.Log("TimeManager Instance is null");
        }
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateClockVisibility();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (uiDoc != null)
        {
            uiDoc.UnregisterUIReloadCallback(OnUIReload);
        }

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.onNewDay.RemoveListener(UpdateClock);
            TimeManager.Instance.timeChangedEvent.RemoveListener(UpdateClock);
        }
    }

    private void OnDestroy()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.onNewDay.RemoveListener(UpdateClock);
            TimeManager.Instance.timeChangedEvent.RemoveListener(UpdateClock);
        }
    }

    private void OnUIReload(PanelRenderer renderer, VisualElement root)
    {
        if (root == null) return;

        _minuteHand = root.Q<VisualElement>("MinuteHand");
        _hourHand = root.Q<VisualElement>("HourHand");
        _dayOfWeekText = root.Q<Label>("DayOfWeekText");
        _clockContainer = root.Q<VisualElement>("ClockMajor");

        UpdateClock();
        UpdateClockVisibility();
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (TimeManager.Instance != null)
        {
            showClockInGarage = TimeManager.Instance.GetShowClockInGarage();
        }
        
        UpdateClockVisibility();
    }

    private void UpdateClockVisibility()
    {
        if (_clockContainer == null) return;

        if (SceneManager.GetActiveScene().name != "Garage" || !showClockInGarage)
        {
            _clockContainer.style.display = DisplayStyle.None;
        }
        else
        {
            _clockContainer.style.display = DisplayStyle.Flex;
        }
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
        // Update the day of the week text
        if (_dayOfWeekText != null)
            _dayOfWeekText.text = dayOfWeek;
    }
}