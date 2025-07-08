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
    }

    public void OnStartRaceButtonClicked()
    {
        if (RaceManager.Instance.waitingForAd == true)
        {
            PlayerStatsView.Instance.DisplayInfo("Waiting for ad to show, please wait...", 3);
            return;
        }

        if (!PlayerManager.Instance.playerHasEnoughEnergy(50))
        {
            PlayerStatsView.Instance.DisplayInfo("You Must have 50 Energy to Race", 3);
            return;
        }
        
            GameManager.Instance.StartGame();
            RaceManager.Instance.SpawnShips();
            uiDoc.gameObject.SetActive(false);
            cameraController.MoveCameraToPosition(0);
            PlayerManager.Instance.ModifyPlayerEnergy(-50);
       

    }

    public void OnTrainingButtonClicked()
    {
       trainingMenuPrefab.SetActive(true);
        
        GymBagZipUp = FMODUnity.RuntimeManager.CreateInstance("event:/Training/Gym Bag Zip Up");
        GymBagZipUp.start();

    }
    
    public void OnWorkButtonClicked()
    {
        if (PlayerManager.Instance.playerHasEnoughEnergy(25))
        {
            int randomValue = Random.Range(0, GameManager.Instance.miniGameWorkScenes.Count);
            string selectedScene = GameManager.Instance.miniGameWorkScenes[randomValue];
            
            SceneManager.LoadScene(selectedScene);
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
    }

}
