using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.Localization;

public class PlayerStatsView : MonoBehaviour
{
    [SerializeField] private PanelRenderer uiDoc;
    private Label _playerEnergyLabel, _playerCurrencyLabel, _displayInfo;
    private VisualElement _displayInfoBackground;
    [SerializeField] private LocalizedString _localizedPlayerEnergyText;
    [SerializeField] private LocalizedString _localizedPlayerCurrencyText;

    [SerializeField] private bool showCurrencyTexts = false;
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

        if (_displayInfo != null)
        {
            _displayInfo.style.color = Color.white;
        }
    }

    private void OnEnable()
    {
        if (uiDoc == null)
        {
            uiDoc = GetComponent<PanelRenderer>();
        }

        if (uiDoc != null)
        {
            uiDoc.RegisterUIReloadCallback(OnUIReload);
        }

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged.AddListener(UpdatePlayerStats);
        }

        if (!showCurrencyTexts)
            HideStatsView();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (uiDoc != null)
        {
            uiDoc.UnregisterUIReloadCallback(OnUIReload);
        }

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged.RemoveListener(UpdatePlayerStats);
        }
    }

    private void OnUIReload(PanelRenderer renderer, VisualElement root)
    {
        if (root == null) return;

        _playerEnergyLabel = root.Q<Label>("PlayerEnergy");
        _playerCurrencyLabel = root.Q<Label>("PlayerCurrency");
        _displayInfo = root.Q<Label>("DisplayInfo");
        _displayInfoBackground = root.Q<VisualElement>("DisplayInfoBackground");

        if (PlayerManager.Instance != null)
        {
            _localizedPlayerEnergyText.Arguments = new object[] { PlayerManager.Instance.GetPlayerEnergy() };
            _localizedPlayerCurrencyText.Arguments = new object[] { Math.Round(PlayerManager.Instance.GetPlayerCurrency(), 2) };
            _localizedPlayerEnergyText.RefreshString();
            if (_playerEnergyLabel != null)
                _playerEnergyLabel.text = _localizedPlayerEnergyText.GetLocalizedString();
            _localizedPlayerCurrencyText.RefreshString();
            if (_playerCurrencyLabel != null)
                _playerCurrencyLabel.text = _localizedPlayerCurrencyText.GetLocalizedString();
            PlayerManager.Instance.playerStatsView = this;
        }

        UpdatePlayerStats();
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (uiDoc != null)
        {
            uiDoc.enabled = scene.name != "Main Menu";
        }
    }

    public void UpdatePlayerStats()
    {
        if (PlayerManager.Instance == null)
        {
            Debug.LogWarning("PlayerManager instance is null. Cannot update player stats.");
            return;
        }

        if (_localizedPlayerEnergyText == null || _localizedPlayerCurrencyText == null)
            return;

        if (_localizedPlayerEnergyText.Arguments != null && _localizedPlayerEnergyText.Arguments.Count > 0)
        {
            _localizedPlayerEnergyText.Arguments[0] = PlayerManager.Instance.GetPlayerEnergy();
            _localizedPlayerEnergyText.RefreshString();
        }

        if (_localizedPlayerCurrencyText.Arguments != null && _localizedPlayerCurrencyText.Arguments.Count > 0)
        {
            _localizedPlayerCurrencyText.Arguments[0] = Math.Round(PlayerManager.Instance.GetPlayerCurrency(), 2);
            _localizedPlayerCurrencyText.RefreshString();
        }

        if (_playerEnergyLabel != null)
            _playerEnergyLabel.text = _localizedPlayerEnergyText.GetLocalizedString();

        if (_playerCurrencyLabel != null)
            _playerCurrencyLabel.text = _localizedPlayerCurrencyText.GetLocalizedString();
    }

    public void DisplayInfo(string info, float duration = 3, Color txtColor = default)
    {
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.IsGameOver())
                return; // Exit if the player is busy
        }
        if (txtColor == default)
        {
            txtColor = Color.white;
        }

        if (_displayInfoBackground != null)
            _displayInfoBackground.style.display = DisplayStyle.Flex;

        if (_displayInfo != null)
        {
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
        }

        CancelInvoke(nameof(ClearInfo));
        Invoke(nameof(ClearInfo), duration);
    }

    public void DisplayEndGame(string info, float duration = 3, Color txtColor = default)
    {
        if (txtColor == default)
        {
            txtColor = Color.white;
        }

        if (_displayInfoBackground != null)
            _displayInfoBackground.style.display = DisplayStyle.Flex;

        if (_displayInfo != null)
        {
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
        }

        CancelInvoke(nameof(ClearInfo));
        Invoke(nameof(ClearInfo), duration);
    }

    public void HideStatsView()
    {
        if (_playerEnergyLabel != null)
            _playerEnergyLabel.style.display = DisplayStyle.None;
        if (_playerCurrencyLabel != null)
            _playerCurrencyLabel.style.display = DisplayStyle.None;
    }

    public void ShowStatsView()
    {
        if (!showCurrencyTexts)
        {
            HideStatsView();
            return;
        }
        if (_playerEnergyLabel != null)
            _playerEnergyLabel.style.display = DisplayStyle.Flex;
        if (_playerCurrencyLabel != null)
            _playerCurrencyLabel.style.display = DisplayStyle.Flex;
    }

    public void ClearInfo()
    {
        if (_displayInfoBackground != null)
            _displayInfoBackground.style.display = DisplayStyle.None;
        if (_displayInfo != null)
            _displayInfo.text = "";
    }
    
    
}
