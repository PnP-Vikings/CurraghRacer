using FMOD.Studio;
using MiniGames;
using System.Collections;
using UnityEngine;

public class RadioManager : MonoBehaviour
{
    public static RadioManager instance;
    public PLAYBACK_STATE newsReportOrAdPlaybackState;
    Coroutine RadioReportOrAdCoroutine;
    private bool newsReportHasJustPlayed = false;

    void Awake()
    {
        // Singleton pattern to ensure only one instance of RadioManager exists
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

    void Start()
    {
        if (AudioManager.instance != null)                                  // Checks for AudioManager
        {
            AudioManager.instance.radioSong.start();                        // Starts radio song
        }

        RadioReportOrAdCoroutine = StartCoroutine(PlayRadioReportOrAd());   // Starts radio coroutine
    }

    IEnumerator PlayRadioReportOrAd()                                       // Radio coroutine
    {
        float randomNumber = Random.Range(2f, 5f);                          // Assigns a random number between given numbers to float randomNumber
        //Debug.Log(randomNumber);

        yield return new WaitForSeconds(randomNumber);                      // Waits randomNumber seconds

        if (AudioManager.instance != null)                                  
        {
            AudioManager.instance.radioSong.setParameterByName("Radio Song fadeout", 1f);   // Extendes radio song fade out
        }

        yield return null;                                                  // Waits 1 frame

        if (AudioManager.instance != null)                                  
        {
            AudioManager.instance.radioSong.stop(STOP_MODE.ALLOWFADEOUT);   // stops radio song
        }

        yield return new WaitForSeconds(1.5f);                              // Waits 1.5 seconds

        if (AudioManager.instance != null)
        {
            AudioManager.instance.newsReportOrAd.start();                   // News starts
            AudioManager.instance.radioSong.setParameterByName("Radio Song fadeout", 0f); // radio song fade out is reset
        }

        newsReportHasJustPlayed = true;                                     // newsReportHasJustPlayed boolean is set to true
    }

    private void Update()
    {
        if (AudioManager.instance != null)                                                            // Checks every frame if AudioManager is running    // ("news" refers to news report or ad)
        {
            AudioManager.instance.newsReportOrAd.getPlaybackState(out newsReportOrAdPlaybackState);   // Gets the playback state of the news and assigns it to field

            if (newsReportOrAdPlaybackState == PLAYBACK_STATE.STOPPING)                               // if the playback state is "stopping" the radio starts
            {
                AudioManager.instance.radioSong.start();
            }
        }

        if (newsReportHasJustPlayed && newsReportOrAdPlaybackState == PLAYBACK_STATE.STOPPED)    // if the news has just played AND the playback state is "stopped" the coroutine is called
        {
            StartCoroutine(PlayRadioReportOrAd());
            newsReportHasJustPlayed = false;                                                     // the news has just played bool is reset
        }

        if (RaceManager.Instance != null & MiniGameManager.Instance != null & GameManager.Instance != null)
        {
            bool gameActiveBool = MiniGameManager.Instance.ReturnGameActiveBool();

            if (RaceManager.Instance.loadedRaceScene | gameActiveBool | GameManager.Instance.SleepAudioChangesCoroutineIsActive)
            {
                MuteRadio();                                                                     // Mutes Radio if the race scene is loaded OR a minigame is active OR the sleep audio coroutine is running
            }
            else
            {
                UnMuteRadio();
            }
        }
    }

    public void MuteRadio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.radioSong.setParameterByName("Radio Song Volume", 0f);
            AudioManager.instance.newsReportOrAd.setParameterByName("News Report Or Ad Volume", 0f);
            //Debug.Log("Radio is muted");
        }
    }

    public void UnMuteRadio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.radioSong.setParameterByName("Radio Song Volume", 1f);
            AudioManager.instance.newsReportOrAd.setParameterByName("News Report Or Ad Volume", 1f);
            //Debug.Log("Radio is unmuted");
        }
    }
}
