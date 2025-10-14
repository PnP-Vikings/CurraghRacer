using System;
using League;
using UnityEngine;

public class LeagueUi : MonoBehaviour
{
    public static LeagueUi Instance { get; private set; }
    public LeagueTeamUiHandler teamCellPrefab;
    public Transform gridParent;
    public TMPro.TMP_Text leagueNameText,playerHasNotJoinedText;
    public GameObject leagueTablePanel;
    

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
    }
    
    public void Start()
    {
        if (LeagueController.Instance != null && LeagueController.Instance.currentLeague != null)
        {
            UpdateLeague(LeagueController.Instance.currentLeague.leagueName, LeagueController.Instance.currentLeague.standings);
        }
        else
        {
            Debug.LogWarning("LeagueController or current league is not set!");
        }
    }

    public void ShowLeagueTable()
    {
        leagueTablePanel.SetActive(true);
        if (LeagueController.Instance != null && LeagueController.Instance.currentLeague != null)
        {
            UpdateLeague(LeagueController.Instance.currentLeague.leagueName, LeagueController.Instance.currentLeague.standings);
        }
        else
        {
            Debug.LogWarning("LeagueController or current league is not set!");
        }
    }
    
    public void HideLeagueTable()
    {
        leagueTablePanel.SetActive(false);
    }

    public void UpdateLeague(string leagueName, TeamStanding[] teams)
    {
        if (leagueNameText == null || gridParent == null || teamCellPrefab == null)
        {
            Debug.LogError("LeagueUi is not properly set up in the inspector.");
            return;
        }
        
        if (!LeagueController.Instance.currentLeague.playerHasJoined)
        {
            playerHasNotJoinedText.gameObject.SetActive(true);
            return;
        }
        else
        {
            playerHasNotJoinedText.gameObject.SetActive(false);
        }
        
        leagueNameText.text = leagueName;
        
        // Clear old cells
        foreach (Transform child in gridParent) Destroy(child.gameObject);
        
        if (teams == null || teams.Length == 0)
        {
            Debug.LogWarning("No teams available to display in the league.");
            return;
        }
        
        
        // Create new cells for each team
        foreach (var team in teams)
        {
            LeagueTeamUiHandler cell = Instantiate(teamCellPrefab, gridParent);
            cell.SetTeamData(team.team,
                             team.points.ToString(), 
                             team.wins.ToString(), 
                             team.position.ToString());
            
           if(team.position %2 == 0)
           {
               cell.SetBackgroundColor(new Color(39f/255f, 77f/255f, 96f/255f)); // Light blue for even rows
           }
           else
           {
               cell.SetBackgroundColor(new Color(47f/255f, 93f/255f, 112f/255f)); // Darker blue for odd rows
           }
           
           if(team.team.teamType == TeamType.Player)
           {
               cell.SetBackgroundColor(new Color(215f/255f, 182f/255f, 93f/255f)) ; // Gold color for player's team
           }
            
        }
    }
   
}
