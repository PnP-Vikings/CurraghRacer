using League;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class TeamMemberUiHandler : MonoBehaviour
{
    [Header("Member Data")]
    public string memberName,memberDescription,attitude;
    public int strength,stamina,technique,teamWork;
    public int age,starRating;
    [Header("UI Elements")]
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
    
    
    [Header("Localization")]
    [SerializeField] private LocalizedString _localizedAgeText;
    [SerializeField] private LocalizedString _localizedStrengthText;
    [SerializeField] private LocalizedString _localizedStaminaText;
    [SerializeField] private LocalizedString _localizedTechniqueText;
    [SerializeField] private LocalizedString _localizedTeamWorkText;
    [SerializeField] private LocalizedString _localizedStarRatingText;
    
    public void SetMemberData(TeamMember member)
    {
        if (member == null)
        {
            Debug.LogError("Team member data is null!");
            return;
        }

        memberName = member.memberName;
        memberDescription = member.memberDescription;
        attitude = member.GetLocalizedAttitudeString();
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
            memberIconImage.color = Color.white; // Ensure the image is visible
        }
        else
        {
            memberIconImage.sprite = null;
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
        if(!_localizedStrengthText.IsEmpty)
        {
            _localizedStrengthText.Arguments = new object[] { strength };
            _localizedStrengthText.RefreshString();
            strengthText.text = _localizedStrengthText.GetLocalizedString();
        }
        else
        {
            strengthText.text = "Strength: "+ strength.ToString();
        }
        if(!_localizedStaminaText.IsEmpty)
        {
            _localizedStaminaText.Arguments = new object[] { stamina };
            _localizedStaminaText.RefreshString();
            staminaText.text = _localizedStaminaText.GetLocalizedString();
        }
        else
        {
            staminaText.text = "Stamina: "+ stamina.ToString();
        }
        if(!_localizedTechniqueText.IsEmpty)
        {
            _localizedTechniqueText.Arguments = new object[] { technique };
            _localizedTechniqueText.RefreshString();
            techniqueText.text = _localizedTechniqueText.GetLocalizedString();
        }
        else
        {
            techniqueText.text = "Technique: "+ technique.ToString();
        }
        if(!_localizedTeamWorkText.IsEmpty)
        {
            _localizedTeamWorkText.Arguments = new object[] { teamWork };
            _localizedTeamWorkText.RefreshString();
            teamWorkText.text = _localizedTeamWorkText.GetLocalizedString();
        }
        else
        {
            teamWorkText.text = "Team Work: "+ teamWork.ToString();
        }
        if(!_localizedAgeText.IsEmpty)
        {
            _localizedAgeText.Arguments = new object[] { age };
            _localizedAgeText.RefreshString();
            ageText.text = _localizedAgeText.GetLocalizedString();
        }
        else
        {
            ageText.text = "Age: "+age.ToString();
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
        
        if(!_localizedStrengthText.IsEmpty)
        {
            _localizedStrengthText.Arguments = new object[] { "N/A" };
            _localizedStrengthText.RefreshString();
            strengthText.text = _localizedStrengthText.GetLocalizedString();
        }
        else
        {
            strengthText.text = "Strength: N/A";
        }
        if(!_localizedStaminaText.IsEmpty)
        {
            _localizedStaminaText.Arguments = new object[] { "N/A" };
            _localizedStaminaText.RefreshString();
            staminaText.text = _localizedStaminaText.GetLocalizedString();
        }
        else
        {
            staminaText.text = "Stamina: N/A";
        }
        if(!_localizedTechniqueText.IsEmpty)
        {
            _localizedTechniqueText.Arguments = new object[] { "N/A" };
            _localizedTechniqueText.RefreshString();
            techniqueText.text = _localizedTechniqueText.GetLocalizedString();
        }
        else
        {
            techniqueText.text = "Technique: N/A";
        }
        if(!_localizedTeamWorkText.IsEmpty)
        {
            _localizedTeamWorkText.Arguments = new object[] { "N/A" };
            _localizedTeamWorkText.RefreshString();
            teamWorkText.text = _localizedTeamWorkText.GetLocalizedString();
        }
        else
        {
            teamWorkText.text = "Team Work: N/A";
        }
        if(!_localizedAgeText.IsEmpty)
        {
            _localizedAgeText.Arguments = new object[] { "N/A" };
            _localizedAgeText.RefreshString();
            ageText.text = _localizedAgeText.GetLocalizedString();
        }
        else
        {
            ageText.text = "Age: N/A";
        }
        if(!_localizedStarRatingText.IsEmpty)
        {
            _localizedStarRatingText.Arguments = new object[] { "N/A" };
            _localizedStarRatingText.RefreshString();
            starRatingText.text = _localizedStarRatingText.GetLocalizedString();
        }
        else
        {
            starRatingText.text = "Star Rating: N/A";
        }

        if (memberIconImage != null)
        {
            memberIconImage.gameObject.SetActive(false);
        }
    }
  
    
}
