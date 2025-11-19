using FMOD.Studio;
using MiniGames;
using System.Collections;
using UnityEngine;

public class RadioManager : MonoBehaviour
{
    public static RadioManager instance;
    public PLAYBACK_STATE radioAdOrNews1PlaybackState;
    public PLAYBACK_STATE radioAdOrNews2PlaybackState;
    private bool radioAdOrNewsHasJustPlayed = false;

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
        StartCoroutine(RadioCoroutine());                                   // Radio coroutine Starts
    }

    IEnumerator RadioCoroutine()                                            // Radio coroutine
    {
        PlayRadioSong();                                                    // A random song plays

        float randomNumber = Random.Range(4f, 9f);                          // A random number between the given numbers is assigned to the float 'randomNumber'

        yield return new WaitForSeconds(randomNumber);                      // Waits randomNumber seconds

        StopAllRadioSongs();                                                 // All radio songs stop

        yield return new WaitForSeconds(1.5f);                               // Waits 1.5 seconds

        PlayRadioAdOrNews();                                                 // A random ad or news report is played

        radioAdOrNewsHasJustPlayed = true;                                   // radioAdOrNewsHasJustPlayed boolean is set to true
    }

    private void Update()
    {
        if (AudioManager.instance != null)                                                            // Checks every frame if AudioManager is running    // ("news" refers to Radio Ad Or News)
        {

            AudioManager.instance.radioAdOrNews1.getPlaybackState(out radioAdOrNews1PlaybackState);   // Gets the playback state of the news 1 and assigns it to the variable
            AudioManager.instance.radioAdOrNews2.getPlaybackState(out radioAdOrNews2PlaybackState);   // Gets the playback state of the news 2 and assigns it to the variable

            if (radioAdOrNews1PlaybackState == PLAYBACK_STATE.STOPPING | radioAdOrNews2PlaybackState == PLAYBACK_STATE.STOPPING)   // if either of the playback states are "stopping" a song starts
            {
                PlayRadioSong();
            }
        }

        if (radioAdOrNewsHasJustPlayed)                                                                // if the news has just played (boolean is true)
        {
            if (radioAdOrNews1PlaybackState == PLAYBACK_STATE.STOPPED & radioAdOrNews2PlaybackState == PLAYBACK_STATE.STOPPED) // AND if either of the playback states are "stopped" the coroutine is called
            {
                StartCoroutine(RadioCoroutine());                                          
                radioAdOrNewsHasJustPlayed = false;                                                    // the news has just played bool is reset
            }
        }

        if (RaceManager.Instance != null & MiniGameManager.Instance != null & GameManager.Instance != null)
        {
            bool gameActiveBool = MiniGameManager.Instance.ReturnGameActiveBool();

            if (RaceManager.Instance.loadedRaceScene | gameActiveBool | GameManager.Instance.SleepAudioChangesCoroutineIsActive)
            {
                MuteRadio();                                                                           // Mutes Radio if the race scene is loaded OR a minigame is active OR the sleep audio coroutine is running
            }
            else
            {
                UnMuteRadio();
            }
        }

        Debug.Log("Loaded Race Scene = " + RaceManager.Instance.loadedRaceScene);
    }

    public void MuteRadio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.radioSong1.setParameterByName("Radio Song 1 Volume", 0f);
            AudioManager.instance.radioSong2.setParameterByName("Radio Song 2 Volume", 0f);
            AudioManager.instance.radioAdOrNews1.setParameterByName("Radio Ad or News 1 Volume", 0f);
            AudioManager.instance.radioAdOrNews2.setParameterByName("Radio Ad or News 2 Volume", 0f);
        }
    }

    public void UnMuteRadio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.radioSong1.setParameterByName("Radio Song 1 Volume", 1f);
            AudioManager.instance.radioSong2.setParameterByName("Radio Song 2 Volume", 1f);
            AudioManager.instance.radioAdOrNews1.setParameterByName("Radio Ad or News 1 Volume", 1f);
            AudioManager.instance.radioAdOrNews2.setParameterByName("Radio Ad or News 2 Volume", 1f);
        }
    }

    private void PlayRadioSong()
    {
        int randomNumber = Random.Range(1, 3);

        if (AudioManager.instance != null)
        {
            if (randomNumber == 1)
            {
                AudioManager.instance.radioSong2.stop(STOP_MODE.IMMEDIATE);
                AudioManager.instance.radioSong1.start();
            }
            else if (randomNumber == 2)
            {
                AudioManager.instance.radioSong1.stop(STOP_MODE.IMMEDIATE);
                AudioManager.instance.radioSong2.start();
            }
        }
    }

    private void PlayRadioAdOrNews()
    {
        int randomNumber = Random.Range(1, 3);

        if (AudioManager.instance != null)
        {
            if (randomNumber == 1)
            {
                AudioManager.instance.radioAdOrNews1.start();
            }
            else if (randomNumber == 2)
            {
                AudioManager.instance.radioAdOrNews2.start();
            }
        }
    }
    public void StopAllRadioSongs()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.radioSong1.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.radioSong2.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }
}
