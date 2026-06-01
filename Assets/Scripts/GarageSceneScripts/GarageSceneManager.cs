using DG.Tweening;
using League;
using UnityEngine;

public class GarageSceneManager : MonoBehaviour
{
    public static GarageSceneManager Instance { get; private set; }
    public DecisionCardUiMaster De;
    public float inviteDelay = 30f;  
    
    public WelcomeCardUi welcomeCardUi;
    public GameObject DebtWarningScreen;
    [Tooltip("Delay before showing the welcome card for new players. Adjust as needed.")]
    public float showIntroDelay = 1.2f;
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
        
        
    }
    
    
    
    void OnEnable()
    {
        CheckAndShowLeagueInvite();
    }

    public void CheckAndShowLeagueInvite()
    {
        if(LeagueController.Instance == null)
        {
            Debug.LogWarning("LeagueController instance is null. Cannot check league status.");
            return;
        }
        
        if(LeagueController.Instance.currentLeague == null || !LeagueController.Instance.currentLeague.playerHasJoined)
        {
            Debug.Log("Player not in league, showing join message after delay.");
            StartCoroutine(LeagueController.Instance.StartLeagueInviteMessageAfterDelay(inviteDelay));
        }
    }
    
    public void ShowDebtWarningScreen()
    {
        if(GameManager.Instance != null && !GameManager.Instance.GetHasBeenShownWarningAboutDebt())
        {
            DebtWarningScreen.SetActive(true);
            GameManager.Instance.SetHasBeenShownWarningAboutDebt(true);
        }
       
    }
    
    
    
    
}
