using FMODUnity;
using FMOD.Studio;
using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    private Bus masterBus;
        // UI
    public EventInstance UIClick1;
    public EventInstance UIClick2;
        // Main Menu
    public EventInstance deleteSave;
        // Race
    public EventInstance raceAmbience;
    public EventInstance raceWin;
    public EventInstance raceLose;
    // Garage

    public EventInstance storyUpdate1;
    public EventInstance storyUpdate2;
    public EventInstance storyUpdate3;

    public EventInstance radioSong1;
    public EventInstance radioSong2;
    public EventInstance radioSong3;
    public EventInstance radioSong4;
    public EventInstance radioSong5;

    public EventInstance radioAdOrNews1;
    public EventInstance radioAdOrNews2;
    public EventInstance radioAdOrNews3;
    public EventInstance radioAdOrNews4;
    public EventInstance radioAdOrNews5;

    public EventInstance loadingScreenSong;
    public EventInstance tvButtonPushOut;
    public EventInstance tvButtonPushIn;
    public EventInstance showInviteAudio;
        // Bulletin Board
    public EventInstance sleepAudio;
    public EventInstance sleepOutsideAudio;
    public EventInstance payBill;
    public EventInstance rooster;
        // Training
    public EventInstance gymBagZipUp;
    public EventInstance dumbbell;
    //public EventInstance rowingGameAmbience;
    public EventInstance rowingGameSuccess;
    public EventInstance rowingGameFail;
        // Jobs
    //public EventInstance kitchenAmbience;
    public EventInstance movePlateAudio;
    public EventInstance spongeAudio;

    public EventInstance pouringPint;

    public EventInstance punchBagAudio;

    //public EventInstance footingTurfAmbience;
    public EventInstance turfStackComplete;
    public EventInstance placeTurf;

    public EventInstance dumbbellSlide;
    public EventInstance grunt;
    public EventInstance barGrip;

    public EventInstance running;
    public EventInstance jump;
    public EventInstance slide;
    public EventInstance crashIntoFence;

    public EventInstance rowing;


    void Awake()
    {
        // Singleton pattern to ensure only one instance of AudioManager exists
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize the master bus immediately
            masterBus = RuntimeManager.GetBus("bus:/");
            
            // Load saved volume or use default
            float savedVolume = SettingsSaveSystem.LoadMasterVolume();
            masterBus.setVolume(savedVolume);
            
            Debug.Log($"AudioManager initialized with volume: {savedVolume}");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UIClick1 = RuntimeManager.CreateInstance("event:/UI/Click 1");
        UIClick2 = RuntimeManager.CreateInstance("event:/UI/Click 2");

        deleteSave = RuntimeManager.CreateInstance("event:/UI/Delete Save");

        raceAmbience = RuntimeManager.CreateInstance("event:/Race/Race Ambience");
        raceWin = RuntimeManager.CreateInstance("event:/Race/Race Win");
        raceLose = RuntimeManager.CreateInstance("event:/Race/Race Lose");

        storyUpdate1 = RuntimeManager.CreateInstance("event:/Radio/Story Update 1");   // Declan Kelly has returned
        storyUpdate2 = RuntimeManager.CreateInstance("event:/Radio/Story Update 2");   // Player won a race
        storyUpdate3 = RuntimeManager.CreateInstance("event:/Radio/Story Update 3");   // Player lost a race

        radioSong1 = RuntimeManager.CreateInstance("event:/Radio/Radio Song 1");
        radioSong2 = RuntimeManager.CreateInstance("event:/Radio/Radio Song 2");
        radioSong3 = RuntimeManager.CreateInstance("event:/Radio/Radio Song 3");
        radioSong4 = RuntimeManager.CreateInstance("event:/Radio/Radio Song 4");
        radioSong5 = RuntimeManager.CreateInstance("event:/Radio/Radio Song 5");

        radioAdOrNews1 = RuntimeManager.CreateInstance("event:/Radio/Radio Ad Or News 1");
        radioAdOrNews2 = RuntimeManager.CreateInstance("event:/Radio/Radio Ad Or News 2");
        radioAdOrNews3 = RuntimeManager.CreateInstance("event:/Radio/Radio Ad Or News 3");
        radioAdOrNews4 = RuntimeManager.CreateInstance("event:/Radio/Radio Ad Or News 4");
        radioAdOrNews5 = RuntimeManager.CreateInstance("event:/Radio/Radio Ad Or News 5");

        loadingScreenSong = RuntimeManager.CreateInstance("event:/Soundtrack/Loading Screen Song");
        tvButtonPushOut = RuntimeManager.CreateInstance("event:/UI/TV Button Push Out");
        tvButtonPushIn = RuntimeManager.CreateInstance("event:/UI/TV Button Push In");
        showInviteAudio = RuntimeManager.CreateInstance("event:/Garage/Show Invite");

        sleepAudio = RuntimeManager.CreateInstance("event:/Bulletin Board/Sleep");
        sleepOutsideAudio = RuntimeManager.CreateInstance("event:/Bulletin Board/Sleep Outside");
        rooster = RuntimeManager.CreateInstance("event:/Bulletin Board/Rooster");
        payBill = RuntimeManager.CreateInstance("event:/Bulletin Board/Pay Bill");

        gymBagZipUp = RuntimeManager.CreateInstance("event:/Training/Gym Bag Zip Up");
        dumbbell = RuntimeManager.CreateInstance("event:/Training/Dumbbell");
        //rowingGameAmbience = RuntimeManager.CreateInstance("event:/Rowing Rhythm Game/Rowing Game Ambience");
        rowingGameSuccess = RuntimeManager.CreateInstance("event:/Rowing Rhythm Game/Success");
        rowingGameFail = RuntimeManager.CreateInstance("event:/Rowing Rhythm Game/Fail");
        rowing = RuntimeManager.CreateInstance("event:/Rowing Rhythm Game/Rowing");

        //kitchenAmbience = RuntimeManager.CreateInstance("event:/Kitchen/Kitchen Ambience");
        movePlateAudio = RuntimeManager.CreateInstance("event:/Kitchen/Move Plate");
        spongeAudio = RuntimeManager.CreateInstance("event:/Kitchen/Sponge");                                // isn't being used because calling it via AudioManager.instance in the sponge script didn't work properly, idk why
        
        pouringPint = RuntimeManager.CreateInstance("event:/Bar/Pouring Pint");

        punchBagAudio = RuntimeManager.CreateInstance("event:/Training/Punch Bag");
        
        //footingTurfAmbience = RuntimeManager.CreateInstance("event:/Footing Turf/Footing Turf Ambience");
        turfStackComplete = RuntimeManager.CreateInstance("event:/Footing Turf/Turf Stack Complete");
        placeTurf = RuntimeManager.CreateInstance("event:/Footing Turf/Place Turf");

        dumbbellSlide = RuntimeManager.CreateInstance("event:/Weight Lifting/Dumbell Slide");
        barGrip = RuntimeManager.CreateInstance("event:/Weight Lifting/Bar Grip");
        grunt = RuntimeManager.CreateInstance("event:/Weight Lifting/Grunt");

        running = RuntimeManager.CreateInstance("event:/Foot Race/Running");
        jump = RuntimeManager.CreateInstance("event:/Foot Race/Jump");
        slide = RuntimeManager.CreateInstance("event:/Foot Race/Slide");
        crashIntoFence = RuntimeManager.CreateInstance("event:/Foot Race/Crash Into Fence");

    }

    public float GetMasterVolume()
    {
        masterBus.getVolume(out float volume);
        return volume; // volume: 0.0f (silent) to 1.0f (full volume)
    }

    public void SetMasterVolume(float volume)
    {
        // volume: 0.0f (silent) to 1.0f (full volume)
        masterBus.setVolume(volume);
        
        // Save the setting immediately
        SettingsSaveSystem.SaveMasterVolume(volume);
    }

    public void IncreaseMasterVolume(float amount)
    {
        masterBus.getVolume(out float currentVolume);
        float newVolume = Mathf.Clamp01(currentVolume + amount);
        SetMasterVolume(newVolume); // Use SetMasterVolume to trigger save
    }

    public void DecreaseMasterVolume(float amount)
    {
        masterBus.getVolume(out float currentVolume);
        float newVolume = Mathf.Clamp01(currentVolume - amount);
        SetMasterVolume(newVolume); // Use SetMasterVolume to trigger save
    }
}