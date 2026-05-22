using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class RowingGameAudio : MonoBehaviour
{
    private bool hasJustMissed = false;
    private PLAYBACK_STATE rowingGameFailDialoguePlaybackState;
    private PLAYBACK_STATE rowingGameSuccessDialoguePlaybackState;

    public void PlayRowingGameSuccess()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.rowingGameSuccessDialogue.getPlaybackState(out rowingGameSuccessDialoguePlaybackState);

            if(rowingGameSuccessDialoguePlaybackState == PLAYBACK_STATE.STOPPING | rowingGameSuccessDialoguePlaybackState == PLAYBACK_STATE.STOPPED)
            {
                if (hasJustMissed == false)
                {
                    AudioManager.instance.rowingGameSuccess.start();
                    AudioManager.instance.rowingGameSuccessDialogue.start();
                }
                else
                {
                    AudioManager.instance.rowingGameSuccessAfterFail.start();
                }
            }
            else
            {
                //Debug.Log("RowingGameSuccess was not called as it is already playing - AudioDebug");
            }
        }

        hasJustMissed = false;
    }

    public void PlayRowingGameFail()
    {
        hasJustMissed = true;

        if(AudioManager.instance != null)
        {
            AudioManager.instance.rowingGameFailDialogue.getPlaybackState(out rowingGameFailDialoguePlaybackState);

            if(rowingGameFailDialoguePlaybackState == PLAYBACK_STATE.STOPPING | rowingGameFailDialoguePlaybackState == PLAYBACK_STATE.STOPPED)
            {
                AudioManager.instance.rowingGameFail.start();
                AudioManager.instance.rowingGameFailDialogue.start();
            }
            else
            {
                //Debug.Log("RowingGameFail was not called as it is already playing - AudioDebug");
            }
        }
    }
}
