using League;
using UnityEngine;
using UnityEngine.UI;

public class LeagueInviteCardsUi : MonoBehaviour
{
  
  
  public TMPro.TMP_Text leagueNameText, leagueDescriptionText;
  public Image leagueLogoImage;
  
  
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
    
    if (leagueNameText == null || leagueDescriptionText == null || leagueLogoImage == null)
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

    leagueNameText.text = "You have been invited to participate in " + league.leagueName;
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
    LeagueController.Instance.GenerateRaceSchedule();
    LeagueController.Instance.SetPlayerHasAcceptedInvite();
    Destroy(gameObject);
  }
  
}
