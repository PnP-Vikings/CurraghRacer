using MiniGames;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MinigameCanvasUI : MonoBehaviour
{
    
    public TMPro.TMP_Text scoreText,timerText,playerLivesText,gameOverText,additionalInfoText;
    
    [Header("Bonus Flash (optional – separate from additionalInfo)")]
    public TMPro.TMP_Text bonusFlashText;
    
    [Header("Warning Banner (optional)")]
    public TMPro.TMP_Text warningText;
    
    [Header("Multiplier Display (optional)")]
    public TMPro.TMP_Text multiplierText;
    
    bool showRestartButtons = false;
    public GameObject testDemoButtons;
    public Button backToMainMenuButton, restartMinigameButton;
    
    Coroutine bonusFlashCoroutine;
    Coroutine warningCoroutine;

    [SerializeField] TrafficWardenAudio trafficWardenAudio;

    private Scene activeScene;

    public void SetUpUI(bool useScore, bool useTimer, bool useLives, bool showTestRestartsButtons, bool showAdditionalInfo = false)
    {
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(useScore);
        }
        if (timerText != null)
        {
            timerText.gameObject.SetActive(useTimer);
        }
        if (playerLivesText != null)
        {
            playerLivesText.gameObject.SetActive(useLives);
        }

        if (additionalInfoText != null)
        {
            additionalInfoText.gameObject.SetActive(showAdditionalInfo);
        }
        showRestartButtons = showTestRestartsButtons;
        if (showRestartButtons != false)
        {
            if (testDemoButtons != null)
            {
                testDemoButtons.SetActive(false);
            }
            if (backToMainMenuButton != null)
            {
                backToMainMenuButton.gameObject.SetActive(true);
                backToMainMenuButton.onClick.AddListener(ReturnToMainMenu);
            }
            if (restartMinigameButton != null)
            {
                restartMinigameButton.gameObject.SetActive(true);
                restartMinigameButton.onClick.AddListener(RestartMinigame);
            }
        }
    }

    public virtual void UpdateScore(int score)
    {
        if(scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }
    public virtual void UpdateScore(string score)
    {
        if(scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }
   
    public void UpdateTimer(float timeRemaining)
    {
        if(timerText != null)
        {
            timerText.text = "Time: " + Mathf.CeilToInt(timeRemaining).ToString();
        }
    }
    public void UpdateTimer(float timeRemaining, bool showDifferentColors)
    {
        if (showDifferentColors)
        {
            timerText.color = Color.white;
            if(timeRemaining <= 10f)
            {
                timerText.color = Color.red;
            }
            else if(timeRemaining <= 20f)
            {
                timerText.color = Color.yellow;
            }
            else if (timeRemaining <= 30f)
            {
                timerText.color = Color.orange;
            }
           
        }
        if(timerText != null)
        {
            timerText.text = "Time: " + Mathf.CeilToInt(timeRemaining).ToString();
        }
    }
    
    public void UpdateTimer(string timeRemaining)
    {
        if(timerText != null)
        {
            timerText.text = timeRemaining.ToString();
        }
    }
    public virtual void UpdatePlayerLives(int lives)
    {
        if(playerLivesText != null)
        {
            playerLivesText.text = "Lives: " + lives.ToString();
            if(AudioManager.instance != null)
            {
                if(lives < 3 && lives > 0)
                {
                    AudioManager.instance.miniGame_lifeLost.start();
                }

                activeScene = SceneManager.GetActiveScene();

                if (lives == 1 & activeScene.name == "BoxingMiniGameScene")
                {
                    StartCoroutine(BoxingAudio.instance.BoxingEncouragementIEnum());
                }
            }
        }
    }
    
    public virtual void UpdatePlayerLives(string lives)
    {
        if(playerLivesText != null)
        {
            playerLivesText.text = lives.ToString();
        }
    }
    
    public void ShowGameOver()
    {
        if(timerText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "Game Over!";

            if (AudioManager.instance != null)
            {
                //AudioManager.instance.gameOver_Lost.start();

                //if (boxingAudio != null)
                //{
                //    StartCoroutine(boxingAudio.TemporarilyDecreaseBoomBapVolume());
                //}

                if (trafficWardenAudio != null)
                {
                    trafficWardenAudio.StopRainAndRoadworks();
                }
            }
        }

        if (showRestartButtons)
        {
            if (testDemoButtons != null)
            {
                testDemoButtons.SetActive(true);
            }
        }
    }
    
    public virtual void ShowGameOver(string message)
    {
        if(timerText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = message;

            if (AudioManager.instance != null)
            {
                //AudioManager.instance.gameOver_Lost.start();

                //if (boxingAudio != null)
                //{
                //    StartCoroutine(boxingAudio.TemporarilyDecreaseBoomBapVolume());
                //}

                if (trafficWardenAudio != null)
                {
                    trafficWardenAudio.StopRainAndRoadworks();
                }
            }
        }

        if (showRestartButtons)
        {
            if (testDemoButtons != null)
            {
                testDemoButtons.SetActive(true);
            }
        }
    }
    
    public virtual void UpdateAdditionalInfo(string info)
    {
        if(additionalInfoText != null)
        {
            additionalInfoText.text = info;
        }
    }
    
    public virtual void UpdateAdditionalInfo(int info)
    {
        if(additionalInfoText != null)
        {
            additionalInfoText.text = info.ToString();
        }
    }
    
    public virtual void UpdateAdditionalInfo(float info)
    {
        if(additionalInfoText != null)
        {
            additionalInfoText.text = info.ToString();
        }
    }
    
    public virtual void ClearAdditionalInfo()
    {
        if(additionalInfoText != null)
        {
            additionalInfoText.text = "";
        }
    }
    
    public virtual void ShowAdditionalInfo()
    {
        if(additionalInfoText != null)
        {
            additionalInfoText.gameObject.SetActive(true);
        }
    }
    
    public virtual void HideAdditionalInfo()
    {
        if(additionalInfoText != null)
        {
            additionalInfoText.gameObject.SetActive(false);
        }
    }
    
    public void ShowVictory()
    {
        if(timerText != null)
        {
            timerText.text = "Victory!";
            trafficWardenAudio.StopRainAndRoadworks();
        }
    }
    
    public void ReturnToMainMenu()
    {
        Debug.Log("Returning to Main Menu");
        if (GameManager.Instance != null)
        {
            SceneManager.LoadScene(GameManager.Instance.startSceneName);
        }
        else
        {
            SceneManager.LoadScene("Main Menu");
        }
    }

    public void RestartMinigame()
    {
        Debug.Log("RestartMinigame");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    // ═══════════════════════════════════════════════════
    //  BONUS FLASH (separate from additional info)
    // ═══════════════════════════════════════════════════
    
    /// <summary>Show a brief bonus message that fades out. Uses bonusFlashText if assigned, 
    /// otherwise falls back to additionalInfoText.</summary>
    public void ShowBonusFlash(string message, float duration = 1.5f)
    {
        if (bonusFlashCoroutine != null) StopCoroutine(bonusFlashCoroutine);
        bonusFlashCoroutine = StartCoroutine(BonusFlashRoutine(message, duration));
    }
    
    IEnumerator BonusFlashRoutine(string message, float duration)
    {
        TMPro.TMP_Text target = bonusFlashText != null ? bonusFlashText : additionalInfoText;
        if (target == null) yield break;
        
        target.gameObject.SetActive(true);
        target.text = message;
        
        // Quick scale-up punch
        Vector3 originalScale = target.transform.localScale;
        target.transform.localScale = originalScale * 1.4f;
        
        float elapsed = 0f;
        float punchDuration = 0.25f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;
            target.transform.localScale = Vector3.Lerp(originalScale * 1.4f, originalScale, t);
            yield return null;
        }
        target.transform.localScale = originalScale;
        
        // Hold for duration then fade
        yield return new WaitForSeconds(duration - punchDuration);
        
        // Fade out
        Color original = target.color;
        float fadeDuration = 0.3f;
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            target.color = new Color(original.r, original.g, original.b, alpha);
            yield return null;
        }
        
        target.gameObject.SetActive(false);
        target.color = original; // restore
    }
    
    // ═══════════════════════════════════════════════════
    //  WARNING BANNER
    // ═══════════════════════════════════════════════════
    
    /// <summary>Show a brief warning message before an event/pattern starts.</summary>
    public void ShowWarning(string message, float duration = 2f)
    {
        if (warningCoroutine != null) StopCoroutine(warningCoroutine);
        warningCoroutine = StartCoroutine(WarningRoutine(message, duration));
    }
    
    IEnumerator WarningRoutine(string message, float duration)
    {
        TMPro.TMP_Text target = warningText != null ? warningText : additionalInfoText;
        if (target == null) yield break;
        
        target.gameObject.SetActive(true);
        target.text = message;
        target.color = new Color(1f, 0.85f, 0f, 1f); // warning yellow
        
        yield return new WaitForSeconds(duration);
        
        // Fade out
        Color c = target.color;
        float fadeDuration = 0.4f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            target.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }
        
        target.gameObject.SetActive(false);
        target.color = Color.white; // restore
    }
    
    // ═══════════════════════════════════════════════════
    //  MULTIPLIER DISPLAY
    // ═══════════════════════════════════════════════════
    
    public void UpdateMultiplier(string text)
    {
        if (multiplierText != null)
        {
            multiplierText.gameObject.SetActive(true);
            multiplierText.text = text;
        }
    }

  
     public  IEnumerator FadeTextRoutine(TMP_Text text, float duration)
    {
        text.color = new Color(1f, 1f, 1f, 1f);
        while (text.color.a > 0f)
        {
            text.color = new Color(text.color.r, text.color.g, text.color.b, text.color.a - Time.deltaTime / duration);
            yield return null;
        }
        text.gameObject.SetActive(false);
    }
    
    public void HideMultiplier()
    {
        if (multiplierText != null)
            multiplierText.gameObject.SetActive(false);
    }
    
}
