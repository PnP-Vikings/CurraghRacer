using System;
using System.Collections;
using System.Collections.Generic;
using League;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
   public static GameManager Instance { get; private set; }
   public bool isRewarded = false;
   public bool GameStarted = false;
   public Transform cameraStartPosition;
   public int racesTillNextAd = 3;
   public string startSceneName = "Main Menu";
   public string mainSceneName = "Garage";
   public int sleepsTillNextAd = 3; // Number of sleeps before the next ad can be shown
   public bool playerIsBusy = false;
   public UnityEvent OnGameStarted;

   public List<String> miniGameWorkScenes = new List<string>
   {
       "BeerPourMinigame",
       "DishWashingMinigame" 
   };
   
   private void Awake()
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
       
       StartCoroutine(DisplayBannerWithDelay());
   }
   
    private IEnumerator DisplayBannerWithDelay()
    {
         yield return new WaitForSeconds(2f); // Adjust the delay as needed
         AdsManager.Instance.bannerAds.ShowBannerAd();
    }
    
    public void HideBannerAd()
    {
        AdsManager.Instance.bannerAds.HideBannerAd();
    }
    
    public void StartGame()
    {
        AdsManager.Instance.bannerAds.HideBannerAd();
        GameStarted = true;
        OnGameStarted?.Invoke();
        SceneManager.LoadScene(mainSceneName);
    }
    
    public bool GetGameStarted()
    {
        return GameStarted;
    }
    
    public void SetPlayerBusy(bool busy)
    {
        playerIsBusy = busy;
    }
    
    public void SetGameStarted(bool started)
    {
        GameStarted = started;
    }
    
    
    /// <summary>
    /// Trigger an auto-save at important game events
    /// </summary>
    public void TriggerAutoSave()
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveGame(SaveSystem.Instance.maxSaveSlots - 1, "Auto Save");
            Debug.Log("Auto-save triggered");
        }
    }
    
    
    
    public bool CanShowAd()
    {
        racesTillNextAd--;
        if (racesTillNextAd <= 0)
        {
            racesTillNextAd = 5; // Reset to default value
            return true; // Allow ad to be shown
        }
        
        
        return false; // Do not allow ad to be shown
        
    }

    public bool CanShowSleepAd()
    {
        sleepsTillNextAd--;
        if (sleepsTillNextAd <= 0)
        {
            sleepsTillNextAd = 3; // Reset to default value
            return true; // Allow ad to be shown
        }
        
        return false; // Do not allow ad to be shown
    }

    public void Sleep(int sleepCost)
    {
        if(PlayerManager.Instance.PlayerHasEnoughEnergy(100))
        {
            PlayerStatsView.Instance.DisplayInfo("You are not Tired", 3);
            return; // Not enough energy to sleep
        }
        
        if(PlayerManager.Instance.PurchaseItem(sleepCost))
        {
            PlayerManager.Instance.ModifyPlayerEnergy(100);
            PlayerStatsView.Instance.DisplayInfo($"You Spent {sleepCost} on a place to sleep", 3);
            PlayerStatsView.Instance.DisplayInfo("You Have Regained 100 Energy", 3);
            TimeManager.Instance.SleepTime(); // Reset time of day to 6 AM
        }
        else
        {
            PlayerStatsView.Instance.DisplayInfo($"You could not afford a place to sleep so slept on street", 3);
            PlayerManager.Instance.ModifyPlayerEnergy(25);
            PlayerStatsView.Instance.DisplayInfo("You Have Regained 25 Energy", 3);
            PlayerStatsView.Instance.DisplayInfo($"Use the energy you regained to go to work", 3);
            TimeManager.Instance.SleepTime(); // Reset time of day to 6 AM
        }
    }

    public void PlayerWorked(int rewardedCoins =50, int energyCost = -25)
    {
        PlayerManager.Instance.ModifyPlayerCoins(rewardedCoins);
        PlayerManager.Instance.ModifyPlayerEnergy(energyCost);
        PlayerStatsView.Instance.DisplayInfo($"You Worked and Earned {rewardedCoins} Coins", 3);
        TimeManager.Instance.UpdateTime(); // Update the time after working
    }
    
    public Transform GetCameraStartPosition()
    {
        
        if (cameraStartPosition == null)
        {
          cameraStartPosition =  FindFirstObjectByType<cameraStartPosition>().transform;
        }
        return cameraStartPosition;
    }
   
}
