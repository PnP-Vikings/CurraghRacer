using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialTaskPrefab : MonoBehaviour
{
    [SerializeField] private TutorialTaskType tutorialTaskType;
    [SerializeField] private Image taskBackgroundImage;
    [SerializeField] private TMP_Text taskName;
    public Image taskCheckBoxImage;
    public Image taskCompletedImage;
    public Color taskActiveColor;
    public Color taskInactiveColor;
    public Color taskCompletedColor;
    
    
    public void SetTutorialTask(TutorialTaskType tutorialTaskType)
    {
        if(tutorialTaskType == null)
        {
            Debug.LogError("TutorialTaskType is None. Please set a valid TutorialTaskType.");
            return;
        }
        
        this.tutorialTaskType = tutorialTaskType;
        taskName.text = tutorialTaskType.ToString();
        
        if (GameManager.Instance != null && GameManager.Instance.IsTutorialTaskCompleted(tutorialTaskType))
        {
            taskCompletedImage.gameObject.SetActive(true);
        }
        else
        {
            taskCompletedImage.gameObject.SetActive(false);
        }
        
        if (GameManager.Instance != null && GameManager.Instance.IsTutorialTaskActive(tutorialTaskType))
        {
           taskBackgroundImage.color = taskActiveColor;
        }
        else if (GameManager.Instance != null && GameManager.Instance.IsTutorialTaskCompleted(tutorialTaskType))
        {
            taskBackgroundImage.color = taskCompletedColor;
        }
        else
        {
            taskBackgroundImage.color = taskInactiveColor;
        }
    }
    
    public void SetTutorialTask(TutorialTask tutorialTask)
    {
        if(tutorialTask == null)
        {
            Debug.LogError("TutorialTaskType is None. Please set a valid TutorialTaskType.");
            return;
        }
        
        tutorialTaskType = tutorialTask.taskType;
        taskName.text = tutorialTask.taskName;
        
        if (tutorialTask.completed)
        {
            taskCompletedImage.gameObject.SetActive(true);
        }
        else
        {
            taskCompletedImage.gameObject.SetActive(false);
        }
        
        if (GameManager.Instance != null && GameManager.Instance.IsTutorialTaskActive(tutorialTaskType))
        {
            taskBackgroundImage.color = taskActiveColor;
        }
        else if (GameManager.Instance != null && GameManager.Instance.IsTutorialTaskCompleted(tutorialTaskType))
        {
            taskBackgroundImage.color = taskCompletedColor;
        }
        else
        {
            taskBackgroundImage.color = taskInactiveColor;
        }
    }
}
