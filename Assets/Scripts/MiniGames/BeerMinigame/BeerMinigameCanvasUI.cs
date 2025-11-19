using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BeerMinigameCanvasUI : MinigameCanvasUI
{
    [Header("Beer Minigame Specific UI")]
    [Tooltip("Order info text for each tap (size 4)")]
    public TMP_Text[] tapOrderTexts = new TMP_Text[4];
    
    [Tooltip("Timer text for each tap at top-left (size 4)")]
    public TMP_Text[] tapTimerTexts = new TMP_Text[4];
    
    [Tooltip("Perfect streak display at top-right")]
    public TMP_Text perfectStreakText;
    
    [Tooltip("Round feedback text")]
    public TMP_Text roundFeedbackText;
    
    [Tooltip("Summary panel for final results")]
    public GameObject summaryPanel;
    
    [Tooltip("Container for summary entries")]
    public Transform summaryContainer;

    public void UpdateTapOrder(int tapIndex, string beerType, string customerName)
    {
        if (tapIndex >= 0 && tapIndex < tapOrderTexts.Length && tapOrderTexts[tapIndex] != null)
        {
            tapOrderTexts[tapIndex].text = $"{beerType} for {customerName}";
            tapOrderTexts[tapIndex].gameObject.SetActive(true);
        }
    }

    public void UpdateTapTimer(int tapIndex, float time, bool isActive)
    {
        if (tapIndex >= 0 && tapIndex < tapTimerTexts.Length && tapTimerTexts[tapIndex] != null)
        {
            if (isActive)
            {
                tapTimerTexts[tapIndex].text = $"Tap {tapIndex + 1}: {Mathf.CeilToInt(time)}s";
                tapTimerTexts[tapIndex].gameObject.SetActive(true);
            }
            else
            {
                tapTimerTexts[tapIndex].gameObject.SetActive(false);
            }
        }
    }

    public void UpdatePerfectStreak(int streak, float multiplier)
    {
        if (perfectStreakText != null)
        {
            if (streak > 0)
            {
                perfectStreakText.text = $"Perfect Streak: {streak} ({multiplier:F1}x)";
                perfectStreakText.gameObject.SetActive(true);
            }
            else
            {
                perfectStreakText.text = "";
                perfectStreakText.gameObject.SetActive(false);
            }
        }
    }

    public void ShowTapPourResult(int tapIndex, PourQuality quality, int points, float multiplier)
    {
        // Show brief feedback at tap location
        string qualityText = quality switch
        {
            PourQuality.Perfect => "PERFECT!",
            PourQuality.Good => "Good",
            PourQuality.Acceptable => "Acceptable",
            PourQuality.Poor => "Poor",
            _ => ""
        };

        string resultText = $"{qualityText} +{points}";
        if (multiplier > 1.0f && quality == PourQuality.Perfect)
        {
            resultText += $" ({multiplier:F1}x)";
        }

        Debug.Log($"Tap {tapIndex + 1}: {resultText}");
        
        // Could add visual popup here if needed
    }

    public IEnumerator ShowRoundSummary(int roundNum, int totalRounds, int basePoints, int bonusPoints, int totalPoints, int currentStreak)
    {
        if (roundFeedbackText != null)
        {
            roundFeedbackText.text = $"Round {roundNum}/{totalRounds} Complete!\n" +
                                     $"Base: +{basePoints}\n" +
                                     $"Bonus: +{bonusPoints}\n" +
                                     $"Total: +{totalPoints}";
            roundFeedbackText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(3f);

        if (roundFeedbackText != null)
        {
            roundFeedbackText.gameObject.SetActive(false);
        }
    }

    public void ShowFinalSummary(List<PourQuality> results, int totalScore, int maxStreak)
    {
        if (summaryPanel != null)
        {
            summaryPanel.SetActive(true);
        }

        // Count each quality type
        int perfectCount = 0, goodCount = 0, acceptableCount = 0, poorCount = 0;
        foreach (var quality in results)
        {
            switch (quality)
            {
                case PourQuality.Perfect: perfectCount++; break;
                case PourQuality.Good: goodCount++; break;
                case PourQuality.Acceptable: acceptableCount++; break;
                case PourQuality.Poor: poorCount++; break;
            }
        }

        string summaryText = $"GAME COMPLETE!\n\n" +
                            $"Total Score: {totalScore}\n" +
                            $"Max Streak: {maxStreak}\n\n" +
                            $"Perfect: {perfectCount}\n" +
                            $"Good: {goodCount}\n" +
                            $"Acceptable: {acceptableCount}\n" +
                            $"Poor: {poorCount}";

        Debug.Log(summaryText);
        
        // Display in summary panel if available
        if (summaryContainer != null && summaryContainer.childCount > 0)
        {
            var summaryText_UI = summaryContainer.GetChild(0).GetComponent<TMP_Text>();
            if (summaryText_UI != null)
            {
                summaryText_UI.text = summaryText;
            }
        }
    }

    public void HideTapUI(int tapIndex)
    {
        if (tapIndex >= 0 && tapIndex < tapOrderTexts.Length)
        {
            if (tapOrderTexts[tapIndex] != null)
                tapOrderTexts[tapIndex].gameObject.SetActive(false);
            
            if (tapTimerTexts[tapIndex] != null)
                tapTimerTexts[tapIndex].gameObject.SetActive(false);
        }
    }
}

