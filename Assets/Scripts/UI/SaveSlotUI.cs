using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
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
    
    [Header("Slot Data")]
    public int slotIndex;
    private SaveSlotInfo slotInfo;
    private SaveMenuUI parentMenu;

    [Header("Localization")]
    [SerializeField] private LocalizedString _localizedEmptySlotText;

    
    public void Initialize(int index, SaveSlotInfo info, SaveMenuUI menu)
    {
        slotIndex = index;
        slotInfo = info;
        parentMenu = menu;
        
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
            slotIndexText.text = $"Slot {slotIndex + 1}";
        
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
            playTimeText.text = $"Play Time: {time.Hours:D2}:{time.Minutes:D2}";
        }
        
        // Display player stats
        if (playerStatsText != null && saveData.playerData != null)
        {
            playerStatsText.text = $"Energy: {saveData.playerData.energy:F0} | Coins: {saveData.playerData.coins:F0}";
        }
        
        // Display league info
        if (leagueInfoText != null && saveData.leagueData != null)
        {
            string leagueName = string.IsNullOrEmpty(saveData.leagueData.currentLeagueName) ? 
                "No League" : saveData.leagueData.currentLeagueName;
            string joinStatus = saveData.leagueData.playerHasJoined ? "Joined" : "Not Joined";
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
            saveName = $"Save {slotIndex + 1}"; // Default name if none provided
        }
        
        if (SaveSystem.Instance.SaveGame(slotIndex, saveName))
        {
            parentMenu?.RefreshAllSlots();
            parentMenu?.ShowMessage($"Game saved to Slot {slotIndex + 1}!", false);
        }
        else
        {
            parentMenu?.ShowMessage($"Failed to save game to Slot {slotIndex + 1}!", true);
        }
    }
    
    private void OnLoadClicked()
    {
        if (SaveSystem.Instance == null)
        {
            parentMenu?.ShowMessage("Save system not available.", true);
            return;
        }
        
        if (!SaveSystem.Instance.SaveSlotExists(slotIndex))
        {
            parentMenu?.ShowMessage($"No save file exists in Slot {slotIndex + 1}.", true);
            return;
        }
        
        if (SaveSystem.Instance.LoadGame(slotIndex))
        {
            parentMenu?.ShowMessage($"Game loaded from Slot {slotIndex + 1}!", false);
            GameManager.Instance.StartGame();
            parentMenu?.CloseMenu();

            if(AudioManager.instance != null)
            {
                AudioManager.instance.UIClick1.start();
            }
        }
        else
        {
            parentMenu?.ShowMessage($"Failed to load game from Slot {slotIndex + 1}!", true);
        }
    }
    
    private void OnStartNewGameSaveClicked()
    {
        if(saveNameInputField !=null && !string.IsNullOrEmpty(saveNameInputField.text))
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
        }
       
    }
    
    private void OnDeleteClicked()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.UIClick1.start();
        }

        parentMenu?.ShowConfirmationDialog(
            $"Are you sure you want to delete Save Slot {slotIndex + 1}?",
            () => {

                if (SaveSystem.Instance.DeleteSave(slotIndex))
                {
                    parentMenu?.RefreshAllSlots();
                    parentMenu?.ShowMessage($"Save Slot {slotIndex + 1} deleted!", false);

                    if (AudioManager.instance != null)
                    {
                        AudioManager.instance.deleteSave.start();
                    }
                }
                else
                {
                    parentMenu?.ShowMessage($"Failed to delete Save Slot {slotIndex + 1}!", true);
                }
            }
        );
    }
}
