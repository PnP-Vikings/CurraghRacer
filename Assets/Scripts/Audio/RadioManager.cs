using FMOD.Studio;
using League;
using MiniGames;
using System.Collections;
//using Unity.VisualScripting;
using UnityEngine;
//using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class RadioManager : MonoBehaviour
{
    public static RadioManager instance;
    private PLAYBACK_STATE angelusPlaybackState;
    private PLAYBACK_STATE radioAdOrNews1PlaybackState;
    private PLAYBACK_STATE radioAdOrNews2PlaybackState;
    private PLAYBACK_STATE radioAdOrNews3PlaybackState;
    private PLAYBACK_STATE radioAdOrNews4PlaybackState;
    private PLAYBACK_STATE radioAdOrNews5PlaybackState;
    private PLAYBACK_STATE storyUpdateIntroPlaybackState;
    private PLAYBACK_STATE storyUpdateFirstRaceWonPlaybackState;
    private PLAYBACK_STATE storyUpdateFirstRaceLostPlaybackState;
    private PLAYBACK_STATE storyUpdateSecondRaceWonPlaybackState;  // When adding a story update you need a playbackstate & get it in update
    private Scene activeScene;
    private bool radioAdOrNewsHasJustPlayed = false;
    private bool storyUpdateIntroHasPlayed = false;
    public bool hasJustLostRace = false;
    private int previousRandomNumberAd = 0;
    private int racesWon = 0;
    //private int previousRandomNumberSong = 0;

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
        // Comment out the line below to turn off the radio
        StartCoroutine(RadioCoroutine());                                   // Radio coroutine Starts
    }

    IEnumerator RadioCoroutine()                                            
    {
        PlayRadioSong();                                                    // A random song plays

        //float randomNumber = Random.Range(5f, 6f);                          // A random number between the given numbers is assigned to the float 'randomNumber' (For Testing)
        float randomNumber = Random.Range(85f, 190f);                     // A random number between the given numbers is assigned to the float 'randomNumber' (For actual Build)
        //Debug.Log("Song playing for " +  randomNumber + " Seconds - RadioDebug");

        yield return new WaitForSeconds(randomNumber);                      // Waits randomNumber seconds

        StopAllRadioSongs();                                                // All radio songs stop

        yield return new WaitForSeconds(1.5f);                              // Waits 1.5 seconds

        PlayAdOrNewsOrOverrideWithStoryUpdate();                            // A random ad or news plays unless overridden by a story update
                                                                 
        radioAdOrNewsHasJustPlayed = true;                                  // radioAdOrNewsHasJustPlayed boolean is set to true
    }

    private void Update()
    {
        if (AudioManager.instance != null)                                                            // Checks every frame if AudioManager is running    // ("news" refers to Radio Ad Or News)
        {
            AudioManager.instance.storyUpdateIntro.getPlaybackState(out storyUpdateIntroPlaybackState);       // Gets the playback state of story updates and assigns it to the variable
            AudioManager.instance.storyUpdateFirstRaceWon.getPlaybackState(out storyUpdateFirstRaceWonPlaybackState);
            AudioManager.instance.storyUpdateFirstRaceLost.getPlaybackState(out storyUpdateFirstRaceLostPlaybackState);
            AudioManager.instance.storyUpdateSecondRaceWon.getPlaybackState(out storyUpdateSecondRaceWonPlaybackState);

            AudioManager.instance.angelus.getPlaybackState(out angelusPlaybackState);   // Gets the playback state of the news and assigns it to the variable

            AudioManager.instance.radioAdOrNews1.getPlaybackState(out radioAdOrNews1PlaybackState);   // Gets the playback state of the news and assigns it to the variable
            AudioManager.instance.radioAdOrNews2.getPlaybackState(out radioAdOrNews2PlaybackState);
            AudioManager.instance.radioAdOrNews3.getPlaybackState(out radioAdOrNews3PlaybackState);
            AudioManager.instance.radioAdOrNews4.getPlaybackState(out radioAdOrNews4PlaybackState);
            AudioManager.instance.radioAdOrNews5.getPlaybackState(out radioAdOrNews5PlaybackState);

            if (angelusPlaybackState == PLAYBACK_STATE.STOPPING | radioAdOrNews1PlaybackState == PLAYBACK_STATE.STOPPING | radioAdOrNews2PlaybackState == PLAYBACK_STATE.STOPPING | radioAdOrNews3PlaybackState == PLAYBACK_STATE.STOPPING | radioAdOrNews4PlaybackState == PLAYBACK_STATE.STOPPING | radioAdOrNews5PlaybackState == PLAYBACK_STATE.STOPPING | storyUpdateIntroPlaybackState == PLAYBACK_STATE.STOPPING | storyUpdateFirstRaceWonPlaybackState == PLAYBACK_STATE.STOPPING | storyUpdateFirstRaceLostPlaybackState == PLAYBACK_STATE.STOPPING | storyUpdateSecondRaceWonPlaybackState == PLAYBACK_STATE.STOPPING)
            {
                PlayRadioSong();                                                                      // if any of the playback states are "stopping" a random song starts
            }

            if (radioAdOrNewsHasJustPlayed)                                                           // checks if the news has just played (if the boolean is true)
            {
                if (angelusPlaybackState == PLAYBACK_STATE.STOPPED & radioAdOrNews1PlaybackState == PLAYBACK_STATE.STOPPED & radioAdOrNews2PlaybackState == PLAYBACK_STATE.STOPPED & radioAdOrNews3PlaybackState == PLAYBACK_STATE.STOPPED & radioAdOrNews4PlaybackState == PLAYBACK_STATE.STOPPED & radioAdOrNews5PlaybackState == PLAYBACK_STATE.STOPPED & storyUpdateIntroPlaybackState == PLAYBACK_STATE.STOPPED & storyUpdateFirstRaceWonPlaybackState == PLAYBACK_STATE.STOPPED & storyUpdateFirstRaceLostPlaybackState == PLAYBACK_STATE.STOPPED & storyUpdateSecondRaceWonPlaybackState == PLAYBACK_STATE.STOPPED)
                {
                    StartCoroutine(RadioCoroutine());                                                 // AND if any of the playback states are "stopped" the coroutine is called
                    radioAdOrNewsHasJustPlayed = false;                                               // the news has just played boolean is reset
                }
            }

            if (angelusPlaybackState == PLAYBACK_STATE.PLAYING | radioAdOrNews1PlaybackState == PLAYBACK_STATE.PLAYING | radioAdOrNews2PlaybackState == PLAYBACK_STATE.PLAYING | radioAdOrNews3PlaybackState == PLAYBACK_STATE.PLAYING | radioAdOrNews4PlaybackState == PLAYBACK_STATE.PLAYING | radioAdOrNews5PlaybackState == PLAYBACK_STATE.PLAYING | storyUpdateIntroPlaybackState == PLAYBACK_STATE.PLAYING | storyUpdateFirstRaceWonPlaybackState == PLAYBACK_STATE.PLAYING | storyUpdateFirstRaceLostPlaybackState == PLAYBACK_STATE.PLAYING | storyUpdateSecondRaceWonPlaybackState == PLAYBACK_STATE.PLAYING)
            {
                StopAllRadioSongs();                                                                  // Prevents Songs from starting if ads are playing
            }
        }

        if (RaceManager.Instance != null & MiniGameManager.Instance != null & GameManager.Instance != null & AudioManager.instance != null)
        {
            bool gameActiveBool = MiniGameManager.Instance.ReturnGameActiveBool();                                // Gets the status of the minigames (if one is active)              

            if (RaceManager.Instance.loadedRaceScene | gameActiveBool | GameManager.Instance.SleepAudioChangesCoroutineIsActive)
            {
                StopAllRadioSongs();
                StopAllAdOrNews();                // Mutes Radio and news if the race scene is loaded OR a minigame is active OR the sleep audio coroutine is running
            }
            else
            {
                UnMuteRadio();
            }
        }

        activeScene = SceneManager.GetActiveScene();
        if (GameManager.Instance != null)
        {
            if (activeScene.name != "Garage" & GameManager.Instance.SleepAudioChangesCoroutineIsActive)
            {
                //Debug.Log("Sleep Audio is muted - AudioDebug");
                MuteSleepAudio();
            }
        }

        if (activeScene.name != "Garage")
        {
            //Debug.Log("Radio is muted because Garage scene is not loaded - AudioDebug");
            MuteRadio();
        }

    }
    private void PlayAdOrNewsOrOverrideWithStoryUpdate()
    {
        if (RaceManager.Instance != null)
        {
            if (storyUpdateIntroHasPlayed == false | RaceManager.Instance.hasJustWonRace == true | hasJustLostRace) // checks if story update 1 has played or if the player has just won a race and calls the override method if true
            {
                OverriedRadioWithStoryUpdates();
            }
            else
            {
                PlayRadioAdOrNews();                                        // a regular news is played if false
            }
        }
    }

    private void OverriedRadioWithStoryUpdates()
    {
        activeScene = SceneManager.GetActiveScene();

        if (AudioManager.instance != null)
        {
            if (activeScene.name == "Garage")                            // Checks if the active scene is the Garage scene
            {
                // Story update 1 (Declan Kelly Returns)
                if (!storyUpdateIntroHasPlayed && LeagueController.Instance.currentLeague.playerHasJoined == true)                              // checks if story update 1 has played and if it hasn't it plays it sets the boolean to true 
                {
                    //Debug.Log("Player has accepted the league invite - AudioDebug");
                    AudioManager.instance.storyUpdateIntro.start();
                    storyUpdateIntroHasPlayed = true;
                }
                else
                {
                    PlayRadioAdOrNews();
                }

                // Story update 2 (Player wins race)
                if (RaceManager.Instance != null)
                {
                    if (RaceManager.Instance.hasJustWonRace == true)     // checks if the player has just won a race and if they have it plays story update 2 and sets the boolean to false
                    {
                        Debug.Log("Place has won a race, overriding ads with story update - AudioDebug");
                        StopAllAdOrNewsExceptStoryUpdates();

                        if (racesWon == 0)
                        {
                            AudioManager.instance.storyUpdateFirstRaceWon.start();
                            RaceManager.Instance.hasJustWonRace = false;
                            racesWon++;
                        }
                        else if (racesWon == 1)
                        {
                            AudioManager.instance.storyUpdateSecondRaceWon.start();
                            RaceManager.Instance.hasJustWonRace = false;
                            racesWon++;
                        }   
                    }
                }

                // Story update 3 (Player lost race)
                if (hasJustLostRace)
                {
                    AudioManager.instance.storyUpdateFirstRaceLost.start();
                    hasJustLostRace = false;
                }
            }
        }
    }

    private void PlayRadioSong()
    {
        int newRandomNumberSong = Random.Range(1, 6);
        //Debug.Log("newRandomNumberSong is " + newRandomNumberSong);

        //while (newRandomNumberSong == previousRandomNumberSong)
        //{
        //    Debug.Log("a newRandomNumberSong was chosen as it was equal to previousRandomNumberSong");
        //    newRandomNumberSong = Random.Range(1, 3);
        //    Debug.Log("newRandomNumberSong is " + newRandomNumberSong);
        //}

        if (AudioManager.instance != null)
        {
            if (newRandomNumberSong == 1)
            {
                StopAllRadioSongs();
                AudioManager.instance.radioSong1.start();
            }
            else if (newRandomNumberSong == 2)
            {
                StopAllRadioSongs();
                AudioManager.instance.radioSong2.start();
            }
            else if (newRandomNumberSong == 3)
            {
                StopAllRadioSongs();
                AudioManager.instance.radioSong3.start();
            }
            else if (newRandomNumberSong == 4)
            {
                StopAllRadioSongs();
                AudioManager.instance.radioSong4.start();
            }
            else if (newRandomNumberSong == 5)
            {
                StopAllRadioSongs();
                AudioManager.instance.radioSong5.start();
            }
        }
        //previousRandomNumberSong = newRandomNumberSong;

        //Debug.Log("Song " + newRandomNumberSong + " Playing - RadioDebug");
    }

    private void PlayRadioAdOrNews()
    {
        int newRandomNumberAd = Random.Range(1, 7);
        //Debug.Log("newRandomNumberAd is " + newRandomNumberAd);

        while (newRandomNumberAd == previousRandomNumberAd)
        {
            //Debug.Log("a newRandomNumberAd was chosen as it was equal to previousRandomNumberAd");
            newRandomNumberAd = Random.Range(1, 3);
            //Debug.Log("newRandomNumberAd is " + newRandomNumberAd);
        }

        if (AudioManager.instance != null)
        {
            if (newRandomNumberAd == 1)
            {
                AudioManager.instance.angelus.start();
            }
            else if (newRandomNumberAd == 2)
            {
                AudioManager.instance.radioAdOrNews1.start();
            }
            else if (newRandomNumberAd == 3)
            {
                AudioManager.instance.radioAdOrNews2.start();
            }
            else if (newRandomNumberAd == 4)
            {
                AudioManager.instance.radioAdOrNews3.start();
            }
            else if (newRandomNumberAd == 5)
            {
                AudioManager.instance.radioAdOrNews4.start();
            }
            else if (newRandomNumberAd == 6)
            {
                AudioManager.instance.radioAdOrNews5.start();
            }
        }
        previousRandomNumberAd = newRandomNumberAd;
    }
    public void StopAllAdOrNews()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.angelus.stop(STOP_MODE.ALLOWFADEOUT);

            AudioManager.instance.storyUpdateIntro.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.storyUpdateFirstRaceWon.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.storyUpdateFirstRaceLost.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.storyUpdateSecondRaceWon.stop(STOP_MODE.ALLOWFADEOUT);

            AudioManager.instance.radioAdOrNews1.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.radioAdOrNews2.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.radioAdOrNews3.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.radioAdOrNews4.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.radioAdOrNews5.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }
    public void StopAllAdOrNewsExceptStoryUpdates()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.angelus.stop(STOP_MODE.ALLOWFADEOUT);
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

    public void MuteRadio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.angelus.setParameterByName("Angelus Volume", 0f);
            AudioManager.instance.storyUpdateIntro.setParameterByName("Story Update - Intro - Volume", 0f);
            AudioManager.instance.storyUpdateFirstRaceWon.setParameterByName("Story Update - First Race Won - Volume", 0f);
            AudioManager.instance.storyUpdateFirstRaceLost.setParameterByName("Story Update - First Race Lost - Volume", 0f);
            AudioManager.instance.storyUpdateSecondRaceWon.setParameterByName("Story Update - Second Race Won - Volume", 0f);
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
            AudioManager.instance.angelus.setParameterByName("Angelus Volume", 1f);
            AudioManager.instance.storyUpdateIntro.setParameterByName("Story Update - Intro - Volume", 1f);
            AudioManager.instance.storyUpdateFirstRaceWon.setParameterByName("Story Update - First Race Won - Volume", 1f);
            AudioManager.instance.storyUpdateFirstRaceLost.setParameterByName("Story Update - First Race Lost - Volume", 1f);
            AudioManager.instance.storyUpdateSecondRaceWon.setParameterByName("Story Update - Second Race Won - Volume", 1f);
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

    public void MuteSleepAudio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.sleepAudio.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.sleepOutsideAudio.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.rooster.stop(STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.tvButtonPushIn.setParameterByName("TV Button Push In Volume", 0f);
        }
    }
}
