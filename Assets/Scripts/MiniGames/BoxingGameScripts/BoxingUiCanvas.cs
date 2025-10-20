using UnityEngine;
using UnityEngine.UI;

public class BoxingUiCanvas : MinigameCanvasUI
{
    public Image healthIndicator;

    public override void UpdatePlayerLives(int lives)
    {
        base.UpdatePlayerLives(lives);
        if (healthIndicator != null)
        {
            healthIndicator.fillAmount = Mathf.Clamp01(lives / 3f);
            if (lives >= 3)
            {
                healthIndicator.color = Color.Lerp(Color.red, Color.green, healthIndicator.fillAmount/2);
            }
            else
            {
                healthIndicator.color = Color.Lerp(Color.red, Color.yellow, healthIndicator.fillAmount);
            }
            
        }
    }
    
        
    

}
