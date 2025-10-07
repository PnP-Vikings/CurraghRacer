using UnityEngine;


[CreateAssetMenu(fileName = "NewHireableTeamMember", menuName = "League/HireableTeamMember")]

public class HireableTeamMembers : TeamMember
{
    public int hireCost;
    public int skillLevel;
    public override CharacterStats GetDefaultStatsBasedOnAttitude(int teamQuality = 1)
    {
        switch (attitude)
        {
            case Attitude.Positive:
                return new CharacterStats(6f * skillLevel, 5f * skillLevel, 6f * skillLevel, 7f * skillLevel);
            case Attitude.Competitive:
                return new CharacterStats(7f * skillLevel, 5f * skillLevel, 6f * skillLevel, 4f * skillLevel);
            case Attitude.Cooperative:
                return new CharacterStats(5f * skillLevel, 5f * skillLevel, 6f * skillLevel, 8f * skillLevel);
            case Attitude.Aggressive:
                return new CharacterStats(7f * skillLevel, 6f * skillLevel, 5f * skillLevel, 3f * skillLevel);
            case Attitude.Energetic:
                return new CharacterStats(6f * skillLevel, 7f * skillLevel, 5f * skillLevel, 5f * skillLevel);
            case Attitude.Cautious:
                return new CharacterStats(5f * skillLevel, 6f * skillLevel, 7f * skillLevel, 6f * skillLevel);
            case Attitude.Lazy:
                return new CharacterStats(4f * skillLevel, 4f * skillLevel, 5f * skillLevel, 5f * skillLevel);
            case Attitude.Negative:
                return new CharacterStats(5f * skillLevel, 5f * skillLevel, 5f * skillLevel, 3f * skillLevel);
            case Attitude.Neutral:
            default:
                return new CharacterStats(5f * skillLevel, 5f * skillLevel, 5f * skillLevel, 5f * skillLevel);
        }
    }
    
    
}
