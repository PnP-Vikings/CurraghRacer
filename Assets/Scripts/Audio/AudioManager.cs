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
    [System.NonSerialized] public EventInstance UIClick1; // Confirm selection 
    [System.NonSerialized] public EventInstance UIClick2; // Return
    [System.NonSerialized] public EventInstance UIClick3; // Hover
        // Main Menu
    [System.NonSerialized] public EventInstance deleteSave;
        // Race
    [System.NonSerialized] public EventInstance raceAmbience;
    [System.NonSerialized] public EventInstance raceWon;
    [System.NonSerialized] public EventInstance raceLost;
    [System.NonSerialized] public EventInstance shout;
        // Garage

    [System.NonSerialized] public EventInstance storyUpdateIntro; // FKA storyUpdate1
    [System.NonSerialized] public EventInstance storyUpdateFirstRaceWon;   // FKA storyUpdate2
    [System.NonSerialized] public EventInstance storyUpdateFirstRaceLost;  // FKA storyUpdate3
    [System.NonSerialized] public EventInstance storyUpdateSecondRaceWon;  // FKA storyUpdate3

    [System.NonSerialized] public EventInstance radioSong1;
    [System.NonSerialized] public EventInstance radioSong2;
    [System.NonSerialized] public EventInstance radioSong3;
    [System.NonSerialized] public EventInstance radioSong4;
    [System.NonSerialized] public EventInstance radioSong5;

    [System.NonSerialized] public EventInstance radioAdOrNews1;
    [System.NonSerialized] public EventInstance radioAdOrNews2;
    [System.NonSerialized] public EventInstance radioAdOrNews3;
    [System.NonSerialized] public EventInstance radioAdOrNews4;
    [System.NonSerialized] public EventInstance radioAdOrNews5;

    [System.NonSerialized] public EventInstance angelus;
    [System.NonSerialized] public EventInstance boglandRadioJingle;

    [System.NonSerialized] public EventInstance tvButtonPushOut;
    [System.NonSerialized] public EventInstance tvButtonPushIn;
    [System.NonSerialized] public EventInstance showInviteAudio;
    [System.NonSerialized] public EventInstance openDoor;
        // Bulletin Board
    [System.NonSerialized] public EventInstance sleepAudio;
    [System.NonSerialized] public EventInstance sleepOutsideAudio;
    [System.NonSerialized] public EventInstance rooster;
    [System.NonSerialized] public EventInstance payBill;
    [System.NonSerialized] public EventInstance payBillWhilePoorDialogue;
    [System.NonSerialized] public EventInstance card;
        // Training
    [System.NonSerialized] public EventInstance gymBagZipUp;
    [System.NonSerialized] public EventInstance dumbbell;

    [System.NonSerialized] public EventInstance rowing;
    [System.NonSerialized] public EventInstance rowingGameSuccess;
    [System.NonSerialized] public EventInstance rowingGameSuccessDialogue;
    [System.NonSerialized] public EventInstance rowingGameFail;
    [System.NonSerialized] public EventInstance rowingGameFailDialogue;
    [System.NonSerialized] public EventInstance rowingGameSuccessAfterFail;
        // Jobs

    [System.NonSerialized] public EventInstance spawnPlates;
    [System.NonSerialized] public EventInstance movePlate;
    [System.NonSerialized] public EventInstance dunkPlate;
    [System.NonSerialized] public EventInstance spongeAudio;

    [System.NonSerialized] public EventInstance barAmbience;
    [System.NonSerialized] public EventInstance pouringPint;
    [System.NonSerialized] public EventInstance acceptablePour;
    [System.NonSerialized] public EventInstance poorPour;
    [System.NonSerialized] public EventInstance setDownPint;

    [System.NonSerialized] public EventInstance punchBagAudio;

    [System.NonSerialized] public EventInstance miniGame_lifeLost;
    [System.NonSerialized] public EventInstance miniGameProgression;
    [System.NonSerialized] public EventInstance miniGame_Win;
    [System.NonSerialized] public EventInstance miniGame_Over;
    [System.NonSerialized] public EventInstance miniGameCompleteDialogue;

    [System.NonSerialized] public EventInstance placeTurf;

    [System.NonSerialized] public EventInstance weightSelectionResponse;
    [System.NonSerialized] public EventInstance dumbbellSlide;
    [System.NonSerialized] public EventInstance grunt;
    [System.NonSerialized] public EventInstance barGrip;
    [System.NonSerialized] public EventInstance barPlacedOnStand;
    [System.NonSerialized] public EventInstance weightliftingPhaseFailedDialogue;
    [System.NonSerialized] public EventInstance inTheGreenDialogue;
    [System.NonSerialized] public EventInstance cementBag;
    [System.NonSerialized] public EventInstance weightliftingRoundEndDialogue;
    [System.NonSerialized] public EventInstance aWarmUp;
    [System.NonSerialized] public EventInstance liftPhaseEncouragement;
    [System.NonSerialized] public EventInstance holdPhaseEncouragement;

    [System.NonSerialized] public EventInstance running;
    [System.NonSerialized] public EventInstance jump;
    [System.NonSerialized] public EventInstance footRaceEncouragement;
    [System.NonSerialized] public EventInstance slide;
    [System.NonSerialized] public EventInstance crashIntoFence;

    [System.NonSerialized] public EventInstance rockSelect;
    [System.NonSerialized] public EventInstance closeRockCase;
    [System.NonSerialized] public EventInstance rockSkip;
    [System.NonSerialized] public EventInstance rockThrow;
    [System.NonSerialized] public EventInstance rockBounceOnWood;
    [System.NonSerialized] public EventInstance rockSink;

    [System.NonSerialized] public EventInstance tractor;

    [System.NonSerialized] public EventInstance carCrash;
    [System.NonSerialized] public EventInstance rain;
    [System.NonSerialized] public EventInstance roadworks;

    [System.NonSerialized] public EventInstance strikeRemoved;
    [System.NonSerialized] public EventInstance scribble;

    [System.NonSerialized] public EventInstance PowerMeter;

    [System.NonSerialized] public EventInstance boxingEncouragementOnLastLife;
    [System.NonSerialized] public EventInstance boxingSuccessAfterFail;

    [System.NonSerialized] public EventInstance bodhran;

    [System.NonSerialized] public EventInstance tutorialGuy;

    [System.NonSerialized] public EventInstance danceTrack;
    [System.NonSerialized] public EventInstance mainTheme;

    [System.NonSerialized] public EventInstance countrysideAmbience;
    
    [System.NonSerialized] public EventInstance walking;
    [System.NonSerialized] public EventInstance taxi;
    [System.NonSerialized] public EventInstance coins;
    [System.NonSerialized] public EventInstance payTaxi;

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
        countrysideAmbience = RuntimeManager.CreateInstance("event:/Ambiences/Countryside");

                            // RACE //
        raceAmbience = RuntimeManager.CreateInstance("event:/Race/Race Ambience");
        raceWon = RuntimeManager.CreateInstance("event:/Race/Race Won");
        raceLost = RuntimeManager.CreateInstance("event:/Race/Race Lost");
        shout = RuntimeManager.CreateInstance("event:/Race/Shout");

                            // RADIO //
        storyUpdateIntro = RuntimeManager.CreateInstance("event:/Radio/Story Update - Intro");   // Declan Kelly has returned
        storyUpdateFirstRaceWon = RuntimeManager.CreateInstance("event:/Radio/Story Update - First Race Won");      // Player won a race
        storyUpdateFirstRaceLost = RuntimeManager.CreateInstance("event:/Radio/Story Update  - First Race Lost");   // Player lost a race
        storyUpdateSecondRaceWon = RuntimeManager.CreateInstance("event:/Radio/Story Update - Second Race Won");   // Player lost a race

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
        boglandRadioJingle = RuntimeManager.CreateInstance("event:/Radio/Bogland Radio Jingle");

                            // OST //
        danceTrack = RuntimeManager.CreateInstance("event:/Soundtrack/Dance Track");
        bodhran = RuntimeManager.CreateInstance("event:/Soundtrack/Bodhrán");
        mainTheme = RuntimeManager.CreateInstance("event:/Soundtrack/Main Theme");

                            // GARAGE //
        tvButtonPushOut = RuntimeManager.CreateInstance("event:/UI/TV Button Push Out");
        tvButtonPushIn = RuntimeManager.CreateInstance("event:/UI/TV Button Push In");
        showInviteAudio = RuntimeManager.CreateInstance("event:/Garage/Show Invite");
        openDoor = RuntimeManager.CreateInstance("event:/Garage/Open Door");

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
        boxingEncouragementOnLastLife = RuntimeManager.CreateInstance("event:/Boxing/Boxing Encouragement On Last Life");
        boxingSuccessAfterFail = RuntimeManager.CreateInstance("event:/Boxing/Boxing Success After Fail");

                            // MINIGAME NOTIFICATION //
        miniGame_lifeLost = RuntimeManager.CreateInstance("event:/Notifications/Minigame_Life Lost");
        miniGameProgression = RuntimeManager.CreateInstance("event:/Notifications/Minigame_Progression");
        miniGame_Win = RuntimeManager.CreateInstance("event:/Notifications/Minigame_Win");
        miniGame_Over = RuntimeManager.CreateInstance("event:/Notifications/Minigame_Over");
        miniGameCompleteDialogue = RuntimeManager.CreateInstance("event:/Notifications/Mini Game Complete Dialogue");

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

                            // Tutorial //
        tutorialGuy = RuntimeManager.CreateInstance("event:/Tutorial/Tutorial Guy");

                            // Map //
        walking = RuntimeManager.CreateInstance("event:/Map/Walking");
        taxi = RuntimeManager.CreateInstance("event:/Map/Taxi");
        coins = RuntimeManager.CreateInstance("event:/Map/Coins");
        payTaxi = RuntimeManager.CreateInstance("event:/Map/Pay Taxi");

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