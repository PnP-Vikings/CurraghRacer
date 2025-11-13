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
    bool inputDetected = false;
    Vector2 inputPosition = Vector2.zero;

    // Check for touch input first (mobile)
    var ts = Touchscreen.current;
    if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
    {
      inputDetected = true;
      inputPosition = ts.primaryTouch.position.ReadValue();
    }
    // Fall back to mouse input (desktop/editor)
    else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
    {
      inputDetected = true;
      inputPosition = Mouse.current.position.ReadValue();
    }

    if (inputDetected && !EventSystem.current.IsPointerOverGameObject())
    {
      if (_lastSelectedStack != null)
      {
        _lastSelectedStack.SetSelectedVisible(false);
        _gameFTController.SelectedStack = null;
        _lastSelectedStack = null;
      }

      var ray = GetRayOnInputPosition(inputPosition);
      RaycastHit rayCastHit;

      if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out rayCastHit, 500.0f))
      {
        //Debug.DrawRay(ray.origin, ray.direction * rayCastHit.distance, Color.green, 5.0f);
        if (rayCastHit.transform.CompareTag(_selectableTag))
        {
          _lastSelectedStack = rayCastHit.transform.GetComponent<UnitStack>();
          if (_lastSelectedStack != null)
          {
            _lastSelectedStack.SetSelectedVisible(true);
            _gameFTController.SelectedStack = _lastSelectedStack;
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
