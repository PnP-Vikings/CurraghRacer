using System.Collections.Generic;
using UnityEngine;

public enum CrossingState { Go, Stop }
public class StopLine : MonoBehaviour
{
    [Header("State")]
    public CrossingState state = CrossingState.Go;

    [Header("StopLanes")]
    public GameObject goStopLines,stopStopLines;


    public void ChangeState()
    {
        if (state == CrossingState.Go)
        {
            state = CrossingState.Stop;
        }
        else if (state == CrossingState.Stop)
        {
            state = CrossingState.Go;
        }
        
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
    
    
}
