using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UiFollowMouse : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
{
    Quaternion _base;
    Tween _currentTween;
    bool isPointerOver;
    PlayerInputs playerInput;
    
    [SerializeField] private float maxTiltAngle = 5f;
    [SerializeField] private float tiltSpeed = 0.2f;
    
    private RectTransform _rectTransform;
    private Canvas _canvas;
    
    void Awake()
    {
        _base = transform.localRotation;
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        
    }

    private void OnEnable()
    {
        playerInput = new PlayerInputs();
    }
    void OnDisable()
    {
        _currentTween?.Kill();
        transform.localRotation = _base;
    }
    
    void Update()
    {
        if (!isPointerOver) return;
        
        Vector2 inputPosition = Vector2.zero;
        bool hasInput = false;

        // Check for touch input first (mobile)
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.isPressed)
        {
            inputPosition = ts.primaryTouch.position.ReadValue();
            hasInput = true;
        }
        // Fall back to mouse input (desktop/editor)
        else if (Mouse.current != null)
        {
            inputPosition = Mouse.current.position.ReadValue();
            hasInput = true;
        }

        if (!hasInput) return;
        
        Vector3 mousePos = inputPosition;
        Vector3 objectScreenPos = RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, transform.position);
        
        Vector3 offset = mousePos - objectScreenPos;
        
        float targetX = -offset.y / Screen.height * maxTiltAngle * 2f;
        float targetY = offset.x / Screen.width * maxTiltAngle * 2f;

        Quaternion targetRotation = _base * Quaternion.Euler(targetX, targetY, 0);

        _currentTween?.Kill();
        _currentTween = transform.DOLocalRotateQuaternion(targetRotation, tiltSpeed)
            .SetUpdate(true)
            .SetEase(Ease.OutQuad);
    }
    
    public void OnPointerDown(PointerEventData e)
    {
        isPointerOver = true;
    }

    public void OnPointerUp(PointerEventData e)
    {
        isPointerOver = false;
        _currentTween?.Kill();
        _currentTween = transform.DOLocalRotateQuaternion(_base, tiltSpeed)
            .SetUpdate(true)
            .SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData e)
    {
        isPointerOver = false;
        _currentTween?.Kill();
        _currentTween = transform.DOLocalRotateQuaternion(_base, tiltSpeed)
            .SetUpdate(true)
            .SetEase(Ease.OutQuad);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        isPointerOver = true;
    }
}
