using UnityEngine;

public class TeamManagerUi : MonoBehaviour
{
   public Transform teamManagerUiParent;
   public Transform teamBenchUiParent;
   public Transform racersforHireUiParent;
   public TeamMemberUiHandler teamManagerUiPrefab;
   public BenchTeamMemberUiHandler benchTeamMemberUiHandler;
   public HireableSailorsUiHandler hireableSailorsUiHandler;
   
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
     RefreshManagerUi();
     
     if(TeamManager.Instance != null)
     {
         TeamManager.Instance.onTeamMemberHired.AddListener(RefreshManagerUi);
            TeamManager.Instance.onTeamMembersUpdated.AddListener(RefreshManagerUi);
     }
     else
     {
         Debug.LogError("TeamManagerUi: TeamManager instance is null on OnEnable.");
     }
   }
   
   
   public void RefreshManagerUi()
   {
       ClearTeamMemberUis();
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
                   GameObject crewMemberUi = Instantiate(teamManagerUiPrefab.gameObject, teamManagerUiParent);
                   if (crewMemberUi != null)
                   {
                       TeamMemberUiHandler handler = crewMemberUi.GetComponent<TeamMemberUiHandler>();
                       if (handler != null)
                       {
                           handler.ClearMemberData();
                           handler.SetMemberData(crewMember);
                       }
                   }
               }
               foreach (TeamMember benchMember in TeamManager.Instance.benchTeamMembers)
               {
                   Debug.Log("TeamManagerUi: Creating UI for bench member: " + benchMember.memberName);
                   GameObject benchMemberUi = Instantiate(benchTeamMemberUiHandler.gameObject, teamBenchUiParent);
                    if (benchMemberUi != null)
                   {
                       BenchTeamMemberUiHandler handler = benchMemberUi.GetComponent<BenchTeamMemberUiHandler>();
                       if (handler != null)
                       {
                           handler.ClearMemberData();
                           handler.SetMemberData(benchMember);
                       }
                   }
               }
                foreach (HireableTeamMembers hireMember in TeamManager.Instance.racersForHire)
                {
                     Debug.Log("TeamManagerUi: Creating UI for hire member: " + hireMember.memberName);
                     GameObject hireMemberUi = Instantiate(hireableSailorsUiHandler.gameObject, racersforHireUiParent);
                      if (hireMemberUi != null)
                     {
                         HireableSailorsUiHandler handler = hireMemberUi.GetComponent<HireableSailorsUiHandler>();
                         if (handler != null)
                         {
                             handler.SetMemberData(hireMember);
                         }
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
