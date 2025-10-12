using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DecisionCardUiMaster : MonoBehaviour
{
   public Image decisionCardColorImage;
   public Color positiveColor,negativeColor,neutralColor;
   public List<DecisionCardUi> decisionCards;
   public bool isDragging = false;
   public DecisionCardUi activeDecisionCard;
   public float swipeThreshold = 50f; // Minimum x position to consider as a swipe
   
   
   public void DragStarted(DecisionCardUi card)
   {
       isDragging = true;
       activeDecisionCard = card;

       StartCoroutine(UpdateBackgroundDecisionColor());
   }
   
   public void DragEnded()
   {
       isDragging = false;
       activeDecisionCard = null;
       StopAllCoroutines();
       ResetBackgroundColor();
   }

   public void ResetBackgroundColor()
   {
       decisionCardColorImage.color = neutralColor;
   }
  IEnumerator UpdateBackgroundDecisionColor()
   {
       while (isDragging)
       {
           if (activeDecisionCard != null)
           {
               float xPos = activeDecisionCard.GetComponent<RectTransform>().anchoredPosition.x;
               
               if (xPos > 0 + swipeThreshold)
               {
                   decisionCardColorImage.color = Color.Lerp(decisionCardColorImage.color, positiveColor, Time.deltaTime * 5f);
                   activeDecisionCard.ShowAcceptText();
               }
               else if (xPos < 0-swipeThreshold)
               {
                   decisionCardColorImage.color = Color.Lerp(decisionCardColorImage.color, negativeColor, Time.deltaTime * 5f);
                   activeDecisionCard.ShowRejectText();
               }
               else
               {
                   decisionCardColorImage.color = Color.Lerp(decisionCardColorImage.color, neutralColor, Time.deltaTime * 5f);
                   activeDecisionCard.HideDecisionText();
               }
           }
           
           yield return null; // ✅ Moved inside the loop
       }
   }
}
