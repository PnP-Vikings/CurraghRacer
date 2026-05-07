using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class RockSkippingAudio : MonoBehaviour
{
    private PLAYBACK_STATE rockSkipPlaybackState;

    // As a result of the rockSkip sound playing when the rock sinks I made this function to stop any rockSkip instances when they are called on sinking

    public void PlayRockSinkAudio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.rockSkip.getPlaybackState(out rockSkipPlaybackState);
            //Debug.Log("rockSkip is " + rockSkipPlaybackState + " - AudioDebug");

            if (rockSkipPlaybackState == PLAYBACK_STATE.STARTING | rockSkipPlaybackState == PLAYBACK_STATE.PLAYING | rockSkipPlaybackState == PLAYBACK_STATE.STOPPING)
            {
                AudioManager.instance.rockSkip.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                //Debug.Log("rockSkip has been stopped - AudioDebug");
            }

            AudioManager.instance.rockSink.start();
        }
    }
}
