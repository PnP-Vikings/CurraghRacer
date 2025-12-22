using DG.Tweening;
using UnityEngine;

public class RotateObject : MonoBehaviour
{
    Vector3 _base;
    Tween _currentTween;
    void Awake()
    {
        _base = transform.localRotation.eulerAngles;
    }


    void OnEnable()
    {
        _currentTween?.Kill();
        _currentTween = transform.DORotate(_base + new Vector3(0, 360f, 0), 5f, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Restart)
            .SetEase(Ease.Linear);
    }
}
