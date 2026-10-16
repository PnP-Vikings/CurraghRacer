using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapCurrency : MonoBehaviour
{
   [SerializeField] private GameObject mapCurrencyPanel;
   [SerializeField] private GameObject mapBackgroundImage;
   [SerializeField] private TMP_Text energyText;
   [SerializeField] private TMP_Text currencyText;
   [SerializeField] private TMP_Text timeText;
   [SerializeField] private TMP_Text amPmText;

   [Header("Currency Popup Stuff")]
   [SerializeField] private Vector3 defaultPoolSpawnPosition;
   [SerializeField] private Transform currencyPopupParent;
   [SerializeField] private int currencyPoolSize = 10;
   [SerializeField] private CurrencyChangePopup currencyPopupPrefab;
   [SerializeField] private List<CurrencyChangePopup> spawnedCurrencyPopupList;
   
   
   
   public static MapCurrency Instance { get; private set; }

   private void OnEnable()
   {
      if (Instance == null)
      {
         Instance = this;
         DontDestroyOnLoad(gameObject);
      }
      else
      {
         Destroy(gameObject);
      }
      if (mapCurrencyPanel == null)
      {
         mapCurrencyPanel = this.gameObject;
      }
      
      UpdateUi();
      if (TimeManager.Instance != null)
      {
         TimeManager.Instance.timeChangedEvent.AddListener(UpdateUi);
      }
      
      SceneManager.sceneLoaded += OnSceneLoaded;
      SpawnCurrencyPopupPool();
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
      if(!mapBackgroundImage.gameObject.activeSelf)
         return;
      
      
      if (PlayerManager.Instance != null)
      {
         energyText.text = PlayerManager.Instance.GetPlayerEnergy().ToString();
         currencyText.text = PlayerManager.Instance.GetPlayerCurrency().ToString();
      }
      if (TimeManager.Instance != null)
      {
         float totalHours = TimeManager.Instance.TimeOfDay;

         string amOrPmText = "";

         if (totalHours < 12)
         {
            amOrPmText = "AM";
         }
         else
         {
            amOrPmText = "PM";
         }
         
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

         amPmText.text =  $"{amOrPmText}";
      }

   }

   private void ToggleAllUi(bool isActive)
   {
      if (mapBackgroundImage != null)
      {
         mapBackgroundImage.SetActive(isActive);
      }
   }
   
   public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
   {
     
      ToggleAllUi(scene.name != "Main Menu");
     
   }

   public void SpawnCurrencyPopupPool()
   {
      if(currencyPopupParent == null ||currencyPopupPrefab ==null)
         return;
      for (int i = 0; i < currencyPoolSize; i++)
      {
        CurrencyChangePopup spawnedPopUp =Instantiate(currencyPopupPrefab, currencyPopupParent);
        spawnedCurrencyPopupList.Add(spawnedPopUp);
        spawnedPopUp.gameObject.SetActive(false);
      }
   }

   public void OnCurrencyChange(CurrencyTypes injectedCurrencyType,int amount)
   {
      CurrencyChangePopup chosenPopup = null;

      foreach (CurrencyChangePopup popup in spawnedCurrencyPopupList)
      {
         if (!popup.gameObject.activeSelf)
         {
            chosenPopup = popup;
            break;
         }
      }

      if (chosenPopup != null)
      {
         chosenPopup.gameObject.transform.localPosition = defaultPoolSpawnPosition;
         chosenPopup.UpdateUi(injectedCurrencyType, amount);
         chosenPopup.gameObject.SetActive(true);
      }
   }


}
