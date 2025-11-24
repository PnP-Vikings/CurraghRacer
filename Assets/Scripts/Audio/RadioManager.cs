using FMOD.Studio;
using MiniGames;
using System.Collections;
using UnityEngine;

public class RadioManager : MonoBehaviour
{
    public static RadioManager instance;
    public PLAYBACK_STATE radioAdOrNews1PlaybackState;
    public PLAYBACK_STATE radioAdOrNews2PlaybackState;
    public PLAYBACK_STATE radioAdOrNews3PlaybackState;
    public PLAYBACK_STATE radioAdOrNews4PlaybackState;
    public PLAYBACK_STATE radioAdOrNews5PlaybackState;
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

            AudioManager.instance.radioAdOrNews1.getPlaybackState(out radioAdOrNews1PlaybackState);   // Gets the playback state of the news and assigns it to the variable
            AudioManager.instance.radioAdOrNews2.getPlaybackState(out radioAdOrNews2PlaybackState);   
            AudioManager.instance.radioAdOrNews3.getPlaybackState(out radioAdOrNews3PlaybackState);   
            AudioManager.instance.radioAdOrNews4.getPlaybackState(out radioAdOrNews4PlaybackState);   
            AudioManager.instance.radioAdOrNews5.getPlaybackState(out radioAdOrNews5PlaybackState);   

            if (radioAdOrNews1PlaybackState == PLAYBACK_STATE.STOPPING | radioAdOrNews2PlaybackState == PLAYBACK_STATE.STOPPING | radioAdOrNews3PlaybackState == PLAYBACK_STATE.STOPPING | radioAdOrNews4PlaybackState == PLAYBACK_STATE.STOPPING | radioAdOrNews5PlaybackState == PLAYBACK_STATE.STOPPING)   // if either of the playback states are "stopping" a song starts
            {
                PlayRadioSong();
            }
        }

        if (radioAdOrNewsHasJustPlayed)                                                                // if the news has just played (boolean is true)
        {
            if (radioAdOrNews1PlaybackState == PLAYBACK_STATE.STOPPED & radioAdOrNews2PlaybackState == PLAYBACK_STATE.STOPPED & radioAdOrNews3PlaybackState == PLAYBACK_STATE.STOPPED & radioAdOrNews4PlaybackState == PLAYBACK_STATE.STOPPED & radioAdOrNews5PlaybackState == PLAYBACK_STATE.STOPPED) // AND if either of the playback states are "stopped" the coroutine is called
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
    }

    public void MuteRadio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.radioSong1.setParameterByName("Radio Song 1 Volume", 0f);
            AudioManager.instance.radioSong2.setParameterByName("Radio Song 2 Volume", 0f);
            AudioManager.instance.radioSong3.setParameterByName("Radio Song 3 Volume", 0f);
            AudioManager.instance.radioSong4.setParameterByName("Radio Song 4 Volume", 0f);
            AudioManager.instance.radioAdOrNews1.setParameterByName("Radio Ad or News 1 Volume", 0f);
            AudioManager.instance.radioAdOrNews2.setParameterByName("Radio Ad or News 2 Volume", 0f);
            AudioManager.instance.radioAdOrNews3.setParameterByName("Radio Ad or News 3 Volume", 0f);
            AudioManager.instance.radioAdOrNews4.setParameterByName("Radio Ad or News 4 Volume", 0f);
            AudioManager.instance.radioAdOrNews5.setParameterByName("Radio Ad or News 5 Volume", 0f);
        }
    }

    public void UnMuteRadio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.radioSong1.setParameterByName("Radio Song 1 Volume", 1f);
            AudioManager.instance.radioSong2.setParameterByName("Radio Song 2 Volume", 1f);
            AudioManager.instance.radioSong3.setParameterByName("Radio Song 3 Volume", 1f);
            AudioManager.instance.radioSong4.setParameterByName("Radio Song 4 Volume", 1f);
            AudioManager.instance.radioAdOrNews1.setParameterByName("Radio Ad or News 1 Volume", 1f);
            AudioManager.instance.radioAdOrNews2.setParameterByName("Radio Ad or News 2 Volume", 1f);
            AudioManager.instance.radioAdOrNews3.setParameterByName("Radio Ad or News 3 Volume", 1f);
            AudioManager.instance.radioAdOrNews4.setParameterByName("Radio Ad or News 4 Volume", 1f);
            AudioManager.instance.radioAdOrNews5.setParameterByName("Radio Ad or News 5 Volume", 1f);
        }
    }

    private void PlayRadioSong()
    {
        int randomNumber = Random.Range(1, 6);

        if (AudioManager.instance != null)
        {
            if (randomNumber == 1)
            {
                StopAllRadioSongs();
                AudioManager.instance.radioSong1.start();
            }
            else if (randomNumber == 2)
            {
                StopAllRadioSongs();
                AudioManager.instance.radioSong2.start();
            }
            else if (randomNumber == 3)
            {
                StopAllRadioSongs();
                AudioManager.instance.radioSong3.start();
            }
            else if (randomNumber == 4)
            {
                StopAllRadioSongs();
                AudioManager.instance.radioSong4.start();
            }
            else if (randomNumber == 5)
            {
                StopAllRadioSongs();
                AudioManager.instance.radioSong5.start();
            }
        }
    }

    private void PlayRadioAdOrNews()
    {
        int randomNumber = Random.Range(1, 6);

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
            else if (randomNumber == 3)
            {
                AudioManager.instance.radioAdOrNews3.start();
            }
            else if (randomNumber == 4)
            {
                AudioManager.instance.radioAdOrNews4.start();
            } else if (randomNumber == 5)
            {
                AudioManager.instance.radioAdOrNews5.start();
            }
        }
    }
    public void StopAllRadioSongs()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.radioSong1.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.radioSong2.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.radioSong3.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.radioSong4.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.radioSong5.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }
}
