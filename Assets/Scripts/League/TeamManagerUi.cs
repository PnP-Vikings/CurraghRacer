using UnityEngine;

public class TeamManagerUi : MonoBehaviour
{
   public Transform teamManagerUiParent;
   public GameObject teamManagerUiPrefab;
   
   void OnEnable()
   {
       if (teamManagerUiParent != null && teamManagerUiPrefab != null)
       {
           /*if (TeamManager.Instance != null)
           {
               if (TeamManager.Instance.crewMembers == null || TeamManager.Instance.crewMembers.Count == 0)
               {
                   Debug.Log("TeamManagerUi: No crew members to display.");
                   return;
               }
               foreach (CrewMember crewMember in TeamManager.Instance.crewMembers)
               {
                   Debug.Log("TeamManagerUi: Creating UI for crew member: " + crewMember.memberName);
                   GameObject crewMemberUi = Instantiate(teamManagerUiPrefab, teamManagerUiParent);
                   CrewMemberUi crewMemberUiComponent = crewMemberUi.GetComponent<CrewMemberUi>();
                   if (crewMemberUiComponent != null)
                   {
                       crewMemberUiComponent.SetCrewMemberUi(crewMember);
                   }
               }
           }
           else
           {
               Debug.LogError("TeamManagerUi: TeamManager instance is null.");
               return;
           }*/
       }
   }
   
   
}


