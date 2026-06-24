using FMODUnity;
using FMOD.Studio;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    private Bus masterBus;
        // UI
    public EventInstance UIClick1; // Confirm selection 
    public EventInstance UIClick2; // Return
    public EventInstance UIClick3; // Hover
        // Main Menu
    public EventInstance deleteSave;
        // Race
    public EventInstance raceAmbience;
    public EventInstance raceWon;
    public EventInstance raceLost;
    public EventInstance shout;
        // Garage

    public EventInstance storyUpdateIntro; // storyUpdate
    public EventInstance storyUpdateFirstRaceWon;   // FKA storyUpdate2
    public EventInstance storyUpdateFirstRaceLost;  // FKA storyUpdate3

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

    public EventInstance angelus;

    public EventInstance loadingScreenSong;
    public EventInstance tvButtonPushOut;
    public EventInstance tvButtonPushIn;
    public EventInstance showInviteAudio;
        // Bulletin Board
    public EventInstance sleepAudio;
    public EventInstance sleepOutsideAudio;
    public EventInstance rooster;
    public EventInstance payBill;
    public EventInstance payBillWhilePoorDialogue;
    public EventInstance card;
        // Training
    public EventInstance gymBagZipUp;
    public EventInstance dumbbell;

    public EventInstance rowing;
    public EventInstance rowingGameSuccess;
    public EventInstance rowingGameSuccessDialogue;
    public EventInstance rowingGameFail;
    public EventInstance rowingGameFailDialogue;
    public EventInstance rowingGameSuccessAfterFail;
    // Jobs

    public EventInstance spawnPlates;
    public EventInstance movePlate;
    public EventInstance dunkPlate;
    public EventInstance spongeAudio;

    public EventInstance barAmbience;
    public EventInstance pouringPint;
    public EventInstance acceptablePour;
    public EventInstance poorPour;
    public EventInstance setDownPint;

    public EventInstance punchBagAudio;

    public EventInstance miniGame_lifeLost;
    public EventInstance miniGameProgression;
    public EventInstance miniGame_Win;
    public EventInstance miniGame_Over;
    public EventInstance miniGameOverDialogue;

    public EventInstance placeTurf;

    public EventInstance weightSelectionResponse;
    public EventInstance dumbbellSlide;
    public EventInstance grunt;
    public EventInstance barGrip;
    public EventInstance barPlacedOnStand;
    public EventInstance weightliftingPhaseFailedDialogue;
    public EventInstance inTheGreenDialogue;
    public EventInstance cementBag;
    public EventInstance weightliftingRoundEndDialogue;
    public EventInstance aWarmUp;
    public EventInstance liftPhaseEncouragement;
    public EventInstance holdPhaseEncouragement;

    public EventInstance running;
    public EventInstance jump;
    public EventInstance footRaceEncouragement;
    public EventInstance slide;
    public EventInstance crashIntoFence;

    public EventInstance rockSelect;
    public EventInstance closeRockCase;
    public EventInstance rockSkip;
    public EventInstance rockThrow;
    public EventInstance rockBounceOnWood;
    public EventInstance rockSink;

    public EventInstance tractor;

    public EventInstance carCrash;
    public EventInstance rain;
    public EventInstance roadworks;

    public EventInstance strikeRemoved;
    public EventInstance scribble;

    public EventInstance PowerMeter;

    public EventInstance boxingEncouragement;
    public EventInstance boxingSuccessAfterFail;

    public EventInstance bodhran;

    private Scene activeScene;
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
                            // UI //
        UIClick1 = RuntimeManager.CreateInstance("event:/UI/Click 1");
        UIClick2 = RuntimeManager.CreateInstance("event:/UI/Click 2");
        UIClick3 = RuntimeManager.CreateInstance("event:/UI/Click 3");

                            // MAIN MENU //
        deleteSave = RuntimeManager.CreateInstance("event:/UI/Delete Save");

                            // RACE //
        raceAmbience = RuntimeManager.CreateInstance("event:/Race/Race Ambience");
        raceWon = RuntimeManager.CreateInstance("event:/Race/Race Won");
        raceLost = RuntimeManager.CreateInstance("event:/Race/Race Lost");
        shout = RuntimeManager.CreateInstance("event:/Race/Shout");

                            // RADIO //
        storyUpdateIntro = RuntimeManager.CreateInstance("event:/Radio/Story Update - Intro");   // Declan Kelly has returned
        storyUpdateFirstRaceWon = RuntimeManager.CreateInstance("event:/Radio/Story Update - First Race Won");      // Player won a race
        storyUpdateFirstRaceLost = RuntimeManager.CreateInstance("event:/Radio/Story Update  - First Race Lost");   // Player lost a race

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

        angelus = RuntimeManager.CreateInstance("event:/Radio/Angelus");

                            // OST //
        loadingScreenSong = RuntimeManager.CreateInstance("event:/Soundtrack/Loading Screen Song");
        bodhran = RuntimeManager.CreateInstance("event:/Soundtrack/Bodhrán");

                            // GARAGE //
        tvButtonPushOut = RuntimeManager.CreateInstance("event:/UI/TV Button Push Out");
        tvButtonPushIn = RuntimeManager.CreateInstance("event:/UI/TV Button Push In");
        showInviteAudio = RuntimeManager.CreateInstance("event:/Garage/Show Invite");

                           // BULLETIN BOARD //
        sleepAudio = RuntimeManager.CreateInstance("event:/Bulletin Board/Sleep");
        sleepOutsideAudio = RuntimeManager.CreateInstance("event:/Bulletin Board/Sleep Outside");
        rooster = RuntimeManager.CreateInstance("event:/Bulletin Board/Rooster");
        payBill = RuntimeManager.CreateInstance("event:/Bulletin Board/Pay Bill");
        payBillWhilePoorDialogue = RuntimeManager.CreateInstance("event:/Bulletin Board/Pay Bill While Poor Dialogue");
        card = RuntimeManager.CreateInstance("event:/Bulletin Board/Card");
        gymBagZipUp = RuntimeManager.CreateInstance("event:/Training/Gym Bag Zip Up");
        scribble = RuntimeManager.CreateInstance("event:/UI/Scribble On Bulletin Board");

                            // ROWING //
        rowing = RuntimeManager.CreateInstance("event:/Rowing Rhythm Game/Rowing");
        rowingGameSuccess = RuntimeManager.CreateInstance("event:/Rowing Rhythm Game/Success");
        rowingGameSuccessDialogue = RuntimeManager.CreateInstance("event:/Rowing Rhythm Game/Success Dialogue");
        rowingGameFail = RuntimeManager.CreateInstance("event:/Rowing Rhythm Game/Fail");
        rowingGameFailDialogue = RuntimeManager.CreateInstance("event:/Rowing Rhythm Game/Fail Dialogue");
        rowingGameSuccessAfterFail = RuntimeManager.CreateInstance("event:/Rowing Rhythm Game/Success after Fail");

                            // DISHWASHING //
        spawnPlates = RuntimeManager.CreateInstance("event:/Kitchen/Spawn Plates");
        movePlate = RuntimeManager.CreateInstance("event:/Kitchen/Move Plate");
        spongeAudio = RuntimeManager.CreateInstance("event:/Kitchen/Sponge");            // isn't being used because calling it via AudioManager.instance in the sponge script didn't work properly, idk why
        dunkPlate = RuntimeManager.CreateInstance("event:/Kitchen/Dunk Plate");

                            // BAR //
        barAmbience = RuntimeManager.CreateInstance("event:/Bar/Bar Ambience");
        pouringPint = RuntimeManager.CreateInstance("event:/Bar/Pouring Pint");
        acceptablePour = RuntimeManager.CreateInstance("event:/Bar/Acceptable Pour");
        poorPour = RuntimeManager.CreateInstance("event:/Bar/Poor Pour");
        setDownPint = RuntimeManager.CreateInstance("event:/Bar/Set Pint Down");

                            // BOXING //
        punchBagAudio = RuntimeManager.CreateInstance("event:/Training/Punch Bag");
        boxingEncouragement = RuntimeManager.CreateInstance("event:/Boxing/Boxing Encouragement");
        boxingSuccessAfterFail = RuntimeManager.CreateInstance("event:/Boxing/Boxing Success After Fail");

                            // MINIGAME NOTIFICATION //
        miniGame_lifeLost = RuntimeManager.CreateInstance("event:/Notifications/Minigame_Life Lost");
        miniGameProgression = RuntimeManager.CreateInstance("event:/Notifications/Minigame_Progression");
        miniGame_Win = RuntimeManager.CreateInstance("event:/Notifications/Minigame_Win");
        miniGame_Over = RuntimeManager.CreateInstance("event:/Notifications/Minigame_Over");
        miniGameOverDialogue = RuntimeManager.CreateInstance("event:/Notifications/Mini Game Over Dialogue");

                            // TURF //
        placeTurf = RuntimeManager.CreateInstance("event:/Footing Turf/Place Turf");

                            // WEIGHTLIFTING //
        weightSelectionResponse = RuntimeManager.CreateInstance("event:/Weight Lifting/Weight Selection Response");
        dumbbellSlide = RuntimeManager.CreateInstance("event:/Weight Lifting/Dumbell Slide");
        barGrip = RuntimeManager.CreateInstance("event:/Weight Lifting/Bar Grip");
        grunt = RuntimeManager.CreateInstance("event:/Weight Lifting/Grunt");
        barPlacedOnStand = RuntimeManager.CreateInstance("event:/Weight Lifting/Metal Bar");
        weightliftingPhaseFailedDialogue = RuntimeManager.CreateInstance("event:/Weight Lifting/Weightlifting Phase Failed");
        inTheGreenDialogue = RuntimeManager.CreateInstance("event:/Weight Lifting/In The Green Dialogue");
        cementBag = RuntimeManager.CreateInstance("event:/Weight Lifting/Cement Bag");
        PowerMeter = RuntimeManager.CreateInstance("event:/Weight Lifting/Power Meter Moving");
        aWarmUp = RuntimeManager.CreateInstance("event:/Weight Lifting/A warm-up");
        weightliftingRoundEndDialogue = RuntimeManager.CreateInstance("event:/Weight Lifting/Weightlifting Round End");
        liftPhaseEncouragement = RuntimeManager.CreateInstance("event:/Weight Lifting/Lifting phase encouragement");
        holdPhaseEncouragement = RuntimeManager.CreateInstance("event:/Weight Lifting/Hold phase encouragement");


                            // FOOT RACE //
        running = RuntimeManager.CreateInstance("event:/Foot Race/Running");
        jump = RuntimeManager.CreateInstance("event:/Foot Race/Jump");
        footRaceEncouragement = RuntimeManager.CreateInstance("event:/Foot Race/Foot Race Encouragement");
        slide = RuntimeManager.CreateInstance("event:/Foot Race/Slide");
        crashIntoFence = RuntimeManager.CreateInstance("event:/Foot Race/Crash Into Fence");

                            // ROCKSKIPPING //
        rockSelect = RuntimeManager.CreateInstance("event:/Rock Skipping/Rock Select");
        closeRockCase = RuntimeManager.CreateInstance("event:/Rock Skipping/Close Rock Case");
        rockSkip = RuntimeManager.CreateInstance("event:/Rock Skipping/Rock Skip");
        rockThrow = RuntimeManager.CreateInstance("event:/Rock Skipping/Rock Throwing");
        rockBounceOnWood = RuntimeManager.CreateInstance("event:/Rock Skipping/Rock Bounce On Wood");
        rockSink = RuntimeManager.CreateInstance("event:/Rock Skipping/Rock Sink");

                            // BAILING //
        tractor = RuntimeManager.CreateInstance("event:/Bailing/Tractor");

                            // TRAFFIC WARDEN //
        carCrash = RuntimeManager.CreateInstance("event:/Traffic Warden/Car Crash");
        rain = RuntimeManager.CreateInstance("event:/Ambiences/Rain");
        roadworks = RuntimeManager.CreateInstance("event:/Ambiences/Roadworks");


                            // UNUSED //
        //dumbbell = RuntimeManager.CreateInstance("event:/Training/Dumbbell");
        //PowerMeter = RuntimeManager.CreateInstance("event:/Weight Lifting/Power Meter");
    }

    private void Update()
    {
        activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "BeerPourMinigame")
        {
            acceptablePour.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            poorPour.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }

        if(activeScene.name != "Garage")
        {
            angelus.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
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