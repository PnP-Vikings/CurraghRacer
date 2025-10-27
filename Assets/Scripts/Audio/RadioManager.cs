using FMOD.Studio;
using System.Collections;
using UnityEditor.Timeline;
using UnityEngine;

public class RadioManager : MonoBehaviour
{
    private FMOD.Studio.EventInstance radioSong;
    private FMOD.Studio.EventInstance newsReportOrAd;
    private PLAYBACK_STATE newsReportOrAdPlaybackState;
    private int newsReportOrAdCounter = 0;
    Coroutine RadioReportOrAdCoroutine;

    void Start()
    {
        radioSong = FMODUnity.RuntimeManager.CreateInstance("event:/Garage/Radio Song");
        radioSong.start();

        newsReportOrAd = FMODUnity.RuntimeManager.CreateInstance("event:/Garage/News Report or Ad");

        RadioReportOrAdCoroutine = StartCoroutine(PlayRadioReportOrAd());
    }

    IEnumerator PlayRadioReportOrAd()
    {
        float randomNumber = Random.Range(3f, 10f);
        Debug.Log(randomNumber);
        yield return new WaitForSeconds(randomNumber);
        radioSong.setParameterByName("Radio Song fadeout", 1f);

        yield return null;
        radioSong.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

        yield return new WaitForSeconds(1.5f);
        newsReportOrAd.start();
        radioSong.setParameterByName("Radio Song fadeout", 0f);
        newsReportOrAdCounter++;
        Debug.Log(newsReportOrAdCounter.ToString());
    }

    private void Update()
    {
        newsReportOrAd.getPlaybackState(out newsReportOrAdPlaybackState);

        if (newsReportOrAdPlaybackState == PLAYBACK_STATE.STOPPING)
        {
            radioSong.start();
        }

        if (newsReportOrAdCounter > 0 && newsReportOrAdPlaybackState == PLAYBACK_STATE.STOPPED)
        {
            RadioReportOrAdCoroutine = StartCoroutine(PlayRadioReportOrAd());
            newsReportOrAdCounter--;
            Debug.Log(newsReportOrAdCounter.ToString());
        }
    }
}
