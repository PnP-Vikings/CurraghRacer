using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CalendarUI : MonoBehaviour
{
    public DayEventHandler dayCellPrefab;
    public Transform gridParent;
    public TMP_Text monthYearText;
    private int displayedMonth, displayedYear;
    public List<DayEventType> dayEventTypes; // List of day event types

    void Start()
    {
        displayedMonth = TimeManager.Instance.GetCurrentMonth();
        displayedYear = TimeManager.Instance.GetCurrentYear();
        UpdateCalendar();
        
        TimeManager.Instance.onNewDay.AddListener(UpdateCalendar); // Subscribe to time changes
    }

    public void UpdateCalendar()
    {
        // Clear old cells
        foreach (Transform child in gridParent) Destroy(child.gameObject);
        monthYearText.text = $"{TimeManager.Instance.monthNames[displayedMonth]} {displayedYear}";
        int days = TimeManager.Instance.GetDaysInMonth(displayedMonth, displayedYear);
        Debug.Log($"Days in month: {days}");
        for (int i = 1; i <= days; i++)
        {
            DayEventHandler cell = Instantiate(dayCellPrefab, gridParent);
            
            var textComponent = cell.dateText;
            textComponent.text = i.ToString();

            Image cellImage = cell.backgroundColorImage;

            var daytext = cell.dayoftheweekText;
            daytext.text = TimeManager.Instance.GetDayOfWeek(i, displayedMonth, displayedYear).ToString();
            
            
            
            
            if (i % 2 == 0)
            {
                // Even: white background, black text
                cellImage.color = Color.white;
                cell.SetAllTextColor(Color.black);
            }
            else
            {
                // Odd: dark blue background, white text
                cellImage.color = new Color(0.1f, 0.2f, 0.5f); // dark blue
                cell.SetAllTextColor(Color.white);
            }
            
            if(i == TimeManager.Instance.GetCurrentDay() && displayedMonth == TimeManager.Instance.GetCurrentMonth() && displayedYear == TimeManager.Instance.GetCurrentYear())
            {
                // Highlight the current day
                cellImage.color = Color.green; // Example highlight color
                cell.SetAllTextColor(Color.black); // Change text color for visibility
            }
            
            if(dayEventTypes != null && dayEventTypes.Count > 0)
            {
                if (daytext.text.ToUpper() == "SUNDAY")
                {
                    cell.dayEventType = dayEventTypes[0];
                    cell.Initialize();
                }
            }
           
                
            
          
        }
        
    }
    
    public void NextMonth()
    {
        Debug.Log("Next Month");
        displayedMonth++;
        if (displayedMonth >= TimeManager.Instance.monthNames.Length)
        {
            displayedMonth = 0;
            displayedYear++;
        }
        UpdateCalendar();
    }
    
    public void PreviousMonth()
    {
        Debug.Log("Previous Month");
        displayedMonth--;
        if (displayedMonth < 0)
        {
            displayedMonth = TimeManager.Instance.monthNames.Length - 1;
            displayedYear--;
        }
        UpdateCalendar();
    }

    public void CurrentMonth()
    {
        Debug.Log("Current Month");
        displayedMonth = TimeManager.Instance.GetCurrentMonth();
        displayedYear = TimeManager.Instance.GetCurrentYear();
        UpdateCalendar();
    }
}
