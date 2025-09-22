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
    [SerializeField] CameraController cameraController;
    public GameObject trainingMenuPrefab;

    FMOD.Studio.EventInstance GymBagZipUp;
    
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

        var root = uiDoc.rootVisualElement;
        _startRaceButton = root.Q<Button>("StartRaceButton");
        _trainButton = root.Q<Button>("TrainingButton");
        _workButton = root.Q<Button>("WorkButton");
        _sleepButton = root.Q<Button>("SleepButton");
        

        _startRaceButton.clicked += OnStartRaceButtonClicked;
        _trainButton.clicked += OnTrainingButtonClicked;
        _workButton.clicked += OnWorkButtonClicked;
        _sleepButton.clicked +=OnSleepButtonClicked;
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
        RaceDayStatus status = GetRaceDayStatus();
        switch (status)
        {
            case RaceDayStatus.CanRace:
                _startRaceButton.SetEnabled(true);
                _startRaceButton.text = RaceManager.Instance.isRaceDay ? "Start Race" : "Practice";
                break;
            case RaceDayStatus.NotInLeague:
                _startRaceButton.SetEnabled(false);
                _startRaceButton.text = "No Race Available";
                
                StartCoroutine(ShowLeagueJoinMessageAfterDelay(16f));
                break;
            case RaceDayStatus.NotRaceDay:
            default:
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

        if (!PlayerManager.Instance.PlayerHasEnoughEnergy(25))
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
            uiDoc.gameObject.SetActive(false);
            cameraController.MoveCameraToPosition(0);
            
       

    }

    private System.Collections.IEnumerator ShowLeagueJoinMessageAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds/3);
        PlayerStatsView.Instance.DisplayInfo("You are not in a league, join a league to proceed.", 3);
        
        yield return new WaitForSeconds(delaySeconds);
        LeagueController.Instance.ShowLeagueInvite();
    }
    public void OnTrainingButtonClicked()
    {
       trainingMenuPrefab.SetActive(true);
        
        GymBagZipUp = FMODUnity.RuntimeManager.CreateInstance("event:/Training/Gym Bag Zip Up");
        GymBagZipUp.start();
        

    }
    
    public void OnWorkButtonClicked()
    {
        if (PlayerManager.Instance.PlayerHasEnoughEnergy(25))
        {
            // Use MiniGameManager instead of loading separate scenes
            if (MiniGames.MiniGameManager.Instance != null)
            {
                // Start a random work minigame through the manager
                MiniGames.MiniGameManager.Instance.StartRandomWorkActivity();
                
                // Hide the start menu UI
                uiDoc.gameObject.SetActive(false);
                
                // Deduct energy cost
                PlayerManager.Instance.ModifyPlayerEnergy(-25);
            }
            else
            {
                // Fallback to old system if MiniGameManager not available
                int randomValue = Random.Range(0, GameManager.Instance.miniGameWorkScenes.Count);
                string selectedScene = GameManager.Instance.miniGameWorkScenes[randomValue];
                SceneManager.LoadScene(selectedScene);
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
        _startRaceButton.clicked -= OnStartRaceButtonClicked;
        _trainButton.clicked -= OnTrainingButtonClicked;
        TimeManager.Instance.onNewDay.RemoveListener(UpdateRaceDayStatus);
        LeagueController.Instance.onPlayerJoinedLeague.RemoveListener(UpdateRaceDayStatus);
     
    }

}
