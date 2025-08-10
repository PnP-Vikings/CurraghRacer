using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamMemberUiHandler : MonoBehaviour
{
    public string memberName,memberDescription,attitude;
    public int strength,stamina,technique,teamWork;
    public int age;
    
    public TMP_Text memberNameText,
        memberDescriptionText,
        attitudeText,
        strengthText,
        staminaText,
        techniqueText,
        teamWorkText,
        ageText;
    
    public Image memberIconImage;
    
    
    
    
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
        
        if (member.memberIcon != null)
        {
            memberIconImage.sprite = member.memberIcon;
        }
        else
        {
            Debug.LogWarning("Member icon is not set!");
        }

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
    
  
    
}
