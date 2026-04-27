using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameFTController : MonoBehaviour
{
  // display finished stacks
  [SerializeField]
  private TextMeshProUGUI _Counter;
  [SerializeField] private Button stackButton;

  // Current selected stack
  [SerializeField]
  private UnitStack _selectedStack = null;
  public UnitStack SelectedStack
  {
    get => _selectedStack;
    set => _selectedStack = value;
  }

  private int _StacksDone = 0;

  private void Start()
  {
    if (stackButton != null)
    {
      stackButton.gameObject.SetActive(false);
    }
  }

  public void SetSelectedStack(UnitStack stack)
  {
    _selectedStack = stack;
    
    if (stackButton != null)
    {
      stackButton.gameObject.SetActive(_selectedStack != null);
    }
  }
  private bool AnimatorIsPlaying(Animator animator)
  {
    return animator.GetCurrentAnimatorStateInfo(0).length > animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
  }

  // stack animation control, part of game mechanics, when 1 stack is finished its added to the counter
  public void PlayAnimation()
  {
    if (_selectedStack == null)
    {
      return;
    }

    // Play Stack anim 
    // get next animator only if its exist on list and has been assigned.
    if (_selectedStack.Index < _selectedStack.Animators.Count && _selectedStack.Animators[_selectedStack.Index] != null)
    {
      _selectedStack.Animators[_selectedStack.Index].enabled = true; // Enable the animator, this makes the animation play automatically without triggers or params or transitions.
      _selectedStack.Index++; // Inc index counter to play next turf stack animation.

      if (_selectedStack.Index == _selectedStack.Animators.Count)
      {
        _StacksDone++; // add fineshed stack to counter.
        _Counter.text = string.Format("X {0}", _StacksDone); // Update display counter
        _selectedStack.SetCompleted(true); // mark stack as completed to be unselectable and hide selection marker.
        if (AudioManager.instance != null)
        {
            AudioManager.instance.turfStackComplete.start();
        }       
       
      }
    }
    //animator.SetTrigger(triggerName); // Trigger the animation using a parameter
  }

  public void MoveButtonRandomly(RectTransform buttonRectTransform)
  {
    if (buttonRectTransform == null)
    {
      Debug.LogWarning("Button RectTransform not assigned!");
      return;
    }

    // randomize button pos withing set bounds
    float randomX1 = Random.Range(-40, -200);
    float randomX2 = Random.Range(-1520, -1680);
    float randomY = Random.Range(40, 980);

    int randomIntSelectX = Random.Range(0, 2);

    if (randomIntSelectX == 0)
    {
      // Apply the new position to the button's RectTransform
      buttonRectTransform.anchoredPosition = new Vector2(randomX1, randomY);
    }
    else
    {
      // Apply the new position to the button's RectTransform
      buttonRectTransform.anchoredPosition = new Vector2(randomX2, randomY);
    }    
  }

    public void PlayPlaceTurfAudio()
    {
        if (AudioManager.instance != null & _selectedStack != null)
        {
            AudioManager.instance.placeTurf.start();
        }
    }
}
