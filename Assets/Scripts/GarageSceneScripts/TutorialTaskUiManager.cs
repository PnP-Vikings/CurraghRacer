using System.Collections.Generic;
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
            foreach (TutorialTask tutorialTask in tutorialTasks)
            {
                TutorialTaskPrefab taskUi = Instantiate(taskPrefab, taskUiParent);
                taskUi.SetTutorialTask(tutorialTask);
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
