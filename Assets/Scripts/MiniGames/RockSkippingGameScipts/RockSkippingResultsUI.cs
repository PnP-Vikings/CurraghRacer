using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Displays results for the rock skipping game
/// Shows each player's distance per round and highlights the winner
/// </summary>
public class RockSkippingResultsUI : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private GameObject inGameScorePanel;
    
    [Header("Player Name Labels")]
    [SerializeField] private TextMeshProUGUI[] playerNameTexts;
    
    [Header("Round Score Texts (Player x Round grid)")]
    [SerializeField] private TextMeshProUGUI[] round1Scores; // Index 0-3 for players 0-3
    [SerializeField] private TextMeshProUGUI[] round2Scores;
    [SerializeField] private TextMeshProUGUI[] round3Scores;
    
    [Header("Total Score Texts")]
    [SerializeField] private TextMeshProUGUI[] totalScoreTexts;
    
    [Header("Winner Display")]
    [SerializeField] private GameObject winnerPanel;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI winnerDistanceText;
    
    [Header("Buttons")]
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button exitButton;
    
    [Header("Current Turn Indicator")]
    [SerializeField] private TextMeshProUGUI currentTurnText;
    [SerializeField] private TextMeshProUGUI currentRoundText;
    
    [Header("Row Backgrounds (for highlighting)")]
    [SerializeField] private Image[] playerRowBackgrounds;
    
    [Header("Colors")]
    [SerializeField] private Color playerColor = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color ai1Color = new Color(1f, 0.4f, 0.4f);
    [SerializeField] private Color ai2Color = new Color(0.4f, 1f, 0.4f);
    [SerializeField] private Color ai3Color = new Color(1f, 1f, 0.4f);
    [SerializeField] private Color winnerHighlightColor = new Color(1f, 0.84f, 0f, 0.5f);
    [SerializeField] private Color currentTurnHighlightColor = new Color(1f, 1f, 1f, 0.3f);
    
    [Header("Animation Settings")]
    [SerializeField] private float scoreCountDuration = 0.5f;
    [SerializeField] private float resultRevealDelay = 0.3f;
    
    private string[] playerNames = { "You", "AI 1", "AI 2", "AI 3" };
    private Color[] playerColors;
    
    private void Awake()
    {
        playerColors = new Color[] { playerColor, ai1Color, ai2Color, ai3Color };
        
        if (resultsPanel != null)
            resultsPanel.SetActive(false);
        
        if (winnerPanel != null)
            winnerPanel.SetActive(false);
        
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgainPressed);
        
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitPressed);
        
        // Initialize player names
        for (int i = 0; i < playerNameTexts.Length && i < playerNames.Length; i++)
        {
            if (playerNameTexts[i] != null)
            {
                playerNameTexts[i].text = playerNames[i];
                playerNameTexts[i].color = playerColors[i];
            }
        }
        
        // Clear all scores initially
        ClearAllScores();
    }
    
    private void ClearAllScores()
    {
        TextMeshProUGUI[][] allRoundScores = { round1Scores, round2Scores, round3Scores };
        
        foreach (var roundScores in allRoundScores)
        {
            if (roundScores == null) continue;
            foreach (var scoreText in roundScores)
            {
                if (scoreText != null)
                    scoreText.text = "-";
            }
        }
        
        if (totalScoreTexts != null)
        {
            foreach (var totalText in totalScoreTexts)
            {
                if (totalText != null)
                    totalText.text = "-";
            }
        }
    }
    
    /// <summary>
    /// Update a single score during gameplay
    /// </summary>
    public void UpdateScore(int playerIndex, int round, float distance)
    {
        if (playerIndex < 0 || playerIndex > 3 || round < 1 || round > 3) return;
        
        TextMeshProUGUI[] targetRound = round switch
        {
            1 => round1Scores,
            2 => round2Scores,
            3 => round3Scores,
            _ => null
        };
        
        if (targetRound != null && playerIndex < targetRound.Length && targetRound[playerIndex] != null)
        {
            // Animate the score counting up
            TextMeshProUGUI scoreText = targetRound[playerIndex];
            float currentValue = 0f;
            
            DOTween.To(() => currentValue, x => {
                currentValue = x;
                scoreText.text = $"{currentValue:F1}m";
            }, distance, scoreCountDuration).SetEase(Ease.OutQuad);
            
            // Punch scale for emphasis
            scoreText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);
        }
        
        // Show in-game score panel if hidden
        if (inGameScorePanel != null && !inGameScorePanel.activeSelf)
        {
            inGameScorePanel.SetActive(true);
        }
    }
    
    public void HideInGameScorePanel()
    {
        if (inGameScorePanel != null)
        {
            inGameScorePanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Update current turn indicator
    /// </summary>
    public void UpdateTurnIndicator(int playerIndex, int round)
    {
        if (currentTurnText != null)
        {
            currentTurnText.gameObject.SetActive(true);
            string playerName = playerIndex < playerNames.Length ? playerNames[playerIndex] : $"Player {playerIndex}";
            currentTurnText.text = $"{playerName}'s Turn";
            currentTurnText.color = playerIndex < playerColors.Length ? playerColors[playerIndex] : Color.white;
        }
        
        if (currentRoundText != null)
        {
            currentRoundText.gameObject.SetActive(true);
            currentRoundText.text = $"Round {round}/3";
        }
        
        // Highlight current player's row
        if (playerRowBackgrounds != null)
        {
            for (int i = 0; i < playerRowBackgrounds.Length; i++)
            {
                if (playerRowBackgrounds[i] != null)
                {
                    playerRowBackgrounds[i].color = (i == playerIndex) 
                        ? currentTurnHighlightColor 
                        : Color.clear;
                }
            }
        }
    }
    
    /// <summary>
    /// Show final results with all scores and winner
    /// </summary>
    public void ShowFinalResults(Dictionary<int, List<float>> playerDistances, int winnerIndex)
    {
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(true);
            resultsPanel.transform.localScale = Vector3.zero;
            resultsPanel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
        }
        
        StartCoroutine(RevealResultsSequence(playerDistances, winnerIndex));
    }
    
    private System.Collections.IEnumerator RevealResultsSequence(Dictionary<int, List<float>> playerDistances, int winnerIndex)
    {
        TextMeshProUGUI[][] allRoundScores = { round1Scores, round2Scores, round3Scores };
        
        // Reveal scores round by round
        for (int round = 0; round < 3; round++)
        {
            for (int player = 0; player < 4; player++)
            {
                if (playerDistances.ContainsKey(player) && 
                    playerDistances[player].Count > round &&
                    allRoundScores[round] != null &&
                    player < allRoundScores[round].Length &&
                    allRoundScores[round][player] != null)
                {
                    float distance = playerDistances[player][round];
                    TextMeshProUGUI scoreText = allRoundScores[round][player];
                    
                    // Animate score
                    float currentValue = 0f;
                    DOTween.To(() => currentValue, x => {
                        currentValue = x;
                        scoreText.text = $"{currentValue:F1}m";
                    }, distance, scoreCountDuration).SetEase(Ease.OutQuad);
                    
                    scoreText.transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 3);
                }
                
                yield return new WaitForSeconds(resultRevealDelay * 0.5f);
            }
            
            yield return new WaitForSeconds(resultRevealDelay);
        }
        
        // Calculate and show totals
        yield return new WaitForSeconds(0.5f);
        
        for (int player = 0; player < 4; player++)
        {
            if (totalScoreTexts != null && player < totalScoreTexts.Length && totalScoreTexts[player] != null)
            {
                float total = 0f;
                if (playerDistances.ContainsKey(player))
                {
                    foreach (float d in playerDistances[player])
                    {
                        total += d;
                    }
                }
                
                TextMeshProUGUI totalText = totalScoreTexts[player];
                float currentValue = 0f;
                
                DOTween.To(() => currentValue, x => {
                    currentValue = x;
                    totalText.text = $"{currentValue:F1}m";
                }, total, scoreCountDuration * 1.5f).SetEase(Ease.OutQuad);
                
                totalText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);
            }
            
            yield return new WaitForSeconds(resultRevealDelay);
        }
        
        // Highlight winner row
        yield return new WaitForSeconds(0.5f);
        
        if (playerRowBackgrounds != null && winnerIndex < playerRowBackgrounds.Length && playerRowBackgrounds[winnerIndex] != null)
        {
            playerRowBackgrounds[winnerIndex].DOColor(winnerHighlightColor, 0.5f);
        }
        
        // Show winner panel
        yield return new WaitForSeconds(0.3f);
        
        if (winnerPanel != null)
        {
            winnerPanel.SetActive(true);
            winnerPanel.transform.localScale = Vector3.zero;
            winnerPanel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            
            if (winnerText != null)
            {
                string winnerName = winnerIndex < playerNames.Length ? playerNames[winnerIndex] : $"Player {winnerIndex}";
                winnerText.text = winnerIndex == 0 ? "YOU WIN!" : $"{winnerName} Wins!";
                winnerText.color = winnerIndex < playerColors.Length ? playerColors[winnerIndex] : Color.white;
            }
            
            if (winnerDistanceText != null && playerDistances.ContainsKey(winnerIndex))
            {
                float total = 0f;
                foreach (float d in playerDistances[winnerIndex])
                {
                    total += d;
                }
                winnerDistanceText.text = $"Total: {total:F1}m";
            }
        }
        
        // Play win/lose sound
        if (AudioManager.instance != null)
        {
            if (winnerIndex == 0)
            {
                AudioManager.instance.raceWon.start();
                AudioManager.instance.raceWon.setParameterByName("Cheering Volume", 0f);
            }
            else
            {
                AudioManager.instance.raceLost.start();
            }
        }
    }
    
    public void HideResults()
    {
        if (resultsPanel != null)
        {
            resultsPanel.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack)
                .OnComplete(() => resultsPanel.SetActive(false));
        }
        
        if (winnerPanel != null)
        {
            winnerPanel.SetActive(false);
        }
    }
    
    private void OnPlayAgainPressed()
    {
        // Reload the current scene or reset game
        if (AudioManager.instance != null)
        {
            AudioManager.instance.UIClick1.start();
        }
        
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    
    private void OnExitPressed()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.UIClick2.start();
        }
        
        // Return to main menu or previous scene
        if (GameManager.Instance != null)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(GameManager.Instance.mainSceneName);
        }
    }
    
    private void OnDestroy()
    {
        DOTween.Kill(this);
    }
}
