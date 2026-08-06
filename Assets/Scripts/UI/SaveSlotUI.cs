using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class SaveSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Button saveButton;
    public Button startNewGameSaveButton;
    public Button loadButton;
    public Button deleteButton;
    public TMP_Text slotIndexText;
    public TMP_Text saveNameText;
    public TMP_Text saveDateText;
    public TMP_Text playTimeText;
    public TMP_Text playerStatsText;
    public TMP_Text leagueInfoText;
    public GameObject emptySlotIndicator;
    public GameObject filledSlotContainer;
    public TMP_InputField saveNameInputField;
    public MoreSavesTutorialPrompt moreSavesTutorialPrompt;
    
    
    [Header("Slot Data")]
    public int slotIndex;
    private SaveSlotInfo slotInfo;
    private SaveMenuUI parentMenu;

    [Header("Localization")]
    [SerializeField] private LocalizedString _localizedEmptySlotText;
    [SerializeField] private LocalizedString _localizedEnergySlotText;
    [SerializeField] private LocalizedString _localizedPlayTimeText;
    [SerializeField] private LocalizedString _localizedNoLeagueText;
    [SerializeField] private LocalizedString _localizedJoinedText;
    [SerializeField] private LocalizedString _localizedNotJoinedText;
    [SerializeField] private LocalizedString _localizedSlotIndexText;
    [SerializeField] private LocalizedString _localizedDefaultSaveNameText;
    [SerializeField] private LocalizedString _localizedOnGameSavedText;
    [SerializeField] private LocalizedString _localizedFailedToSaveText;
    [SerializeField] private LocalizedString _localizedFailedToLoadSaveNotAvailableText;
    [SerializeField] private LocalizedString _localizedGameLoadedText;
    [SerializeField] private LocalizedString _localizedFailedToLoadSystemUnavailableText;
    [SerializeField] private LocalizedString _localizedFailedToLoadText;
    [SerializeField] private LocalizedString _localizedDeleteAreYouSureText;
    [SerializeField] private LocalizedString _localizedDeleteSuccessText;
    [SerializeField] private LocalizedString _localizedDeleteFailedText;
    
    
    
    
    public void Initialize(int index, SaveSlotInfo info, SaveMenuUI menu)
    {
        slotIndex = index;
        slotInfo = info;
        parentMenu = menu;
        if(menu != null&& menu.moreSavesTutorialPrompt != null)
            moreSavesTutorialPrompt = menu.moreSavesTutorialPrompt;
        
        RefreshDisplay();
        
        // Setup button events
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveClicked);
        if (loadButton != null)
            loadButton.onClick.AddListener(OnLoadClicked);
        if (deleteButton != null)
            deleteButton.onClick.AddListener(OnDeleteClicked);
        if (startNewGameSaveButton != null)
            startNewGameSaveButton.onClick.AddListener(OnStartNewGameSaveClicked);
            
    }

    
    
    private void OnEnable()
    {
        RefreshDisplay();
    }
    public void RefreshDisplay()
    {
        // Update slot index display
        if (slotIndexText != null)
        {
            if (!_localizedSlotIndexText.IsEmpty)
            {
                _localizedSlotIndexText.Arguments = new object[] { slotIndex + 1 };
                _localizedSlotIndexText.RefreshString();
                slotIndexText.text = _localizedSlotIndexText.GetLocalizedString();
            }
            else
            {
                slotIndexText.text = $"Slot {slotIndex + 1}";
            }
        }
        bool hasData = slotInfo != null && slotInfo.exists && slotInfo.saveData != null;
        
        // Show/hide appropriate UI elements
        if (emptySlotIndicator != null)
            emptySlotIndicator.SetActive(!hasData);
        if (filledSlotContainer != null)
            filledSlotContainer.SetActive(hasData);
        
        // Enable/disable buttons based on slot state
        if (saveButton != null)
            saveButton.interactable = SaveSystem.Instance.WasLoadedFromSave || SaveSystem.Instance.IsNewGame; // Always allow saving
        if (loadButton != null)
            loadButton.interactable = hasData ;
        if (deleteButton != null)
            deleteButton.interactable = hasData;
        if (startNewGameSaveButton != null)
        {
            startNewGameSaveButton.gameObject.SetActive(!SaveSystem.Instance.WasLoadedFromSave); // Always allow starting new game
            startNewGameSaveButton.gameObject.SetActive(!hasData); // Only show if slot is empty
        }
        if (hasData)
        {
            DisplaySaveData(slotInfo.saveData);
        }
        else
        {
            ClearDisplay();
        }
    }
    
    private void DisplaySaveData(SaveData saveData)
    {
        // Display save name
        if (saveNameText != null)
        {
            saveNameText.text = string.IsNullOrEmpty(saveData.saveName) ? 
                $"Save {slotIndex + 1}" : saveData.saveName;
        }
        
        // Display save date
        if (saveDateText != null)
        {
            /*if (DateTime.TryParse(saveData.saveDate, out DateTime saveDate))
            {
                saveDateText.text = saveDate.ToString("MMM dd, yyyy HH:mm");
            }
            else
            {
                saveDateText.text = "Unknown Date";
            }*/
            if (DateTime.TryParse(saveData.saveDate, out DateTime saveDate))
            {
                var culture = LocalizationSettings.SelectedLocale?.Identifier.CultureInfo;
                saveDateText.text = saveDate.ToString("g", culture); // localized short date+time
            }
            else
            {
                saveDateText.text = "Unknown Date";
            }
        }
        
        // Display play time
        if (playTimeText != null)
        {
            TimeSpan time = TimeSpan.FromMinutes(saveData.playTime);
            string formattedTime = $"{time.Hours:D2}:{time.Minutes:D2}";
            if (!_localizedPlayTimeText.IsEmpty)
            {
                _localizedPlayTimeText.Arguments = new object[] { formattedTime };
                _localizedPlayTimeText.RefreshString();
                playTimeText.text = _localizedPlayTimeText.GetLocalizedString();
            }
            else
            {
                playTimeText.text = $"Play Time: {formattedTime}";
            }
        }
        
        // Display player stats
        if (playerStatsText != null && saveData.playerData != null)
        {
            int energyString=  (int)saveData.playerData.energy;
            int coinsString= (int)saveData.playerData.coins;
            if(!_localizedEnergySlotText.IsEmpty)
            {
                _localizedEnergySlotText.Arguments = new object[] {
                    energyString,
                    coinsString
                };
                _localizedEnergySlotText.Arguments[0] = energyString;
                _localizedEnergySlotText.Arguments[1] = coinsString;
                _localizedEnergySlotText.RefreshString();
                playerStatsText.text = _localizedEnergySlotText.GetLocalizedString();
            }
            else
            {
                playerStatsText.text = $"Energy: {saveData.playerData.energy:F0} | Coins: {saveData.playerData.coins:F0}";
            }
          
        }
        
        // Display league info
        if (leagueInfoText != null && saveData.leagueData != null)
        {
            string leagueName ="";
            string joinStatus ="";
            if(!_localizedNoLeagueText.IsEmpty && !_localizedJoinedText.IsEmpty && !_localizedNotJoinedText.IsEmpty)
            {
                 leagueName = string.IsNullOrEmpty(saveData.leagueData.currentLeagueName) ? _localizedNoLeagueText.GetLocalizedString() : saveData.leagueData.currentLeagueName;
                 joinStatus = saveData.leagueData.playerHasJoined ? _localizedJoinedText.GetLocalizedString() : _localizedNotJoinedText.GetLocalizedString();
            }
            else
            {

                 leagueName = string.IsNullOrEmpty(saveData.leagueData.currentLeagueName) ? "No League" : saveData.leagueData.currentLeagueName;
                 joinStatus = saveData.leagueData.playerHasJoined ? "Joined" : "Not Joined";
                
            }
            leagueInfoText.text = $"{leagueName} ({joinStatus})";
        }
    }
    
    
    
    
    private void ClearDisplay()
    {
        if (saveNameText != null)
            if (!_localizedEmptySlotText.IsEmpty)
            {
                saveNameText.text = _localizedEmptySlotText.GetLocalizedString();
            }
            else
            {
                saveNameText.text = "Empty Slot";
            }
        if (saveDateText != null)
            saveDateText.text = "";
        if (playTimeText != null)
            playTimeText.text = "";
        if (playerStatsText != null)
            playerStatsText.text = "";
        if (leagueInfoText != null)
            leagueInfoText.text = "";
    }
    
    private void OnSaveClicked()
    {
        string saveName = "";
        if (saveNameInputField != null && !string.IsNullOrEmpty(saveNameInputField.text))
        {
            saveName = saveNameInputField.text;
        }
        else if (slotInfo != null && slotInfo.exists && slotInfo.saveData != null && !string.IsNullOrEmpty(slotInfo.saveData.saveName))
        {
            saveName = slotInfo.saveData.saveName; // Retain existing name if no new name provided
        }
        else
        {
            int slotNumber = slotIndex + 1; // Convert to 1-based index for display
            if(_localizedDefaultSaveNameText != null && !_localizedDefaultSaveNameText.IsEmpty)
            {
                _localizedDefaultSaveNameText.Arguments = new object[] { slotNumber };
                _localizedDefaultSaveNameText.RefreshString();
                saveName = _localizedDefaultSaveNameText.GetLocalizedString();
            }
            else
            {
                saveName = $"Save {slotIndex + 1}"; // Default name if none provided
            }

        }
        
        if (SaveSystem.Instance.SaveGame(slotIndex, saveName))
        {
            parentMenu?.RefreshAllSlots();
            if(_localizedOnGameSavedText != null && !_localizedOnGameSavedText.IsEmpty)
            {
                _localizedOnGameSavedText.Arguments = new object[] { slotIndex + 1 };
                _localizedOnGameSavedText.RefreshString();
                parentMenu?.ShowMessage(_localizedOnGameSavedText.GetLocalizedString(), false);
            }
            else
            {
                parentMenu?.ShowMessage($"Game saved to Slot {slotIndex + 1}!", false);
            }
        }
        else
        {
            if(_localizedFailedToSaveText != null && !_localizedFailedToSaveText.IsEmpty)
            {
                _localizedFailedToSaveText.Arguments = new object[] { slotIndex + 1 };
                _localizedFailedToSaveText.RefreshString();
                parentMenu?.ShowMessage(_localizedFailedToSaveText.GetLocalizedString(), true);
            }
            else
            {
                parentMenu?.ShowMessage($"Failed to save game to Slot {slotIndex + 1}!", true);
            }
        }
    }
    
    private void OnLoadClicked()
    {
        if (SaveSystem.Instance == null)
        {
            if(_localizedFailedToLoadSystemUnavailableText != null && !_localizedFailedToLoadSystemUnavailableText.IsEmpty)
            {
                _localizedFailedToLoadSystemUnavailableText.Arguments = new object[] { slotIndex + 1 };
                _localizedFailedToLoadSystemUnavailableText.RefreshString();
                parentMenu?.ShowMessage(_localizedFailedToLoadSystemUnavailableText.GetLocalizedString(), true);
            }
            else
            {
                parentMenu?.ShowMessage("Save system not available.", true);
            }
          
            return;
        }
        
        if (!SaveSystem.Instance.SaveSlotExists(slotIndex))
        {
            if(_localizedFailedToLoadSaveNotAvailableText != null && !_localizedFailedToLoadSaveNotAvailableText.IsEmpty)
            {
                _localizedFailedToLoadSaveNotAvailableText.Arguments = new object[] { slotIndex + 1 };
                _localizedFailedToLoadSaveNotAvailableText.RefreshString();
                parentMenu?.ShowMessage(_localizedFailedToLoadSaveNotAvailableText.GetLocalizedString(), true);
            }
            else
            {
                parentMenu?.ShowMessage($"No save file exists in Slot {slotIndex + 1}.", true);
            }
            return;
        }
        
        if (SaveSystem.Instance.LoadGame(slotIndex))
        {
            if(_localizedGameLoadedText != null && !_localizedGameLoadedText.IsEmpty)
            {
                _localizedGameLoadedText.Arguments = new object[] { slotIndex + 1 };
                _localizedGameLoadedText.RefreshString();
                parentMenu?.ShowMessage(_localizedGameLoadedText.GetLocalizedString(), false);
            }
            else
            {
                parentMenu?.ShowMessage($"Game loaded from Slot {slotIndex + 1}!", false);
            }
            if(AudioManager.instance != null)
            {
                AudioManager.instance.UIClick1.start();
            }
            DOVirtual.DelayedCall(0.5f, () => {
                GameManager.Instance.StartGame();
                parentMenu?.CloseMenu();
            });
            
        }
        else
        {
            if(_localizedFailedToLoadText != null && !_localizedFailedToLoadText.IsEmpty)
            {
                _localizedFailedToLoadText.Arguments = new object[] { slotIndex + 1 };
                _localizedFailedToLoadText.RefreshString();
                parentMenu?.ShowMessage(_localizedFailedToLoadText.GetLocalizedString(), true);
            }
            else
            {
                parentMenu?.ShowMessage($"Failed to load game from Slot {slotIndex + 1}!", true);
            }
        }
    }
    
    private void OnStartNewGameSaveClicked()
    {
        
        if(moreSavesTutorialPrompt != null)
        {
            moreSavesTutorialPrompt.gameObject.SetActive(true);
            moreSavesTutorialPrompt.SetPreferredSaveSlot(slotIndex+1);
            moreSavesTutorialPrompt.SetSaveName(saveNameInputField != null ? saveNameInputField.text : "");
        }
        
        /*if(saveNameInputField !=null && !string.IsNullOrEmpty(saveNameInputField.text))
        {
            if (SaveSystem.Instance.NewGame(saveNameInputField.text, slotIndex + 1))
            {
                GameManager.Instance.StartGame();

                if (AudioManager.instance != null)
                {
                    AudioManager.instance.UIClick1.start();
                }
            }
            else
            {
                Debug.LogError("Failed to start a new game in the selected slot.");
            }
        }
        else
        {
            if (SaveSystem.Instance.NewGame("Game Save", slotIndex + 1))
            {
                GameManager.Instance.StartGame();

                if (AudioManager.instance != null)
                {
                    AudioManager.instance.UIClick1.start();
                }
            }
            else
            {
                Debug.LogError("Failed to start a new game in the selected slot.");
            }
        }*/
       
    }
    
    private void OnDeleteClicked()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.UIClick1.start();
        }
        string confirmationMessage = $"Are you sure you want to delete Save Slot {slotIndex + 1}?";
        if(_localizedDeleteAreYouSureText != null && !_localizedDeleteAreYouSureText.IsEmpty)
        {
            _localizedDeleteAreYouSureText.Arguments = new object[] { slotIndex + 1 };
            _localizedDeleteAreYouSureText.RefreshString();
            confirmationMessage = _localizedDeleteAreYouSureText.GetLocalizedString();
        }
        
        parentMenu?.ShowConfirmationDialog(
            confirmationMessage,
            () => {

                if (SaveSystem.Instance.DeleteSave(slotIndex))
                {
                    parentMenu?.RefreshAllSlots();
                    if(_localizedDeleteSuccessText != null && !_localizedDeleteSuccessText.IsEmpty)
                    {
                        _localizedDeleteSuccessText.Arguments = new object[] { slotIndex + 1 };
                        _localizedDeleteSuccessText.RefreshString();
                        parentMenu?.ShowMessage(_localizedDeleteSuccessText.GetLocalizedString(), false);
                    }
                    else
                    {
                        parentMenu?.ShowMessage($"Save Slot {slotIndex + 1} deleted!", false);
                    }

                    if (AudioManager.instance != null)
                    {
                        AudioManager.instance.deleteSave.start();
                    }
                }
                else
                {
                    if(_localizedDeleteFailedText != null && !_localizedDeleteFailedText.IsEmpty)
                    {
                        _localizedDeleteFailedText.Arguments = new object[] { slotIndex + 1 };
                        _localizedDeleteFailedText.RefreshString();
                        parentMenu?.ShowMessage(_localizedDeleteFailedText.GetLocalizedString(), true);
                    }
                    else
                    {
                        parentMenu?.ShowMessage($"Failed to delete Save Slot {slotIndex + 1}!", true);
                    }
                }
            }
        );
    }
}
