using System.Linq;
using UnityEngine;

public class TeamManager : MonoBehaviour
{
    public static TeamManager Instance { get; private set; }

    public Team playerTeam;
    public TeamMember[] benchTeamMembers;
    public TeamMember[] racersForHire;
    public bool isAllActiveTeamMembersHealthy = false;
    
    
    public void SetRacersForHire(TeamMember[] availableRacers)
    {
        racersForHire = availableRacers;
    }
    
    public void SetBenchTeamMembers(TeamMember[] benchMembers)
    {
        benchTeamMembers = benchMembers;
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
    
    
    public void AddTeamMember(TeamMember newMember)
    {
        if (playerTeam.teamMembers.Count < 3)
        {
            playerTeam.teamMembers.Add(newMember);
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
            Debug.Log($"Removed {memberToRemove.memberName} from the team.");
        }
        else
        {
            Debug.Log($"{memberToRemove.memberName} is not in the team.");
        }
    }
    
    
    
}
