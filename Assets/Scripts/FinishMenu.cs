using FMOD.Studio;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

public class FinishMenu : MonoBehaviour
{
    [SerializeField] private UIDocument uiDoc;
    private Label header,_pos1, _pos2, _pos3, _pos4, _playerMessage;
    private Button _backButton;
    [SerializeField] private GameObject startingMenuPrefab;
    [SerializeField] private LocalizedString _localizedBackButtonText = new LocalizedString { TableReference = "RaceScene", TableEntryReference = "BackButtonTxt" };
    [SerializeField] private LocalizedString _localizedPositionText = new LocalizedString { TableReference = "RaceScene", TableEntryReference = "PositionTxt" };
    [SerializeField] private LocalizedString _localizedCongratulationsText = new LocalizedString { TableReference = "RaceScene", TableEntryReference = "CongratulationsTxt" };
    [SerializeField] private LocalizedString _localizedBetterLuckText = new LocalizedString { TableReference = "RaceScene", TableEntryReference = "BetterLuckTxt" };
    [SerializeField] private LocalizedString _localizedFinishResultsHeaderText = new LocalizedString { TableReference = "RaceScene", TableEntryReference = "FinishResults.HeaderTxt" };

    void OnEnable()
    {
        uiDoc = GetComponent<UIDocument>();

        var root = uiDoc.rootVisualElement;

        _backButton = root.Q<Button>("BackButton");
        header = root.Q<Label>("Header");
        _pos1 = root.Q<Label>("pos1");
        _pos3 = root.Q<Label>("pos3");
        _pos2 = root.Q<Label>("pos2");
        _pos4 = root.Q<Label>("pos4");

        if(!_localizedFinishResultsHeaderText.IsEmpty)
        {
            header.text = _localizedFinishResultsHeaderText.GetLocalizedString();
        }
        else
        {
            header.text = "Finish Results";
        }

        _playerMessage  = root.Q<Label>("playerMessage");

        _backButton.clicked += OnCloseFinishMenuButtonClicked;
        if(!_localizedBackButtonText.IsEmpty)
        {
            _backButton.text = _localizedBackButtonText.GetLocalizedString();
        }
        else
        {
            _backButton.text = "Back";
        }
    }

    public void OnCloseFinishMenuButtonClicked()
    {
       // startingMenuPrefab.SetActive(true);
        
      // uiDoc.gameObject.SetActive(false);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.UIClick2.start();
            AudioManager.instance.raceAmbience.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.raceLost.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.raceWon.stop(STOP_MODE.ALLOWFADEOUT);
        }

        RaceManager.Instance.EndRace();
    }

    public void UpdatePositions(string pos1, string pos2, string pos3, string pos4)
    {
        if(!_localizedPositionText.IsEmpty)
        {
            _pos1.text = _localizedPositionText.GetLocalizedString() + " 1: " + pos1;
            _pos2.text = _localizedPositionText.GetLocalizedString() + " 2: " + pos2;
            _pos3.text = _localizedPositionText.GetLocalizedString() + " 3: " + pos3;
            _pos4.text = _localizedPositionText.GetLocalizedString() + " 4: " + pos4;
        }
        else
        {
            _pos1.text = "Position 1: " + pos1;
            _pos2.text = "Position 2: " + pos2;
            _pos3.text = "Position 3: " + pos3;
            _pos4.text = "Position 4: " + pos4;
        }
    }
    
    public void UpdatePlayerMessage(bool isWinner, string message)
    {
        if (isWinner)
        {
            if(!_localizedCongratulationsText.IsEmpty)
            {
                _playerMessage.text = _localizedCongratulationsText.GetLocalizedString() + " " + message;
            }
            else
            {
                _playerMessage.text = "Congratulations! " + message;
            }
        }
        else
        {
            if(!_localizedBetterLuckText.IsEmpty)
            {
                _playerMessage.text = _localizedBetterLuckText.GetLocalizedString() + " " + message;
            }
            else
            {
                _playerMessage.text = "Better luck next time! " + message;
            }
            if (AudioManager.instance != null)
            {
                AudioManager.instance.raceAmbience.setParameterByName("Encouragement Volume", 0f);
                AudioManager.instance.raceLost.start();
            }
            if (RaceManager.Instance.isRaceDay)
            {
                if (RadioManager.instance)
                {
                        RadioManager.instance.hasJustLostRace = true;
                        Debug.Log("Player Lost Race - AudioDebug");
                }
            }
        }
        AudioManager.instance.rowing.stop(STOP_MODE.ALLOWFADEOUT);
    }

    private void OnDisable()
    {
        _backButton.clicked -= OnCloseFinishMenuButtonClicked;
    }
}