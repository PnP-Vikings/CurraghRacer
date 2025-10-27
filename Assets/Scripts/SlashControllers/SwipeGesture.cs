using UnityEngine;
using UnityEngine.InputSystem;


/*Directional “gesture” (Temple Run–style)
record touch start, then if the finger travels fast 
beyond a min distance in a max time,
trigger a direction.*/
public class SwipeGesture : MonoBehaviour
{
    public float minDistance = 80f;   // pixels
    public float maxTime = 0.25f;     // seconds
    public float directionBias = 0.5f;// 0..1 (0 = diagonal ok, 1 = strict axis)

    Vector2 _startPos; float _startTime; bool _tracking;

    public System.Action OnSwipeUp, OnSwipeDown, OnSwipeLeft, OnSwipeRight;

    void Update()
    {
        // Try touchscreen first
        var ts = Touchscreen.current;
        if (ts != null)
        {
            var touch = ts.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                _tracking = true;
                _startPos = touch.position.ReadValue();
                _startTime = Time.unscaledTime;
            }
            else if (touch.press.isPressed && _tracking)
            {
                // you can add a visual hint here if you want
            }
            else if (touch.press.wasReleasedThisFrame && _tracking)
            {
                _tracking = false;
                Vector2 endPos = touch.position.ReadValue();
                ProcessSwipe(endPos);
            }
        }
        // Fall back to mouse if no touch
        else
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Debug.Log("Mouse swipe started");
                _tracking = true;
                _startPos = mouse.position.ReadValue();
                _startTime = Time.unscaledTime;
            }
            else if (mouse.leftButton.isPressed && _tracking)
            {
                // you can add a visual hint here if you want
            }
            else if (mouse.leftButton.wasReleasedThisFrame && _tracking)
            {
                Debug.Log("Mouse swipe ended");
                _tracking = false;
                Vector2 endPos = mouse.position.ReadValue();
                ProcessSwipe(endPos);
            }
        }
    }

    void ProcessSwipe(Vector2 endPos)
    {
        float dt = Time.unscaledTime - _startTime;
        Vector2 delta = endPos - _startPos;
        if (dt <= maxTime && delta.magnitude >= minDistance)
        {
            Vector2 dir = delta.normalized;
            // choose dominant axis with bias
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y) + directionBias * 0.01f)
                (dir.x > 0 ? OnSwipeRight : OnSwipeLeft)?.Invoke();
            else
            {
                (dir.y > 0 ? OnSwipeUp : OnSwipeDown)?.Invoke();
            }
        }
    }
}