using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
    public bool isGameOver = false;
    public Transform cameraStartPosition;
    public int racesTillNextAd = 3;
    public string startSceneName = "Main Menu";
    public string mainSceneName = "Garage";
    public int sleepsTillNextAd = 3; // Number of sleeps before the next ad can be shown
    public bool playerIsBusy = false;
    private bool falsePlayerHasBeenShownIntro = false;
    public UnityEvent OnGameStarted;
    [SerializeField] private float totalPlayTime = 0; // Total playtime in minutes
    [HideInInspector] public bool SleepAudioChangesCoroutineIsActive = false;


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
        StartCoroutine(TrackPlayTime());

    }



    private IEnumerator TrackPlayTime()
    {
        while( true )
        {
            yield return new WaitForSeconds(60f); // Wait for 1 minute
            totalPlayTime++;
        }
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
        if (SaveSystem.Instance != null && SaveSystem.Instance.CanAutoSaveGame())
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
        if (PlayerManager.Instance.PlayerHasEnoughEnergy(100))
        {
            PlayerStatsView.Instance.DisplayInfo("You are not Tired", 3);
            return; // Not enough energy to sleep
        }

        if (PlayerManager.Instance.PurchaseItem(sleepCost))
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
        StartCoroutine(SleepAudioChanges());
    }

    public void PlayerWorked(int rewardedCoins = 50, int energyCost = -25)
    {
        PlayerManager.Instance.ModifyPlayerCoins(rewardedCoins);
        PlayerManager.Instance.ModifyPlayerEnergy(energyCost);
        PlayerStatsView.Instance.DisplayInfo($"You Worked and Earned {rewardedCoins} Coins", 3);
        TimeManager.Instance.UpdateTime(); // Update the time after working
    }

    public void PlayerTrained(TeamMember selectedTeamMember, TeamMember.StatType selectedStatType, int amountGained)
    {
        // Apply stat changes immediately BEFORE scene transition to prevent cache overwrite
        Debug.Log($"Training {selectedTeamMember.memberName} in {selectedStatType} for {amountGained} points.");

        switch (selectedStatType)
        {
            case TeamMember.StatType.Strength:
                PlayerManager.Instance.ModifyTeamMemberStat(selectedTeamMember, TeamMember.StatType.Strength, amountGained);
                break;
            case TeamMember.StatType.Technique:
                PlayerManager.Instance.ModifyTeamMemberStat(selectedTeamMember, TeamMember.StatType.Technique, amountGained);
                break;
            case TeamMember.StatType.Stamina:
                PlayerManager.Instance.ModifyTeamMemberStat(selectedTeamMember, TeamMember.StatType.Stamina, amountGained);
                break;
            case TeamMember.StatType.TeamWork:
                PlayerManager.Instance.ModifyTeamMemberStat(selectedTeamMember, TeamMember.StatType.TeamWork, amountGained);
                break;
            default:
                Debug.LogError("Invalid stat type selected for training.");
                if (PlayerStatsView.Instance != null)
                    PlayerStatsView.Instance.DisplayInfo("Invalid stat type selected for training.", 3);
                break;
        }

        // Update the cached save data to include the new stats
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.UpdateCachedSaveData();
        }

        TimeManager.Instance.UpdateTime();
    }


    public float GetTotalPlayTime()
    {
        return totalPlayTime;
    }

    public void ResetTotalPlayTime()
    {
        totalPlayTime = 0;
    }

    public void SetTotalPlayTime(float minutes)
    {
        totalPlayTime = minutes;
    }

    public Transform GetCameraStartPosition()
    {

        if (cameraStartPosition == null)
        {
            cameraStartPosition = FindFirstObjectByType<cameraStartPosition>().transform;
        }
        return cameraStartPosition;
    }

    public IEnumerator SleepAudioChanges()
    {
        yield return null;
        if (AudioManager.instance != null)
        {
            yield return new WaitForSeconds(0.5f);

            SleepAudioChangesCoroutineIsActive = true;

            AudioManager.instance.tvButtonPushOut.start();

            if (RadioManager.instance != null)
            {
                RadioManager.instance.MuteRadio();
            }

            if (PlayerManager.Instance != null)
            {
                float coins = PlayerManager.Instance.GetPlayerCoins();
                if (coins >= 20.0f)
                {
                    AudioManager.instance.sleepAudio.start();
                }
                else
                {
                    AudioManager.instance.sleepOutsideAudio.start();
                }
            }

            yield return new WaitForSeconds(2f);

            AudioManager.instance.rooster.start();

            yield return new WaitForSeconds(1.5f);

            AudioManager.instance.tvButtonPushIn.start();

            yield return new WaitForSeconds(0.5f);

            if (RadioManager.instance != null)
            {
                RadioManager.instance.UnMuteRadio();
            }

            SleepAudioChangesCoroutineIsActive = false;
        }
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }

    public void TriggerGameOver()
    {
        isGameOver = true;
        GameStarted = false;
        SceneManager.LoadScene(startSceneName);
        //DOVirtual.DelayedCall(.1f, () => ShowGameOverText());
        ShowGameOverText();

    }

    public void ShowGameOverText()
    {
        if (PlayerStatsView.Instance != null)
        {
            PlayerStatsView.Instance.ClearInfo();
            PlayerStatsView.Instance.DisplayEndGame("GAME OVER\nThe mob got tired of waiting for you to pay up\nand destroyed the boat \nYou were forced to flee the country", 5);
        }
    }

    public void SetHasBeenShownIntro(bool shown)
    {
        falsePlayerHasBeenShownIntro = shown;
    }
    public bool GetHasBeenShownIntro()
    {
        return falsePlayerHasBeenShownIntro;
    }
    
    public void ResetForNewGame()
    {
       SetHasBeenShownIntro(false); // Reset intro flag for new game
       StartGame();
    }

}
