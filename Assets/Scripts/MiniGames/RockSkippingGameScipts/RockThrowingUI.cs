using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// UI feedback for rock throwing mechanics
/// Handles power bar, angle indicator, bounce timing circle, and result popups
/// </summary>
public class RockThrowingUI : MonoBehaviour
{
    [Header("Throwing UI Panel")]
    [SerializeField] private GameObject throwingPanel;
    
    [Header("Power Bar")]
    [SerializeField] private GameObject powerBarSlider;
    [SerializeField] private Image powerBarFill;
    [SerializeField] private Image powerBarBackground;
    [SerializeField] private Gradient powerGradient;
    
    [Header("Angle Indicator")]
    [SerializeField] private RectTransform angleIndicator;
    [SerializeField] private float angleIndicatorRange = 200f; // Pixels left/right from center
    
    [Header("Bounce Timing UI")]
    [SerializeField] private GameObject bounceTimingPanel;
    [SerializeField] private Image timingCircleOuter;
    [SerializeField] private Image timingCircleTarget;
    [SerializeField] private Image timingCircleShrinking;
    
    [Header("Result Popup")]
    [SerializeField] private GameObject resultPopup;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI comboText;
    
    [Header("Distance Display")]
    [SerializeField] private GameObject distancePanel;
    [SerializeField] private TextMeshProUGUI distanceText;
    
    [Header("Ai Distance Display")]
    [SerializeField] private GameObject aiDistancePanel;
    [SerializeField] private TextMeshProUGUI aiDistanceText;
    
    [Header("Message Display")]
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TextMeshProUGUI messageText;
    
    [Header("Rhythm Mode")]
    [SerializeField] private GameObject beatIndicator;
    [SerializeField] private Image beatPulseImage;
    
    [Header("Mode Instructions")]
    [SerializeField] private TextMeshProUGUI instructionText;
    
    [Header("Colors")]
    [SerializeField] private Color perfectColor = new Color(1f, 0.84f, 0f); // Gold
    [SerializeField] private Color goodColor = new Color(0.2f, 0.8f, 0.2f); // Green
    [SerializeField] private Color okayColor = new Color(0.8f, 0.8f, 0.2f); // Yellow
    [SerializeField] private Color missColor = new Color(0.8f, 0.2f, 0.2f); // Red
    
    [Header("Animation Settings")]
    [SerializeField] private float resultPopupDuration = 1f;
    [SerializeField] private float distanceCountDuration = 1.5f;
    
    private Sequence currentTimingSequence;
    private Coroutine messageCoroutine;
    
    private void Awake()
    {
        // Initialize gradient if not set
        if (powerGradient == null)
        {
            powerGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(Color.red, 0f);
            colorKeys[1] = new GradientColorKey(Color.yellow, 0.5f);
            colorKeys[2] = new GradientColorKey(Color.green, 1f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            powerGradient.SetKeys(colorKeys, alphaKeys);
        }
        
        HideAll();
    }
    
    #region Show/Hide Panels
    
    public void ShowThrowingUI(RockThrowingController.ThrowingMode mode)
    {
        HideAll();
        
        if (throwingPanel != null)
            throwingPanel.SetActive(true);
        
        // Reset power bar
        if (powerBarFill != null)
        {
            powerBarFill.fillAmount = 0f;
            powerBarFill.color = powerGradient.Evaluate(0f);
        }
        
        // Reset angle indicator
        if (angleIndicator != null)
        {
            angleIndicator.anchoredPosition = new Vector2(0f, angleIndicator.anchoredPosition.y);
        }
        
        // Show mode-specific UI
        if (beatIndicator != null)
            beatIndicator.SetActive(mode == RockThrowingController.ThrowingMode.Rhythm);
        
        // Update instructions
        if (instructionText != null)
        {
            instructionText.text = mode switch
            {
                RockThrowingController.ThrowingMode.Flick => "Swipe to throw!\nDirection = Angle, Distance = Power",
                RockThrowingController.ThrowingMode.Rhythm => "Press on the BEAT to throw!",
                RockThrowingController.ThrowingMode.Oscillator => "Hold to charge power\nRelease to throw!",
                _ => ""
            };
            instructionText.gameObject.SetActive(true);
        }
    }
    
    public void HideThrowingUI()
    {
        /*if (throwingPanel != null)
            throwingPanel.SetActive(false);*/
        
        if (bounceTimingPanel != null) bounceTimingPanel.SetActive(false);
        if (resultPopup != null) resultPopup.SetActive(false);
        if (powerBarSlider != null) powerBarSlider.SetActive(false);
        if (distancePanel != null) distancePanel.SetActive(false);
        if (aiDistancePanel != null) aiDistancePanel.SetActive(false);
        if (messagePanel != null) messagePanel.SetActive(false);
        if (beatIndicator != null) beatIndicator.SetActive(false);
        if (instructionText != null) instructionText.gameObject.SetActive(false);
    }
    
    public void HideThrowingInterface()
    {
        if (bounceTimingPanel != null) bounceTimingPanel.SetActive(false);
        if (resultPopup != null) resultPopup.SetActive(false);
        if (powerBarSlider != null) powerBarSlider.SetActive(false);
        if (messagePanel != null) messagePanel.SetActive(false);
        if (beatIndicator != null) beatIndicator.SetActive(false);
        if (instructionText != null) instructionText.gameObject.SetActive(false);
    }
    
    public void ShowBounceUI()
    {
        if (bounceTimingPanel != null)
            bounceTimingPanel.SetActive(true);
        
        if (instructionText != null)
        {
            instructionText.text = "Tap when the circle hits the target!";
            instructionText.gameObject.SetActive(true);
        }
    }
    
    public void HideBounceUI()
    {
        if (bounceTimingPanel != null)
            bounceTimingPanel.SetActive(false);
        
        if (currentTimingSequence != null && currentTimingSequence.IsActive())
        {
            currentTimingSequence.Kill();
        }
    }
    
    private void HideAll()
    {
        if(angleIndicator != null) angleIndicator.gameObject.SetActive(false);
        if (throwingPanel != null) throwingPanel.SetActive(false);
        if (bounceTimingPanel != null) bounceTimingPanel.SetActive(false);
        if (resultPopup != null) resultPopup.SetActive(false);
        if (distancePanel != null) distancePanel.SetActive(false);
        if (aiDistancePanel != null) aiDistancePanel.SetActive(false);
        if (messagePanel != null) messagePanel.SetActive(false);
        if (beatIndicator != null) beatIndicator.SetActive(false);
        if (instructionText != null) instructionText.gameObject.SetActive(false);
        if(powerBarSlider != null) powerBarSlider.SetActive(false);
    }
    
    #endregion
    
    #region Power & Angle Updates
    
    public void UpdatePowerBar(float normalizedPower)
    {
        if (powerBarFill == null) return;
        powerBarSlider?.SetActive(true);
        powerBarFill.fillAmount = normalizedPower;
        powerBarFill.color = powerGradient.Evaluate(normalizedPower);
        
        // Add pulse effect at max power
        if (normalizedPower >= 0.95f)
        {
            powerBarFill.transform.DOKill();
            powerBarFill.transform.DOScale(1.1f, 0.1f).SetLoops(2, LoopType.Yoyo);
        }
    }
    
    public void UpdateAngleIndicator(float normalizedAngle)
    {
        if (angleIndicator == null) return;
        angleIndicator.gameObject.SetActive(true);
        
        // normalizedAngle is -1 to 1, map to pixel position
        float xPos = normalizedAngle * angleIndicatorRange;
        angleIndicator.anchoredPosition = new Vector2(xPos, angleIndicator.anchoredPosition.y);
    }
    
    #endregion
    
    #region Bounce Timing Circle
    
    public void StartBounceTimingCircle(float duration)
    {
        if (timingCircleShrinking == null || timingCircleTarget == null) return;
        
        // Kill any existing sequence
        if (currentTimingSequence != null && currentTimingSequence.IsActive())
        {
            currentTimingSequence.Kill();
        }
        
        // Reset shrinking circle to large size
        timingCircleShrinking.transform.localScale = Vector3.one * 3f;
        timingCircleShrinking.color = new Color(1f, 1f, 1f, 0.8f);
        timingCircleShrinking.gameObject.SetActive(true);
        timingCircleTarget.gameObject.SetActive(true);
        
        // Animate shrinking to target size
        currentTimingSequence = DOTween.Sequence();
        
        // Shrink to target at halfway point (when timing is perfect)
        currentTimingSequence.Append(
            timingCircleShrinking.transform.DOScale(1f, duration / 2f).SetEase(Ease.Linear)
        );
        
        // Then shrink past target
        currentTimingSequence.Append(
            timingCircleShrinking.transform.DOScale(0.5f, duration / 2f).SetEase(Ease.Linear)
        );
        
        // Fade out at end
        currentTimingSequence.Join(
            timingCircleShrinking.DOFade(0f, duration / 2f).SetEase(Ease.InQuad)
        );
        
        currentTimingSequence.OnComplete(() => {
            timingCircleShrinking.gameObject.SetActive(false);
        });
    }
    
    #endregion
    
    #region Result Displays
    
    public void ShowBounceResult(BounceResult result, int comboCount)
    {
        if (resultPopup == null || resultText == null) return;
        
        resultPopup.SetActive(true);
        
        // Set text and color based on result
        switch (result)
        {
            case BounceResult.Perfect:
                resultText.text = "PERFECT!";
                resultText.color = perfectColor;
                break;
            case BounceResult.Good:
                resultText.text = "GOOD!";
                resultText.color = goodColor;
                break;
            case BounceResult.Okay:
                resultText.text = "OK";
                resultText.color = okayColor;
                break;
            case BounceResult.Miss:
                resultText.text = "MISS";
                resultText.color = missColor;
                break;
        }
        
        // Show combo if applicable
        if (comboText != null)
        {
            if (comboCount > 1)
            {
                comboText.text = $"x{comboCount} COMBO!";
                comboText.gameObject.SetActive(true);
                comboText.transform.DOKill();
                comboText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5);
            }
            else
            {
                comboText.gameObject.SetActive(false);
            }
        }
        
        // Animate popup
        resultPopup.transform.DOKill();
        resultPopup.transform.localScale = Vector3.zero;
        resultPopup.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        resultPopup.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);
        
        // Hide after duration
        DOVirtual.DelayedCall(resultPopupDuration, () => {
            if (resultPopup != null)
            {
                resultPopup.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack)
                    .OnComplete(() => resultPopup.SetActive(false));
            }
        });
        
        // Play sound
        if (AudioManager.instance != null)
        {
            switch (result)
            {
                case BounceResult.Perfect:
                    AudioManager.instance.rowingGameSuccess.start();
                    break;
                case BounceResult.Miss:
                    AudioManager.instance.rowingGameFail.start();
                    break;
            }
        }
    }
    
    public void ShowTimingResult(string text)
    {
        if (resultPopup == null || resultText == null) return;
        
        resultPopup.SetActive(true);
        resultText.text = text;
        
        if (text == "PERFECT!")
            resultText.color = perfectColor;
        else if (text == "GOOD!")
            resultText.color = goodColor;
        else
            resultText.color = okayColor;
        
        // Animate
        resultPopup.transform.DOKill();
        resultPopup.transform.localScale = Vector3.zero;
        resultPopup.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        
        DOVirtual.DelayedCall(resultPopupDuration, () => {
            if (resultPopup != null)
            {
                resultPopup.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack)
                    .OnComplete(() => resultPopup.SetActive(false));
            }
        });
    }
    
    public void ShowDistanceResult(float distance)
    {
        if (distancePanel == null || distanceText == null) return;
        
        distancePanel.SetActive(true);
        
        // Animate counting up
        float currentValue = 0f;
        DOTween.To(() => currentValue, x => {
            currentValue = x;
            distanceText.text = $"Player Distance: {currentValue:F1}m";
        }, distance, distanceCountDuration).SetEase(Ease.OutQuad);
        
        // Punch scale on complete
        DOVirtual.DelayedCall(distanceCountDuration, () => {
            distanceText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);
        });
    }
    
    public void HideDistanceResult()
    {
        if (distancePanel != null)
        {
            distancePanel.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack)
                .OnComplete(() => distancePanel.SetActive(false));
        }
    }
    
    public void ShowAIDistanceResult(float distance)
    {
        if (aiDistancePanel == null || aiDistanceText == null) return;
        
        aiDistancePanel.SetActive(true);
        aiDistanceText.text = $"AI Distance: {distance:F1}m";
        
        // Animate punch
        aiDistanceText.transform.DOKill();
        aiDistanceText.transform.localScale = Vector3.one;
        aiDistanceText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5);
    }
    
    
    public void HideAIDistanceResult()
    {
        if (aiDistancePanel != null)
        {
            aiDistancePanel.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack)
                .OnComplete(() => aiDistancePanel.SetActive(false));
        }
    }
    
    #endregion
    
    #region Message Display
    
    public void ShowMessage(string message, float duration)
    {
        if (messagePanel == null || messageText == null) return;
        
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }
        
        messageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration));
    }
    
    private IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        messagePanel.SetActive(true);
        messageText.text = message;
        
        messagePanel.transform.DOKill();
        messagePanel.transform.localScale = Vector3.zero;
        messagePanel.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        
        yield return new WaitForSeconds(duration);
        
        messagePanel.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack)
            .OnComplete(() => messagePanel.SetActive(false));
    }
    
    #endregion
    
    public void UpdateInstructionText(string instruction)
    {
        if (instructionText != null)
        {
            instructionText.text = instruction;
            instructionText.gameObject.SetActive(true);
        }
    }
    
    #region Rhythm Mode
    
    public void PulseBeat()
    {
        if (beatPulseImage == null) return;
        
        beatPulseImage.transform.DOKill();
        beatPulseImage.transform.localScale = Vector3.one;
        beatPulseImage.transform.DOPunchScale(Vector3.one * 0.5f, 0.2f, 1);
        
        // Also pulse the power bar background for visual feedback
        if (powerBarBackground != null)
        {
            powerBarBackground.DOKill();
            powerBarBackground.DOColor(Color.white, 0.1f).OnComplete(() => {
                powerBarBackground.DOColor(new Color(0.2f, 0.2f, 0.2f), 0.2f);
            });
        }
        
        // Play beat sound
        if (AudioManager.instance != null)
        {
            AudioManager.instance.UIClick1.start();
        }
    }
    
    #endregion
    
    private void OnDestroy()
    {
        // Kill all tweens
        if (currentTimingSequence != null && currentTimingSequence.IsActive())
        {
            currentTimingSequence.Kill();
        }
        
        DOTween.Kill(this);
    }
}
