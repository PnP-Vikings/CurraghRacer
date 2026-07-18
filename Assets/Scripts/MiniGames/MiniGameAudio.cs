using FMODUnity;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MiniGameAudio : MonoBehaviour
{
    public static MiniGameAudio instance;

    void Awake()
    {
        // Singleton pattern to ensure only one instance of MiniGameAudio exists
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMiniGameOverAudio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.miniGame_Over.start();
            //Debug.Log("gameOver_Lost");
        }
    }

    public void PlayMiniGameOverWinAudio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.miniGame_Win.start();
            //Debug.Log("gameOver_Win");
        }
    }

    public IEnumerator StartFootRaceMiniGameOverIEnum()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.running.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.bodhran.setParameterByName("Bodhran pitch", 0f);
            AudioManager.instance.crashIntoFence.start();
            yield return new WaitForSeconds(0.7f);
            AudioManager.instance.bodhran.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.miniGame_Over.start();
            yield return new WaitForSeconds(1.7f);
            AudioManager.instance.miniGameCompleteDialogue.start();
            //Debug.Log("Foot Race ended");
        }
    }
}
