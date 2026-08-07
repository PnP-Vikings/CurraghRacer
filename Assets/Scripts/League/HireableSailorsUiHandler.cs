using League;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
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
    
    [Header("Localization")]
    [SerializeField] private LocalizedString _localizedAgeText;
    [SerializeField] private LocalizedString _localizedStrengthText;
    [SerializeField] private LocalizedString _localizedStaminaText;
    [SerializeField] private LocalizedString _localizedTechniqueText;
    [SerializeField] private LocalizedString _localizedTeamWorkText;
    [SerializeField] private LocalizedString _localizedStarRatingText;
    [SerializeField] private LocalizedString _localizedCostText;
    [SerializeField] private LocalizedString _localizedAvailabilityText;
    
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
            if (!_localizedStarRatingText.IsEmpty)
            {
                _localizedStarRatingText.Arguments = new object[] { starRating };
                _localizedStarRatingText.RefreshString();
                starRatingText.text = _localizedStarRatingText.GetLocalizedString();
            }
            else
            {
                starRatingText.text = "Star Rating: " + starRating.ToString();
            }
        }
        else if (starRatingText != null)
        {
            if (!_localizedStarRatingText.IsEmpty)
            {
                _localizedStarRatingText.Arguments = new object[] {  "N/A" };
                _localizedStarRatingText.RefreshString();
                starRatingText.text = _localizedStarRatingText.GetLocalizedString();
            }
            else
            {
             
                starRatingText.text = "Star Rating: N/A";
            }
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
        
        if (!_localizedStrengthText.IsEmpty)
        {
            _localizedStrengthText.Arguments = new object[] { strength };
            _localizedStrengthText.RefreshString();
            strengthText.text = _localizedStrengthText.GetLocalizedString();
        }
        else
        {
            strengthText.text = "Strength: " + strength.ToString();
        }

        if (!_localizedStaminaText.IsEmpty)
        {
            _localizedStaminaText.Arguments = new object[] { stamina };
            _localizedStaminaText.RefreshString();
            staminaText.text = _localizedStaminaText.GetLocalizedString();
        }
        else
        {
            staminaText.text = "Stamina: " + stamina.ToString();
        }

        if (!_localizedTechniqueText.IsEmpty)
        {
            _localizedTechniqueText.Arguments = new object[] { technique };
            _localizedTechniqueText.RefreshString();
            techniqueText.text = _localizedTechniqueText.GetLocalizedString();
        }
        else
        {
            techniqueText.text = "Technique: " + technique.ToString();
        }

        if (!_localizedTeamWorkText.IsEmpty)
        {
            _localizedTeamWorkText.Arguments = new object[] { teamWork };
            _localizedTeamWorkText.RefreshString();
            teamWorkText.text = _localizedTeamWorkText.GetLocalizedString();
        }
        else
        {
            teamWorkText.text = "Team Work: " + teamWork.ToString();
        }

        if (!_localizedAgeText.IsEmpty)
        {
            _localizedAgeText.Arguments = new object[] { age };
            _localizedAgeText.RefreshString();
            ageText.text = _localizedAgeText.GetLocalizedString();
        }
        else
        {
            ageText.text = "Age: " + age.ToString();
        }

        if (!_localizedCostText.IsEmpty)
        {
            _localizedCostText.Arguments = new object[] { cost };
            _localizedCostText.RefreshString();
            costText.text = _localizedCostText.GetLocalizedString();
        }
        else
        {
            costText.text = "Cost: " + cost.ToString();
        }

        if (!_localizedAvailabilityText.IsEmpty)
        {
            _localizedAvailabilityText.Arguments = new object[] { availability };
            _localizedAvailabilityText.RefreshString();
            availabilityText.text = _localizedAvailabilityText.GetLocalizedString();
        }
        else
        {
            availabilityText.text = "Available for: " + availability.ToString() + " races";
        }

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
