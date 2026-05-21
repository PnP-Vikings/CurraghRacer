using FMODUnity;
using UnityEngine;

public class RowingGameAudio : MonoBehaviour
{
    private bool hasJustMissed = false;

    public void PlayRowingGameSuccess()
    {
        if (AudioManager.instance != null)
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

        hasJustMissed = false;
    }

    public void PlayRowingGameFail()
    {
        hasJustMissed = true;

        if(AudioManager.instance != null)
        {
            AudioManager.instance.rowingGameFail.start();
            AudioManager.instance.rowingGameFailDialogue.start();
        }
    }
}
