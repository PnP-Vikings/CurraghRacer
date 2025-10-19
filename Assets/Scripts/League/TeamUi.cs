using League;
using UnityEngine;
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
                        TeamPointsText.text = "Points: " + selectedLeague.GetTeamPoints(team);
                        TeamWinsText.text = "Wins: " + selectedLeague.GetTeamWins(team);
                        TeamPositionText.text = "Position: " + selectedLeague.GetTeamPosition(team);


                        int starRating = LeagueController.Instance.CalculateTeamStarRating(team);
                        Debug.Log("Calculated star rating for team " + team.teamName + ": " + starRating);
                        starRatingText.text = "Star Rating: " + starRating.ToString();


                  }
                  else
                  {
                        TeamPointsText.text = "Points: 0";
                        TeamWinsText.text = "Wins: 0";
                        TeamPositionText.text = "Position: N/A";
                  }
            }



            TeamUiPanel.SetActive(true);
      }
      
      
     
}