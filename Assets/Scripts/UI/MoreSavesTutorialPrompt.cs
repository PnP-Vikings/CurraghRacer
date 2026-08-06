using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class MoreSavesTutorialPrompt : MonoBehaviour
{
    public Button yesButton;
    public Button noButton;
    public int preferredSaveSlot = 0; // Default to slot 0, can be changed in the inspector
    public string newSaveName = ""; // Default save name, can be changed in the inspector
    [SerializeField] private LocalizedString _localizedNewGameStartNameText;
    
    private void Start()
    {
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(OnYesButtonClicked);
        }
        else
        {
            Debug.LogError("Yes button is not assigned in the inspector.");
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(OnNoButtonClicked);
        }
        else
        {
            Debug.LogError("No button is not assigned in the inspector.");
        }
    }

    private void OnYesButtonClicked()
    {
        NewGameStart(true);
    }
    
    private void OnNoButtonClicked()
    {
     NewGameStart(false);
    }
    
    public void NewGameStart(bool playTutorial = false)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ActivateTutorialMode(playTutorial,true);
        }
        
        string saveName = "My First Game";
        if (newSaveName == null || newSaveName == "")
        {
            if (!_localizedNewGameStartNameText.IsEmpty)
            {
                saveName = _localizedNewGameStartNameText.GetLocalizedString();
            }
        }
        else
        {
            saveName = newSaveName;
        }
        if (SaveSystem.Instance.NewGame(saveName, preferredSaveSlot))
        {
            // New game started successfully
            // Maybe show a success message or transition to game
            Debug.Log("New game started!");
            if (GameManager.Instance == null)
                return;
            GameManager.Instance.ResetForNewGame();
            
        }
        else
        {
            // Handle error
            Debug.LogError("Failed to start new game");
        }
    }
    
    
    public void SetPreferredSaveSlot(int slot)
    {
        preferredSaveSlot = slot;
    }
    public void SetSaveName(string saveName)
    {
        newSaveName = saveName;
    }
    
}
