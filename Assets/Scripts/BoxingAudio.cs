using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using System.Collections;
using UnityEngine;

public class BoxingAudio : MonoBehaviour
{
    [SerializeField] StudioEventEmitter boomBapEmitter;
    [SerializeField] StudioEventEmitter AmbientEncouragementEmitter;

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
        AmbientEncouragementEmitter.Stop();
        //Debug.Log("Boom Bap volume decreased - AudioDebug");

        if(AudioManager.instance != null)
        {
            AudioManager.instance.miniGame_Over.start();
        }

        yield return new WaitForSeconds(1.5f);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.boxingSuccessAfterFail.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.miniGameCompleteDialogue.start();
        }

        boomBapEmitter.SetParameter("Boom Bap Volume", 1f, false);
        //Debug.Log("Boom Bap volume increased - AudioDebug");
    }

    public IEnumerator BoxingSuccessAfterFailIEnum()
    {
        AmbientEncouragementEmitter.SetParameter("Ambient Boxing Encouragement Volume", 0f, false);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.boxingEncouragementOnLastLife.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.boxingSuccessAfterFail.start();
        }

        yield return new WaitForSeconds(1.9f);

        AmbientEncouragementEmitter.SetParameter("Ambient Boxing Encouragement Volume", 1f, false);
    }
    public IEnumerator BoxingEncouragementOnLastLifeIEnum()
    {
        AmbientEncouragementEmitter.SetParameter("Ambient Boxing Encouragement Volume", 0f, false);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.boxingEncouragementOnLastLife.start();
        }

        yield return new WaitForSeconds(1.2f);

        AmbientEncouragementEmitter.SetParameter("Ambient Boxing Encouragement Volume", 1f, false);
    }
}
