using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using System.Collections;
using UnityEngine;

public class BoxingAudio : MonoBehaviour
{
    [SerializeField] StudioEventEmitter boomBapEmitter;
    [SerializeField] StudioEventEmitter ambientEncouragementEmitter;
    [SerializeField] StudioEventEmitter gymAmbienceEmitter;

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
        ambientEncouragementEmitter.Stop();
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

        boomBapEmitter.Stop();
        gymAmbienceEmitter.Stop();
    }

    public IEnumerator BoxingSuccessAfterFailIEnum()
    {
        ambientEncouragementEmitter.SetParameter("Ambient Boxing Encouragement Volume", 0f, false);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.boxingEncouragementOnLastLife.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.boxingSuccessAfterFail.start();
        }

        yield return new WaitForSeconds(1.9f);

        ambientEncouragementEmitter.SetParameter("Ambient Boxing Encouragement Volume", 1f, false);
    }
    public IEnumerator BoxingEncouragementOnLastLifeIEnum()
    {
        ambientEncouragementEmitter.SetParameter("Ambient Boxing Encouragement Volume", 0f, false);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.boxingEncouragementOnLastLife.start();
        }

        yield return new WaitForSeconds(1.2f);

        ambientEncouragementEmitter.SetParameter("Ambient Boxing Encouragement Volume", 1f, false);
    }
}
