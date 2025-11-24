using FMOD.Studio;
using MiniGames;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RadioManager : MonoBehaviour
{
    public static RadioManager instance;
    private PLAYBACK_STATE radioAdOrNews1PlaybackState;
    private PLAYBACK_STATE radioAdOrNews2PlaybackState;
    private PLAYBACK_STATE radioAdOrNews3PlaybackState;
    private PLAYBACK_STATE radioAdOrNews4PlaybackState;
    private PLAYBACK_STATE radioAdOrNews5PlaybackState;
    //private PLAYBACK_STATE StoryUpdate1PlaybackState;
    private PLAYBACK_STATE StoryUpdate2PlaybackState;
    private bool radioAdOrNewsHasJustPlayed = false;
    private Scene activeScene;

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

    IEnumerator RadioCoroutine()                                            
    {
        PlayRadioSong();                                                    // A random song plays

        float randomNumber = Random.Range(4f, 9f);                          // A random number between the given numbers is assigned to the float 'randomNumber'

        yield return new WaitForSeconds(randomNumber);                      // Waits randomNumber seconds

        StopAllRadioSongs();                                                // All radio songs stop

        yield return new WaitForSeconds(1.5f);                              // Waits 1.5 seconds

        activeScene = SceneManager.GetActiveScene();                        // Get the active scene (this is used to ensure that certain audio only plays when the garage scene is load as the radio is constantly active in the background

        if (RaceManager.Instance != null)
        {
            if (RaceManager.Instance.hasJustWonRace == true)                // Checks if the player has just won a race
            {
                if(activeScene.name == "Garage")                            // Checks if the active scene is the Garage scene
                {
                    OverriedRadioWithStoryUpdates();                        // If both of the above checks return true the function to override the news with the a report specific to what just happened is called
                }
            }
            else
            {
                PlayRadioAdOrNews();                                        // If either of 2 checks above return false a regular news report will play
            }
        }                                                                    
        radioAdOrNewsHasJustPlayed = true;                                  // radioAdOrNewsHasJustPlayed boolean is set to true
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

            if (radioAdOrNews1PlaybackState == PLAYBACK_STATE.STOPPING | radioAdOrNews2PlaybackState == PLAYBACK_STATE.STOPPING | radioAdOrNews3PlaybackState == PLAYBACK_STATE.STOPPING | radioAdOrNews4PlaybackState == PLAYBACK_STATE.STOPPING | radioAdOrNews5PlaybackState == PLAYBACK_STATE.STOPPING)
            {
                PlayRadioSong();                                                                      // if any of the playback states are "stopping" a random song starts
            }

            if (radioAdOrNewsHasJustPlayed)                                                           // checks if the news has just played (boolean is true)
            {
                if (radioAdOrNews1PlaybackState == PLAYBACK_STATE.STOPPED & radioAdOrNews2PlaybackState == PLAYBACK_STATE.STOPPED & radioAdOrNews3PlaybackState == PLAYBACK_STATE.STOPPED & radioAdOrNews4PlaybackState == PLAYBACK_STATE.STOPPED & radioAdOrNews5PlaybackState == PLAYBACK_STATE.STOPPED)
                {
                    StartCoroutine(RadioCoroutine());                                                 // AND if any of the playback states are "stopped" the coroutine is called
                    radioAdOrNewsHasJustPlayed = false;                                               // the news has just played bool is reset
                }
            }
        }

        if (RaceManager.Instance != null & MiniGameManager.Instance != null & GameManager.Instance != null & AudioManager.instance != null)
        {
            bool gameActiveBool = MiniGameManager.Instance.ReturnGameActiveBool();                                // Gets the status of the minigames (if one is active)
            AudioManager.instance.StoryUpdate2.getPlaybackState(out StoryUpdate2PlaybackState); // Gets the playback state of the game update report

            if (RaceManager.Instance.loadedRaceScene | gameActiveBool | GameManager.Instance.SleepAudioChangesCoroutineIsActive | StoryUpdate2PlaybackState == PLAYBACK_STATE.PLAYING)
            {
                StopAllRadioSongs();
                StopAllAdOrNews();                // Mutes Radio if the race scene is loaded OR a minigame is active OR the sleep audio coroutine is running Or a game update is playing
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
            AudioManager.instance.radioSong5.setParameterByName("Radio Song 5 Volume", 0f);
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
            AudioManager.instance.radioSong5.setParameterByName("Radio Song 5 Volume", 1f);
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
            }
            else if (randomNumber == 5)
            {
                AudioManager.instance.radioAdOrNews5.start();
            }
        }
    }
    public void StopAllAdOrNews()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.radioAdOrNews1.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.radioAdOrNews2.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.radioAdOrNews3.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.radioAdOrNews4.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.radioAdOrNews5.stop(STOP_MODE.ALLOWFADEOUT);
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

    private void OverriedRadioWithStoryUpdates()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.StoryUpdate2.start();
            RaceManager.Instance.hasJustWonRace = false;
        }
    }
}
