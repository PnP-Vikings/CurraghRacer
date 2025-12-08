using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UiFollowMouse : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
{
    Quaternion _base;
    protected Tween _currentTween;
    bool isPointerOver;

    [SerializeField] private float maxTiltAngle = 5f;
    [SerializeField] private float tiltSpeed = 0.2f;

    private RectTransform _rectTransform;
    private Canvas _canvas;
    private Camera _uiCamera;

  
    protected virtual void OnEnable()
    {
        _base = transform.localRotation;
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        
        // Get the correct camera based on render mode
        if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            _uiCamera = null; // Overlay doesn't use a camera
        }
        else
        {
            _uiCamera = _canvas.worldCamera;
        }
    }

    protected virtual void OnDisable()
    {
        _currentTween?.Kill();
        transform.localRotation = _base;
        isPointerOver = false;
    }

    protected virtual void Update()
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
        Vector3 objectScreenPos;

        // Handle both overlay and camera-based canvases
        if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            objectScreenPos = _rectTransform.position;
        }
        else
        {
            objectScreenPos = RectTransformUtility.WorldToScreenPoint(_uiCamera, _rectTransform.position);
        }

        Vector3 offset = new Vector3(inputPosition.x - objectScreenPos.x, inputPosition.y - objectScreenPos.y, objectScreenPos.z);

        float targetX = -offset.y / Screen.height * maxTiltAngle * 2f;
        float targetY = offset.x / Screen.width * maxTiltAngle * 2f;

        Quaternion targetRotation = _base * Quaternion.Euler(targetX, targetY, 0);

        _currentTween?.Kill();
        _currentTween = transform.DOLocalRotateQuaternion(targetRotation, tiltSpeed)
            .SetUpdate(true)
            .SetEase(Ease.OutQuad);
    }

    public virtual void OnPointerDown(PointerEventData e)
    {
        isPointerOver = true;
    }

    public virtual void OnPointerUp(PointerEventData e)
    {
        isPointerOver = false;
        ResetRotation();
    }

    public void OnPointerExit(PointerEventData e)
    {
        isPointerOver = false;
        ResetRotation();
    }

    public void OnPointerEnter(PointerEventData e)
    {
        isPointerOver = true;
    }

    private void ResetRotation()
    {
        _currentTween?.Kill();
        _currentTween = transform.DOLocalRotateQuaternion(_base, tiltSpeed)
            .SetUpdate(true)
            .SetEase(Ease.OutQuad);
    }
}
