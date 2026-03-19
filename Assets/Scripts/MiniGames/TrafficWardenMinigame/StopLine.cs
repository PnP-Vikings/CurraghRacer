using System.Collections.Generic;
using UnityEngine;

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
    public SpriteRenderer moodRenderer;

    [Tooltip("Current mood — updated automatically by the controller.")]
    public AngerMood currentMood = AngerMood.Happy;

    [Header("Mood Thresholds")]
    [Tooltip("Anger value below this → Happy")]
    public float happyThreshold = 0.33f;
    [Tooltip("Anger value below this → Neutral (above → Angry)")]
    public float angryThreshold = 0.66f;

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
        if (moodRenderer == null || moodSprites == null) return;

        int index = (int)currentMood; // Happy=0, Neutral=1, Angry=2
        if (index >= 0 && index < moodSprites.Length && moodSprites[index] != null)
            moodRenderer.sprite = moodSprites[index];
    }
}
