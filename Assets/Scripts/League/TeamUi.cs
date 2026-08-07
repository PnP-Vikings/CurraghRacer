using League;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class TeamUi : MonoBehaviour
{
      public TeamMemberUiHandler teamMemberCellPrefab;
      public Transform gridParent;
      public Team selectedTeam;
      public League.League selectedLeague;
      public TMPro.TMP_Text TeamNameText,TeamPointsText,TeamWinsText,TeamPositionText,starRatingText;
      public GameObject TeamUiPanel;
      public Image teamLogo;
      public static TeamUi Instance { get; private set; }
      
      
      
      [Header("Localization")]
      [SerializeField] private LocalizedString _localizedWinsText = new LocalizedString { TableReference = "GarageScene", TableEntryReference = "Garage.LeagueTable.LeagueTeamUi.Wins" };
      [SerializeField] private LocalizedString _localizedPointsText = new LocalizedString { TableReference = "GarageScene", TableEntryReference = "Garage.LeagueTable.LeagueTeamUi.Points" };
      [SerializeField] private LocalizedString _localizedPositionText = new LocalizedString { TableReference = "GarageScene", TableEntryReference = "Garage.LeagueTable.LeagueTeamUi.Position" };
      [SerializeField] private LocalizedString _localizedStarRatingText = new LocalizedString { TableReference = "GarageScene", TableEntryReference = "Garage.LeagueTable.LeagueTeamUi.StarRating" };
      
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
            
            if (teamMemberCellPrefab == null || gridParent == null || TeamNameText == null || TeamUiPanel == null)
            {
                  Debug.LogError("One or more UI components are not assigned in the inspector!");
            }
      }
     
      public void SetSelectedTeam(Team team)
      {
            if (team == null)
            {
                  Debug.LogError("Team data is null!");
                  return;
            }

            selectedTeam = team;
            SetTeamData(team);
            TeamUiPanel.SetActive(true);
      }
      

      public void SetTeamData(Team team)
      {
            if (team == null)
            {
                  Debug.LogError("Team data is null!");
                  return;
            }

            TeamNameText.text = team.teamName;

            // Clear existing team members
            foreach (Transform child in gridParent)
            {
                  Destroy(child.gameObject);
            }

            // Populate team members
            foreach (var member in team.teamMembers)
            {
                  if (member == null)
                  {
                        Debug.LogWarning("Found a null team member in the team!");
                        continue;
                  }

                  var memberCell = Instantiate(teamMemberCellPrefab, gridParent);
                  memberCell.SetMemberData(member);
            }
            if (LeagueController.Instance != null)
            {
                  selectedLeague = LeagueController.Instance.currentLeague;

                  if (selectedLeague != null)
                  {
                        if(!_localizedPointsText.IsEmpty)
                        {
                              _localizedPointsText.Arguments = new object[] { selectedLeague.GetTeamPoints(team) };
                              _localizedPointsText.RefreshString();
                              TeamPointsText.text = _localizedPointsText.GetLocalizedString();
                        }
                        else
                        {
                              TeamPointsText.text = "Points: " + selectedLeague.GetTeamPoints(team);
                        }

                        if(!_localizedWinsText.IsEmpty)
                        {
                              _localizedWinsText.Arguments = new object[] { selectedLeague.GetTeamWins(team) };
                              _localizedWinsText.RefreshString();
                              TeamWinsText.text = _localizedWinsText.GetLocalizedString();
                        }
                        else
                        {
                              TeamWinsText.text = "Wins: " + selectedLeague.GetTeamWins(team);
                        }

                        if(!_localizedPositionText.IsEmpty)
                        {
                              _localizedPositionText.Arguments = new object[] { selectedLeague.GetTeamPosition(team) };
                              _localizedPositionText.RefreshString();
                              TeamPositionText.text = _localizedPositionText.GetLocalizedString();
                        }
                        else
                        {
                              TeamPositionText.text = "Position: " + selectedLeague.GetTeamPosition(team);
                        }

                        int starRating = LeagueController.Instance.CalculateTeamStarRating(team);
                        Debug.Log("Calculated star rating for team " + team.teamName + ": " + starRating);
                        
                        if(!_localizedStarRatingText.IsEmpty)
                        {
                              _localizedStarRatingText.Arguments = new object[] { starRating };
                              _localizedStarRatingText.RefreshString();
                              starRatingText.text = _localizedStarRatingText.GetLocalizedString();
                        }
                        else
                        {
                              starRatingText.text = "Star Rating: " + starRating.ToString();
                        }
                        


                  }
                  else
                  {
                        if(!_localizedPointsText.IsEmpty)
                        {
                              _localizedPointsText.Arguments = new object[] { 0 };
                              _localizedPointsText.RefreshString();
                              TeamPointsText.text = _localizedPointsText.GetLocalizedString();
                        }
                        else
                        {
                              TeamPointsText.text = "Points: 0";
                        }
                        
                        if(!_localizedWinsText.IsEmpty)
                        {
                              _localizedWinsText.Arguments = new object[] { 0 };
                              _localizedWinsText.RefreshString();
                              TeamWinsText.text = _localizedWinsText.GetLocalizedString();
                        }
                        else
                        {
                              TeamWinsText.text = "Wins: 0";
                        }

                        if(!_localizedPositionText.IsEmpty)
                        {
                              _localizedPositionText.Arguments = new object[] { "N/A" };
                              _localizedPositionText.RefreshString();
                              TeamPositionText.text = _localizedPositionText.GetLocalizedString();
                        }
                        else
                        {
                              TeamPositionText.text = "Position: N/A";
                        }
                  }
            }



            TeamUiPanel.SetActive(true);
      }
      
      
     
}