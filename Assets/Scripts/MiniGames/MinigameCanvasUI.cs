using UnityEngine;

public class MinigameCanvasUI : MonoBehaviour
{
    public TMPro.TMP_Text scoreText,timerText,playerLivesText,gameOverText;
   
   
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
        }
    }
    
    public void ShowVictory()
    {
        if(timerText != null)
        {
            timerText.text = "Victory!";
        }
    }
    
    public void SetUpUI(bool useScore, bool useTimer, bool useLives)
    {
        if(scoreText != null)
        {
            scoreText.gameObject.SetActive(useScore);
        }
        if(timerText != null)
        {
            timerText.gameObject.SetActive(useTimer);
        }
        if(playerLivesText != null)
        {
            playerLivesText.gameObject.SetActive(useLives);
        }
    }
}
