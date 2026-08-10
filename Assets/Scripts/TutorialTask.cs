using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[Serializable]
public class TutorialTask
{
  public string taskName;
  public LocalizedString taskNameLocalizedString;
  public string taskDescription;
  public LocalizedString taskDescriptionLocalizedString;
  [Tooltip("Dialogs to show when the task is activated")]
  public List<string> taskDialogs = new  List<string>();
  public List<LocalizedString> taskDialogsLocalizedStrings = new List<LocalizedString>();
  [Tooltip("Dialogs to show when the task is completed")]
  public List<string> CompletedtaskDialogs = new  List<string>();
  public List<LocalizedString> CompletedtaskDialogsLocalizedStrings = new List<LocalizedString>();
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
    CloseBillMenuTask2,
} 
