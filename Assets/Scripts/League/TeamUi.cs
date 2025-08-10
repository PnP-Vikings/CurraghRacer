using UnityEngine;
using UnityEngine.UI;

public class TeamUi : MonoBehaviour
{
      public TeamMemberUiHandler teamMemberCellPrefab;
      public Transform gridParent;
      public Team selctedTeam;
      public TMPro.TMP_Text TeamNameText,TeamPointsText,TeamWinsText;
      public GameObject TeamUiPanel;
      public Image teamLogo;
      public static TeamUi Instance { get; private set; }
      

      private void Awake()
      {
            if (Instance == null)
            {
                  Instance = this;
                  DontDestroyOnLoad(gameObject);
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

            selctedTeam = team;
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

            TeamUiPanel.SetActive(true);
      }
      
}