using DG.Tweening;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonPop : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
{
    Vector3 _base;
    Tween _currentTween;
    private PLAYBACK_STATE UIClick3PlaybackState;

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

        if (AudioManager.instance != null)
        {
            AudioManager.instance.UIClick3.getPlaybackState(out UIClick3PlaybackState);
            if (UIClick3PlaybackState == PLAYBACK_STATE.STOPPING || UIClick3PlaybackState == PLAYBACK_STATE.STOPPED)
            {
                AudioManager.instance.UIClick3.start();
            }
        }

    }
}