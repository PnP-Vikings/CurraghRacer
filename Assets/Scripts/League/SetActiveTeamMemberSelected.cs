using System;
using UnityEngine;
using UnityEngine.UI;

public class SetActiveTeamMemberSelected : MonoBehaviour
{
   public GameObject teamMemberSelectedUi;
   public Button btn;

   public void OnEnable()
   {
       
       teamMemberSelectedUi.SetActive(false);
       if (btn != null)
       {
           btn.interactable = true;
           btn.onClick.RemoveAllListeners();
           btn.onClick.AddListener(TeamMemberSelected);
       }
       else
       {
           Debug.LogWarning("SetBenchTeamMemberSelected: Button reference is null on OnEnable.");
       }
   }

   public void TeamMemberSelected()
   {
      if(TeamManager.Instance != null && teamMemberSelectedUi != null)
      {
          teamMemberSelectedUi.SetActive(true);
          btn.interactable = false;
          TeamManager.Instance.selectedActiveTeamMember = this.GetComponent<TeamMemberUiHandler>().teamMember;
          TeamManager.Instance.TrySwapSelectedMembers();
      }
      else
      {
          Debug.LogWarning("SetActiveTeamMemberSelected: TeamManager instance or TeamMemberSelected is null.");
      }
   }

   private void OnDisable()
   {
       if (TeamManager.Instance != null && teamMemberSelectedUi != null)
       {
           TeamManager.Instance.ClearSelectedTeamMembers();
       }
       
   }
}
