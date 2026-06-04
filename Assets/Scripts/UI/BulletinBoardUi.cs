using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BulletinBoardUi : MonoBehaviour
{
    public List <GameObject> bulletinBoardMenuOptions;
    
    public void CloseAllMenuOptions()
    {
        foreach (var menuOption in bulletinBoardMenuOptions)
        {
            menuOption.SetActive(false);
        }
    }
    
    public void OpenAllMenuOption()
    {
        foreach (var menuOption in bulletinBoardMenuOptions)
        {
            menuOption.SetActive(true);
        }
    }

    public void CheckIfBulletinBoardTaskIsCompleted()
    {
        if(GameManager.Instance != null && GameManager.Instance.IsTutorialModeActive())
        {
          GameManager.Instance.CompleteTutorialTask(TutorialTaskType.BulletinBoardTask);
        }
    }
}
