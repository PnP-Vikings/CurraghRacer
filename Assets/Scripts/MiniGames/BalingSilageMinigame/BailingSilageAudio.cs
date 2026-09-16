using UnityEngine;

public class BailingSilageAudio : MonoBehaviour
{
    public void PlayTractorCrashAudio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.carCrash.start();
            AudioManager.instance.crashIntoFence.start();
        }
    }
}
