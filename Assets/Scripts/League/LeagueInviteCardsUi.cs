using System.Collections;
using League;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class LeagueInviteCardsUi : MonoBehaviour
{
  
  
  public TMPro.TMP_Text leagueNameText, leagueDescriptionText;
  public Image leagueLogoImage;
  public GameObject leagueInvitePanel;
  public LocalizedString leagueInviteLocalizedText = new LocalizedString("League"," LeagueInviteLocalizedText");
  private string inviteText;
  
  public static LeagueInviteCardsUi Instance { get; private set; }

  private void Awake()
  {
    if (Instance == null)
    {
      Instance =this;
    }

    else
    {
      Destroy(gameObject);
    }
    
    if (leagueNameText == null || leagueDescriptionText == null || leagueLogoImage == null || leagueInvitePanel == null)
    {
      Debug.LogError("One or more UI components are not assigned in the inspector!");
    }
  }

    private void Start()
    {
        if (AudioManager.instance != null) 
        {
            AudioManager.instance.showInviteAudio.start();
        }
    }
    public void SetLeagueData(League.League league)
  {
    if (league == null)
    {
      Debug.LogError("League data is null!");
      return;
    }

    Debug.Log($"Setting league data for invite: {league.leagueName}");


    inviteText = "You have been invited to participate in\nThe ";
    
    if(leagueInviteLocalizedText != null && !leagueInviteLocalizedText.IsEmpty)
    {
        inviteText = leagueInviteLocalizedText.GetLocalizedString();
    }
    if(league.localizedLeagueName != null && !league.localizedLeagueName.IsEmpty)
    {
        leagueNameText.text = inviteText + league.localizedLeagueName.GetLocalizedString();
    }
    else
    { if (leagueNameText != null)
        leagueNameText.text = inviteText + league.leagueName;
    }
   
    if (league.localizedDescription != null && !league.localizedDescription.IsEmpty)
    {
        leagueDescriptionText.text = league.localizedDescription.GetLocalizedString();
    }
    else
    {
        if (leagueDescriptionText != null)
            leagueDescriptionText.text = league.description;
    }

    if (leagueLogoImage != null)
    {
      if (league.leagueIcon != null)
      {
        leagueLogoImage.sprite = league.leagueIcon;
        leagueLogoImage.gameObject.SetActive(true);
      }
      else
      {
        Debug.LogWarning($"League logo is not set for league: {league.leagueName}");
        leagueLogoImage.gameObject.SetActive(false);
      }
    }
  }
    
    
  
  public void OnClickAcceptInvite()
  {
    // CRITICAL: Do NOT call RegenerateRaceSchedule() here!
    // The race schedule should already exist from when the league was initialized.
    // Regenerating it creates NEW Race objects with NEW team references, which wipes all recorded stats!
    
    // Only regenerate if the schedule doesn't exist yet
    if (LeagueController.Instance.currentLeague.raceDays == null || LeagueController.Instance.currentLeague.raceDays.Length == 0)
    {
      Debug.Log("Race schedule doesn't exist - generating for first time");
      LeagueController.Instance.RegenerateRaceSchedule();
    }
    else
    {
      Debug.Log($"Race schedule already exists ({LeagueController.Instance.currentLeague.raceDays.Length} race days) - preserving it");
    }
    
    LeagueController.Instance.currentLeague.RecalculateStandings();
    LeagueController.Instance.SetPlayerHasAcceptedInvite();
    Destroy(gameObject);
  }
  
  public void OnClickDeclineInvite()
  {
    leagueInvitePanel.SetActive(false);
    StartCoroutine(ShowLeaguePanelAfterDelay(30f));
  }

  public void ShowLeagueInviteAgain()
  {
      leagueInvitePanel.SetActive(true);
     
  }
  
  public IEnumerator ShowLeaguePanelAfterDelay(float delay)
  {
    if (!GameManager.Instance.playerIsBusy && TimeManager.Instance != null && LeagueController.Instance.currentLeague != null)
    {
      
      yield return new WaitForSeconds(delay);
           
      ShowLeagueInviteAgain();
    }
    else
    {
      if (TimeManager.Instance == null)
        Debug.LogWarning("TimeManager instance is null, cannot show league invite message.");
      if (LeagueController.Instance.currentLeague == null)
        Debug.LogWarning("Current league is null, cannot show league invite message.");
      if (GameManager.Instance.playerIsBusy)
        Debug.Log("Player is busy, delaying league invite message.");
      // Retry after some time
      yield return new WaitForSeconds(10f);
      StartCoroutine(ShowLeaguePanelAfterDelay(25f));
    }
  }
}
