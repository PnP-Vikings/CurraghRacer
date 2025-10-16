using UnityEngine;

public class BoxingUiCanvas : MonoBehaviour
{
   public TMPro.TMP_Text scoreText,timerText,playerLivesText,gameOverText;
   
   
   public void UpdateScore(int score)
   {
       if(scoreText != null)
       {
           scoreText.text = "Score: " + score;
       }
   }
   
    public void UpdateTimer(float timeRemaining)
    {
         if(timerText != null)
         {
              timerText.text = "Time: " + Mathf.CeilToInt(timeRemaining).ToString();
         }
    }
    public void UpdatePlayerLives(int lives)
    {
         if(playerLivesText != null)
         {
              playerLivesText.text = "Lives: " + lives.ToString();
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
