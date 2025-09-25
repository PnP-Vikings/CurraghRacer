using System;
using League;
using UnityEngine;

public class LeagueUi : MonoBehaviour
{
    public static LeagueUi Instance { get; private set; }
    public LeagueTeamUiHandler teamCellPrefab;
    public Transform gridParent;
    public TMPro.TMP_Text leagueNameText;
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
            
        }
    }
   
}
