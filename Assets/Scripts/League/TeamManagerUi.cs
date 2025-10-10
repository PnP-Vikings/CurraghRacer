using UnityEngine;

public class TeamManagerUi : MonoBehaviour
{
   public Transform teamManagerUiParent;
   public Transform teamBenchUiParent;
   public Transform racersforHireUiParent;
   public GameObject teamManagerUiPrefab;
   
    public void ClearTeamMemberUis()
    {
         foreach (Transform child in teamManagerUiParent)
         {
              Destroy(child.gameObject);
         }
         foreach (Transform child in teamBenchUiParent)
         {
              Destroy(child.gameObject);
         }
        foreach (Transform child in racersforHireUiParent)
        {
            Destroy(child.gameObject);
        }
    }
   
   void OnEnable()
   {
       if (teamManagerUiParent != null && teamManagerUiPrefab != null)
       {
           if (TeamManager.Instance != null)
           {
               if (TeamManager.Instance.activeCrewMembers == null || TeamManager.Instance.activeCrewMembers.Count == 0)
               {
                   Debug.Log("TeamManagerUi: No crew members to display.");
                   return;
               }
               foreach (TeamMember crewMember in TeamManager.Instance.activeCrewMembers)
               {
                   Debug.Log("TeamManagerUi: Creating UI for crew member: " + crewMember.memberName);
                   GameObject crewMemberUi = Instantiate(teamManagerUiPrefab, teamManagerUiParent);
                   TeamMemberUiHandler crewMemberUiComponent = crewMemberUi.GetComponent<TeamMemberUiHandler>();
                   if (crewMemberUiComponent != null)
                   {
                       crewMemberUiComponent.SetMemberData(crewMember);
                   }
               }
               foreach (TeamMember benchMember in TeamManager.Instance.benchTeamMembers)
               {
                   Debug.Log("TeamManagerUi: Creating UI for bench member: " + benchMember.memberName);
                   GameObject benchMemberUi = Instantiate(teamManagerUiPrefab, teamBenchUiParent);
                   TeamMemberUiHandler benchMemberUiComponent = benchMemberUi.GetComponent<TeamMemberUiHandler>();
                   if (benchMemberUiComponent != null)
                   {
                       benchMemberUiComponent.SetMemberData(benchMember);
                   }
               }
                foreach (TeamMember hireMember in TeamManager.Instance.racersForHire)
                {
                     Debug.Log("TeamManagerUi: Creating UI for hire member: " + hireMember.memberName);
                     GameObject hireMemberUi = Instantiate(teamManagerUiPrefab, racersforHireUiParent);
                     TeamMemberUiHandler hireMemberUiComponent = hireMemberUi.GetComponent<TeamMemberUiHandler>();
                     if (hireMemberUiComponent != null)
                     {
                          hireMemberUiComponent.SetMemberData(hireMember);
                     }
                }
           }
           else
           {
               Debug.LogError("TeamManagerUi: TeamManager instance is null.");
               return;
           }
       }
   }
   
   void OnDisable()
   {
       ClearTeamMemberUis();
   }
   
   
}


