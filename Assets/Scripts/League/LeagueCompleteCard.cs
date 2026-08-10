using League;
using UnityEngine;
using UnityEngine.Localization;

public class LeagueCompleteCard : MonoBehaviour
{
    public TMPro.TMP_Text leagueNameText, leaguCompletionDescriptionText;
    
    public static LeagueCompleteCard Instance { get; private set; }
    
    [SerializeField] private string leagueCompleteTitle = "";
    public LocalizedString leagueCompletionTitleLocalizedString = new LocalizedString("League","LeagueComplete.leagueNameTxt");
    public LocalizedString leagueCompletionDescriptionPlayerWasRelegatedLocalizedString= new LocalizedString("League","LeagueCompleted.PlayerWasRelegatedTxt");
    public LocalizedString leagueCompletionDescriptionPlayerWasPromotedPlacedFirstLocalizedString= new LocalizedString("League","LeagueCompleted.PlayerWasPromoted.PlacedFirst");
    public LocalizedString leagueCompletionDescriptionPlayerWasPromotedPlacedSecondLocalizedString= new LocalizedString("League","LeagueCompleted.PlayerWasPromoted.PlacedSecond");
    public LocalizedString leagueCompletionDescriptionPlayerWasPromotedPlacedThirdLocalizedString= new LocalizedString("League","LeagueCompleted.PlayerWasPromoted.PlacedThird");
    public LocalizedString leagueCompletionDescriptionPlayerWasPromotedPlacedHigherThanThirdLocalizedString= new LocalizedString("League","LeagueCompleted.PlayerWasPromoted.PlacedHigherThanThird");
    public LocalizedString leagueCompletionDescriptionPlayerWasNotPromotedPlacedFirstLocalizedString= new LocalizedString("League","LeagueCompleted.PlayerNotPromoted.PlacedFirst");
    public LocalizedString leagueCompletionDescriptionPlayerWasNotPromotedPlacedSecondLocalizedString= new LocalizedString("League","LeagueCompleted.PlayerNotPromoted.PlacedSecond");
    public LocalizedString leagueCompletionDescriptionPlayerWasNotPromotedPlacedThirdLocalizedString= new LocalizedString("League","LeagueCompleted.PlayerNotPromoted.PlacedThird");
    public LocalizedString leagueCompletionDescriptionPlayerWasPromotedPlacedLowerThanThirdLocalizedString= new LocalizedString("League","LeagueCompleted.PlayerPromoted.OutsideTopThree");
    public LocalizedString leagueCompletionDescriptionDefaultLocalizedString= new LocalizedString("League","LeagueCompleted.PlayerNotPromoted.DefaultDescription");
    
    
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


        if (leagueCompletionTitleLocalizedString != null && !leagueCompletionTitleLocalizedString.IsEmpty)
        {
            leagueCompleteTitle = leagueCompletionTitleLocalizedString.GetLocalizedString() + " " + league.leagueName;
            
        }
        else
        {
            leagueCompleteTitle =  "You have completed all of the races in \nThe " + league.leagueName;;
        }
        
        leagueNameText.text =leagueCompleteTitle;
        
        
        if (playerWasRelegated)
        {
            if(leagueCompletionDescriptionPlayerWasRelegatedLocalizedString != null && !leagueCompletionDescriptionPlayerWasRelegatedLocalizedString.IsEmpty)
            {
                leagueCompletionDescriptionPlayerWasRelegatedLocalizedString.Arguments = new object[] { position, totalTeams, points, racesWon };
                leagueCompletionDescriptionPlayerWasRelegatedLocalizedString.RefreshString();
                leaguCompletionDescriptionText.text = leagueCompletionDescriptionPlayerWasRelegatedLocalizedString.GetLocalizedString();
            }
            else
            {
                leaguCompletionDescriptionText.text = $"Unfortunately, your team finished in position {position} out of {totalTeams} competitors, and has been relegated to a lower league. You earned {points} points with {racesWon} wins. Don't be disheartened, use this as motivation to come back stronger!";
            }
        }
        else if (playerWasPromoted && position == 1)
        {
            if(leagueCompletionDescriptionPlayerWasPromotedPlacedFirstLocalizedString != null && !leagueCompletionDescriptionPlayerWasPromotedPlacedFirstLocalizedString.IsEmpty)
            {
                leagueCompletionDescriptionPlayerWasPromotedPlacedFirstLocalizedString.Arguments = new object[] { totalTeams, points, racesWon };
                leagueCompletionDescriptionPlayerWasPromotedPlacedFirstLocalizedString.RefreshString();
                leaguCompletionDescriptionText.text = leagueCompletionDescriptionPlayerWasPromotedPlacedFirstLocalizedString.GetLocalizedString();
            }
            else
            {
                leaguCompletionDescriptionText.text = $"Outstanding! Your team finished in 1st position out of {totalTeams} competitors, and has been promoted to a higher league. You earned {points} points with {racesWon} wins. Keep up the great work!";
            }
        }
        else if (playerWasPromoted && position == 2)
        {
            if(leagueCompletionDescriptionPlayerWasPromotedPlacedSecondLocalizedString != null && !leagueCompletionDescriptionPlayerWasPromotedPlacedSecondLocalizedString.IsEmpty)
            {
                leagueCompletionDescriptionPlayerWasPromotedPlacedSecondLocalizedString.Arguments = new object[] { totalTeams, points, racesWon };
                leagueCompletionDescriptionPlayerWasPromotedPlacedSecondLocalizedString.RefreshString();
                leaguCompletionDescriptionText.text = leagueCompletionDescriptionPlayerWasPromotedPlacedSecondLocalizedString.GetLocalizedString();
            }
            else
            {
                leaguCompletionDescriptionText.text = $"Excellent! Your team finished in 2nd position out of {totalTeams} competitors, and has been promoted to a higher league. You earned {points} points with {racesWon} wins. Keep up the great work!";
            }
        }
        else if (playerWasPromoted && position == 3)
        {
            if (leagueCompletionDescriptionPlayerWasPromotedPlacedThirdLocalizedString != null && !leagueCompletionDescriptionPlayerWasPromotedPlacedThirdLocalizedString.IsEmpty)
            {
                leagueCompletionDescriptionPlayerWasPromotedPlacedThirdLocalizedString.Arguments = new object[]
                {
                    totalTeams, points, racesWon
                };
                leagueCompletionDescriptionPlayerWasPromotedPlacedThirdLocalizedString.RefreshString();
                leaguCompletionDescriptionText.text = leagueCompletionDescriptionPlayerWasPromotedPlacedThirdLocalizedString.GetLocalizedString();
            }
            else
            {
                leaguCompletionDescriptionText.text = $"Great work! Your team finished in 3rd position out of {totalTeams} competitors, and has been promoted to a higher league. You earned {points} points with {racesWon} wins. Keep up the great work!";
            }
        }
        else if(playerWasPromoted && position > 3)
        {
            if(leagueCompletionDescriptionPlayerWasPromotedPlacedHigherThanThirdLocalizedString != null && !leagueCompletionDescriptionPlayerWasPromotedPlacedHigherThanThirdLocalizedString.IsEmpty)
            {
                leagueCompletionDescriptionPlayerWasPromotedPlacedHigherThanThirdLocalizedString.Arguments = new object[]
                {
                    position, totalTeams, points, racesWon
                };
                leagueCompletionDescriptionPlayerWasPromotedPlacedHigherThanThirdLocalizedString.RefreshString();
                leaguCompletionDescriptionText.text = leagueCompletionDescriptionPlayerWasPromotedPlacedHigherThanThirdLocalizedString.GetLocalizedString();
            }
            else
            {
                leaguCompletionDescriptionText.text = $"Well done! Your team finished in position {position} out of {totalTeams} competitors, and has been promoted to a higher league. You earned {points} points with {racesWon} wins. Keep up the great work!";
            }
        }
        else if (position == 1)
        {
            if (leagueCompletionDescriptionPlayerWasNotPromotedPlacedFirstLocalizedString != null && !leagueCompletionDescriptionPlayerWasNotPromotedPlacedFirstLocalizedString.IsEmpty)
            {
                leagueCompletionDescriptionPlayerWasNotPromotedPlacedFirstLocalizedString.Arguments = new object[]
                {
                    totalTeams, points, racesWon
                };
                leagueCompletionDescriptionPlayerWasNotPromotedPlacedFirstLocalizedString.RefreshString();
                leaguCompletionDescriptionText.text = leagueCompletionDescriptionPlayerWasNotPromotedPlacedFirstLocalizedString.GetLocalizedString();
            }
            else
            {
                leaguCompletionDescriptionText.text = $"Fantastic! Your team are Champions finishing 1st out of {totalTeams} competitors. You earned {points} points with {racesWon} wins. Keep up the great work!";
            }
        }
        else if (position == 2)
        {
            if(leagueCompletionDescriptionPlayerWasNotPromotedPlacedSecondLocalizedString != null && !leagueCompletionDescriptionPlayerWasNotPromotedPlacedSecondLocalizedString.IsEmpty)
            {
                leagueCompletionDescriptionPlayerWasNotPromotedPlacedSecondLocalizedString.Arguments = new object[]
                {
                    totalTeams, points, racesWon
                };
                leagueCompletionDescriptionPlayerWasNotPromotedPlacedSecondLocalizedString.RefreshString();
                leaguCompletionDescriptionText.text = leagueCompletionDescriptionPlayerWasNotPromotedPlacedSecondLocalizedString.GetLocalizedString();
            }
            else
            {
                leaguCompletionDescriptionText.text = $"Awesome! Your team finished in 2nd position out of {totalTeams} competitors. You earned {points} points with {racesWon} wins. Keep up the great work!";
            }
        }
        else if (position == 3)
        {
            if(leagueCompletionDescriptionPlayerWasNotPromotedPlacedThirdLocalizedString != null && !leagueCompletionDescriptionPlayerWasNotPromotedPlacedThirdLocalizedString.IsEmpty)
            {
                leagueCompletionDescriptionPlayerWasNotPromotedPlacedThirdLocalizedString.Arguments = new object[]
                {
                    totalTeams, points, racesWon
                };
                leagueCompletionDescriptionPlayerWasNotPromotedPlacedThirdLocalizedString.RefreshString();
                leaguCompletionDescriptionText.text = leagueCompletionDescriptionPlayerWasNotPromotedPlacedThirdLocalizedString.GetLocalizedString();
            }
            else
            {
                leaguCompletionDescriptionText.text = $"Great job! Your team finished in 3rd position out of {totalTeams} competitors. You earned {points} points with {racesWon} wins.";
            }
        }
        else if (playerWasPromoted)
        {
            if(leagueCompletionDescriptionPlayerWasPromotedPlacedLowerThanThirdLocalizedString != null && !leagueCompletionDescriptionPlayerWasPromotedPlacedLowerThanThirdLocalizedString.IsEmpty)
            {
                leagueCompletionDescriptionPlayerWasPromotedPlacedLowerThanThirdLocalizedString.Arguments = new object[] { position, totalTeams, points, racesWon };
                leagueCompletionDescriptionPlayerWasPromotedPlacedLowerThanThirdLocalizedString.RefreshString();
                leaguCompletionDescriptionText.text = leagueCompletionDescriptionPlayerWasPromotedPlacedLowerThanThirdLocalizedString.GetLocalizedString();
            }
            else
            {
                leaguCompletionDescriptionText.text = $"Congratulations! Your team finished in position {position} out of {totalTeams} competitors, and has been promoted to a higher league. You earned {points} points with {racesWon} wins. Keep up the great work!";
            }
        }
        else
        {
            if(leagueCompletionDescriptionDefaultLocalizedString != null && !leagueCompletionDescriptionDefaultLocalizedString.IsEmpty)
            {
                leagueCompletionDescriptionDefaultLocalizedString.Arguments = new object[] { position, totalTeams, points, racesWon };
                leagueCompletionDescriptionDefaultLocalizedString.RefreshString();
                leaguCompletionDescriptionText.text = leagueCompletionDescriptionDefaultLocalizedString.GetLocalizedString();
            }
            else
            {
                leaguCompletionDescriptionText.text = $"Great job! Your team finished in position {position} out of {totalTeams} competitors. You earned {points} points with {racesWon} wins.";
            }
        }

    }
    
    public void OnClickAcceptCompletion()
    {
       LeagueController.Instance.StartNewSeason();
        Destroy(gameObject);
    }
    
}
