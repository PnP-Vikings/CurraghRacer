using UnityEngine;
using UnityEngine.UIElements;

public class FinishMenu : MonoBehaviour
{
    [SerializeField] private UIDocument uiDoc;
    private Label _pos1, _pos2, _pos3, _pos4, _playerMessage;
    private Button _backButton;
    [SerializeField] private GameObject startingMenuPrefab;

    FMOD.Studio.EventInstance UIClick2;
    FMOD.Studio.EventInstance GarageAmbience;
    public RaceManager raceManager;

    void OnEnable()
    {
        uiDoc = GetComponent<UIDocument>();

        var root = uiDoc.rootVisualElement;

        _backButton = root.Q<Button>("BackButton");

        _pos1 = root.Q<Label>("pos1");
        _pos3 = root.Q<Label>("pos3");
        _pos2 = root.Q<Label>("pos2");
        _pos4 = root.Q<Label>("pos4");

        _playerMessage  = root.Q<Label>("playerMessage");

        _backButton.clicked += OnCloseFinishMenuButtonClicked;
    }


    public void OnCloseFinishMenuButtonClicked()
    {
        startingMenuPrefab.SetActive(true);
        RaceManager.Instance.EndRace();
        uiDoc.gameObject.SetActive(false);

        UIClick2 = FMODUnity.RuntimeManager.CreateInstance("event:/UI/Click 2");
        UIClick2.start();

        GarageAmbience = FMODUnity.RuntimeManager.CreateInstance("event:/Garage/Garage Ambience");
        GarageAmbience.start();
        raceManager.CheeringAndClapping.setParameterByName("Mute Cheering and Clapping", 0f);
    }

    public void UpdatePositions(string pos1, string pos2, string pos3, string pos4)
    {
        _pos1.text = "Position 1: " + pos1;
        _pos2.text = "Position 2: " + pos2;
        _pos3.text = "Position 3: " + pos3;
        _pos4.text = "Position 4: " + pos4;
    }
    
    public void UpdatePlayerMessage(bool isWinner, string message)
    {
        if (isWinner)
        {
            _playerMessage.text = "Congratulations! " + message;
        }
        else
        {
            _playerMessage.text = "Better luck next time! " + message;
        }
    }

    private void OnDisable()
    {
        _backButton.clicked -= OnCloseFinishMenuButtonClicked;
    }
}