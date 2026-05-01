using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class RaycastSelectionManager : MonoBehaviour
{
  private const string _selectableTag = "Selectable";

  // var to save/store last select stack
  [SerializeField]
  private UnitStack _lastSelectedStack = null;

  // ref to game manager controller
  [SerializeField]
  private GameFTController _gameFTController;

  // Update is called once per frame
  void Update()
  {
    if(_gameFTController == null)
    {
      Debug.LogWarning("GameFTController reference is missing in RaycastSelectionManager.");
      return;
    }
    if(_gameFTController.gameStarted == false)
    {
      return;
    }
    
    bool inputDetected = false;
    Vector2 inputPosition = Vector2.zero;

    // Use new input helper to read primary pointer position (works for touch and mouse)
    if (InputHelpers.TryGetPrimaryPointerPosition(out inputPosition))
    {
      // If we have input we also have a pointer press state - filter by performed press for selection
      // For touch we want wasPressedThisFrame; for mouse we want leftButton.wasPressedThisFrame
      var ts = Touchscreen.current;
      if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
      {
        inputDetected = true;
      }
      else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
      {
        inputDetected = true;
      }
    }

    if (inputDetected && !InputHelpers.IsPointerOverUI(inputPosition))
    {
      
      if (_lastSelectedStack != null &&!_lastSelectedStack.isCompleted)
      {
        _gameFTController.SetSelectedStack(_lastSelectedStack);
        return;
      }
      
      if (_lastSelectedStack != null && _lastSelectedStack.isCompleted)
      {
        _lastSelectedStack.SetSelectedVisible(false);
        _gameFTController.SetSelectedStack(null);
        _lastSelectedStack = null;
      }

      var ray = GetRayOnInputPosition(inputPosition);
      RaycastHit rayCastHit;

      if (!InputHelpers.IsPointerOverUI(inputPosition) && Physics.Raycast(ray, out rayCastHit, 500.0f))
      {
        
        
        //Debug.DrawRay(ray.origin, ray.direction * rayCastHit.distance, Color.green, 5.0f);
        if (rayCastHit.transform.CompareTag(_selectableTag))
        {
          _lastSelectedStack = rayCastHit.transform.GetComponent<UnitStack>();
          if (_lastSelectedStack != null && !_lastSelectedStack.isCompleted)
          {
            _lastSelectedStack.SetSelectedVisible(true);
            _gameFTController.SetSelectedStack(_lastSelectedStack);
          }
        }
        else
        {
          //_lastSelectedStack.SetSelectedVisible(false);
          //_gameFTController.SelectedStack = null;
          _lastSelectedStack = null;
        }

      }
    }
  }

  // helper func to get ray cast on screen coords depends on the main camera rendering the scene
  private Ray GetRayOnInputPosition(Vector2 screenPosition)
  {
    var ray = Camera.main.ScreenPointToRay(screenPosition);
    return ray;
  }

}
