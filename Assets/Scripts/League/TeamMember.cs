using UnityEngine;

public enum Attitude
{
    Positive,    // generally upbeat and optimistic
    Neutral,     // balanced, neither overly positive nor negative
    Negative,    // pessimistic or downbeat
    Competitive, // thrives on competition and challenges
    Cooperative, // works well with others, team-oriented
    Cautious,    // careful and risk-averse
    Aggressive,  // assertive and driven
    Energetic,   // high energy, enthusiastic
    Lazy         // low motivation, needs encouragement
}






[CreateAssetMenu(fileName = "NewTeamMember", menuName = "League/TeamMember")]
public class TeamMember : ScriptableObject
{
    [Header("Team Member Info")]
    [Tooltip("The display name of the team member.")]
    public string memberName;

    public Sprite memberIcon;

    [Tooltip("A short biography or backstory for this member.")]
    public string memberDescription;

    public int age;

    [Tooltip("The general attitude of this member during races/events.")]
    public Attitude attitude = Attitude.Neutral;

    [Header("Team Member Stats")]
    [Tooltip("Base stats: strength, stamina, technique, team work.")]
    public CharacterStats characterStats = new CharacterStats(5f, 5f, 5f, 5f); // default stats: strength, stamina, technique, teamWork

    [Header("Progression")]
    [Tooltip("Current level of this team member.")]
    public int level = 1;

    [Tooltip("Current XP earned towards next level.")]
    public int experience = 0;

    [Tooltip("XP required to reach the next level.")]
    public int xpToNextLevel = 100;

    [Header("Economy")]
    [Tooltip("Base salary or cost for hiring this member.")]
    public int salary = 50;
}
