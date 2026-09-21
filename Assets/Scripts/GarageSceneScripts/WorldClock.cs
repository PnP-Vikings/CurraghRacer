using TMPro;
using UnityEngine;

public class WorldClock : MonoBehaviour
{
   [SerializeField] private TMP_Text worldClockDayText;
   [SerializeField] private TMP_Text worldClockTimeText;
   [SerializeField] private TMP_Text worldClockAMPMText;

    private void OnEnable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.timeChangedEvent.AddListener(UpdateWorldClock);
            UpdateWorldClock();
        }
    }

    private void OnDisable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.timeChangedEvent.RemoveListener(UpdateWorldClock);
        }
    }

    private void UpdateWorldClock()
    {
        if (worldClockDayText != null && worldClockTimeText != null && worldClockAMPMText != null && TimeManager.Instance != null)
        {
            float totalHours = TimeManager.Instance.TimeOfDay;
            worldClockDayText.text = TimeManager.Instance.GetCurrentDayOfWeekString();
            
            if(totalHours >= 12f)
            {
                worldClockAMPMText.text = "PM";
            }
            else
            {
                worldClockAMPMText.text = "AM";
            }
            
            
            float hours = totalHours % 12f; // Convert to 12-hour format (includes fractional minutes)
            float minutes = (totalHours % 1f) * 60f; // Extract minutes from decimal portion
            worldClockTimeText.text = $"{Mathf.FloorToInt(hours)}:{Mathf.FloorToInt(minutes):00}";
        }
    }
}
