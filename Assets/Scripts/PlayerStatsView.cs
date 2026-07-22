using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.Localization;
public class PlayerStatsView : MonoBehaviour
{
    [SerializeField] private UIDocument uiDoc;
    private Label _playerEnergyLabel,_playerCurrencyLabel,_displayInfo;
    private VisualElement _displayInfoBackground;
    [SerializeField] private LocalizedString _localizedPlayerEnergyText;
    [SerializeField] private LocalizedString _localizedPlayerCurrencyText;
    
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
        
        _localizedPlayerEnergyText.Arguments = new object[] { PlayerManager.Instance.GetPlayerEnergy() };
        _localizedPlayerCurrencyText.Arguments = new object[] { PlayerManager.Instance.GetPlayerCurrency() };
        _localizedPlayerEnergyText.RefreshString();
        _playerEnergyLabel.text =_localizedPlayerEnergyText.GetLocalizedString();
        _localizedPlayerCurrencyText.RefreshString();
        _playerCurrencyLabel.text = _localizedPlayerCurrencyText.GetLocalizedString();
        
        PlayerManager.Instance.playerStatsView = this; // Set the reference to PlayerStatsView in PlayerManager
        UpdatePlayerStats();
        
        if(LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged.AddListener(UpdatePlayerStats);
        }
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if(LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged.RemoveListener(UpdatePlayerStats);
        }
    }
    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SceneManager.GetActiveScene().name == "Main Menu")
        {
            uiDoc.rootVisualElement.style.display = DisplayStyle.None;
        }
        else
        {
            uiDoc.rootVisualElement.style.display = DisplayStyle.Flex;
        }
    }
    
    public void UpdatePlayerStats()
    {
        _localizedPlayerEnergyText.Arguments[0] = PlayerManager.Instance.GetPlayerEnergy();
        _localizedPlayerCurrencyText.Arguments[0] = PlayerManager.Instance.GetPlayerCurrency();
        _localizedPlayerEnergyText.RefreshString();
        _localizedPlayerCurrencyText.RefreshString();
        _playerEnergyLabel.text = _localizedPlayerEnergyText.GetLocalizedString();
        _playerCurrencyLabel.text = _localizedPlayerCurrencyText.GetLocalizedString();
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
