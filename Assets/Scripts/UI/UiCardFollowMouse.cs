using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class UiCardFollowMouse : UiFollowMouse
{
    [SerializeField] private DecisionCardUi _card;
    private Quaternion _originalRotation;
    bool canRotate = true;

    protected override void OnEnable()
    {
        base.OnEnable();
        _card = GetComponent<DecisionCardUi>();
        if (_card != null)
        {
            _originalRotation = transform.rotation;
        }
    }

    protected override void Update()
    {
        if (_card == null || !canRotate)
        {
            return; // Don't update position/rotation during swipe
        }
        
        base.Update();    
        
    }
    
    protected override void OnDisable()
    {
        base.OnDisable();
        ResetRotation();
    }

    public override void OnPointerDown(PointerEventData e)
    {
        canRotate = false;
        _currentTween?.Kill();
        ResetRotation();
    }
    
    public override void OnPointerUp(PointerEventData e)
    {
        canRotate = true;
        /*ResetRotation();*/
    }
    
    
    
    
    

    // Reset rotation when card is released
    public void ResetRotation()
    {
        if (transform != null)
        {
            transform.rotation = _originalRotation;
        }
    }
}