using System.Collections;
using System.Collections.Generic;
using Calendar;
using League;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class StartMenu : MonoBehaviour
{
    [SerializeField] private UIDocument uiDoc;
    private Button _startRaceButton,_trainButton,_workButton,_sleepButton;
    [SerializeField] private UnityEngine.UI.Button startRaceButtonGarage;
    [SerializeField] private TMPro.TMP_Text _startRaceButtonText;
    [SerializeField] CameraController cameraController;
    public GameObject trainingMenuPrefab;
    public bool isTooLateForActivities = false;
    public static StartMenu Instance { get; private set; }

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnEnable()
    {
        uiDoc = GetComponent<UIDocument>();

        if (uiDoc != null)
        {
            var root = uiDoc.rootVisualElement;
            _startRaceButton = root.Q<Button>("StartRaceButton");
            _trainButton = root.Q<Button>("TrainingButton");
            _workButton = root.Q<Button>("WorkButton");
            _sleepButton = root.Q<Button>("SleepButton");


            _startRaceButton.clicked += OnStartRaceButtonClicked;
            _trainButton.clicked += OnTrainingButtonClicked;
            _workButton.clicked += OnWorkButtonClicked;
            _sleepButton.clicked += OnSleepButtonClicked;
        }
        UpdateRaceDayStatus();
        
        TimeManager.Instance.onNewDay.AddListener(UpdateRaceDayStatus); 
        LeagueController.Instance.onPlayerJoinedLeague.AddListener(UpdateRaceDayStatus);
    }
    
    public enum RaceDayStatus
    {
        CanRace,
        NotInLeague,
        NotRaceDay
    }

    public void UpdateRaceDayStatus()
    {
        if(LeagueController.Instance == null || RaceManager.Instance == null)
            return;

        
        RaceDayStatus status = GetRaceDayStatus();
        switch (status)
        {
            case RaceDayStatus.CanRace:
                if (startRaceButtonGarage != null && _startRaceButtonText != null)
                {
                     startRaceButtonGarage.interactable = true;
                    _startRaceButtonText.text = RaceManager.Instance.isRaceDay ? "Start Race" : "Practice";
                    break;
                }
                _startRaceButton.SetEnabled(true);
                _startRaceButton.text = RaceManager.Instance.isRaceDay ? "Start Race" : "Practice";
                break;
            case RaceDayStatus.NotInLeague:
                if (startRaceButtonGarage != null && _startRaceButtonText != null)
                {
                    startRaceButtonGarage.interactable = false;
                    _startRaceButtonText.text = "No Race Available";
                    break;
                }
                _startRaceButton.SetEnabled(false);
                _startRaceButton.text = "No Race Available";
                break;
            case RaceDayStatus.NotRaceDay:
            default:
                if (startRaceButtonGarage != null && _startRaceButtonText != null)
                {
                    startRaceButtonGarage.interactable = true;
                    _startRaceButtonText.text = "Practice";
                    break;
                }
                _startRaceButton.SetEnabled(true);
                _startRaceButton.text = "Practice";
                break;
        }
    }

    public RaceDayStatus GetRaceDayStatus()
    {
        if (LeagueController.Instance.currentLeague != null && RaceManager.Instance.isRaceDay)
        {
            if (!LeagueController.Instance.currentLeague.playerHasJoined)
            {
                return RaceDayStatus.NotInLeague;
            }
            else
            {
                return RaceDayStatus.CanRace;
            }
        }
        else
        {
            return RaceDayStatus.NotRaceDay;
        }
    }


    public void OnStartRaceButtonClicked()
    {
        

        if (RaceManager.Instance.waitingForAd == true)
        {
            PlayerStatsView.Instance.DisplayInfo("Waiting for ad to show, please wait...", 3);
            return;
        }

        if (!PlayerManager.Instance.PlayerHasEnoughEnergy(25) && !RaceManager.Instance.isRaceDay)
        {
            PlayerStatsView.Instance.DisplayInfo("You Must have 25 Energy to Race", 3);
            return;
        }

      
        
        if (RaceManager.Instance.isRaceDay)
        {
            int raceCost = LeagueController.Instance.currentLeague != null
                ? LeagueController.Instance.currentLeague.leagueRaceEntryCost
                : 15;
        
            Debug.Log($"Race Cost: {raceCost}");
            
            if (!PlayerManager.Instance.PurchaseItem(raceCost,PurchaseType.RaceEntry))
            {
                PlayerStatsView.Instance.ClearInfo();
                PlayerStatsView.Instance.DisplayInfo(
                    $"Couldn't Afford The Race Entry Fee \n the Gang covered You. You are now in debt. \n IF YOU GO 200 IN DEBT THE GAME WILL BE OVER!!!", 3);
            }
            else
            {
                PlayerStatsView.Instance.DisplayInfo(
                    $"You have paid the {raceCost} entry fee", 3);
            }

        }
        
            GameManager.Instance.StartGame();
            TimeManager.Instance.UpdateTime();
            RaceManager.Instance.StartRace();
            if(uiDoc != null)
                uiDoc.gameObject.SetActive(false);
            if(cameraController)
                cameraController.MoveCameraToPosition(0);
            
       

    }

    public void OnTrainingButtonClicked()
    {
       trainingMenuPrefab.SetActive(true);
        
        if (AudioManager.instance != null)
        {
            AudioManager.instance.gymBagZipUp.start();
        }
    }
    
    public void OnWorkButtonClicked()
    {
      
        
        if (TimeManager.Instance != null)
        {
            isTooLateForActivities = TimeManager.Instance.IsTooLateForActivities();
            
            if (isTooLateForActivities)
            {
                PlayerStatsView.Instance.DisplayInfo("It's too late to work today. Try again tomorrow.", 3);
                return;
            }
        }
        
        if(GameManager.Instance.IsTutorialModeActive() && GameManager.Instance.IsTutorialTaskActive(TutorialTaskType.WorkJobTask))
        {
            if (MiniGames.MiniGameManager.Instance != null)
            {
               
                // Start a random work minigame through the manager
                MiniGames.MiniGameManager.Instance.StartRandomWorkActivity();
                
                // Hide the start menu UI
                if(uiDoc != null)
                    uiDoc.gameObject.SetActive(false);
                
                return;
                // Deduct energy cost
               // PlayerManager.Instance.ModifyPlayerEnergy(-25);
            }
        }
        

        if (PlayerManager.Instance.PlayerHasEnoughEnergy(25) && !isTooLateForActivities )
        {
            // Use MiniGameManager instead of loading separate scenes
            if (MiniGames.MiniGameManager.Instance != null)
            {
                if (GameManager.Instance != null && GameManager.Instance.IsTutorialModeActive())
                {
                    GameManager.Instance.CompleteTutorialTask(TutorialTaskType.WorkJobTask);
                }
                // Start a random work minigame through the manager
                MiniGames.MiniGameManager.Instance.StartRandomWorkActivity();
                
                // Hide the start menu UI
                if(uiDoc != null)
                    uiDoc.gameObject.SetActive(false);
                
                // Deduct energy cost
                PlayerManager.Instance.ModifyPlayerEnergy(-25);
            }
        }
        else
        {
            PlayerStatsView.Instance.DisplayInfo("You Must have 25 Energy to Work", 3);
        }
    }
    public void OnSleepButtonClicked()
    {
        if (GameManager.Instance.CanShowSleepAd())
        {
            AdsManager.Instance.rewardedAds.ShowRewardedAd();
        }
        else
        {
            GameManager.Instance.Sleep(30);
        }
    }
    
    private void OnDisable()
    {
        if(_startRaceButton != null)
          _startRaceButton.clicked -= OnStartRaceButtonClicked;
        if(_workButton != null)
          _workButton.clicked -= OnWorkButtonClicked;
        if(_sleepButton != null)
            _sleepButton.clicked -= OnSleepButtonClicked;
        if(_trainButton != null)
            _trainButton.clicked -= OnTrainingButtonClicked;
        TimeManager.Instance.onNewDay.RemoveListener(UpdateRaceDayStatus);
        LeagueController.Instance.onPlayerJoinedLeague.RemoveListener(UpdateRaceDayStatus);
     
    }

}
