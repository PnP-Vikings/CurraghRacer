using League;
using UnityEngine;

public class LeagueCompleteCard : MonoBehaviour
{
    public TMPro.TMP_Text leagueNameText, leaguCompletionDescriptionText;
    
    public static LeagueCompleteCard Instance { get; private set; }
    
    
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
        
        if (leagueNameText == null || leaguCompletionDescriptionText == null)
        {
            Debug.LogError("One or more UI components are not assigned in the inspector!");
        }
    }
    
    public void SetLeagueCompletionData(League.League league , int position, int totalTeams, int points, int racesWon,bool playerWasRelegated ,bool playerWasPromoted)
    {
        if (league == null)
        {
            Debug.LogError("League data is null!");
            return;
        }

        leagueNameText.text = "You have completed " + league.leagueName;
        
        
        if (playerWasRelegated)
        {
            leaguCompletionDescriptionText.text = "Unfortunately, your team finished in position " + position + " out of " + totalTeams + " and has been relegated to a lower league. You earned " + points + " points with " + racesWon + " Wins." + " Don't be disheartened, use this as motivation to come back stronger!";
        }
        else if (playerWasPromoted && position == 1)
        {
            leaguCompletionDescriptionText.text = "Outstanding! Your team finished in 1st position out of " + totalTeams + " and has been promoted to a higher league. You earned " + points + " points with " + racesWon + " Wins." + " Keep up the great work!";
        }
        else if (playerWasPromoted && position == 2)
        {
            leaguCompletionDescriptionText.text = "Excellent! Your team finished in 2nd position out of " + totalTeams + " and has been promoted to a higher league. You earned " + points + " points with " + racesWon + " Wins." + " Keep up the great work!";
        }
        else if (playerWasPromoted && position == 3)
        {
            leaguCompletionDescriptionText.text = "Great work! Your team finished in 3rd position out of " + totalTeams + " and has been promoted to a higher league. You earned " + points + " points with " + racesWon + " Wins." + " Keep up the great work!";
        }
        else if(playerWasPromoted && position > 3)
        {
            leaguCompletionDescriptionText.text = "Well done! Your team finished in position " + position + " out of " + totalTeams + " and has been promoted to a higher league. You earned " + points + " points with " + racesWon + " Wins." + " Keep up the great work!";
        }
        else if (position == 1)
        {
            leaguCompletionDescriptionText.text = "Fantastic! Your team are Champions Finishing 1st out of " + totalTeams + ". You earned " + points + " points with " + racesWon + " Wins." + " Keep up the great work!";
        }
        else if (position == 2)
        {
            leaguCompletionDescriptionText.text = "Awesome! Your team finished in 2nd position out of " + totalTeams + ". You earned " + points + " points with " + racesWon  + " Wins." + " Keep up the great work!";;
        }
        else if (position == 3)
        {
            leaguCompletionDescriptionText.text = "Great job! Your team finished in 3rd position out of " + totalTeams + ". You earned " + points + " points with " + racesWon + " Wins." ;
        }
        else if (playerWasPromoted)
        {
            leaguCompletionDescriptionText.text = "Congratulations! Your team finished in position " + position + " out of " + totalTeams + " and has been promoted to a higher league. You earned " + points + " points with " + racesWon + " Wins." + " Keep up the great work!";
        }
        else
        {
            leaguCompletionDescriptionText.text = "Great job! Your team finished in position " + position + " out of " + totalTeams + ". You earned " + points + " points with " + racesWon + " Wins.";
        }

    }
    
    public void OnClickAcceptCompletion()
    {
       LeagueController.Instance.StartNewSeason();
        Destroy(gameObject);
    }
    
}
