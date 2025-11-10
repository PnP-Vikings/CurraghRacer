using System.Collections.Generic;
using UnityEngine;

public class TextLevelsTestingMenu : MonoBehaviour
{
   public List<string> levelNames;
   public GameObject levelButtonPrefab;
   public Transform levelButtonContainer;
   
   private void Start()
   {
       foreach (var levelName in levelNames)
       {
           GameObject buttonObj = Instantiate(levelButtonPrefab, levelButtonContainer);
           LevelButton levelButton = buttonObj.GetComponent<LevelButton>();
           if (levelButton != null)
           {
               levelButton.SetLevelSceneName(levelName);
           }
           else
           {
               Debug.LogError("LevelButton component missing on prefab.");
           }
       }
   }
}
