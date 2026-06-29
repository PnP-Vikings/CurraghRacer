using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialTaskUiDialogue : MonoBehaviour
{
   public TMP_Text dialogueText;
   public TMP_Text taskTitleText;
   public List<Sprite> dialogueSprite;
   public Image dialogueCharacterImage;
   public List<string> dialogueLines;
   public int currentDialogueIndex = 0;
   public TutorialTask activeTask;
   public void Setup(TutorialTask injectedActiveTask)
   {
      activeTask = injectedActiveTask;
      this.taskTitleText.text = activeTask.taskName;
      this.dialogueLines = activeTask.taskDialogs;
      this.currentDialogueIndex = 0;
      this.dialogueText.text = dialogueLines[currentDialogueIndex];
   }

   public void NextDialogue()
   {
      currentDialogueIndex++;
      if (currentDialogueIndex < dialogueLines.Count)
      {
         dialogueText.text = dialogueLines[currentDialogueIndex];
      }
      else
      {
         if(GameManager.Instance != null && GameManager.Instance.IsTutorialModeActive() && activeTask != null)
         {
            GameManager.Instance.MarkTutorialTaskDialogsAsShown(activeTask);
         }
        this.gameObject.SetActive(false);
      }
   }
  
   
   void Update()
   {
      if (Input.GetKeyDown(KeyCode.Space))
      {
         NextDialogue();
      }
   }
   
}
