using League;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class BenchTeamMemberUiHandler : MonoBehaviour
{
    
    [Header("UI Elements")]
    public TMP_Text memberNameText,
        memberStarRatingText;
    public Image memberIconImage;
    public TeamMember teamMember;
    [Header("Localization")]
    [SerializeField] private LocalizedString _localizedStarRatingText;
    public void ClearMemberData()
    {
        memberNameText.text = "N/A";
        if (!_localizedStarRatingText.IsEmpty)
        {
            _localizedStarRatingText.Arguments = new object[] { "N/A" };
            _localizedStarRatingText.RefreshString();
            memberStarRatingText.text = _localizedStarRatingText.GetLocalizedString();
        }
        else
        {
            memberStarRatingText.text = "Star Rating: N/A";
            Debug.LogWarning("Localized star rating text is not set!");
        }
        memberIconImage.sprite = null;
        memberIconImage.gameObject.SetActive(false);
        teamMember = null;
    }
    public void SetMemberData(TeamMember member)
    {
        memberIconImage.sprite = member != null ? member.memberIcon : null;

        if (memberIconImage.sprite != null)
        {
            memberIconImage.gameObject.SetActive(true);
        }
        else
        {
            memberIconImage.gameObject.SetActive(false);
            Debug.LogWarning("Member icon image or sprite is not set!");
        }
        
        memberNameText.text = member.memberName;
        if (LeagueController.Instance != null && LeagueController.Instance.currentLeague != null)
        {
            if (!_localizedStarRatingText.IsEmpty)
            {
                _localizedStarRatingText.Arguments = new object[] { LeagueController.Instance.CalculateTeamMemberStarRating(member) };
                _localizedStarRatingText.RefreshString();
                memberStarRatingText.text = _localizedStarRatingText.GetLocalizedString();
            }
            else
            {
                memberStarRatingText.text =
                    "Star Rating: " + LeagueController.Instance.CalculateTeamMemberStarRating(member);
            }
        }
        else
        {
            if (!_localizedStarRatingText.IsEmpty)
            {
                _localizedStarRatingText.Arguments = new object[] { "N/A" };
                _localizedStarRatingText.RefreshString();
                memberStarRatingText.text = _localizedStarRatingText.GetLocalizedString();
            }
            else
            {
                memberStarRatingText.text = "Star Rating: N/A";
                Debug.LogWarning("Localized star rating text is not set!");
            }
            Debug.LogWarning("LeagueController or currentLeague is null!");
        }
    
        teamMember = member;
      
    }
}
