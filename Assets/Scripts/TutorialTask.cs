using System;

[Serializable]
public class TutorialTask
{
  public string taskName;
  public string taskDescription;
  public TutorialTaskType taskType;
  public bool isTaskActive;
  public bool completed;
}

public enum TutorialTaskType
{
    BulletinBoardTask,
    CalendarTask,
    TeamManagerTask,
    PracticeRaceTask,
    TrainTeamMemberTask,
    OpenBillMenuTask,
    WorkJobTask,
    PayBillTask,
    SleepTask,
    JoinLeagueTask,
    RaceTask,
    Other
} 
