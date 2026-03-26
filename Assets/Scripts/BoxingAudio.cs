using DG.Tweening;
using FMODUnity;
using System.Collections;
using UnityEngine;

public class BoxingAudio : MonoBehaviour
{
    [SerializeField] StudioEventEmitter boomBapEmitter;

    public IEnumerator TemporarilyDecreaseBoomBapVolume()
    {
        boomBapEmitter.SetParameter("Boom Bap Volume", 0f, false);
        Debug.Log("Boom Bap volume decreased - AudioDebug");


        yield return new WaitForSeconds(1.5f);

        boomBapEmitter.SetParameter("Boom Bap Volume", 1f, false);
        Debug.Log("Boom Bap volume increased - AudioDebug");
    }
}
