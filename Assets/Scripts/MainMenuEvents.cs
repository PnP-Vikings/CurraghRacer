using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
public class MainMenuEvents : MonoBehaviour
{
    private UIDocument uiDocument;
    private Button _button;
    private List<Button> _buttons = new List<Button>();
    public GameObject _gameUi,playerStatsView;
    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        _button = uiDocument.rootVisualElement.Q<Button>("StartGameButton");
        _button.RegisterCallback<ClickEvent>(e => OnStartButtonClicked());
        
        
        // Find all buttons in the UI and add them to the list
        _buttons = uiDocument.rootVisualElement.Query<Button>().ToList();
        
        foreach (var button in _buttons)
        {
            // Register a callback for each button
            button.RegisterCallback<ClickEvent>(e => OnButtonClicked(button));
        }
    }
    
    private void OnButtonClicked(Button button)
    {
        Debug.Log($"Button {button.name} clicked");
        // Add your logic for button click here
    }
    
    private void OnStartButtonClicked()
    {
        Debug.Log("Start Game Button Clicked");

        FMOD.Studio.EventInstance UIClick1;
        UIClick1 = FMODUnity.RuntimeManager.CreateInstance("event:/UI/Click 1");
        UIClick1.start();

        if (_gameUi != null)
        {
            _gameUi.SetActive(true);
        }
        if(playerStatsView != null)
        {
            playerStatsView.SetActive(true);
        }
        
        AdsManager.Instance.bannerAds.HideBannerAd();
        
        this.gameObject.SetActive(false);
        
      
    }
    
    private void onDisable()
    {
        // Unregister the callback when the object is disabled
        _button.UnregisterCallback<ClickEvent>(e => OnStartButtonClicked());
        foreach (var button in _buttons)
        {
            button.UnregisterCallback<ClickEvent>(e => OnButtonClicked(button));
        }
    }
    
}
