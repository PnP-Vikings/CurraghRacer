using System.Collections.Generic;
using System.Net.Mail;
using UnityEngine;

public class TutorialTaskUiManager : MonoBehaviour
{
    public Transform taskUiParent;
    public TutorialTaskPrefab taskPrefab;
    public TutorialTaskUiDialogue taskUiDialogueUi;
    TutorialTask activeTask = null;

    public void UpdateTaskUis()
    {
        if(GameManager.Instance == null)
        {
            Debug.LogError("GameManager instance is null. Cannot update task UIs.");
            return;
        }

        if (GameManager.Instance.IsTutorialModeActive())
        { 
            List<TutorialTask> tutorialTasks = ProcessedTutorialTasks(GameManager.Instance.GetTutorialTaskList());
            
            ClearTaskUis();
           
            foreach (TutorialTask tutorialTask in tutorialTasks)
            {
                TutorialTaskPrefab taskUi = null;
                
                taskUi = Instantiate(taskPrefab, taskUiParent);
                Debug.Log($"Instantiated task UI for task: {tutorialTask.taskName}");
                taskUi.SetTutorialTask(tutorialTask);
            }
            Debug.Log("Task UIs updated.");
            ProcessActiveTaskDialogue();
        }
        else
        {
            ClearTaskUis();
            this.gameObject.SetActive(false);
        }
    }
    public void ProcessActiveTaskDialogue()
    {
        if (activeTask != null && taskUiDialogueUi != null && activeTask.taskDialogs.Count > 0 && !activeTask.hasTutorialDialogsBeenShown)
        {
            taskUiDialogueUi.gameObject.SetActive(true);
            taskUiDialogueUi.Setup(activeTask);
        }
        else
        {
            taskUiDialogueUi.gameObject.SetActive(false);
        }
    }
   
    public List<TutorialTask> ProcessedTutorialTasks(List<TutorialTask> tutorialTasks)
    {
        List<TutorialTask> processedTasks = new List<TutorialTask>();
        int count = 0;
        bool hasBeenProcessed = false;
        foreach (TutorialTask tutorialTask in tutorialTasks)
        {
            TutorialTaskPrefab taskUi = null;
            if (count == 0 && tutorialTask.isTaskActive)
            {
                processedTasks.Add(tutorialTask);
                activeTask = tutorialTask;
                hasBeenProcessed = true;
                break;
            }
            else if (tutorialTask.isTaskActive && count == 1)
            {
                processedTasks.Add(tutorialTasks[0]);
                processedTasks.Add(tutorialTask);
                activeTask = tutorialTask;
                hasBeenProcessed = true;
                break;
            }
            count++;
        }
        
        if (!hasBeenProcessed)
        {
            processedTasks.Clear();
           
            foreach (TutorialTask tutorialTask in tutorialTasks)
            {
                if (tutorialTask.isTaskActive)
                {
                    activeTask = tutorialTask;
                    break;
                }
            }

            if (activeTask != null)
            {
                if(tutorialTasks.IndexOf(activeTask)+1 < tutorialTasks.Count)
                {
                    processedTasks.Add(tutorialTasks[tutorialTasks.IndexOf(activeTask) - 1]);
                    processedTasks.Add(activeTask);
                    processedTasks.Add(tutorialTasks[tutorialTasks.IndexOf(activeTask) + 1]);
                }
                else
                {
                    processedTasks.Add(tutorialTasks[tutorialTasks.IndexOf(activeTask) - 2]);
                    processedTasks.Add(tutorialTasks[tutorialTasks.IndexOf(activeTask) - 1]);
                    processedTasks.Add(activeTask);
                }
                hasBeenProcessed = true;

            }
        }
        
        
        foreach (TutorialTask tutorialTask in processedTasks)
        {
            Debug.Log($"Processed task: {tutorialTask.taskName}, Active: {tutorialTask.isTaskActive}, Completed: {tutorialTask.completed}");
        }
        
        return processedTasks;
    }
    
    public void ClearTaskUis()
    {
        foreach (Transform child in taskUiParent)
        {
            Destroy(child.gameObject);
        }
    }
    
}
