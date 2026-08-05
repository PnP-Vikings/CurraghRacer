using League;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HireableSailorsUiHandler : MonoBehaviour
{
public string memberName,memberDescription,attitude;
    public int strength,stamina,technique,teamWork,cost,availability;
    public int age,starRating;
    
    public TMP_Text memberNameText,
        memberDescriptionText,
        attitudeText,
        strengthText,
        staminaText,
        techniqueText,
        teamWorkText,
        ageText, costText, availabilityText, starRatingText;
    
    public Image memberIconImage;
    
    public HireableTeamMembers hireableTeamMember;
    
    
    
    public void SetMemberData(HireableTeamMembers member)
    {
        if (member == null)
        {
            Debug.LogError("Team member data is null!");
            return;
        }
        
        memberName = member.memberName;
        memberDescription = member.memberDescription;
        attitude = member.attitude.ToString();
        cost = member.hireCost;
        availability = member.racesAvailableFor;
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
            memberIconImage.sprite = null;
            Debug.LogWarning("Member icon is not set!");
        }
        hireableTeamMember = member;

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
        costText.text = "Cost: "+cost.ToString();
        availabilityText.text = "Available for: "+availability.ToString()+" races";

        
        if (memberIconImage.sprite != null)
        {
            memberIconImage.gameObject.SetActive(true);
        }
        else
        {
            memberIconImage.gameObject.SetActive(false);
           Debug.LogWarning("Member icon image or sprite is not set!");
        }
        
       
            
    }
    
    public void OnHireButtonClick()
    {
        if (hireableTeamMember == null)
        {
            Debug.LogError("Team member data is null!");
            return;
        }

        if (TeamManager.Instance!=null && hireableTeamMember != null)
        {
            TeamManager.Instance.HireRacer(hireableTeamMember);
        }
        else
        {
            Debug.LogError("TeamManager instance is null!");
        }
      
    }
    
  
    
}
