using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class RaceBoostButton : MonoBehaviour
{
    public Button boostButton;
    public float boostAmount = 3;
    Vector3 _base;
    Tween _currentTween;
    
    
   
    
    
    
    public void OnBoostButtonPressed()
    {
        float rotDuration = Mathf.Max(0.25f, boostAmount * 0.5f);
        float scaleDuration = Mathf.Clamp(rotDuration * 0.5f, 0.2f, 0.8f);
        
        _currentTween?.Kill();
        _currentTween = DOTween.Sequence()
            // rotation shake: reasonable vibrato/randomness so it looks crisp
            .Append(transform.DOShakeRotation(rotDuration, strength: 10f, vibrato: 8, randomness: 30f, fadeOut: true))
            // scale shake joined so it overlaps naturally; smaller strength keeps it subtle
            .Join(transform.DOShakeScale(scaleDuration, strength: 0.15f, vibrato: 6, randomness: 25f, fadeOut: true))
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .SetId(this)
            .OnKill(() => {
                // restore to base state to avoid residual transform drift
                transform.localRotation = Quaternion.Euler(_base);
                transform.localScale = Vector3.one;
            });
            RaceManager.Instance.ActivateShoutBoost(boostAmount);
            StartCoroutine(DeactivateBtn(boostAmount + 1f));
    }
    
    public void ActivateButton()
    {
        boostButton.interactable = true;
    }
    
    private IEnumerator DeactivateBtn(float duration)
    {
        boostButton.interactable = false;
        // Wait for the boost duration
        yield return new WaitForSeconds(duration);
        
        boostButton.interactable = true;
    }
    
    private void Start()
    {
        boostButton = this.GetComponent<Button>();
        if (boostButton == null)
        {
            Debug.LogWarning("Boost Button component is missing!");
        }
        
        boostButton.interactable = false;
        if( RaceManager.Instance == null)
        {
            Debug.LogWarning("RaceManager instance is null!");
            return;
        }
        RaceManager.Instance.startRace.AddListener(ActivateButton);
        _base = transform.localRotation.eulerAngles;

    }
}
