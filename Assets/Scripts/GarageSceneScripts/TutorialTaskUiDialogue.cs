using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialTaskUiDialogue : MonoBehaviour
{
   public TMP_Text dialogueText;
   public TMP_Text taskTitleText;
   public List<Sprite> dialogueSprites;
   public Image dialogueCharacterImage;
   public List<string> dialogueLines;
   public int currentDialogueIndex = 0;
   public TutorialTask activeTask;
   [SerializeField] private float dialogueSpriteSpeed = .25f;

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
  

   private void OnEnable()
   {
      StartCoroutine(CycleSprites());
   }

   private IEnumerator CycleSprites()
   {
      int index = 0;
      while (true)
      {
         if (dialogueSprites != null && dialogueSprites.Count > 0 && dialogueCharacterImage != null)
         {
            dialogueCharacterImage.sprite = dialogueSprites[index];
            index = (index + 1) % dialogueSprites.Count;
         }
         yield return new WaitForSeconds(dialogueSpriteSpeed);
      }
   }
   
   
}
