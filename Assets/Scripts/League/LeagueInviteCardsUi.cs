using System.Collections;
using League;
using UnityEngine;
using UnityEngine.UI;

public class LeagueInviteCardsUi : MonoBehaviour
{
  
  
  public TMPro.TMP_Text leagueNameText, leagueDescriptionText;
  public Image leagueLogoImage;
  public GameObject leagueInvitePanel;
  
  
  public static LeagueInviteCardsUi Instance { get; private set; }
  
  FMOD.Studio.EventInstance ShowInvite;

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
        ShowInvite = FMODUnity.RuntimeManager.CreateInstance("event:/Main Menu/Show Invite");
        ShowInvite.start();
    }
    public void SetLeagueData(League.League league)
  {
    if (league == null)
    {
      Debug.LogError("League data is null!");
      return;
    }

    leagueNameText.text = "You have been invited to participate in\nThe " + league.leagueName;
    leagueDescriptionText.text = league.description;

    if (league.leagueIcon != null)
    {
      leagueLogoImage.sprite = league.leagueIcon;
    }
    else
    {
      Debug.LogWarning("League logo is not set!");
      leagueLogoImage.gameObject.SetActive(false);
    }
  }
    
    
  
  public void OnClickAcceptInvite()
  {
    LeagueController.Instance.RegenerateRaceSchedule();
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
