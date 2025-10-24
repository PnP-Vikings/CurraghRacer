using FMOD.Studio;
using System.Collections;
using UnityEditor.Timeline;
using UnityEngine;

public class RadioManager : MonoBehaviour
{
    private FMOD.Studio.EventInstance radioSong;
    private FMOD.Studio.EventInstance newsReport;
    private PLAYBACK_STATE newsReportPlaybackState;
    Coroutine RadioReportCoroutine;

    void Start()
    {
        radioSong = FMODUnity.RuntimeManager.CreateInstance("event:/Garage/Radio");
        radioSong.start();

        newsReport = FMODUnity.RuntimeManager.CreateInstance("event:/Garage/News Report");

        RadioReportCoroutine = StartCoroutine(PlayRadioReport());
    }

    IEnumerator PlayRadioReport()
    {
        yield return new WaitForSeconds(3f);
        radioSong.setParameterByName("Radio Song fadeout", 1f);
        yield return null;
        radioSong.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        yield return new WaitForSeconds(1.5f);
        newsReport.start();
    }

    private void Update()
    {
        newsReport.getPlaybackState(out newsReportPlaybackState);

        if (newsReportPlaybackState == PLAYBACK_STATE.STOPPING)
        {
            radioSong.start();
        }
    }
}
