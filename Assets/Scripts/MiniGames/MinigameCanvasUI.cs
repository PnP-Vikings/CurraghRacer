using MiniGames;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MinigameCanvasUI : MonoBehaviour
{

    public TMPro.TMP_Text scoreText, timerText, playerLivesText, gameOverText, additionalInfoText;
    bool showRestartButtons = false;
    public GameObject testDemoButtons;
    public Button backToMainMenuButton, restartMinigameButton;
    [SerializeField] BoxingAudio boxingAudio;


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
                    AudioManager.instance.lifeLost.start();
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

            if(AudioManager.instance != null)
            {
                AudioManager.instance.gameOver.start();
                boxingAudio.DecreaseBoomBapVolume();
                //boxingAudio.StartCoroutine(TemporarilyDecreaseBoomBapVolume());
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
    
   
    
}
