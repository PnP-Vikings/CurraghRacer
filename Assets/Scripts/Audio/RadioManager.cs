using FMOD.Studio;
using MiniGames;
using NUnit.Framework.Constraints;
using System.Collections;
using Unity.VisualScripting;
using UnityEditor.Timeline;
using UnityEngine;

public class RadioManager : MonoBehaviour
{
    public static RadioManager instance;
    public PLAYBACK_STATE newsReportOrAdPlaybackState;
    private int newsReportOrAdCounter = 0;
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
        Debug.Log(randomNumber);

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
        Debug.Log(newsReportOrAdCounter.ToString());
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

            if (newsReportHasJustPlayed && newsReportOrAdPlaybackState == PLAYBACK_STATE.STOPPED)    // if the news has just played AND the playback state is "stopped" the coroutine is called
            {
                StartCoroutine(PlayRadioReportOrAd());
                newsReportHasJustPlayed = false;                                                     // the news has just played bool is reset
                Debug.Log(newsReportOrAdCounter.ToString());
            }

            if (RaceManager.Instance != null)                                                        // Checks if RaceManager is running
            {
                if (RaceManager.Instance.loadedRaceScene)                                            // if the race scene is loaded the radio song and news are muted
                {
                    AudioManager.instance.radioSong.stop(STOP_MODE.ALLOWFADEOUT);
                    AudioManager.instance.newsReportOrAd.stop(STOP_MODE.ALLOWFADEOUT);
                }
            }

            if (MiniGameManager.Instance != null)                                                    // Checks if MiniGameManager is running
            {
                bool gameActiveBool = MiniGameManager.Instance.ReturnGameActiveBool();

                if (gameActiveBool)                                                                  // if a mini game is loaded the radio song and news are muted
                {
                    AudioManager.instance.radioSong.stop(STOP_MODE.ALLOWFADEOUT);
                    AudioManager.instance.newsReportOrAd.stop(STOP_MODE.ALLOWFADEOUT);
                }
            }
        }
    }

    public void MuteRadioForSeconds()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.tvButtonPushOut.start();
            AudioManager.instance.radioSong.setParameterByName("Radio Song Volume", 0f);
            AudioManager.instance.newsReportOrAd.setParameterByName("News Report Or Ad Volume", 0f);

        }
    }
}
