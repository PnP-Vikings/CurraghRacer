using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class TeamManager : MonoBehaviour
{
    public static TeamManager Instance { get; private set; }

    public Team playerTeam;
    public TeamMember teamManager;
    public List<TeamMember> activeCrewMembers;
    public List<TeamMember> benchTeamMembers;
    public List<HireableTeamMembers> startingHireableRacers;
    public List<HireableTeamMembers> racersForHire;
    public bool isAllActiveTeamMembersHealthy = false;
    [FormerlySerializedAs("OnTeamMemberHired")] public UnityEvent onTeamMemberHired;
    public UnityEvent onTeamMembersUpdated;
    
    public TeamMember selectedActiveTeamMember;
    public TeamMember selectedBenchTeamMember;

    public SetActiveTeamMemberSelected oldSelectedActiveTeamMember;
    public SetBenchTeamMemberSelected oldSelectedBenchTeamMember;
    
    public void ClearSelectedTeamMembers()    
    {
        selectedActiveTeamMember = null;
        selectedBenchTeamMember = null;
    }
    
    
    public void SetSelectedActiveTeamMember(TeamMember member,SetActiveTeamMemberSelected caller)
    {
        if(selectedActiveTeamMember != null)
            oldSelectedActiveTeamMember.HideSelectionUi();
        
        oldSelectedActiveTeamMember = caller;
        
        selectedActiveTeamMember = member;
        TrySwapSelectedMembers();
    }
    
    public void SetSelectedBenchTeamMember(TeamMember member,SetBenchTeamMemberSelected caller)
    {
        if(selectedBenchTeamMember != null)
            oldSelectedBenchTeamMember.HideSelectionUi();
        
        oldSelectedBenchTeamMember = caller;
        selectedBenchTeamMember = member;
        TrySwapSelectedMembers();
    }
    public void TrySwapSelectedMembers()
    {
        if (selectedActiveTeamMember != null && selectedBenchTeamMember != null)
        {
            SwapTeamMembers(selectedBenchTeamMember, selectedActiveTeamMember);
            ClearSelectedTeamMembers();
        }
        else
        {
            Debug.LogWarning("Both an active team member and a bench team member must be selected to swap.");
        }
        
        
        
    }
    
    public void SetTeamManager(TeamMember manager)
    {
        teamManager = manager;
    }
    public void SetActiveCrewMembers( List<TeamMember> activeMembers)
    {
        activeCrewMembers = activeMembers;
    }
   
    public void SetRacersForHire(List<HireableTeamMembers> availableRacers)
    {
        racersForHire = availableRacers;
    }
    
    public void SetBenchTeamMembers(List<TeamMember> benchMembers)
    {
        benchTeamMembers = benchMembers;
    }
    
    public void UpdateLists()
    {
        if(playerTeam != null)
        {
            Debug.Log("Updating team lists in TeamManager.");
            SetActiveCrewMembers(playerTeam.teamMembers);
            SetBenchTeamMembers(playerTeam.bench);
            onTeamMembersUpdated?.Invoke();
        }
        else
        {
            Debug.LogWarning("Player team is not set in TeamManager.");
            return;
        }
     
    }

    public void SetBenchTeamMembers()
    {
        if(playerTeam == null)
        {
            Debug.LogWarning("Player team is not set in TeamManager.");
            return;
        }
        benchTeamMembers = playerTeam.bench;
    }
    
    public void SetPlayerTeam(Team team)
    {
            playerTeam = team;
    }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void HireRacer(HireableTeamMembers racer)
    {

        if (playerTeam.bench.Count >= 3)
        {
            PlayerStatsView.Instance.ClearInfo(); 
            PlayerStatsView.Instance.DisplayInfo($"Bench is full! Cannot hire more racers.");
            return;
        }
        
        
        if ( PlayerManager.Instance.PurchaseItem(racer.hireCost,purchaseType: PurchaseType.HireRacer)) // Assuming a max of 3 bench members
        {
            playerTeam.bench.Add(racer);
            SetBenchTeamMembers();
            racersForHire.Remove(racer);
            UpdateLists();
            PlayerStatsView.Instance.ClearInfo();
            PlayerStatsView.Instance.DisplayInfo($"Hired {racer.memberName} for {racer.hireCost} coins.");
            Debug.Log($"Hired {racer.memberName} to the team.");
            onTeamMemberHired?.Invoke();
        }
        else
        { 
            PlayerStatsView.Instance.ClearInfo(); 
            PlayerStatsView.Instance.DisplayInfo($"Could not afford to hire player.");
            Debug.Log("Could not afford to hire player.");
        }
    }
    
    public void CheckActiveTeamHealth()
    {
        isAllActiveTeamMembersHealthy = true; // Assume all are healthy initially
        
        foreach (var member in playerTeam.teamMembers)
        {
            if (!member.fitness.IsPlayerFitToRace())
            {
                isAllActiveTeamMembersHealthy = false;
                Debug.Log($"{member.memberName} is not healthy!");
                break; // No need to check further, one unhealthy member is enough
            }
        }
        
        if (isAllActiveTeamMembersHealthy)
        {
            Debug.Log("All active team members are healthy.");
        }
    }
    
  public void ResetHireableRacersForHire()
    {
        racersForHire.Clear();
        racersForHire = new List<HireableTeamMembers>(startingHireableRacers);
        racersForHire = racersForHire.OrderBy(r => r.hireCost).ToList();
    }
    
  public void SwapTeamMembers(TeamMember memberToActivate, TeamMember memberToBench)
    {
        if (playerTeam.teamMembers.Contains(memberToBench) && playerTeam.bench.Contains(memberToActivate))
        {
            playerTeam.teamMembers.Remove(memberToBench);
            playerTeam.bench.Remove(memberToActivate);
            
            playerTeam.teamMembers.Add(memberToActivate);
            playerTeam.bench.Add(memberToBench);
            
            UpdateLists();
            Debug.Log($"Swapped {memberToActivate.memberName} with {memberToBench.memberName}.");
        }
        else
        {
            Debug.Log("One or both members not found in their respective lists.");
        }
    }
    
    public void AddTeamMember(TeamMember newMember)
    {
        if (playerTeam.teamMembers.Count < 3)
        {
            playerTeam.teamMembers.Add(newMember);
            UpdateLists();
            Debug.Log($"Added {newMember.memberName} to the team.");
        }
        else
        {
            Debug.Log("Team is full! Cannot add more members.");
        }
    }

    public void RemoveTeamMember(TeamMember memberToRemove)
    {
        if (playerTeam.teamMembers.Contains(memberToRemove))
        {
            playerTeam.teamMembers.Remove(memberToRemove);
            UpdateLists();
            Debug.Log($"Removed {memberToRemove.memberName} from the team.");
        }
        else
        {
            Debug.Log($"{memberToRemove.memberName} is not in the team.");
        }
    }
    
    
    
}
