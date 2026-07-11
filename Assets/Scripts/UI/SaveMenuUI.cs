using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SaveMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject saveSlotPrefab;
    public Transform slotsContainer;
    public Button quickSaveButton;
    public Button quickLoadButton;
    public Button autoSaveButton;
  //  public Button newGameButton;
    public Button newGamePlayTutorialButton;
    public Button newGameDontPlayTutorialButton;
    public Button closeButton;
    public TMP_Text messageText;
    public GameObject messagePanel;
    public GameObject confirmationDialog;
    public TMP_Text confirmationText;
    public Button confirmYesButton;
    public Button confirmNoButton;
    
    [Header("Settings")]
    public int quickSaveSlot = 0; // Slot 0 reserved for quick save
    public float messageDisplayTime = 3f;
    
    private SaveSlotUI[] slotUIs;
    private System.Action pendingConfirmationAction;
    
    private void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        // Setup button events
        if (quickSaveButton != null)
        {
            quickSaveButton.onClick.AddListener(QuickSave);
            quickSaveButton.interactable = SaveSystem.Instance != null && SaveSystem.Instance.WasLoadedFromSave || SaveSystem.Instance.IsNewGame;
        }
        if (quickLoadButton != null)
            quickLoadButton.onClick.AddListener(QuickLoad);
        if (autoSaveButton != null)
            autoSaveButton.onClick.AddListener(AutoSave);
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseMenu);

        /*if (newGameButton != null)
        {
            /#1#/ Check if player has saves to show/hide New Game button
            if (!SaveSystem.Instance.HasAnySaves())
            {
                newGameButton.gameObject.SetActive(true);
            }
            else
            {
                newGameButton.gameObject.SetActive(false);
            }#1#
            
            newGameButton.onClick.AddListener(() => {
                if (SaveSystem.Instance.NewGame("My First Game"))
                {
                    // New game started successfully
                    // Maybe show a success message or transition to game
                    Debug.Log("New game started!");
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ResetForNewGame();
                    }
                }
                else
                {
                    // Handle error
                    Debug.LogError("Failed to start new game");
                }
            });

        }*/
        if(newGamePlayTutorialButton != null)
        {
            newGamePlayTutorialButton.onClick.AddListener(() => NewGameStart(true));        
        }

        if (newGameDontPlayTutorialButton != null)
        {
            newGamePlayTutorialButton.onClick.AddListener(() => NewGameStart(false));
        }
        
        // Setup confirmation dialog
        if (confirmYesButton != null)
            confirmYesButton.onClick.AddListener(ConfirmAction);
        if (confirmNoButton != null)
            confirmNoButton.onClick.AddListener(CancelConfirmation);
        
        // Hide UI elements initially
        if (messagePanel != null)
            messagePanel.SetActive(false);
        if (confirmationDialog != null)
            confirmationDialog.SetActive(false);
        
        
        CreateSlotUIs();
        RefreshAllSlots();
    }
    
    private void CreateSlotUIs()
    {
        if (saveSlotPrefab == null || slotsContainer == null)
        {
            Debug.LogError("SaveMenuUI: Missing prefab or container references!");
            return;
        }
        
        // Clear existing slots
        foreach (Transform child in slotsContainer)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        
        // Create slot UIs
        int maxSlots = SaveSystem.Instance != null ? SaveSystem.Instance.maxSaveSlots : 5;
        slotUIs = new SaveSlotUI[maxSlots];
        
        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = Instantiate(saveSlotPrefab, slotsContainer);
            SaveSlotUI slotUI = slotObj.GetComponent<SaveSlotUI>();
            
            if (slotUI != null)
            {
                slotUIs[i] = slotUI;
            }
            else
            {
                Debug.LogError($"SaveSlotUI component not found on slot prefab for slot {i}!");
            }
        }
    }
    
    public void RefreshAllSlots()
    {
        if (SaveSystem.Instance == null)
        {
            Debug.LogError("SaveSystem.Instance is null!");
            return;
        }
        
        SaveSlotInfo[] allSlots = SaveSystem.Instance.GetAllSaveSlots();
        
        // Sort slots by save time (most recent first)
        SaveSlotInfo[] sortedSlots = SortSlotsBySaveTime(allSlots);
        
        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] == null) continue;
            
            // Choose sorted slot info if available, otherwise fall back to the natural slot index
            SaveSlotInfo info = null;
            if (sortedSlots != null && i < sortedSlots.Length)
            {
                info = sortedSlots[i];
            }
            
            if (info == null)
            {
                // Fallback to the natural slot index
                info = new SaveSlotInfo
                {
                    slotIndex = i,
                    exists = SaveSystem.Instance.SaveSlotExists(i),
                    saveData = SaveSystem.Instance.GetSavePreview(i)
                };
            }
            else if (info.slotIndex < 0 || info.slotIndex >= SaveSystem.Instance.maxSaveSlots)
            {
                Debug.LogWarning($"SaveMenuUI: Correcting out-of-range slotIndex {info.slotIndex} to {i}");
                info.slotIndex = i;
            }
            
            slotUIs[i].Initialize(info.slotIndex, info, this);
        }
        
        // Update quick load button state - check for both quick save and autosave
        if (quickLoadButton != null)
        {
            int autoSaveSlot = Mathf.Clamp(SaveSystem.Instance.maxSaveSlots - 1, 0, SaveSystem.Instance.maxSaveSlots - 1);
            bool hasQuickSave = SaveSystem.Instance.SaveSlotExists(quickSaveSlot);
            bool hasAutoSave = SaveSystem.Instance.SaveSlotExists(autoSaveSlot);
            quickLoadButton.interactable = hasQuickSave || hasAutoSave;
        }
    }
    
    
    
    public void QuickSave()
    {
        if (SaveSystem.Instance.SaveGame(quickSaveSlot, "Quick Save"))
        {
            RefreshAllSlots();
            ShowMessage("Quick Save completed!", false);
        }
        else
        {
            ShowMessage("Quick Save failed!", true);
        }
    }
    
    public void QuickLoad()
    {
        int autoSaveSlot = Mathf.Clamp(SaveSystem.Instance.maxSaveSlots - 1, 0, SaveSystem.Instance.maxSaveSlots - 1);
        bool hasQuickSave = SaveSystem.Instance.SaveSlotExists(quickSaveSlot);
        bool hasAutoSave = SaveSystem.Instance.SaveSlotExists(autoSaveSlot);
        
        int slotToLoad = -1;
        string loadType = "";
        
        if (hasQuickSave && hasAutoSave)
        {
            // Both exist, load the most recent one
            SaveData quickSaveData = SaveSystem.Instance.GetSavePreview(quickSaveSlot);
            SaveData autoSaveData = SaveSystem.Instance.GetSavePreview(autoSaveSlot);
            
            // Guard against invalid/corrupt previews
            if (quickSaveData == null && autoSaveData == null)
            {
                ShowMessage("No valid save previews available to quick load.", true);
                return;
            }
            else if (quickSaveData != null && autoSaveData == null)
            {
                slotToLoad = quickSaveSlot;
                loadType = "Quick Save";
            }
            else if (quickSaveData == null && autoSaveData != null)
            {
                slotToLoad = autoSaveSlot;
                loadType = "Auto Save";
            }
            else
            {
                try
                {
                    System.DateTime quickSaveDate = System.DateTime.Parse(quickSaveData.saveDate);
                    System.DateTime autoSaveDate = System.DateTime.Parse(autoSaveData.saveDate);
                    
                    if (autoSaveDate > quickSaveDate)
                    {
                        slotToLoad = autoSaveSlot;
                        loadType = "Auto Save";
                    }
                    else
                    {
                        slotToLoad = quickSaveSlot;
                        loadType = "Quick Save";
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to parse save dates, defaulting to quick save: {e.Message}");
                    slotToLoad = quickSaveSlot;
                    loadType = "Quick Save";
                }
            }
        }
        else if (hasQuickSave)
        {
            slotToLoad = quickSaveSlot;
            loadType = "Quick Save";
        }
        else if (hasAutoSave)
        {
            slotToLoad = autoSaveSlot;
            loadType = "Auto Save";
        }
        
        if (slotToLoad >= 0)
        {
            if (SaveSystem.Instance.LoadGame(slotToLoad))
            {
                ShowMessage($"{loadType} loaded successfully!", false);
                GameManager.Instance.StartGame();
                StartCoroutine(CloseMenuAfterDelay(1f));
            }
            else
            {
                ShowMessage($"Failed to load {loadType}!", true);
            }
        }
        else
        {
            ShowMessage("No save files found to load!", true);
        }
    }
    
    public void AutoSave()
    {
        // Use the last slot for auto save
        int autoSaveSlot = SaveSystem.Instance.maxSaveSlots - 1;
        
        if (SaveSystem.Instance.SaveGame(autoSaveSlot, "Auto Save"))
        {
            RefreshAllSlots();
            ShowMessage("Auto Save completed!", false);
        }
        else
        {
            ShowMessage("Auto Save failed!", true);
        }
    }
    
    public void ShowMessage(string message, bool isError = false)
    {
        if (messageText != null && messagePanel != null)
        {
            messageText.text = message;
            messageText.color = isError ? Color.red : Color.green;
            
            messagePanel.SetActive(true);
            StartCoroutine(HideMessageAfterDelay());
        }
        
        Debug.Log($"SaveMenuUI: {message}");
    }
    
    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDisplayTime);
        
        if (messagePanel != null)
            messagePanel.SetActive(false);
    }
    
    public void ShowConfirmationDialog(string message, System.Action onConfirm)
    {
        if (confirmationDialog != null && confirmationText != null)
        {
            confirmationText.text = message;
            confirmationDialog.SetActive(true);
            pendingConfirmationAction = onConfirm;
        }
    }
    
    private void ConfirmAction()
    {
        pendingConfirmationAction?.Invoke();
        CancelConfirmation();
    }
    
    private void CancelConfirmation()
    {
        if (confirmationDialog != null)
            confirmationDialog.SetActive(false);
        
        pendingConfirmationAction = null;
    }
    
    public void CloseMenu()
    {
        gameObject.SetActive(false);
    }

    public void NewGameStart(bool playTutorial = false)
    {
        if(playTutorial)
        {
            GameManager.Instance.ActivateTutorialMode(true);
        }
        else
        {
            GameManager.Instance.ActivateTutorialMode(false);
        }
        if (SaveSystem.Instance.NewGame("My First Game"))
        {
            // New game started successfully
            // Maybe show a success message or transition to game
            Debug.Log("New game started!");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetForNewGame();
            }
        }
        else
        {
            // Handle error
            Debug.LogError("Failed to start new game");
        }
    }
    
    private IEnumerator CloseMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CloseMenu();
    }
    
    // Public methods for external access
    public void OpenSaveMenu()
    {
        gameObject.SetActive(true);
        RefreshAllSlots();
    }
    
    public void PerformAutoSaveIfEnabled()
    {
        // Call this method periodically or at important game events
        AutoSave();
    }
    
    /// <summary>
    /// Sort save slots by save time (most recent first)
    /// </summary>
    private SaveSlotInfo[] SortSlotsBySaveTime(SaveSlotInfo[] slots)
    {
        if (slots == null || slots.Length == 0)
            return slots;
        
        // Create a copy to avoid modifying the original array
        SaveSlotInfo[] sortedSlots = new SaveSlotInfo[slots.Length];
        System.Array.Copy(slots, sortedSlots, slots.Length);
        
        // Sort using Array.Sort with custom comparison
        System.Array.Sort(sortedSlots, (slot1, slot2) =>
        {
            // Handle empty slots - they should appear at the bottom
            bool slot1HasData = slot1 != null && slot1.exists && slot1.saveData != null;
            bool slot2HasData = slot2 != null && slot2.exists && slot2.saveData != null;
            
            // If one has data and the other doesn't, prioritize the one with data
            if (slot1HasData && !slot2HasData) return -1;
            if (!slot1HasData && slot2HasData) return 1;
            
            // If neither has data, maintain original order
            if (!slot1HasData && !slot2HasData) return 0;
            
            // Both have data, compare by save date
            try
            {
                System.DateTime date1 = System.DateTime.Parse(slot1.saveData.saveDate);
                System.DateTime date2 = System.DateTime.Parse(slot2.saveData.saveDate);
                
                // Sort descending (most recent first)
                return date2.CompareTo(date1);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to parse save dates for sorting: {e.Message}");
                // If parsing fails, maintain original order
                return 0;
            }
        });
        
        return sortedSlots;
    }

   
}
