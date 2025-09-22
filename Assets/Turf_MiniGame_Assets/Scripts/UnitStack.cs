using System.Collections.Generic;
using UnityEngine;

public class UnitStack : MonoBehaviour
{
  // PROP List of turfs that will be placed using animation
  [SerializeField]
  private List<Animator> _Animators = null;
  public List<Animator> Animators
  {
    get => _Animators;
  }
  
  // PROP Current animation stack index to play
  [SerializeField]
  private int _Index = 0;
  public int Index 
  { 
    get => _Index; 
    set => _Index = value; 
  }

  // Selection marker GO
  [SerializeField]
  private GameObject _selectedMark;

  private void Awake()
  {
    // get automatically all turf blocks child objects
    //_Animators = GetComponentsInChildren<Animator>().ToList();
    _Animators.AddRange(GetComponentsInChildren<Animator>());

    // get automatically selected marker child object
    _selectedMark = transform.Find("SelectedMark").gameObject;
  }

  // turn selection marker on or off
  public void SetSelectedVisible(bool visible)
  {
    if (_selectedMark == null)
    {
      return;
    }
    _selectedMark.SetActive(visible);
  }
}
