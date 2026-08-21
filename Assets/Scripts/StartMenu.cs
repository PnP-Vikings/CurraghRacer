using System.Collections;
using System.Collections.Generic;
using Calendar;
using DG.Tweening;
using League;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class StartMenu : MonoBehaviour
{
    [SerializeField] private UIDocument uiDoc;
    private Button _startRaceButton,_trainButton,_workButton,_sleepButton;
    [SerializeField] private UnityEngine.UI.Button startRaceButtonGarage;
    [SerializeField] private TMPro.TMP_Text _startRaceButtonText;
    [SerializeField] CameraController cameraController;
    [SerializeField] UnityEngine.UI.Button closeButton;
    public GameObject trainingMenuPrefab;
    public bool isTooLateForActivities = false;
    public static StartMenu Instance { get; private set; }
    
    [Header("")]
    [SerializeField] private int energyCostForRace = 25;
    [SerializeField] private int energyCostForWork = 25;
    
    [Header("Localization")]
    [SerializeField] private string startRaceText="Start Race";
    [SerializeField] private string practiceRaceText="Practice";
    [SerializeField] private LocalizedString _localizedRaceText = new LocalizedString { TableReference = "GarageScene", TableEntryReference = "Garage.BulletinBoard.Preview.RaceText.Race" };
    [SerializeField] private LocalizedString _localizedPracticeText = new LocalizedString { TableReference = "GarageScene", TableEntryReference = "Garage.BulletinBoard.Preview.RaceText.Practice" };
    [SerializeField] private LocalizedString _localizedNoRaceAvailableText = new LocalizedString { TableReference = "GarageScene", TableEntryReference = "Garage.BulletinBoard.RaceButton.RaceText.NoRaceAvailable" };
    [SerializeField] private LocalizedString localizedWaitingOnAd = new LocalizedString { TableReference = "StartMenu", TableEntryReference = "StartMenu.waitingOnAd" };
    [SerializeField] private LocalizedString localizedYouMustHaveEnergyToRace = new LocalizedString { TableReference = "StartMenu", TableEntryReference = "StartMenu.YouMustHaveEnergyToRace" };
    [SerializeField] private LocalizedString localizedCouldntAffordRaceFee = new LocalizedString { TableReference = "StartMenu", TableEntryReference = "StartMenu.CouldntAffordRaceFee" };
    [SerializeField] private LocalizedString localizedYouMustHaveEntryFee = new LocalizedString { TableReference = "StartMenu", TableEntryReference = "StartMenu.YouMustHaveEntryFee" };
    [SerializeField] private LocalizedString localizedTooLateToWorkToday = new LocalizedString { TableReference = "StartMenu", TableEntryReference = "StartMenu.TooLateToWorkToday" };
    [SerializeField] private LocalizedString localizedCompleteTutorialFirst = new LocalizedString { TableReference = "StartMenu", TableEntryReference = "StartMenu.CompleteTutorialFirst" };
    [SerializeField] private LocalizedString localizedYouMustHaveEnergyToWork = new LocalizedString { TableReference = "StartMenu", TableEntryReference = "StartMenu.YouMustHaveEnergyToWork" };
    
    
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

        TryGetLocalizedStrings();
        
        UpdateRaceDayStatus();
        
        TimeManager.Instance.onNewDay.AddListener(CloseBulletinBoard);
        TimeManager.Instance.onNewDay.AddListener(UpdateRaceDayStatus); 
        LeagueController.Instance.onPlayerJoinedLeague.AddListener(UpdateRaceDayStatus);
    }
    
    public enum RaceDayStatus
    {
        CanRace,
        NotInLeague,
        NotRaceDay
    }

    public void TryGetLocalizedStrings()
    {
        if (!_localizedRaceText.IsEmpty)
        {
            startRaceText = _localizedRaceText.GetLocalizedString();
        }
        else
        {
            startRaceText = "Start Race";
        }


        if (!_localizedPracticeText.IsEmpty)
        {
            practiceRaceText = _localizedPracticeText.GetLocalizedString();
        }
        else
        {
            practiceRaceText = "Practice";
        }
    }
    public void UpdateRaceDayStatus()
    {
        if(LeagueController.Instance == null || RaceManager.Instance == null)
            return;
        DOVirtual.DelayedCall(0.1f, () =>
        {
           
    
        RaceDayStatus status = GetRaceDayStatus();
        TryGetLocalizedStrings();
        switch (status)
        {
            case RaceDayStatus.CanRace:
                if (startRaceButtonGarage != null && _startRaceButtonText != null)
                {
                     startRaceButtonGarage.interactable = true;
                    _startRaceButtonText.text = RaceManager.Instance.isRaceDay ? startRaceText : practiceRaceText;
                    break;
                }
                _startRaceButton.SetEnabled(true);
                _startRaceButton.text = RaceManager.Instance.isRaceDay ? startRaceText : practiceRaceText;
                break;
            case RaceDayStatus.NotInLeague:
                if (startRaceButtonGarage == null || _startRaceButtonText == null)
                    return;
                startRaceButtonGarage.interactable = false;
                if(_localizedNoRaceAvailableText != null && !_localizedNoRaceAvailableText.IsEmpty)
                {
                    _startRaceButtonText.text = _localizedNoRaceAvailableText.GetLocalizedString();
                }
                else
                {
                    _startRaceButtonText.text = "No Race Available";
                }
                _startRaceButton.SetEnabled(false);
                break;

            case RaceDayStatus.NotRaceDay:
            default:
                if (startRaceButtonGarage != null && _startRaceButtonText != null)
                {
                    startRaceButtonGarage.interactable = true;
                    _startRaceButtonText.text = practiceRaceText;
                    break;
                }
                _startRaceButton.SetEnabled(true);
                _startRaceButton.text = practiceRaceText;
                break;
        }
        });
    }

    public RaceDayStatus GetRaceDayStatus()
    {
        if (LeagueController.Instance.currentLeague != null && (RaceManager.Instance.isRaceDay && !RaceManager.Instance.hasPlayerCompletedRace))
        {
            return LeagueController.Instance.currentLeague.playerHasJoined ? RaceDayStatus.CanRace : RaceDayStatus.NotInLeague;
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
            string waitingMessage = localizedWaitingOnAd != null && !localizedWaitingOnAd.IsEmpty
                ? localizedWaitingOnAd.GetLocalizedString()
                : "Waiting for ad to show, please wait...";
            PlayerStatsView.Instance.DisplayInfo(waitingMessage, 3);
            return;
        }

        if (!PlayerManager.Instance.PlayerHasEnoughEnergy(energyCostForRace) && !RaceManager.Instance.isRaceDay)
        {
            string energyMessage = $"You Must have {energyCostForRace} Energy";
            if (localizedYouMustHaveEnergyToRace != null && !localizedYouMustHaveEnergyToRace.IsEmpty)
            {
                localizedYouMustHaveEnergyToRace.Arguments = new object[] { energyCostForRace };
                localizedYouMustHaveEnergyToRace.Arguments[0] = energyCostForRace;
                localizedYouMustHaveEnergyToRace.RefreshString();
                energyMessage = localizedYouMustHaveEnergyToRace.GetLocalizedString();
            }
            PlayerStatsView.Instance.DisplayInfo(energyMessage, 3);
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
                
                float maxDept = 400;
                if(PlayerManager.Instance != null)
                {
                    maxDept = PlayerManager.Instance.maxAmountOfDebt;
                }
                string debtMessage = $"Couldn't Afford The Race Entry Fee \n the Gang covered You. You are now in debt. \n IF YOU GO {maxDept} IN DEBT THE GAME WILL BE OVER!!!";
                if (localizedCouldntAffordRaceFee != null && !localizedCouldntAffordRaceFee.IsEmpty && PlayerManager.Instance != null)
                {
                    localizedCouldntAffordRaceFee.Arguments = new object[] { PlayerManager.Instance.maxAmountOfDebt };
                    localizedCouldntAffordRaceFee.Arguments[0] = PlayerManager.Instance.maxAmountOfDebt;
                    localizedCouldntAffordRaceFee.RefreshString();
                    debtMessage = localizedCouldntAffordRaceFee.GetLocalizedString();
                }
                PlayerStatsView.Instance.DisplayInfo(debtMessage, 3);
            }
            else
            {
                string entryFeeMessage = $"You have paid the {raceCost} entry fee";
                if (localizedYouMustHaveEntryFee != null && !localizedYouMustHaveEntryFee.IsEmpty)
                {
                    localizedYouMustHaveEntryFee.Arguments = new object[] { raceCost };
                    localizedYouMustHaveEntryFee.Arguments[0] = raceCost;
                    localizedYouMustHaveEntryFee.RefreshString();
                    entryFeeMessage = localizedYouMustHaveEntryFee.GetLocalizedString();
                }
                PlayerStatsView.Instance.DisplayInfo(entryFeeMessage, 3);
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
                string tooLateMessage = localizedTooLateToWorkToday != null && !localizedTooLateToWorkToday.IsEmpty
                    ? localizedTooLateToWorkToday.GetLocalizedString()
                    : "It's too late to work today. Try again tomorrow.";
                PlayerStatsView.Instance.DisplayInfo(tooLateMessage, 3);
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
        else if(GameManager.Instance.IsTutorialModeActive() && !GameManager.Instance.IsTutorialTaskActive(TutorialTaskType.WorkJobTask) && !GameManager.Instance.IsTutorialTaskCompleted(TutorialTaskType.WorkJobTask))
        {
            PlayerStatsView.Instance.ClearInfo();
            string completeTutorialMessage = localizedCompleteTutorialFirst != null && !localizedCompleteTutorialFirst.IsEmpty
                ? localizedCompleteTutorialFirst.GetLocalizedString()
                : "You Must complete the current tutorial task first";
            PlayerStatsView.Instance.DisplayInfo(completeTutorialMessage, 1.5f);
            return;
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
            string workCostMessage= $"You Must have {energyCostForWork} Energy to Work";
            if (localizedYouMustHaveEnergyToWork != null && !localizedYouMustHaveEnergyToWork.IsEmpty)
            {
                localizedYouMustHaveEnergyToWork.Arguments = new object[] { energyCostForWork };
                localizedYouMustHaveEnergyToWork.Arguments[0] = energyCostForWork;
                localizedYouMustHaveEnergyToWork.RefreshString();
                workCostMessage = localizedYouMustHaveEnergyToWork.GetLocalizedString();
            }
            PlayerStatsView.Instance.DisplayInfo(workCostMessage, 3);
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
            if(GameManager.Instance != null && GameManager.Instance.IsTutorialModeActive() && !GameManager.Instance.IsTutorialTaskActive(TutorialTaskType.SleepTask))
            {
                PlayerStatsView.Instance.ClearInfo();
                string completeTutorialMessage = localizedCompleteTutorialFirst != null && !localizedCompleteTutorialFirst.IsEmpty
                    ? localizedCompleteTutorialFirst.GetLocalizedString()
                    : "You Must complete the current tutorial task first";
                PlayerStatsView.Instance.DisplayInfo(completeTutorialMessage, 1.5f);
            }
            else
            {
                GameManager.Instance.Sleep(30);
            }
          
        }
    }
    
    
    public void CloseBulletinBoard()
    {
        if (closeButton != null)
        {
            closeButton.onClick.Invoke();
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
