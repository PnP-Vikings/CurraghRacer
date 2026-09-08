using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public enum CrossingState { Go, Stop }
public enum AngerMood { Happy, Neutral, Angry }

public class StopLine : MonoBehaviour
{
    [Header("State")]
    public CrossingState state = CrossingState.Go;

    [Header("StopLanes")]
    public GameObject goStopLines, stopStopLines;

    [Header("Anger Mood Display")]
    [Tooltip("Assign exactly 3 sprites: [0] = Happy, [1] = Neutral, [2] = Angry")]
    public Sprite[] moodSprites = new Sprite[3];

    [Tooltip("The SpriteRenderer (or UI Image) that shows the mood icon.")]
    public Image moodRenderer;

    [Tooltip("Current mood — updated automatically by the controller.")]
    public AngerMood currentMood = AngerMood.Happy;
    
    
    public Image laneAngerIndicator; // Optional UI element to show lane status
    
    public int laneIndex = -1; // Set by controller to identify which lane this is (0, 1, or 2)
    
    [Header("View from TopdownCamera")]
    public LaneOrientation laneOrientation = LaneOrientation.Left; 

    [Header("Mood Thresholds")]
    [Tooltip("Anger value below this → Happy")]
    public float happyThreshold = 0.33f;
    [Tooltip("Anger value below this → Neutral (above → Angry)")]
    public float angryThreshold = 0.66f;
    
    [Header(("Localization"))]
    [Tooltip("Localized name for this lane, e.g. 'Left Lane'")]
    private LocalizedString localizedLeftOrientation = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.TrafficWarden.LeftOrientation" };
    private LocalizedString localizedRightOrientation = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.TrafficWarden.RightOrientation" };
    private LocalizedString localizedTopOrientation = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.TrafficWarden.TopOrientation" };
    private LocalizedString localizedBottomOrientation = new LocalizedString { TableReference = "MiniGames", TableEntryReference = "Minigames.TrafficWarden.BottomOrientation" };

    public void Start()
    {
        UpdateMoodSprite();
    }

    public void SetLaneIndex(int laneIndex)
    {
        this.laneIndex = laneIndex;
    }

    public int GetLaneIndex()
    {
        return laneIndex;
    }
    
    public void ChangeState()
    {
        if (state == CrossingState.Go)
            state = CrossingState.Stop;
        else if (state == CrossingState.Stop)
            state = CrossingState.Go;

        ProcessStates();
    }

    public CrossingState GetState()
    {
        return state;
    }

    public void ProcessStates()
    {
        if (state == CrossingState.Go)
        {
            goStopLines.SetActive(true);
            stopStopLines.SetActive(false);
        }
        else if (state == CrossingState.Stop)
        {
            goStopLines.SetActive(false);
            stopStopLines.SetActive(true);
        }
    }

    /// <summary>
    /// Called by TrafficWardenMinigameController each frame with the lane's anger (0‒1).
    /// Picks the correct mood and swaps the sprite.
    /// </summary>
    public void SetAnger(float anger)
    {
        AngerMood newMood;
        if (anger < happyThreshold)
            newMood = AngerMood.Happy;
        else if (anger < angryThreshold)
            newMood = AngerMood.Neutral;
        else
            newMood = AngerMood.Angry;

        if (newMood != currentMood)
        {
            currentMood = newMood;
            UpdateMoodSprite();
        }
    }

    void UpdateMoodSprite()
    {
        if (laneAngerIndicator != null)
        {
            laneAngerIndicator.gameObject.SetActive(true);
            switch(currentMood)
            {
                case AngerMood.Happy:
                    laneAngerIndicator.color = Color.green;
                    break;
                case AngerMood.Neutral:
                    laneAngerIndicator.color = Color.yellow;
                    break;
                case AngerMood.Angry:
                    laneAngerIndicator.color = Color.red;
                    break;
            }
        }
        
        if (moodRenderer == null || moodSprites == null) return;

        int index = (int)currentMood; // Happy=0, Neutral=1, Angry=2
        if (index >= 0 && index < moodSprites.Length && moodSprites[index] != null)
            moodRenderer.sprite = moodSprites[index];
        
        
    }
    
    public string GetLocalizedLaneOrientation()
    {
      switch (laneOrientation)
      {
          case LaneOrientation.Left:
              return localizedLeftOrientation?.IsEmpty! ==false? localizedLeftOrientation.GetLocalizedString() : "Left";
          case LaneOrientation.Right:
                return localizedRightOrientation?.IsEmpty! ==false? localizedRightOrientation.GetLocalizedString() : "Right";
          case LaneOrientation.Top:
                return localizedTopOrientation?.IsEmpty! ==false? localizedTopOrientation.GetLocalizedString() : "Top";
              break;
          case LaneOrientation.Bottom:
                return localizedBottomOrientation?.IsEmpty! ==false? localizedBottomOrientation.GetLocalizedString() : "Bottom";
      }
      return "Unknown";
    }
    
    public enum LaneOrientation { Left, Right, Top, Bottom }
}
