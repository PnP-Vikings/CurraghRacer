using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class TvSettingsUi : MonoBehaviour
{
    [Header("UI References")]
    public Button quickSaveButton;
    [Header("Settings")]
    public int quickSaveSlot = 0;
    public float messageDisplayTime = 1.5f;
    
    LocalizedString quickSaveName = new LocalizedString { TableReference = "TvSettingsUi", TableEntryReference = "TvSettingsUi.QuickSave.Name" };
    LocalizedString quickSaveCompletedMessage = new LocalizedString { TableReference = "TvSettingsUi", TableEntryReference = "TvSettingsUi.QuickSave.Completed" };
    LocalizedString quickSaveFailedMessage = new LocalizedString { TableReference = "TvSettingsUi", TableEntryReference = "TvSettingsUi.QuickSave.Failed" };
    void Start()
    {
        if (quickSaveButton != null)
        {
            if (SaveSystem.Instance == null)
            {
                Debug.LogError("TvSettingsUi: SaveSystem instance is null. Quick Save button will be disabled.");
                return;
            }
            quickSaveButton.onClick.AddListener(QuickSave);
            quickSaveButton.interactable = SaveSystem.Instance != null && SaveSystem.Instance.WasLoadedFromSave || SaveSystem.Instance.IsNewGame;
        }
    }
    

    public void QuickSave()
    {
        string quickSaveDisplayName = !quickSaveName.IsEmpty ? quickSaveName.GetLocalizedString() : "Quick Save";
        if (SaveSystem.Instance.SaveGame(quickSaveSlot, quickSaveDisplayName))
        {
           if (PlayerStatsView.Instance == null) return;
            PlayerStatsView.Instance.ClearInfo();
            string quickSaveCompleted = !quickSaveCompletedMessage.IsEmpty ? quickSaveCompletedMessage.GetLocalizedString() : "Quick Save completed!";
            PlayerStatsView.Instance.DisplayInfo(quickSaveCompleted , messageDisplayTime);
        }
        else
        {
            if (PlayerStatsView.Instance == null) return;
            PlayerStatsView.Instance.ClearInfo();
            string quickSaveFailed = !quickSaveFailedMessage.IsEmpty ? quickSaveFailedMessage.GetLocalizedString() : "Quick Save failed!";
            PlayerStatsView.Instance.DisplayInfo(quickSaveFailed , messageDisplayTime);
        }
    }
    
    
    private void OnEnable()
    {
        CheckIfTvTaskIsCompleted();
    }
    private void OnDisable()
    {
        CheckIfCloseTvTaskIsCompleted();
    }
        
    public void CheckIfTvTaskIsCompleted()
    {
        if(GameManager.Instance != null && GameManager.Instance.IsTutorialModeActive())
        {
            GameManager.Instance.CompleteTutorialTask(TutorialTaskType.ClickOnTheTv);
        }
    }
        
    public void CheckIfCloseTvTaskIsCompleted()
    {
        if(GameManager.Instance != null && GameManager.Instance.IsTutorialModeActive())
        {
            GameManager.Instance.CompleteTutorialTask(TutorialTaskType.ExitTv);
        }
    }
}
