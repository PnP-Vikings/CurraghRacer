using DG.Tweening;
using FMODUnity;
using System.Collections;
using UnityEngine;

public class BoxingAudio : MonoBehaviour
{
    [SerializeField] StudioEventEmitter boomBapEmitter;

    public static BoxingAudio instance;
    void Awake()
    {
        // Singleton pattern to ensure only one instance of BoxingAudio exists
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

    public IEnumerator BoxingGameOverAudio()
    {
        boomBapEmitter.SetParameter("Boom Bap Volume", 0f, false);
        Debug.Log("Boom Bap volume decreased - AudioDebug");

        if(AudioManager.instance != null)
        {
            AudioManager.instance.miniGame_Over.start();
        }

        yield return new WaitForSeconds(1.5f);

        boomBapEmitter.SetParameter("Boom Bap Volume", 1f, false);
        Debug.Log("Boom Bap volume increased - AudioDebug");
    }
}
