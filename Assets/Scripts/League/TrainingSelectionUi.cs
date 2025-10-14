using UnityEngine;

public class TrainingSelectionUi : MonoBehaviour
{
    public GameObject selectedUiElement;
    public TrainingMenu trainingMenu;
    public TeamMember selectedTeamMember;
    
    public void SetTrainingMenu(TrainingMenu menu,TeamMember member)
    {
        trainingMenu = menu;
        selectedTeamMember = member;
    }
    
    
    public void SetSelectedTeamMember()
    {
        ShowSelectionUi();
        trainingMenu.SetSelectedTeamMember(selectedTeamMember,this);
    }
 
    public void ShowSelectionUi()
    {
        if (selectedUiElement != null)
        {
            selectedUiElement.SetActive(true);
        }
    }
    public void HideSelectionUi()
    {
        if (selectedUiElement != null)
        {
            selectedUiElement.SetActive(false);
        }
    }
    
    
}
