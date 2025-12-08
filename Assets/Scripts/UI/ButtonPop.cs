using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonPop : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
{
    Vector3 _base;
    Tween _currentTween;

    void Awake()
    {
        _base = transform.localScale;
    }

    void OnDisable()
    {
        _currentTween?.Kill();
        transform.localScale = _base;
    }

    public void OnPointerDown(PointerEventData e)
    {
        _currentTween?.Kill();
        _currentTween = transform.DOScale(_base * 0.96f, 0.083f)
            .SetUpdate(true)
            .SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData e)
    {
        _currentTween?.Kill();
        _currentTween = transform.DOScale(_base, 0.083f)
            .SetUpdate(true)
            .SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData e)
    {
        _currentTween?.Kill();
        _currentTween = transform.DOScale(_base, 0.083f)
            .SetUpdate(true)
            .SetEase(Ease.OutQuad);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        _currentTween?.Kill();
        _currentTween = transform.DOScale(_base * 1.05f, 0.083f)
            .SetUpdate(true)
            .SetEase(Ease.OutQuad);
        
        
        /*_currentTween = DOTween.Sequence()
            .Append(transform.DOScale(_base * 1.05f, 0.083f))
            .Join(transform.DOPunchRotation(new Vector3(0, 0, 5f), 0.166f, 10, 1))
            .SetUpdate(true)
            .SetEase(Ease.OutQuad);*/
        
        
    }
}