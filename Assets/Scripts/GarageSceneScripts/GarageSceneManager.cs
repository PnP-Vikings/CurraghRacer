using DG.Tweening;
using League;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class GarageSceneManager : MonoBehaviour
{
    public static GarageSceneManager Instance { get; private set; }
    public DecisionCardUiMaster De;
    public WelcomeCardUi welcomeCardUi;
    [Tooltip("Delay before showing the welcome card for new players. Adjust as needed.")]
    public float showIntroDelay = 1.2f;
    public TutorialTaskUiManager tutorialTaskUiManager;
    public GameObject tutorialUi;
    
    
    
    [Header("Debt Warning")]
    public GameObject DebtWarningScreen;
    public TMP_Text debtWarningTitleText;
    public TMP_Text debtWarningText;
    public TMP_Text debtWarningBtnText;
    
    LocalizedString localizedDebtWarningTitleText = new LocalizedString { TableReference = "GarageScene", TableEntryReference = "Garage.DebtWarning.Title" };
    LocalizedString localizedDebtWarningDescriptionText = new LocalizedString { TableReference = "GarageScene", TableEntryReference = "Garage.DebtWarning.DescriptionText" };
    LocalizedString localizedDebtWarningBtnText = new LocalizedString { TableReference = "GarageScene", TableEntryReference = "Garage.DebtWarning.AcceptBtnTxt" };
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        if(De != null && DecisionCardManager.Instance != null)
        {
            De.gameObject.SetActive(false);
            DecisionCardManager.Instance.SetUiMaster(De);
        }
        
        if(SaveSystem.Instance != null && GameManager.Instance != null)
        {
            if (SaveSystem.Instance.IsNewGame && !GameManager.Instance.GetHasBeenShownIntro())
            {
                if(welcomeCardUi != null)
                {
                    DOVirtual.DelayedCall(showIntroDelay, () =>
                    {

                        if (welcomeCardUi == null)
                            return;
                        welcomeCardUi.gameObject.SetActive(true);
                        GameManager.Instance.SetHasBeenShownIntro(true);
                    });
                }
                
            }

            if (!GameManager.Instance.GetHasBeenShownWarningAboutDebt())
            {

                if (PlayerManager.Instance != null)
                {
                    PlayerManager.Instance.onDebtWarning.AddListener(ShowDebtWarningScreen);
                }   
            }
        }

        ProcessTutorialUi();
    }
    
    void OnEnable()
    {
        CheckAndShowLeagueInvite();
        
        ProcessTutorialUi();
        
        DOVirtual.DelayedCall(6f, () =>
        {
            ShowDebtWarningScreen();
        });
      
    }
    
    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onTaskModified.RemoveListener(UpdateTutorialTask);
        }
    }

    public void CheckAndShowLeagueInvite()
    {
        if(LeagueController.Instance == null)
        {
            Debug.LogWarning("LeagueController instance is null. Cannot check league status.");
            return;
        }
        
        if(LeagueController.Instance.currentLeague == null || !LeagueController.Instance.currentLeague.playerHasJoined && !GameManager.Instance.playerIsBusy && !GameManager.Instance.IsTutorialModeActive())
        {
            Debug.Log("Player not in league, showing join message after delay.");
            LeagueController.Instance.ShowLeagueInviteAfterDelay();
        }
    }
    
    public void ShowDebtWarningScreen()
    {
        if(GameManager.Instance != null && !GameManager.Instance.GetHasBeenShownWarningAboutDebt() && GameManager.Instance.IsTutorialModeActive() == false && GameManager.Instance.playerIsBusy == false && PlayerManager.Instance != null && PlayerManager.Instance.coins < 0)
        {
            if (debtWarningTitleText != null)
            {
                if(!localizedDebtWarningTitleText.IsEmpty)
                {
                    localizedDebtWarningTitleText.RefreshString();
                    debtWarningTitleText.text = localizedDebtWarningTitleText.GetLocalizedString();
                }
            }
            
            if (debtWarningText != null)
            {
                if(!localizedDebtWarningDescriptionText.IsEmpty)
                {
                    localizedDebtWarningDescriptionText.RefreshString();
                    debtWarningText.text = localizedDebtWarningDescriptionText.GetLocalizedString("400");
                    
                    if(PlayerManager.Instance != null)
                    {
                        float maxDebtLimit = (-1 * PlayerManager.Instance.GetMaxDebtLimit()); // Get the max debt limit as a positive value
                        debtWarningText.text = localizedDebtWarningDescriptionText.GetLocalizedString(maxDebtLimit);
                    }
                }
            }
            
            if (debtWarningBtnText != null)
            {
                if(!localizedDebtWarningBtnText.IsEmpty)
                {
                    localizedDebtWarningBtnText.RefreshString();
                    debtWarningBtnText.text = localizedDebtWarningBtnText.GetLocalizedString();
                }
            }
            
            if (DebtWarningScreen == null) return;
            DebtWarningScreen.SetActive(true);
            GameManager.Instance.SetHasBeenShownWarningAboutDebt(true);
        }
        
       
    }
    
    public void UpdateTutorialTask()
    {
        if(tutorialTaskUiManager == null) return;
        if(GameManager.Instance != null && GameManager.Instance.IsTutorialModeActive())
        {
            tutorialTaskUiManager.UpdateTaskUis();
        }
        else
        {
            tutorialTaskUiManager.gameObject.SetActive(false);
        }
    }
    
    
    public void ProcessTutorialUi()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsTutorialModeActive())
        {
            tutorialTaskUiManager.gameObject.SetActive(true);
            tutorialTaskUiManager.UpdateTaskUis();
            GameManager.Instance.onTaskModified.AddListener(UpdateTutorialTask);
        }
        else
        {
            tutorialTaskUiManager.gameObject.SetActive(false);
            if(GameManager.Instance != null)
                GameManager.Instance.onTaskModified.RemoveListener(UpdateTutorialTask);
        }

        if (GameManager.Instance != null && !GameManager.Instance.IsTutorialModeActive() && tutorialUi != null)
        {
            tutorialUi.SetActive(false);
        }
        else if (GameManager.Instance != null && GameManager.Instance.IsTutorialModeActive() && tutorialUi != null)
        {
            tutorialUi.SetActive(true);
        }
    }
    
}
