using System;
using DG.Tweening;
using League;
using TMPro;
using UnityEngine;

public class DaySummaryPanel : MonoBehaviour
{
  [SerializeField] private TMP_Text leagueUpdateText;
  [SerializeField] private TMP_Text euroOLDValueText;
  [SerializeField] private TMP_Text euroNEWValueText;
  [SerializeField] private TMP_Text euroDifferenceValueText;
  [SerializeField] private TMP_Text teamHappinessOLDValueText;
  [SerializeField] private TMP_Text teamHappinessNEWValueText;
  [SerializeField] private TMP_Text teamHappinessDifferenceValueText;
  [SerializeField] private DailyCurrencyTracker dailyCurrencyTracker;

  private void OnEnable()
  {
    if (LeagueController.Instance != null && LeagueController.Instance.currentLeague != null && LeagueController.Instance.currentLeague.playerHasJoined)
    {
      UpdateLeagueValues(LeagueController.Instance.GetPlayerLeaguePosition());
    }
    else
    {
      if(leagueUpdateText != null)
        leagueUpdateText.text = "Toberduff Rowing not in a league";
    }
    
    if(euroOLDValueText != null)
      euroOLDValueText.text = "0";
    if(euroNEWValueText != null)
      euroNEWValueText.text = "0";
    if(teamHappinessOLDValueText != null)
      teamHappinessOLDValueText.text = "0";
    if(teamHappinessNEWValueText != null)
      teamHappinessNEWValueText.text = "0";
    if(euroDifferenceValueText != null)
      euroDifferenceValueText.text = "0";
    if(teamHappinessDifferenceValueText != null)
      teamHappinessDifferenceValueText.text = "0";
    if (PlayerManager.Instance != null && PlayerManager.Instance.GetDailyCurrencyTracker() != null)
    {
      dailyCurrencyTracker = PlayerManager.Instance.GetDailyCurrencyTracker();
    }
    
    if (dailyCurrencyTracker != null && euroOLDValueText != null && euroNEWValueText != null && teamHappinessOLDValueText != null && teamHappinessNEWValueText != null && euroDifferenceValueText != null && teamHappinessDifferenceValueText != null)
    {
      euroOLDValueText.text = dailyCurrencyTracker.euroAtStartOfDay.ToString();
      euroNEWValueText.text = dailyCurrencyTracker.euroAtEndOfDay.ToString();
      teamHappinessOLDValueText.text = dailyCurrencyTracker.teamHappinessStartOfDay.ToString();
      teamHappinessNEWValueText.text = dailyCurrencyTracker.teamHappinessEndOfDay.ToString();

      if (dailyCurrencyTracker.euroAtEndOfDay > dailyCurrencyTracker.euroAtStartOfDay)
      {
        euroDifferenceValueText.color = Color.green;
      }
      else
      {
        euroDifferenceValueText.color = Color.red;
      }
      
      if (dailyCurrencyTracker.teamHappinessEndOfDay > dailyCurrencyTracker.teamHappinessStartOfDay)
      {
        teamHappinessDifferenceValueText.color = Color.green;
      }
      else
      {
        teamHappinessDifferenceValueText.color = Color.red;
      }
      
      euroDifferenceValueText.text = (dailyCurrencyTracker.euroAtEndOfDay - dailyCurrencyTracker.euroAtStartOfDay).ToString();
      teamHappinessDifferenceValueText.text = (dailyCurrencyTracker.teamHappinessEndOfDay - dailyCurrencyTracker.teamHappinessStartOfDay).ToString();
    }
   
    DOVirtual.DelayedCall(15f, () =>
    {
      this.gameObject.SetActive(false);
    });
  }

  public void UpdateLeagueValues(string newValue)
  {
    if (leagueUpdateText != null)
    {
      leagueUpdateText.text = $"Toberduff Rowing league position: {newValue}";
    }
  }
  
  public void UpdateEuroValues(float oldValue, float newValue)
  {
    euroOLDValueText.text = oldValue.ToString();
    euroNEWValueText.text = newValue.ToString();
  }
  
  public void UpdateTeamHappinessValues(int oldValue, int newValue)
  {
    teamHappinessOLDValueText.text = oldValue.ToString();
    teamHappinessNEWValueText.text = newValue.ToString();
  }
  
  
}
