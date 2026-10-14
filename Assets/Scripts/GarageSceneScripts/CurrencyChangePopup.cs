using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class CurrencyChangePopup : MonoBehaviour
{
    [SerializeField] private Image currencyBackground;
    [SerializeField] private Image currencyImage;
    [SerializeField] private TMP_Text currencyChangeText;
    [SerializeField] private List<CurrencyChangeTypes> currencyChangeTypes;
    [SerializeField] private float lifeTime = 3;
    [SerializeField] private float fadeTime = 5;
    private Sequence _currentTween;

    [SerializeField] private float backgroundOpacity;
    [SerializeField] private float currencyImageOpacity;
    [SerializeField] private float currencyChangeTextOpacity;
    private void OnEnable()
    {
        
        float heightToMove = Random.Range(150, 800);
        float horizontalToMove = Random.Range(-700, 700);
        RectTransform rectTransform = GetComponent<RectTransform>();

        _currentTween?.Kill();
        _currentTween = DOTween.Sequence()
            .Append(rectTransform.DOAnchorPos(new Vector2(horizontalToMove, heightToMove), lifeTime)
                .SetRelative(true)
                .SetEase(Ease.InOutSine)).Join(currencyBackground.DOFade(0,fadeTime))
            .Join(currencyImage.DOFade(0,fadeTime))
            .Join(currencyChangeText.DOFade(0,fadeTime))
            .SetUpdate(true).SetEase(Ease.OutQuad);

        _currentTween.OnComplete(() =>
        {
            ResetAllUi();
            this.gameObject.SetActive(false);
        });
    }

    private void ResetAllUi()
    {
        currencyBackground.color = new Color(currencyBackground.color.r,currencyBackground.color.g,currencyBackground.color.b,backgroundOpacity);
        currencyImage.color = new Color(currencyImage.color.r,currencyImage.color.g,currencyImage.color.b,currencyImageOpacity);
        currencyChangeText.color = new Color(currencyChangeText.color.r,currencyChangeText.color.g,currencyChangeText.color.b,currencyChangeTextOpacity);
    }
    
    private void OnDisable()
    {
        _currentTween.Kill();
    }

    public void UpdateUi(CurrencyTypes injectedCurrencyType,int amount)
    {
        foreach (CurrencyChangeTypes cC in currencyChangeTypes)
        {
            if (cC.GetCurrencyType() == injectedCurrencyType)
            {
                currencyImage.sprite = cC.GetImageSprite();
            }
        }
        string amountTxt = "";
        if (amount > 0)
        {
            amountTxt = "+";
        }
        
        amountTxt = $"{amountTxt}{amount}";

        currencyChangeText.text = amountTxt;
    }

}

[System.Serializable]
public class CurrencyChangeTypes
{
    [SerializeField] private Sprite currencyImage;
    [SerializeField] private CurrencyTypes currencyType;


    public CurrencyTypes GetCurrencyType()
    {
        return currencyType;
    }
    public Sprite GetImageSprite()
    {
        return currencyImage;
    }
}

public enum CurrencyTypes
{
    Energy,
    Euro,
    Time
}