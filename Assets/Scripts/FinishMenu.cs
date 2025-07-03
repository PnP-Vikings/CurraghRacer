using UnityEngine;
using UnityEngine.UIElements;

public class FinishMenu : MonoBehaviour
{
    [SerializeField] private UIDocument uiDoc;
    private Label _pos1, _pos2, _pos3, _pos4, _playerMessage;
    private Button _backButton;
    [SerializeField] private GameObject startingMenuPrefab;
    
    

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

        FMOD.Studio.EventInstance UIClick2;
        UIClick2 = FMODUnity.RuntimeManager.CreateInstance("event:/UI/Click 2");
        UIClick2.start();

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