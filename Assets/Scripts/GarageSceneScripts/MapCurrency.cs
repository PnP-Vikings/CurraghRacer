using System;
using TMPro;
using UnityEngine;

public class MapCurrency : MonoBehaviour
{
   [SerializeField] private TMP_Text energyText;
   [SerializeField] private TMP_Text currencyText;
   [SerializeField] private TMP_Text timeText;



   private void OnEnable()
   {
      UpdateUi();
      if (TimeManager.Instance != null)
      {
         TimeManager.Instance.timeChangedEvent.AddListener(UpdateUi);
      }
   }

   private void OnDisable()
   {
      if (TimeManager.Instance != null)
      {
         TimeManager.Instance.timeChangedEvent.RemoveListener(UpdateUi);
      }
   }
   private void UpdateUi()
   {
      if (PlayerManager.Instance != null)
      {
         energyText.text = PlayerManager.Instance.GetPlayerEnergy().ToString();
         currencyText.text = PlayerManager.Instance.GetPlayerCurrency().ToString();
      }
      if (TimeManager.Instance != null)
      {
         float totalHours = TimeManager.Instance.TimeOfDay;
         float hours = totalHours % 12f; // Convert to 12-hour format (includes fractional minutes)
         float minutes = (totalHours % 1f) * 60f; // Extract minutes from decimal portion
         if (hours >=0&& hours<=1 || hours >=12&& hours<=13)
         {
            timeText.text =  $"12:{Mathf.FloorToInt(minutes):00}";
         }
         else
         {
            timeText.text =  $"{Mathf.FloorToInt(hours)}:{Mathf.FloorToInt(minutes):00}";
         }

       
      }

   }


}
