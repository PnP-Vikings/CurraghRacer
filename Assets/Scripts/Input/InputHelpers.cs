using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Helper utilities to read primary pointer position (mouse or touch) using the new Input System
/// and to check whether a screen position is over UI using EventSystem raycasts.
/// </summary>
public static class InputHelpers
{
    /// <summary>
    /// Attempts to get the primary pointer position (touch or mouse). Returns false if no pointer data is available.
    /// </summary>
    public static bool TryGetPrimaryPointerPosition(out Vector2 screenPosition)
    {
        // Touch has priority
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.isPressed)
        {
            screenPosition = ts.primaryTouch.position.ReadValue();
            return true;
        }

        // Mouse fallback
        if (Mouse.current != null)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        screenPosition = Vector2.zero;
        return false;
    }

    /// <summary>
    /// Checks whether the given screen position overlaps UI by performing an EventSystem raycast.
    /// Works for mouse and touch.
    /// </summary>
    public static bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        var eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }
}

