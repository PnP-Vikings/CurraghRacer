using FMODUnity;
using System.Collections;
using UnityEngine;

public class TutorialAudio : MonoBehaviour
{
    public IEnumerator PlayTutorialCompleteAudio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.miniGameProgression.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            yield return new WaitForSeconds(1.2f);
            AudioManager.instance.miniGame_Win.start();
        }
    }
    public void PlayTutorialTaskCompleteAudio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.miniGameProgression.start();
            AudioManager.instance.miniGameProgression.setParameterByName("miniGameProgression Volume", 0.95f);
        }
    }

    public void CallStopTutorialGuyAudio()
    {
        StartCoroutine(StopTutorialGuyAudio());
        //Debug.Log("Call StopTutorialGuyAudio - AudioDebug");
    }

    public IEnumerator StopTutorialGuyAudio()
    {
        if (AudioManager.instance != null)
        {
            yield return null;
            AudioManager.instance.tutorialGuy.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            //Debug.Log("StopTutorialGuyAudio - AudioDebug");
        }
    }
}
