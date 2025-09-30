using UnityEngine;
using UnityEngine.UI;

public class TvSettingsUi : MonoBehaviour
{
    [Header("UI References")]
    public Button quickSaveButton;
    [Header("Settings")]
    public int quickSaveSlot = 0;
    public float messageDisplayTime = 1.5f;
    void Start()
    {
        if (quickSaveButton != null)
        {
            quickSaveButton.onClick.AddListener(QuickSave);
            quickSaveButton.interactable = SaveSystem.Instance != null && SaveSystem.Instance.WasLoadedFromSave || SaveSystem.Instance.IsNewGame;
        }
    }
    

    public void QuickSave()
    {
        if (SaveSystem.Instance.SaveGame(quickSaveSlot, "Quick Save"))
        {
           if (PlayerStatsView.Instance == null) return;
            PlayerStatsView.Instance.ClearInfo();
            PlayerStatsView.Instance.DisplayInfo("Quick Save completed!" , messageDisplayTime);
        }
        else
        {
            if (PlayerStatsView.Instance == null) return;
            PlayerStatsView.Instance.ClearInfo();
            PlayerStatsView.Instance.DisplayInfo("Quick Save failed!" , messageDisplayTime);
        }
    }
}
