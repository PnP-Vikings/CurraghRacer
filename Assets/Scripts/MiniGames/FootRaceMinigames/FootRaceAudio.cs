using FMOD.Studio;
using UnityEngine;

public class FootRaceAudio : MonoBehaviour
{
    public static FootRaceAudio instance;

    private PLAYBACK_STATE footRaceEncouragementPlaybackState;

    public void PlayJumpAudio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.jump.start();

            AudioManager.instance.footRaceEncouragement.getPlaybackState(out footRaceEncouragementPlaybackState);

            if (footRaceEncouragementPlaybackState == PLAYBACK_STATE.STOPPED || footRaceEncouragementPlaybackState == PLAYBACK_STATE.STOPPING)
            {
                AudioManager.instance.footRaceEncouragement.start();
                //Debug.Log("footRaceEncouragement called as no other instance exists");
            }
        }
    }
}
