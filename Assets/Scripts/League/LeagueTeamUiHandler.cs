using League;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeagueTeamUiHandler : MonoBehaviour
{
    public Team team;
    public Image backgroundImage;
   public string teamName,points,wins,position;
   public Image teamLogo;
   public TMP_Text teamNameText,
       pointsText,
       winsText,
       positionText,starRatingText;
   
   public void SetTeamData(Team passedteam, string points, string wins, string position)
   {
         if (passedteam == null)
         {
              Debug.LogError("Team data is null!");
              return;
         }
         
       team = passedteam;
         
       if (team.teamLogo  != null)
       {
           teamLogo.sprite = team.teamLogo.sprite;
       }
        else
        {
            teamLogo.gameObject.SetActive(false);
        }
   
       this.teamName = team.teamName;
       this.points = points;
       this.wins = wins;
       this.position = position;
       
       if(LeagueController.Instance != null && LeagueController.Instance.currentLeague != null && starRatingText != null)
       {
           int starRating = LeagueController.Instance.CalculateTeamStarRating(team);
           starRatingText.text = starRating.ToString();
       }
       else if (starRatingText != null)
       {
           starRatingText.text = "Star Rating: N/A";
           starRatingText.gameObject.SetActive(false);
           Debug.LogWarning("LeagueController or currentLeague is null!");
       }
       SetupUi();
   }

   private void SetupUi()
   { 
       teamNameText.text = teamName;
       pointsText.text = points;
       winsText.text = wins;
       positionText.text = position;
   }
   
    public void OnClick()
    {
        TeamUi.Instance.SetSelectedTeam(team);
    }
    
    public void SetBackgroundColor(Color color)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = color;
        }
        else
        {
            Debug.LogWarning("Background image is not assigned!");
        }
    }
}
