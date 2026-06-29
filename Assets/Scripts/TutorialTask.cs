using System;
using System.Collections.Generic;

[Serializable]
public class TutorialTask
{
  public string taskName;
  public string taskDescription;
  public List<string> taskDialogs = new  List<string>();
  public TutorialTaskType taskType;
  public bool hasTutorialDialogsBeenShown;
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
    OpenTrainingMenuTask,
    Other
} 
