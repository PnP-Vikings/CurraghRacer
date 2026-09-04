using System.Collections;
using DG.Tweening;
using MiniGames;
using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameFTController : MonoBehaviour
{
  // display finished stacks
  [SerializeField]
  private TextMeshProUGUI _Counter,selectAStackText,startGameText;
  [SerializeField] private Button stackButton;
  [SerializeField] private MinigameCanvasUI minigameCanvasUI;
  [SerializeField] private int maxRoundTime =180;
  [SerializeField] private int currentRoundTime = 0;
  [SerializeField] private int minStackFlippedRequired = 7;
  [SerializeField] private bool timerCompleted = false;
  [SerializeField] public bool gameStarted = false;
  [SerializeField] public bool gameOver = false;
  [SerializeField] private TurfFlipperController turfFlipperController;
  

  // Current selected stack
  [SerializeField]
  private UnitStack _selectedStack = null;
  public UnitStack SelectedStack
  {
    get => _selectedStack;
    set => _selectedStack = value;
  }

  [SerializeField] private int _StacksDone = 0;

  
  [Header("Localization")]
  [SerializeField] private LocalizedString localizedStartGameText = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.FootingTurfMinigame.StartGameInfo" };
  [SerializeField] private LocalizedString localizedStackAmountText = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.FootingTurfMinigame.StackAmountText" };
  [SerializeField] private LocalizedString localizedStackCompleteText = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.FootingTurfMinigame.StackComplete" };
  [SerializeField] private LocalizedString localizedGameOverWonText = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.FootingTurfMinigame.GameOver.Won" };
  [SerializeField] private LocalizedString localizedGameOverLostText = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.FootingTurfMinigame.GameOver.Lost" };
  
  private void Awake()
  {
    currentRoundTime = maxRoundTime;
  }

  private void Start()
  {
    
    SetStackBtnVisible(false);
    _Counter.gameObject.SetActive(false);
    SetSelectAStackTextVisible(false);
    
    if (!localizedStartGameText.IsEmpty && localizedStartGameText != null)
    {
      startGameText.text = localizedStartGameText.GetLocalizedString(minStackFlippedRequired);
    }
    else
    {
      startGameText.text = $"Click on a stack to select it \nthen press stack button to flip it \nYou need to flip at least {minStackFlippedRequired} stacks before time runs out to win!";
    }
  }

  public void StartGame()
  {
    StartCoroutine(TimerLoop());
    gameStarted = true;
    
    
    minigameCanvasUI.SetUpUI(false, true, false, false, false);

    if (!localizedStackAmountText.IsEmpty && localizedStackAmountText != null)
    {
      _Counter.text = localizedStackAmountText.GetLocalizedString(_StacksDone);
    }
 
      _Counter.gameObject.SetActive(true);
      
    
    SetSelectAStackTextVisible(true);
  }
  
  
  public void SetSelectedStack(UnitStack stack)
  {
    _selectedStack = stack;
    if(turfFlipperController != null && stack != null)
    {
      turfFlipperController.GoTowardsTarget(_selectedStack.transform);
    }
     
   
    SetStackBtnVisible(_selectedStack != null);
    SetSelectAStackTextVisible(_selectedStack == null);
   
  }

  public void SetStackBtnVisible(bool visible)
  {
    if (stackButton != null)
    {
      stackButton.gameObject.SetActive(visible);
    }
  }
  
  public void SetSelectAStackTextVisible(bool visible)
  {
    if (selectAStackText != null)
    {
      selectAStackText.gameObject.SetActive(visible);
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
  
    if(gameOver)
      return;
    // Play Stack anim 
    // get next animator only if its exist on list and has been assigned.
    if (_selectedStack.Index < _selectedStack.Animators.Count && _selectedStack.Animators[_selectedStack.Index] != null)
    {
      _selectedStack.Animators[_selectedStack.Index].enabled = true; // Enable the animator, this makes the animation play automatically without triggers or params or transitions.
      _selectedStack.Index++; // Inc index counter to play next turf stack animation.

      if (_selectedStack.Index == _selectedStack.Animators.Count)
      {
        _StacksDone++; // add fineshed stack to counter.
        if (!localizedStackAmountText.IsEmpty && localizedStackAmountText != null)
        {
          _Counter.text = localizedStackAmountText.GetLocalizedString(_StacksDone);
        }
        else
        {
          _Counter.text = string.Format("Stack Completed X {0}", _StacksDone); // Update display counter
        }
        DisplayStackComplete();
        _selectedStack.SetCompleted(true); // mark stack as completed to be unselectable and hide selection marker.
        if(turfFlipperController != null)
        {
          turfFlipperController.ReturnHome();
        }
        SetStackBtnVisible(false);
        SetSelectAStackTextVisible(true);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.miniGameProgression.start();
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

  
  

  private void DisplayGameOver()
  {
    if (timerCompleted )
    {
      gameOver = true;
      if (minStackFlippedRequired <= _StacksDone)
      {
        string winMessage = "";

        if (localizedGameOverWonText != null && !localizedGameOverWonText.IsEmpty)
        {
          winMessage = localizedGameOverWonText.GetLocalizedString(_StacksDone, minStackFlippedRequired);
        }
        else
        {
          winMessage = $"Time's Up!\n Congrats You flipped {_StacksDone} stacks! \n You needed to flip at least {minStackFlippedRequired} stacks to win.";
        }
        minigameCanvasUI.ShowGameOver(winMessage);
        Debug.Log("Game Over: Timer completed and minimum stacks flipped requirement met.");
        MiniGameAudio.instance.PlayMiniGameOverWinAudio();
      }
      else
      {
        string loseMessage = "";
        
        if (localizedGameOverLostText != null && !localizedGameOverLostText.IsEmpty)
        {
          loseMessage = localizedGameOverLostText.GetLocalizedString(_StacksDone, minStackFlippedRequired);
        }
        else
        {
          loseMessage = $"Time's Up!\n You flipped {_StacksDone} stacks! \n You needed to flip at least {minStackFlippedRequired} stacks to win.";
        }
        minigameCanvasUI.ShowGameOver(loseMessage);
        Debug.Log("Game Over: Timer completed but minimum stacks flipped requirement not met.");
        MiniGameAudio.instance.PlayMiniGameOverAudio();
      }
      
      if (MiniGameManager.Instance != null)
      {
        Debug.Log($"Calling MiniGameManager.CompleteGame with score: {_StacksDone}");
        MiniGameManager.Instance.CompleteGame(_StacksDone,2f);
      }
      else
      {
        Debug.LogError("MiniGameManager.Instance is null!");
        if (GameManager.Instance != null)
        {
          GameManager.Instance.PlayerWorked();
          SceneManager.LoadScene(GameManager.Instance.mainSceneName);
        }
      
      }
    }
  }

  private void DisplayStackComplete()
  {
    string stackCompleteMessage = "";


    if (localizedStackCompleteText != null && !localizedStackCompleteText.IsEmpty)
    {
    
     stackCompleteMessage = localizedStackCompleteText.GetLocalizedString();
    }
    else
    {
      stackCompleteMessage = $"Stack Complete";
    }
    
    minigameCanvasUI.UpdateMultiplier(stackCompleteMessage);
    
    StartCoroutine(minigameCanvasUI.FadeTextRoutine(minigameCanvasUI.multiplierText, 1.5f));
  }
  
 
  
  private IEnumerator TimerLoop()
  {
    Debug.Log("Timer loop started with maxRoundTime: " + maxRoundTime);
    while (currentRoundTime > 0)
    {
      minigameCanvasUI.UpdateTimer(currentRoundTime,true);
      yield return new WaitForSeconds(1f); // waits 1 scaled second
      currentRoundTime--;
    }

    minigameCanvasUI.UpdateTimer(0);
    timerCompleted = true;
    DisplayGameOver();

  }

    public void PlayPlaceTurfAudio()
    {
        if (AudioManager.instance != null & _selectedStack != null)
        {
            AudioManager.instance.placeTurf.start();
        }
    }
}
