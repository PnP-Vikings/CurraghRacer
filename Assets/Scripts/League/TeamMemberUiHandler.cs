using League;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamMemberUiHandler : MonoBehaviour
{
    public string memberName,memberDescription,attitude;
    public int strength,stamina,technique,teamWork;
    public int age,starRating;
    
    public TMP_Text memberNameText,
        memberDescriptionText,
        attitudeText,
        strengthText,
        staminaText,
        techniqueText,
        teamWorkText,
        ageText,starRatingText;
    
    public Image memberIconImage;
    public TeamMember teamMember;
    
    
   
    
    public void SetMemberData(TeamMember member)
    {
        if (member == null)
        {
            Debug.LogError("Team member data is null!");
            return;
        }

        memberName = member.memberName;
        memberDescription = member.memberDescription;
        attitude = member.attitude.ToString();
        strength = Mathf.RoundToInt(member.characterStats.strength);
        stamina = Mathf.RoundToInt(member.characterStats.stamina);
        technique = Mathf.RoundToInt(member.characterStats.technique);
        teamWork = Mathf.RoundToInt(member.characterStats.teamWork);
        age = member.age;
        
        if (LeagueController.Instance != null && LeagueController.Instance.currentLeague != null && starRatingText != null)
        {
            starRating = LeagueController.Instance.CalculateTeamMemberStarRating(member);
            starRatingText.text = "Star Rating: " + starRating.ToString();
        }
        else if (starRatingText != null)
        {
            starRatingText.text = "Star Rating: N/A";
            starRatingText.gameObject.SetActive(false);
            Debug.LogWarning("LeagueController or currentLeague is null!");
        }
        
        if (member.memberIcon != null)
        {
            memberIconImage.sprite = member.memberIcon;
        }
        else
        {
            Debug.LogWarning("Member icon is not set!");
        }
        
        teamMember = member;

        SetupUi();
    }
    
    private void SetupUi()
    {
        memberNameText.text = memberName;
        memberDescriptionText.text = memberDescription;
        attitudeText.text = attitude;
        strengthText.text = "Strength: "+  strength.ToString();
        staminaText.text = "Stamina: "+ stamina.ToString();
        techniqueText.text = "Technique: "+ technique.ToString();
        teamWorkText.text = "Team Work: "+ teamWork.ToString();
        ageText.text = "Age: "+age.ToString();

        
        if (memberIconImage.sprite != null)
        {
            memberIconImage.gameObject.SetActive(true);
            memberIconImage.sprite = memberIconImage.sprite;
        }
        else
        {
            memberIconImage.gameObject.SetActive(false);
           Debug.LogWarning("Member icon image or sprite is not set!");
        }
    }
    
    
    public void ClearMemberData()
    {
        memberName = "";
        memberDescription = "";
        attitude = "";
        strength = 0;
        stamina = 0;
        technique = 0;
        teamWork = 0;
        age = 0;
        memberIconImage.sprite = null;
        teamMember = null;

        memberNameText.text = "N/A";
        memberDescriptionText.text = "N/A";
        attitudeText.text = "N/A";
        strengthText.text = "Strength: N/A";
        staminaText.text = "Stamina: N/A";
        techniqueText.text = "Technique: N/A";
        teamWorkText.text = "Team Work: N/A";
        ageText.text = "Age: N/A";
        starRatingText.text = "Star Rating: N/A";

        if (memberIconImage != null)
        {
            memberIconImage.gameObject.SetActive(false);
        }
    }
  
    
}
