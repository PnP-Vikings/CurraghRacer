using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TutorialTask
{
  public string taskName;
  public string taskDescription;
  [Tooltip("Dialogs to show when the task is activated")]
  public List<string> taskDialogs = new  List<string>();
  [Tooltip("Dialogs to show when the task is completed")]
  public List<string> CompletedtaskDialogs = new  List<string>();
  public TutorialTaskType taskType;
  public bool hasTutorialDialogsBeenShown;
  public bool isTaskActive;
  public bool completed;
}

public enum TutorialTaskType
{
    BulletinBoardTask,
    CalendarTask,    
    CloseCalendarTask,
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
    Other,
    ClickOnTheTv,
    ExitTv,
    CompleteAllTasks,
    CloseBillMenu,
    OpenHireSailorBillMenu,
    CloseTeamManager,
} 
