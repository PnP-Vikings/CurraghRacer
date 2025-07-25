using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Calendar
{
    public class CalendarUI : MonoBehaviour
    {
        public DayEventHandler dayCellPrefab;
        public Transform gridParent;
        public TMP_Text monthYearText;
        private int displayedMonth, displayedYear;
        public CalendarEvents calendarEvents;
        
        [Header("Common Events Settings")]
        [SerializeField] private bool includeCommonHolidays = true;
        [SerializeField] private bool includeRacingEvents = true;

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
            
            for (int i = 1; i <= days; i++)
            {
                DayEventHandler cell = Instantiate(dayCellPrefab, gridParent);
                
                // Create the date for this cell
                DateTime currentDate = new DateTime(displayedYear, displayedMonth + 1, i);
                cell.SetupDay(currentDate);
                
                // Set default styling
                SetDefaultCellStyling(cell, i);
                
                // Highlight current day
                if (IsCurrentDay(i))
                {
                    cell.backgroundColorImage.color = Color.green;
                    cell.SetAllTextColor(Color.black);
                }
                
                // Check for events on this date
                CheckAndApplyEvents(cell, currentDate);
            }
        }
        
        private void SetDefaultCellStyling(DayEventHandler cell, int dayNumber)
        {
            Image cellImage = cell.backgroundColorImage;
            
            if (dayNumber % 2 == 0)
            {
                // Even: white background, black text
                cellImage.color = Color.white;
                cell.SetAllTextColor(Color.black);
            }
            else
            {
                // Odd: dark blue background, white text
                cellImage.color = new Color(0.1f, 0.2f, 0.5f);
                cell.SetAllTextColor(Color.white);
            }
        }
        
        private bool IsCurrentDay(int day)
        {
            return day == TimeManager.Instance.GetCurrentDay() && 
                   displayedMonth == TimeManager.Instance.GetCurrentMonth() && 
                   displayedYear == TimeManager.Instance.GetCurrentYear();
        }
        
        private void CheckAndApplyEvents(DayEventHandler cell, DateTime date)
        {
            // Check manually added events first
            if (calendarEvents != null && calendarEvents.calendarDayEvents.Count > 0)
            {
                foreach (var eventType in calendarEvents.calendarDayEvents)
                {
                    if (eventType.OccursOnDate(date))
                    {
                        cell.SetEvent(eventType);
                        return; // Event found, no need to check common events
                    }
                }
            }
            
            // Check common events if no manual event was found
            CheckCommonEvents(cell, date);
        }
        
        private void CheckCommonEvents(DayEventHandler cell, DateTime date)
        {
            // Only check events from 1981 onwards
            if (date.Year < 1981) return;
            
            // Check holidays
            if (includeCommonHolidays)
            {
                if (IsChristmas(date))
                {
                    ApplyEventToCell(cell, "Christmas", "Christmas Day celebration", Color.red, Color.white);
                    return;
                }
                if (IsNewYear(date))
                {
                    ApplyEventToCell(cell, "New Year's Day", "Start of the new year", Color.yellow, Color.black);
                    return;
                }
                if (IsHalloween(date))
                {
                    ApplyEventToCell(cell, "Halloween", "Spooky Halloween night", new Color(1f, 0.5f, 0f), Color.black);
                    return;
                }
                if (IsValentinesDay(date))
                {
                    ApplyEventToCell(cell, "Valentine's Day", "Day of love and romance", new Color(1f, 0.4f, 0.7f), Color.white);
                    return;
                }
            }
            
        }
        
        private void ApplyEventToCell(DayEventHandler cell, string eventName, string description, Color bgColor, Color textColor)
        {
            cell.backgroundColorImage.color = bgColor;
            cell.SetAllTextColor(textColor);
            
            if (cell.eventNameText != null)
                cell.eventNameText.text = eventName;
            if (cell.descriptionText != null)
                cell.descriptionText.text = description;
        }
        
        // Holiday check methods
        private bool IsChristmas(DateTime date) => date.Month == 12 && date.Day == 25;
        private bool IsNewYear(DateTime date) => date.Month == 1 && date.Day == 1;
        private bool IsHalloween(DateTime date) => date.Month == 10 && date.Day == 31;
        private bool IsValentinesDay(DateTime date) => date.Month == 2 && date.Day == 14;
        
        // Racing event check methods
        private bool IsWeeklyRace(DateTime date) => date.DayOfWeek == System.DayOfWeek.Sunday;
        private bool IsChampionshipRace(DateTime date) => date.DayOfWeek == System.DayOfWeek.Saturday;
        
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
}
