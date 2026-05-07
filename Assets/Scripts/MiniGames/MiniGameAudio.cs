using FMODUnity;
using System.Collections;
using UnityEngine;

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

    public void PlayGameOverLost()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.gameOver_Lost.start();
            //Debug.Log("gameOver_Lost");
        }
    }

    public void PlayGameOverWin()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.gameOver_Win.start();
            //Debug.Log("gameOver_Win");
        }
    }

    public IEnumerator FootRaceGameOver()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.running.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.crashIntoFence.start();
            yield return new WaitForSeconds(0.7f);
            AudioManager.instance.gameOver_Lost.start();
            //Debug.Log("Foot Race ended");
        }
    }
}
