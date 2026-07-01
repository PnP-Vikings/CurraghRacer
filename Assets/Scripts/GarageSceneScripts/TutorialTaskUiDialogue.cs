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
      this.dialogueLines = activeTask.taskDialogs;
      this.currentDialogueIndex = 0;
      this.currentDialogueLine = dialogueLines[currentDialogueIndex];
      if (!isDialogueActive)
      {
         isDialogueActive = true;   
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
            this.currentDialogueCoroutine = this.StartCoroutine(TypeText(currentDialogueLine));
         }
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

   private void SkipToEndOfDialogue()
   {
      if(isDialogueActive == true)
      {
         this.StopCoroutine(currentDialogueCoroutine);
         dialogueText.text = currentDialogueLine;
         isDialogueActive = false;
      }
   }
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
