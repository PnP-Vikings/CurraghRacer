using DG.Tweening;
using FMODUnity;
using System.Collections;
using UnityEngine;

public class BoxingAudio : MonoBehaviour
{
    [SerializeField] StudioEventEmitter boomBapEmitter;

    public void DecreaseBoomBapVolume()
    {
        boomBapEmitter.SetParameter("Boom Bap Volume", 0f, false);
        Debug.Log("Boom Bap volume decreased");
    }

    //public IEnumerator TemporarilyDecreaseBoomBapVolume()
    //{
    //    boomBapEmitter.SetParameter("Boom Bap Volume", 0f, false);
    //    Debug.Log("Boom Bap volume decreased - AudioDebug");
                          

    //    yield return new WaitForSeconds(2f);

    //    boomBapEmitter.SetParameter("Boom Bap Volume", 1f, false);
    //    Debug.Log("Boom Bap volume increased - AudioDebug");
    //}
}
