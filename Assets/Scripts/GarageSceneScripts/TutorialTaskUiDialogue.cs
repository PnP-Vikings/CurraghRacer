using System.Collections;
using System.Collections.Generic;
using System.Linq;
using League;
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
   public string currentDialogueLine;
   public int currentDialogueIndex = 0;
   public TutorialTask activeTask;
   [SerializeField] private float dialogueSpriteSpeed = .25f;
   [SerializeField] private float dialogueTextSpeed = .1f;
   private bool isDialogueActive = false;
   private Coroutine currentDialogueCoroutine;
   public void Setup(TutorialTask injectedActiveTask)
   {
      activeTask = injectedActiveTask;
      this.taskTitleText.text = activeTask.taskName;
     
      if (!isDialogueActive)
      {
         isDialogueActive = true;   
         foreach (string tl in activeTask.taskDialogs)
         {
            dialogueLines.Add(tl);
         }
      
         this.currentDialogueIndex = 0;
         this.currentDialogueLine = dialogueLines[currentDialogueIndex];
         this.currentDialogueCoroutine = this.StartCoroutine(TypeText(currentDialogueLine));
      }
   }

   public void NextDialogue()
   {
      if (isDialogueActive)
      {
         SkipToEndOfDialogue();
         return;
      }
      currentDialogueIndex++;
      if (currentDialogueIndex < dialogueLines.Count)
      {
         this.currentDialogueLine = dialogueLines[currentDialogueIndex];
         if (!isDialogueActive)
         {
            isDialogueActive = true;  
            this.currentDialogueCoroutine = this.StartCoroutine(TypeText(currentDialogueLine));
         }
      }
      else
      {
         if(GameManager.Instance != null && GameManager.Instance.IsTutorialModeActive() && activeTask != null)
         {
            dialogueLines.Clear();
            
            foreach (string tl in activeTask.CompletedtaskDialogs)
            {
               dialogueLines.Add(tl);
            }
            GameManager.Instance.MarkTutorialTaskDialogsAsShown(activeTask);

            if (activeTask.taskType == TutorialTaskType.JoinLeagueTask)
            {
               if(LeagueController.Instance != null)
               {
                  LeagueController.Instance.ShowLeagueInvite();
               }
            }
            
            if(activeTask.taskType == TutorialTaskType.CompleteAllTasks)
            {
               GameManager.Instance.CompleteTutorialTask(activeTask.taskType);
            }
         }
        this.gameObject.SetActive(false);
      }

      if (AudioManager.instance != null)
      {
          AudioManager.instance.UIClick1.start();
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
   
   //This function is called when the player clicks the "Next" button to skip to the end of the current dialogue line
   private void SkipToEndOfDialogue()
   {
      if(isDialogueActive == true)
      {
         this.StopCoroutine(currentDialogueCoroutine);
         dialogueText.text = currentDialogueLine;
         isDialogueActive = false;
      }
   }
   
   //This is the function that types the text
   private IEnumerator TypeText(string text)
   {
      Debug.Log("TypeText: " + text);
      dialogueText.text = "";
      foreach (char c in text)
      {
         dialogueText.text += c;
         yield return new WaitForSeconds(dialogueTextSpeed);
      }
      isDialogueActive = false;
   }
}
