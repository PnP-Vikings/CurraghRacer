using System.Collections.Generic;
using System.Net.Mail;
using UnityEngine;

public class TutorialTaskUiManager : MonoBehaviour
{
    public Transform taskUiParent;
    public TutorialTaskPrefab taskPrefab;


    public void UpdateTaskUis()
    {
        if(GameManager.Instance == null)
        {
            Debug.LogError("GameManager instance is null. Cannot update task UIs.");
            return;
        }

        if (GameManager.Instance.IsTutorialModeActive())
        { 
            List<TutorialTask> tutorialTasks = GameManager.Instance.GetTutorialTaskList();
            ClearTaskUis();
            int count = 0;
            foreach (TutorialTask tutorialTask in tutorialTasks)
            {
                TutorialTaskPrefab taskUi = null;
                if (count == 0 && tutorialTask.isTaskActive)
                {
                    taskUi = Instantiate(taskPrefab, taskUiParent);
                    taskUi.SetTutorialTask(tutorialTask);
                    break;
                }
                else if (tutorialTask.isTaskActive && count == 1)
                {
                    taskUi = Instantiate(taskPrefab, taskUiParent);
                    taskUi.SetTutorialTask(tutorialTask);
                    break;
                }
                else
                {
                    taskUi = Instantiate(taskPrefab, taskUiParent);
                    taskUi.SetTutorialTask(tutorialTask);
                }
                count++;
            }
            Debug.Log("Task UIs updated.");
        }
    }
    
    public void ClearTaskUis()
    {
        foreach (Transform child in taskUiParent)
        {
            Destroy(child.gameObject);
        }
    }
    
}
