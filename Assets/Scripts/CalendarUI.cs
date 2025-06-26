using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CalendarUI : MonoBehaviour
{
    public GameObject dayCellPrefab;
    public Transform gridParent;
    public TMP_Text monthYearText;
    private int displayedMonth, displayedYear;

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
        int days = TimeManager.Instance.daysInCurrentMonth;
        Debug.Log($"Days in month: {days}");
        for (int i = 1; i <= days; i++)
        {
            GameObject cell = Instantiate(dayCellPrefab, gridParent);
            cell.GetComponentInChildren<TMP_Text>().text = i.ToString();
            
            if(i == TimeManager.Instance.GetCurrentDay() && displayedMonth == TimeManager.Instance.GetCurrentMonth() && displayedYear == TimeManager.Instance.GetCurrentYear())
            {
                // Highlight the current day
                cell.GetComponent<Image>().color = Color.green; // Example highlight color
                cell.GetComponentInChildren<TMP_Text>().color = Color.black; // Change text color for visibility
            }
            
            // Highlight current day, add event listeners, etc.
        }
        
    }
    
    public void NextMonth()
    {
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
        displayedMonth--;
        if (displayedMonth < 0)
        {
            displayedMonth = TimeManager.Instance.monthNames.Length - 1;
            displayedYear--;
        }
        UpdateCalendar();
    }
}
