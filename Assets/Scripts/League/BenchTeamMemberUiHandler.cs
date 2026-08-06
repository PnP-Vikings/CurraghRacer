using League;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BenchTeamMemberUiHandler : MonoBehaviour
{
    
    public TMP_Text memberNameText,
        memberStarRatingText;
    public Image memberIconImage;
    public TeamMember teamMember;
    
  

    public void ClearMemberData()
    {
        memberNameText.text = "N/A";
        memberStarRatingText.text = "Star Rating: N/A";
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
            memberStarRatingText.text =
                "Star Rating: " + LeagueController.Instance.CalculateTeamMemberStarRating(member);
        }
        else
        {
            memberStarRatingText.text = "Star Rating: N/A";
            Debug.LogWarning("LeagueController or currentLeague is null!");
        }
    
        teamMember = member;
      
    }
}
