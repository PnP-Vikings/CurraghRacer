using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RaceBoostButton : MonoBehaviour
{
    public Button boostButton;
    public float boostAmount = 3;
    
    
    
    public void OnBoostButtonPressed()
    {
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
    }
}
