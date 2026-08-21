using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using League;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
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
    [SerializeField] private bool playerHasBeenShownIntro = false;
    [SerializeField] private bool playerHasBeenShownWarningAboutDebt = false;
    public UnityEvent OnGameStarted;
    [SerializeField] private float totalPlayTime = 0; // Total playtime in minutes
    [HideInInspector] public bool SleepAudioChangesCoroutineIsActive = false;
    [Header("Tutorial")]
    [SerializeField] private List<TutorialTask> defaultTutorialTasks = new List<TutorialTask>();
    [SerializeField] private List<TutorialTask> tutorialTasks = new List<TutorialTask>();
    [SerializeField] private bool tutorialModeActive = false;
    [SerializeField] private bool tutorialModeCompleted = false;
    public UnityEvent onTaskModified;
    public UnityEvent onTutorialModeCompleted;
    [SerializeField] TutorialAudio TutorialAudio;

    [Header("Localization")]
    LocalizedString localizedAutoSaveName = new LocalizedString { TableReference = "GameManager", TableEntryReference = "GameManager.AutoSave" };
    LocalizedString localizedCantSleepBeforeRace = new LocalizedString { TableReference = "GameManager", TableEntryReference = "GameManager.Sleep.CantSleepBeforeRace" };
    LocalizedString localizedNotTired = new LocalizedString { TableReference = "GameManager", TableEntryReference = "GameManager.Sleep.NotTired" };
    LocalizedString localizedCantSleepTutorial = new LocalizedString { TableReference = "GameManager", TableEntryReference = "GameManager.Sleep.CantSleepTutorial" };
    LocalizedString localizedYouSpentOnSleep = new LocalizedString { TableReference = "GameManager", TableEntryReference = "GameManager.Sleep.YouSpentOnSleep" };
    LocalizedString localizedCouldntAffordPlaceToSleep = new LocalizedString { TableReference = "GameManager", TableEntryReference = "GameManager.Sleep.CouldntAffordPlaceToSleep" };
    LocalizedString localizedAmountOfEnergyRegained = new LocalizedString { TableReference = "GameManager", TableEntryReference = "GameManager.Sleep.AmountOfEnergyRegained" };
    LocalizedString localizedUseTheEnergyYouRegainedToGoToWork = new LocalizedString { TableReference = "GameManager", TableEntryReference = "GameManager.Sleep.UseTheEnergyYouRegainedToGoToWork" };
    LocalizedString localizedYouHaveWorked = new LocalizedString { TableReference = "GameManager", TableEntryReference = "GameManager.Work.YouHaveWorked" };
    LocalizedString localizedInvalidStatType = new LocalizedString { TableReference = "GameManager", TableEntryReference = "GameManager.Train.InvalidStatType" };
    LocalizedString localizedGameOverText = new LocalizedString { TableReference = "GameManager", TableEntryReference = "GameManager.GameOverText" };
    
    

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

        
        localizedYouSpentOnSleep.Arguments = new object[] { 0 }; // Placeholder for sleep cost
        localizedAmountOfEnergyRegained.Arguments = new object[] { 0 }; // Placeholder for energy regained
        localizedYouHaveWorked.Arguments = new object[] { 0 }; // Placeholder for rewarded coins
      
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
            string autoSaveName = "Auto Save";
            if(localizedAutoSaveName != null && !localizedAutoSaveName.IsEmpty)
            {
                localizedAutoSaveName.RefreshString();
                autoSaveName = localizedAutoSaveName.GetLocalizedString();
            }
            SaveSystem.Instance.SaveGame(SaveSystem.Instance.maxSaveSlots - 1, autoSaveName);
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
        if (RaceManager.Instance != null && (RaceManager.Instance.isRaceDay && !RaceManager.Instance.hasPlayerCompletedRace))
        {
            string cantSleepMessage = "You cannot sleep before completing your race";
            if(localizedCantSleepBeforeRace != null && !localizedCantSleepBeforeRace.IsEmpty)
            {
                localizedCantSleepBeforeRace.RefreshString();
                cantSleepMessage = localizedCantSleepBeforeRace.GetLocalizedString();
            }
            PlayerStatsView.Instance.DisplayInfo(cantSleepMessage, 3);
            return; // Player cannot sleep before completing the race
        }

        if (PlayerManager.Instance.PlayerHasEnoughEnergy(100) && !tutorialModeActive)
        {
            string notTiredMessage = "You are not Tired";
            if(localizedNotTired != null && !localizedNotTired.IsEmpty)
            {
                localizedNotTired.RefreshString();
                notTiredMessage = localizedNotTired.GetLocalizedString();
            }
            PlayerStatsView.Instance.DisplayInfo(notTiredMessage, 3);
            return; // Not enough energy to sleep
        }
        
        if (tutorialModeActive && !tutorialModeCompleted && !IsTutorialTaskActive(TutorialTaskType.SleepTask))
        {
            PlayerStatsView.Instance.ClearInfo();
            string cantSleepTutorialMessage = "You cannot sleep yet. Complete your current tutorial task first.";
            if(localizedCantSleepTutorial != null && !localizedCantSleepTutorial.IsEmpty)
            {
                localizedCantSleepTutorial.RefreshString();
                cantSleepTutorialMessage = localizedCantSleepTutorial.GetLocalizedString();
            }
            PlayerStatsView.Instance.DisplayInfo(cantSleepTutorialMessage, 1.5f);
            return; // Not enough energy to sleep
        }
        
        if(tutorialModeActive && !tutorialModeCompleted && IsTutorialTaskActive(TutorialTaskType.SleepTask))
        {
          sleepCost = 0; // Override sleep cost to 0 during tutorial sleep task
        }

        if (PlayerManager.Instance.PurchaseItem(sleepCost))
        {
            int energyRegained = 100;
            PlayerManager.Instance.ModifyPlayerEnergy(energyRegained);
            string spentMessage = $"You Spent {sleepCost} on a place to sleep";
            if(localizedYouSpentOnSleep != null && !localizedYouSpentOnSleep.IsEmpty)
            {
                localizedYouSpentOnSleep.RefreshString();
                localizedYouSpentOnSleep.Arguments[0] = sleepCost;
                spentMessage = localizedYouSpentOnSleep.GetLocalizedString();
            }
            PlayerStatsView.Instance.DisplayInfo(spentMessage, 3);
            string energyRegainedMessage = $"You Have Regained {energyRegained} Energy";
            if(localizedAmountOfEnergyRegained != null && !localizedAmountOfEnergyRegained.IsEmpty)
            {
                localizedAmountOfEnergyRegained.Arguments[0] = energyRegained;
                localizedAmountOfEnergyRegained.RefreshString();
                energyRegainedMessage = localizedAmountOfEnergyRegained.GetLocalizedString();
            }
            PlayerStatsView.Instance.DisplayInfo(energyRegainedMessage, 3);
            TimeManager.Instance.SleepTime(); // Reset time of day to 6 AM
        }
        else
        {
            int energyRegained = 25;
            string CouldntAffordPlaceToSleepMessage = "You could not afford a place to sleep so slept on street";
            string energyRegainedMessage = $"You Have Regained {energyRegained} Energy";
            if(localizedCouldntAffordPlaceToSleep != null && !localizedCouldntAffordPlaceToSleep.IsEmpty)
            {
                localizedCouldntAffordPlaceToSleep.RefreshString();
                CouldntAffordPlaceToSleepMessage = localizedCouldntAffordPlaceToSleep.GetLocalizedString();
            }
            if(localizedAmountOfEnergyRegained != null && !localizedAmountOfEnergyRegained.IsEmpty)
            {
                localizedAmountOfEnergyRegained.Arguments[0] = energyRegained;
                localizedAmountOfEnergyRegained.RefreshString();
                energyRegainedMessage = localizedAmountOfEnergyRegained.GetLocalizedString();
            }
            
            PlayerStatsView.Instance.DisplayInfo(CouldntAffordPlaceToSleepMessage, 3);
            PlayerManager.Instance.ModifyPlayerEnergy(energyRegained);
            PlayerStatsView.Instance.DisplayInfo(energyRegainedMessage, 3);
            string useEnergyMessage = $"Use the energy you regained to go to work";
            if(localizedUseTheEnergyYouRegainedToGoToWork != null && !localizedUseTheEnergyYouRegainedToGoToWork.IsEmpty)
            {
                localizedUseTheEnergyYouRegainedToGoToWork.RefreshString();
                useEnergyMessage = localizedUseTheEnergyYouRegainedToGoToWork.GetLocalizedString();
            }
            PlayerStatsView.Instance.DisplayInfo(useEnergyMessage, 3);
            TimeManager.Instance.SleepTime(); // Reset time of day to 6 AM
        }
        
        if (GameManager.Instance != null && GameManager.Instance.IsTutorialModeActive() && GameManager.Instance.IsTutorialTaskActive(TutorialTaskType.SleepTask))
        {
            GameManager.Instance.CompleteTutorialTask(TutorialTaskType.SleepTask);
        }
        StartCoroutine(SleepAudioChanges());
    }

    public void PlayerWorked(int rewardedCoins = 50, int energyCost = -25)
    {
        
        if (GameManager.Instance != null && GameManager.Instance.IsTutorialModeActive())
        {
            GameManager.Instance.CompleteTutorialTask(TutorialTaskType.WorkJobTask);
        }
        
        PlayerManager.Instance.ModifyPlayerCoins(rewardedCoins);
        PlayerManager.Instance.ModifyPlayerEnergy(energyCost);
        
        string workedMessage = $"You Worked and Earned {rewardedCoins} Coins";
        if(localizedYouHaveWorked != null && !localizedYouHaveWorked.IsEmpty)
        {
            localizedYouHaveWorked.Arguments[0] = rewardedCoins; // Update the argument for the localized string
            localizedYouHaveWorked.RefreshString();
            workedMessage = localizedYouHaveWorked.GetLocalizedString();
        }
        PlayerStatsView.Instance.DisplayInfo(workedMessage, 3);
        TimeManager.Instance.UpdateTime(); // Update the time after working
    }

    public void PlayerTrained(TeamMember selectedTeamMember, TeamMember.StatType selectedStatType, int amountGained)
    {
        // Apply stat changes immediately BEFORE scene transition to prevent cache overwrite
        Debug.Log($"Training {selectedTeamMember.memberName} in {selectedStatType} for {amountGained} points.");


        if (tutorialModeActive && !tutorialModeCompleted)
        {
            CompleteTutorialTask(TutorialTaskType.TrainTeamMemberTask);
        }
            
            
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
                {
                    string invalidStatTypeMessage = "Invalid stat type selected for training.";
                    if(localizedInvalidStatType != null && !localizedInvalidStatType.IsEmpty)
                    {
                        localizedInvalidStatType.RefreshString();
                        invalidStatTypeMessage = localizedInvalidStatType.GetLocalizedString();
                    }
                    PlayerStatsView.Instance.DisplayInfo(invalidStatTypeMessage, 3);
                }
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
            string gameOverMessage = "GAME OVER\nThe mob got tired of waiting for you to pay up\nand destroyed the boat \nYou were forced to flee the country";
            if(localizedGameOverText != null && !localizedGameOverText.IsEmpty)
            {
                localizedGameOverText.RefreshString();
                gameOverMessage = localizedGameOverText.GetLocalizedString();
            }
            PlayerStatsView.Instance.DisplayEndGame(gameOverMessage, 5);
        }
    }

    public void SetHasBeenShownIntro(bool shown)
    {
        playerHasBeenShownIntro = shown;
    }
    public bool GetHasBeenShownIntro()
    {
        return playerHasBeenShownIntro;
    }

    public void SetHasBeenShownWarningAboutDebt(bool shown)
    {
        playerHasBeenShownWarningAboutDebt = shown;
    }
    public bool GetHasBeenShownWarningAboutDebt()
    {
        return playerHasBeenShownWarningAboutDebt;
    }
    
    public void CompleteTutorialTask(TutorialTaskType taskType)
    {
        foreach (var task in tutorialTasks)
        {
            if(task.taskType == taskType && task.isTaskActive)
            {
                task.isTaskActive = false;
                task.completed = true;
                Debug.Log($"Completed tutorial task: {task.taskName}");
                TutorialAudio.PlayTutorialTaskCompleteAudio();
                CheckIfAllTasksAreCompleted();
                ActivateNextTutorialTask();
                onTaskModified?.Invoke();
            }
            else
            {
                Debug.LogWarning($"Attempted to complete a tutorial task that is not in the active list: {task.taskName}");
            }
        }
    }
    
    public void CompleteTutorialTask(string taskName)
    {
        foreach (var task in tutorialTasks)
        {
            if(task.taskName == taskName && task.isTaskActive)
            {
                task.isTaskActive = false;
                task.completed = true;
                Debug.Log($"Completed tutorial task: {task.taskName}");
                CheckIfAllTasksAreCompleted();
                ActivateNextTutorialTask();
                onTaskModified?.Invoke();
            }
            else
            {
                Debug.LogWarning($"Attempted to complete a tutorial task that is not in the active list: {task.taskName}");
            }
        }
    }

    public void ActivateNextTutorialTask()
    {
        if(tutorialTasks.Count > 0 && !tutorialModeCompleted)
        {
            foreach (var task in tutorialTasks)
            {
                if (!task.completed && !task.isTaskActive)
                {
                    task.isTaskActive = true;
                    Debug.Log($"Activated next tutorial task: {task.taskName}");
                    onTaskModified?.Invoke();
                    return;
                }
            }
            Debug.Log("No more tutorial tasks to activate.");
        }
        else
        {
            Debug.LogWarning("Tutorial task list is empty. Cannot activate next task.");
        }
        onTaskModified?.Invoke();
    }

    public List<TutorialTask> GetTutorialTaskList()
    {
        return tutorialTasks;
    }

    public void ActivateTutorialMode(bool activate =false,bool ResetTutorial = false)
    {
        if (activate)
        {
            /*
            if (tutorialModeActive) return;
            if (tutorialModeCompleted) return;
            */
            if (ResetTutorial)
            {
                Debug.Log("Activating tutorial mode...");
                if (tutorialTasks.Count > 0)
                {
                    ResetTutorialMode();
                    tutorialModeActive = true;
                    tutorialTasks[0].isTaskActive = true;
                    Debug.Log($"Activated tutorial mode. First task: {tutorialTasks[0].taskName}");
                }
                /*if (LeagueController.Instance != null && !tutorialModeCompleted)
                {
                    LeagueController.Instance.AddTutorialLeagueToList();
                }*/
            }
          
        }
        else
        {
            Debug.Log("Deactivating tutorial mode...");
            tutorialModeActive = false;
            if (LeagueController.Instance != null)
            {
                LeagueController.Instance.RemoveTutorialLeagueFromList();
            }
        }
    }

    private void ResetTutorialMode()
    {
        foreach (var task in defaultTutorialTasks)
        {
            task.completed = false;
            task.isTaskActive = false;
            task.hasTutorialDialogsBeenShown = false;
        }
        tutorialTasks = defaultTutorialTasks;
        
        tutorialModeActive = false;
        tutorialModeCompleted = false;
    }
    public void CheckIfAllTasksAreCompleted()
    {
        foreach (var task in tutorialTasks)
        {
            if (!task.completed)
            {
                return; // If any task is not completed, exit the method
            }
        }
        
        if(tutorialModeCompleted)
        {
            Debug.Log("All tutorial tasks are already completed.");
            return; // If tutorial mode is already marked as completed, exit the method
        }
        
        tutorialModeCompleted = true;
        tutorialModeActive = false;
        Debug.Log("All tutorial tasks are completed.");
        onTutorialModeCompleted?.Invoke();
        
        GameManager.Instance.Sleep(0); // Automatically sleep the player after completing all tutorial tasks
        LeagueController.Instance.ShowLeagueInviteAfterDelay(2f);;
        
        if(BillsController.Instance != null)
            BillsController.Instance.GenerateBillsAfterTutorial();
        if (TutorialAudio != null)
        {
            StartCoroutine(TutorialAudio.PlayTutorialCompleteAudio());
        }
    }

    public bool IsTutorialModeActive()
    {
        return tutorialModeActive;
    }
    public bool IsTutorialModeCompleted()
    {
        return tutorialModeCompleted;
    }
    public List<TutorialTask> GetTutorialTasks()
    {
        return tutorialTasks;
    }
    public void RevertToDefaultTutorialTasks()
    {
        ResetTutorialMode();
    }
    public void SetTutorialTasks(List<TutorialTask> tasks)
    {
        tutorialTasks = tasks;
    }
    public void SetTutorialModeCompleted(bool completed)
    {
        tutorialModeCompleted = completed;
    }
    
    public bool IsTutorialTaskCompleted(TutorialTaskType taskType)
    {
        foreach (var task in tutorialTasks)
        {
            if (task.taskType == taskType)
            {
                return task.completed;
            }
        }
        Debug.LogWarning($"Tutorial task of type {taskType} not found.");
        return false; // Task not found
    }

    public bool IsTutorialTaskActive(TutorialTaskType taskType)
    {
        foreach (var task in tutorialTasks)
        {
            if (task.taskType == taskType)
            {
                return task.isTaskActive;
            }
        }
        Debug.LogWarning($"Tutorial task of type {taskType} not found.");
        return false; // Task not found
    }
    
    public void MarkTutorialTaskDialogsAsShown(TutorialTask taskToMark)
    {
        foreach (var task in tutorialTasks)
        {
            if (task.taskType == taskToMark.taskType)
            {
                task.hasTutorialDialogsBeenShown = true;
                Debug.Log($"Marked tutorial task dialogs as shown for: {task.taskName}");
                return;
            }
        }
        Debug.LogWarning($"Tutorial task of type {taskToMark.taskType} not found.");
    }
    
    public void ResetForNewGame()
    {
       SetHasBeenShownIntro(false); // Reset intro flag for new game
       StartGame();
    }

}
