using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeagueTeamUiHandler : MonoBehaviour
{
   public string teamName,points,wins,position;
   public Image teamLogo;
   public TMP_Text teamNameText,
       pointsText,
       winsText,
       positionText;
   
   public void SetTeamData(string teamName, string points, string wins, string position, Image logo = null)
   {
       if (logo != null)
       {
           teamLogo.sprite = logo.sprite;
       }
        else
        {
            teamLogo.gameObject.SetActive(false);
        }
   
       this.teamName = teamName;
       this.points = points;
       this.wins = wins;
       this.position = position;
       SetupUi();
   }

   private void SetupUi()
   { 
       teamNameText.text = teamName;
       pointsText.text = points;
       winsText.text = wins;
       positionText.text = position;
   }
}
