using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Localization;

public class BeerMinigameCanvasUI : MinigameCanvasUI
{
    [Header("Beer Minigame Specific UI")]
    [Tooltip("Order info text for each tap (size 4)")]
    public TMP_Text[] tapOrderTexts = new TMP_Text[4];
    
    [Tooltip("Timer text for each tap at top-left (size 4)")]
    public TMP_Text[] tapTimerTexts = new TMP_Text[4];
    
    [Tooltip("Perfect streak display at top-right")]
    public TMP_Text perfectStreakText;
    
    [Tooltip("Pour Result Feedback Text")]
    public TMP_Text pourResultText;
    
    [Tooltip("Round feedback text")]
    public TMP_Text roundFeedbackText;
    
    [Tooltip("Summary panel for final results")]
    public GameObject summaryPanel;
    
    [Tooltip("Container for summary entries")]
    public Transform summaryContainer;
    
    [Header("Beer Minigames Localization")]
    [SerializeField] LocalizedString localizedTapOrderTextText = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.BeerPouringGame.TapOrderText" };
    [SerializeField] LocalizedString localizedTapTimerTextText = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.BeerPouringGame.TapTimerText" };
    [SerializeField] LocalizedString localizedPerfectStreakTextText = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.BeerPouringGame.PerfectStreakText" };
    [SerializeField] LocalizedString localizedPerfectQualityText = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.BeerPouringGame.Quality.Perfect" };
    [SerializeField] LocalizedString localizedGoodQualityText = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.BeerPouringGame.Quality.Good" };
    [SerializeField] LocalizedString localizedAcceptableQualityText = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.BeerPouringGame.Quality.Acceptable" };
    [SerializeField] LocalizedString localizedPoorQualityText = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.BeerPouringGame.Quality.Poor" };
    [SerializeField] LocalizedString localizedPerfectPourQualityText = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.BeerPouringGame.PourQuality.Perfect" };
    [SerializeField] LocalizedString localizedGoodPourQualityText = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.BeerPouringGame.PourQuality.Good" };
    [SerializeField] LocalizedString localizedAcceptablePourQualityText = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.BeerPouringGame.PourQuality.Acceptable" };
    [SerializeField] LocalizedString localizedPoorPourQualityText = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.BeerPouringGame.PourQuality.Poor" };
    [SerializeField] LocalizedString localizedRoundFeedbackText = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.BeerPouringGame.RoundFeedbackText" };
    [SerializeField] LocalizedString localizedSummaryText = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.BeerPouringGame.SummaryText" };

    public void UpdateTapOrder(int tapIndex, string beerType, string customerName)
    {
        if (tapIndex >= 0 && tapIndex < tapOrderTexts.Length && tapOrderTexts[tapIndex] != null)
        {
            if(localizedTapOrderTextText != null && !localizedTapOrderTextText.IsEmpty)
            {
                localizedTapOrderTextText.Arguments = new object[] { beerType, customerName };
                localizedTapOrderTextText.Arguments[0] = beerType;
                localizedTapOrderTextText.Arguments[1] = customerName;
                localizedTapOrderTextText.RefreshString();
                tapOrderTexts[tapIndex].text = localizedTapOrderTextText.GetLocalizedString();
            }
            else
            {
                tapOrderTexts[tapIndex].text = $"{beerType} for {customerName}";
            }
            tapOrderTexts[tapIndex].gameObject.SetActive(true);
        }
    }

    public void UpdateTapTimer(int tapIndex, float time, bool isActive)
    {
        if (tapIndex >= 0 && tapIndex < tapTimerTexts.Length && tapTimerTexts[tapIndex] != null)
        {
            if (isActive)
            {
                if(localizedTapTimerTextText != null && !localizedTapTimerTextText.IsEmpty)
                {
                    localizedTapTimerTextText.Arguments = new object[] { tapIndex + 1, Mathf.CeilToInt(time) };
                    localizedTapTimerTextText.Arguments[0] = tapIndex + 1;
                    localizedTapTimerTextText.Arguments[1] = Mathf.CeilToInt(time);
                    localizedTapTimerTextText.RefreshString();
                    tapTimerTexts[tapIndex].text = localizedTapTimerTextText.GetLocalizedString();
                }
                else
                {
                    tapTimerTexts[tapIndex].text = $"Tap {tapIndex + 1}: {Mathf.CeilToInt(time)}s";
                }
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
                if(localizedPerfectStreakTextText != null && !localizedPerfectStreakTextText.IsEmpty)
                {
                    localizedPerfectStreakTextText.Arguments = new object[] { streak, multiplier };
                    localizedPerfectStreakTextText.Arguments[0] = streak;
                    localizedPerfectStreakTextText.Arguments[1] = multiplier;
                    localizedPerfectStreakTextText.RefreshString();
                    perfectStreakText.text = localizedPerfectStreakTextText.GetLocalizedString();
                }
                else
                {
                perfectStreakText.text = $"Perfect Streak: {streak} ({multiplier:F1}x)";
                }
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
            PourQuality.Perfect => localizedPerfectQualityText != null && !localizedPerfectQualityText.IsEmpty ? localizedPerfectQualityText.GetLocalizedString() : "PERFECT!",
            PourQuality.Good => localizedGoodQualityText != null && !localizedGoodQualityText.IsEmpty ? localizedGoodQualityText.GetLocalizedString() : "Good",
            PourQuality.Acceptable => localizedAcceptableQualityText != null && !localizedAcceptableQualityText.IsEmpty ? localizedAcceptableQualityText.GetLocalizedString() : "Acceptable",
            PourQuality.Poor => localizedPoorQualityText != null && !localizedPoorQualityText.IsEmpty ? localizedPoorQualityText.GetLocalizedString() : "Poor",
            _ => ""
        };
        
        StartCoroutine(ShowPourResultFeedback(quality));

        string resultText = $"{qualityText} +{points}";
        if (multiplier > 1.0f && quality == PourQuality.Perfect)
        {
            resultText += $" ({multiplier:F1}x)";
        }

        Debug.Log($"Tap {tapIndex + 1}: {resultText}");
        
       
    }
    
   public IEnumerator ShowPourResultFeedback(PourQuality quality)
    {
        if (pourResultText != null)
        {
            pourResultText.gameObject.SetActive(true);
            switch (quality)
            {
                case PourQuality.Perfect:
                    pourResultText.text = localizedPerfectPourQualityText != null && !localizedPerfectPourQualityText.IsEmpty ? localizedPerfectPourQualityText.GetLocalizedString() : "PERFECT POUR!";
                    pourResultText.color = Color.green;
                    break;
                case PourQuality.Good:
                    pourResultText.text = localizedGoodPourQualityText != null && !localizedGoodPourQualityText.IsEmpty ? localizedGoodPourQualityText.GetLocalizedString() : "Good Pour";
                    pourResultText.color = Color.cyan;
                    break;  
                case PourQuality.Acceptable:
                    pourResultText.text = localizedAcceptablePourQualityText != null && !localizedAcceptablePourQualityText.IsEmpty ? localizedAcceptablePourQualityText.GetLocalizedString() : "Acceptable Pour";
                    pourResultText.color = Color.yellow;
                    break;
                case PourQuality.Poor:
                    pourResultText.text = localizedPoorPourQualityText != null && !localizedPoorPourQualityText.IsEmpty ? localizedPoorPourQualityText.GetLocalizedString() : "Poor Pour";
                    pourResultText.color = Color.red;
                    break;
                default:
                    pourResultText.text = "";
                    pourResultText.color = Color.white;
                    break;
            }
        }
        yield return new WaitForSeconds(1f);

        if (pourResultText != null)
        {
            pourResultText.gameObject.SetActive(false);
        }
    }

    public IEnumerator ShowRoundSummary(int roundNum, int totalRounds, int basePoints, int bonusPoints, int totalPoints, int currentStreak)
    {
        if (roundFeedbackText != null)
        {
            if(localizedRoundFeedbackText != null && !localizedRoundFeedbackText.IsEmpty)
            {
                localizedRoundFeedbackText.Arguments = new object[] { roundNum, totalRounds, basePoints, bonusPoints, totalPoints };
                localizedRoundFeedbackText.Arguments[0] = roundNum;
                localizedRoundFeedbackText.Arguments[1] = totalRounds;
                localizedRoundFeedbackText.Arguments[2] = basePoints;
                localizedRoundFeedbackText.Arguments[3] = bonusPoints;
                localizedRoundFeedbackText.Arguments[4] = totalPoints;
                localizedRoundFeedbackText.RefreshString();
                roundFeedbackText.text = localizedRoundFeedbackText.GetLocalizedString();
            }
            else
            {
                roundFeedbackText.text = $"Round {roundNum}/{totalRounds} Complete!\n" +
                    $"Base: +{basePoints}\n" +
                    $"Bonus: +{bonusPoints}\n" +
                    $"Total: +{totalPoints}";
            }
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

        string summaryText = " ";
        
        if(localizedSummaryText != null && !localizedSummaryText.IsEmpty)
        {
            localizedSummaryText.Arguments = new object[] { totalScore, maxStreak, perfectCount, goodCount, acceptableCount, poorCount };
            localizedSummaryText.Arguments[0] = totalScore;
            localizedSummaryText.Arguments[1] = maxStreak;
            localizedSummaryText.Arguments[2] = perfectCount;
            localizedSummaryText.Arguments[3] = goodCount;
            localizedSummaryText.Arguments[4] = acceptableCount;
            localizedSummaryText.Arguments[5] = poorCount;
            localizedSummaryText.RefreshString();
            summaryText = localizedSummaryText.GetLocalizedString();
        }
        else
        {
            summaryText =  $"GAME COMPLETE!\n\n" +
                $"Total Score: {totalScore}\n" +
                $"Max Streak: {maxStreak}\n\n" +
                $"Perfect: {perfectCount}\n" +
                $"Good: {goodCount}\n" +
                $"Acceptable: {acceptableCount}\n" +
                $"Poor: {poorCount}";
        }

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

