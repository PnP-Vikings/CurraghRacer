using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RhythmCanvas : MonoBehaviour
{
   public TMPro.TMP_Text hitFeedbackText; // Template text
   public int poolSize = 10; // Number of text objects in pool
   
   [Header("Text Size Randomization")]
   public float minTextSize = 40f;
   public float maxTextSize = 80f;
   
   [Header("Position Randomization")]
   public float horizontalOffsetRange = 30f; // Random offset range for X position
   public float verticalOffsetRange = 20f;   // Random offset range for Y position
   
   [Header("Colors for Hit Types")]
   public Color perfectColor = new Color(0f, 1f, 0f); // Green
   public Color earlyColor = new Color(1f, 0.65f, 0f); // Orange
   public Color lateColor = new Color(1f, 1f, 0f); // Yellow
   public Color missColor = new Color(1f, 0f, 0f); // Red
   
   private List<TMPro.TMP_Text> hitFeedbackPool = new List<TMPro.TMP_Text>();
   private List<Coroutine> activeCoroutines = new List<Coroutine>();

   private void Awake()
   {
       InitializePool();
   }

   private void InitializePool()
   {
       if (hitFeedbackText == null)
       {
           Debug.LogError("Hit feedback text template is not assigned!");
           return;
       }
       
       // Hide the original template
       hitFeedbackText.gameObject.SetActive(false);
       
       // Create pool of text objects
       for (int i = 0; i < poolSize; i++)
       {
           TMPro.TMP_Text clone = Instantiate(hitFeedbackText, hitFeedbackText.transform.parent);
           clone.name = "HitFeedback_" + i;
           clone.gameObject.SetActive(false);
           hitFeedbackPool.Add(clone);
       }
       
       Debug.Log($"Initialized hit feedback pool with {poolSize} text objects");
   }

   public void ShowHitFeedback(string feedback, PaddleHitResult hitType, float duration = 1f)
   {
       TMPro.TMP_Text availableText = GetAvailableText();
       
       if (availableText != null)
       {
           // Randomize size
           float randomSize = UnityEngine.Random.Range(minTextSize, maxTextSize);
           availableText.fontSize = randomSize;
           
           // Set color based on hit type
           availableText.color = GetColorForHitType(hitType);
           
           // Set text and show
           availableText.text = feedback;
           availableText.gameObject.SetActive(true);
           
           // Add slight random position offset for visual variety
           RectTransform rectTransform = availableText.GetComponent<RectTransform>();
           if (rectTransform != null)
           {
               Vector2 randomOffset = new Vector2(
                   UnityEngine.Random.Range(-horizontalOffsetRange, horizontalOffsetRange),
                   UnityEngine.Random.Range(-verticalOffsetRange, verticalOffsetRange)
               );
               rectTransform.anchoredPosition = hitFeedbackText.GetComponent<RectTransform>().anchoredPosition + randomOffset;
           }
           
           // Start fade out coroutine
           Coroutine fadeCoroutine = StartCoroutine(FadeOutText(availableText, duration));
           activeCoroutines.Add(fadeCoroutine);
       }
   }
   
   // Legacy method for backward compatibility
   public void ShowHitFeedback(string feedback, float duration = 1f)
   {
       ShowHitFeedback(feedback, PaddleHitResult.Perfect, duration);
   }
   
   private TMPro.TMP_Text GetAvailableText()
   {
       // Find an inactive text object
       foreach (var text in hitFeedbackPool)
       {
           if (!text.gameObject.activeSelf)
           {
               return text;
           }
       }
       
       // If all are active, return the first one (will override it)
       Debug.LogWarning("All hit feedback texts are active, reusing oldest one");
       return hitFeedbackPool[0];
   }
   
   private Color GetColorForHitType(PaddleHitResult hitType)
   {
       switch (hitType)
       {
           case PaddleHitResult.Perfect:
               return perfectColor;
           case PaddleHitResult.Early:
               return earlyColor;
           case PaddleHitResult.Late:
               return lateColor;
           case PaddleHitResult.Miss:
               return missColor;
           default:
               return Color.white;
       }
   }
   
   private IEnumerator FadeOutText(TMPro.TMP_Text text, float duration)
   {
       float elapsed = 0f;
       Color startColor = text.color;
       Vector3 startScale = text.transform.localScale;
       Vector3 endScale = startScale * 1.2f; // Slight scale up for effect
       
       while (elapsed < duration)
       {
           elapsed += Time.deltaTime;
           float progress = elapsed / duration;
           
           // Fade out alpha
           Color currentColor = startColor;
           currentColor.a = Mathf.Lerp(1f, 0f, progress);
           text.color = currentColor;
           
           // Scale up slightly
           text.transform.localScale = Vector3.Lerp(startScale, endScale, progress);
           
           yield return null;
       }
       
       // Reset and hide
       text.transform.localScale = startScale;
       text.gameObject.SetActive(false);
   }
   
   private void OnDisable()
   {
       // Stop all active coroutines
       foreach (var coroutine in activeCoroutines)
       {
           if (coroutine != null)
           {
               StopCoroutine(coroutine);
           }
       }
       activeCoroutines.Clear();
   }
}
