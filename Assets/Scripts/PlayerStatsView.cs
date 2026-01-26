using UnityEngine;
using UnityEngine.UIElements;

public class PlayerStatsView : MonoBehaviour
{
    [SerializeField] private UIDocument uiDoc;
    private Label _playerEnergyLabel,_playerCurrencyLabel,_displayInfo;
    private VisualElement _displayInfoBackground;
    
    public static PlayerStatsView Instance { get; private set; }
    
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
        
        if(_displayInfo != null)
        {
            _displayInfo.style.color = Color.white;
        }
    }
    
    void OnEnable()
    {
        uiDoc = GetComponent<UIDocument>();

        var root = uiDoc.rootVisualElement;
        _playerEnergyLabel = root.Q<Label>("PlayerEnergy");
        _playerCurrencyLabel = root.Q<Label>("PlayerCurrency");
        _displayInfo  = root.Q<Label>("DisplayInfo");
        _displayInfoBackground = root.Q<VisualElement>("DisplayInfoBackground");
       _playerEnergyLabel.text = "Player Energy: " + PlayerManager.Instance.GetPlayerEnergy();
        _playerCurrencyLabel.text = "Player Currency: " + PlayerManager.Instance.GetPlayerCurrency();
        
        PlayerManager.Instance.playerStatsView = this; // Set the reference to PlayerStatsView in PlayerManager
        UpdatePlayerStats();
    }
    
    public void UpdatePlayerStats()
    {
        _playerEnergyLabel.text = "Player Energy: " + PlayerManager.Instance.GetPlayerEnergy();
        _playerCurrencyLabel.text = "Player Currency: " + PlayerManager.Instance.GetPlayerCurrency();
    }
        
    
    
    public void DisplayInfo(string info, float duration = 3,Color txtColor = default)
    {
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.IsGameOver())
                return; // Exit if the player is busy
        }
        if(txtColor == default)
        {
            txtColor = Color.white;
        }
        
        _displayInfoBackground.style.display = DisplayStyle.Flex;
        
        if (_displayInfo.text.Length > 0 && _displayInfo.text != info)
        {
            _displayInfo.text += "\n"; // Add a new line if there is existing text
            _displayInfo.text += info;
            _displayInfo.style.color = txtColor;
        }
        else
        {
            _displayInfo.text = info;
            _displayInfo.style.color = txtColor;
        }
       
        Invoke(nameof(ClearInfo), duration);
    }
    
    public void DisplayEndGame(string info, float duration = 3,Color txtColor = default)
    {
     
        if(txtColor == default)
        {
            txtColor = Color.white;
        }
        
        _displayInfoBackground.style.display = DisplayStyle.Flex;
        
        if (_displayInfo.text.Length > 0 && _displayInfo.text != info)
        {
            _displayInfo.text += "\n"; // Add a new line if there is existing text
            _displayInfo.text += info;
            _displayInfo.style.color = txtColor;
        }
        else
        {
            _displayInfo.text = info;
            _displayInfo.style.color = txtColor;
        }
       
        Invoke(nameof(ClearInfo), duration);
    }

    
    public void HideStatsView()
    {
        _playerEnergyLabel.style.display = DisplayStyle.None;
        _playerCurrencyLabel.style.display = DisplayStyle.None;
    }
    
    public void ShowStatsView()
    {
        _playerEnergyLabel.style.display = DisplayStyle.Flex;
        _playerCurrencyLabel.style.display = DisplayStyle.Flex;
    }
    
    public void ClearInfo()
    {
        _displayInfoBackground.style.display = DisplayStyle.None;
        _displayInfo.text = "";
    }
    
   
}
