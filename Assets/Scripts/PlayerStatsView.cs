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
        
        
    }
    
    public void UpdatePlayerStats()
    {
        _playerEnergyLabel.text = "Player Energy: " + PlayerManager.Instance.GetPlayerEnergy();
        _playerCurrencyLabel.text = "Player Currency: " + PlayerManager.Instance.GetPlayerCurrency();
    }
        
    
    public void DisplayInfo(string info, float duration = 3)
    {
        _displayInfoBackground.style.display = DisplayStyle.Flex;
        
        if (_displayInfo.text.Length > 0 && _displayInfo.text != info)
        {
            _displayInfo.text += "\n"; // Add a new line if there is existing text
            _displayInfo.text += info;
        }
        else
        {
            _displayInfo.text = info;
        }
       
        Invoke(nameof(ClearInfo), duration);
    }
    
    public void ClearInfo()
    {
        _displayInfoBackground.style.display = DisplayStyle.None;
        _displayInfo.text = "";
    }
    
   
}
