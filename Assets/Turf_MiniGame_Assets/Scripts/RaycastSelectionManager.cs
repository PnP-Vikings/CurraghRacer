using UnityEngine;
using UnityEngine.EventSystems;

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
    if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
    {
      if (_lastSelectedStack != null)
      {
        _lastSelectedStack.SetSelectedVisible(false);
        _gameFTController.SelectedStack = null;
        _lastSelectedStack = null;
      }

      var ray = GetRayOnMousePosition();
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
  private Ray GetRayOnMousePosition()
  {
    var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    return ray;
  }

}
