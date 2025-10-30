using FMODUnity;
using FMOD.Studio;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    private Bus masterBus;
    public EventInstance UIClick1;
    public EventInstance UIClick2;
    public EventInstance raceAmbience;
    public EventInstance radioSong;
    public EventInstance newsReportOrAd;
    public EventInstance loadingScreenSong;
    public EventInstance sleepAudio;
    public EventInstance sleepOutsideAudio;
    public EventInstance raceWin;
    public EventInstance raceLose;
    public EventInstance kitchenAmbience;
    public EventInstance movePlateAudio;
    public EventInstance spongeAudio;
    public EventInstance tvButtonPushOut;
    public EventInstance tvButtonPushIn;
    public EventInstance showInviteAudio;
    public EventInstance gymBagZipUp;
    public EventInstance pouringPint;
    public EventInstance punchBagAudio;
    public EventInstance deleteSave;
    public EventInstance dumbbell;
    public EventInstance payBill;
    public EventInstance footingTurfAmbience;
    public EventInstance turfStackComplete;

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
        raceAmbience = RuntimeManager.CreateInstance("event:/Race/Race Ambience");
        radioSong = RuntimeManager.CreateInstance("event:/Garage/Radio Song");
        newsReportOrAd = RuntimeManager.CreateInstance("event:/Garage/News Report Or Ad");
        loadingScreenSong = RuntimeManager.CreateInstance("event:/Soundtrack/Loading Screen Song");
        sleepAudio = RuntimeManager.CreateInstance("event:/Bulletin Board/Sleep");
        sleepOutsideAudio = RuntimeManager.CreateInstance("event:/Bulletin Board/Sleep Outside");
        raceWin = RuntimeManager.CreateInstance("event:/Race/Race Win");
        raceLose = RuntimeManager.CreateInstance("event:/Race/Race Lose");
        kitchenAmbience = RuntimeManager.CreateInstance("event:/Kitchen/Kitchen Ambience");
        movePlateAudio = RuntimeManager.CreateInstance("event:/Kitchen/Move Plate");
        spongeAudio = RuntimeManager.CreateInstance("event:/Kitchen/Sponge");
        tvButtonPushOut = RuntimeManager.CreateInstance("event:/UI/TV Button Push Out");
        tvButtonPushIn = RuntimeManager.CreateInstance("event:/UI/TV Button Push In");
        showInviteAudio = RuntimeManager.CreateInstance("event:/Garage/Show Invite");
        gymBagZipUp = RuntimeManager.CreateInstance("event:/Training/Gym Bag Zip Up");
        pouringPint = RuntimeManager.CreateInstance("event:/Bar/Pouring Pint");
        punchBagAudio = RuntimeManager.CreateInstance("event:/Training/Punch Bag");
        deleteSave = RuntimeManager.CreateInstance("event:/UI/Delete Save");
        dumbbell = RuntimeManager.CreateInstance("event:/Training/Dumbbell");
        payBill = RuntimeManager.CreateInstance("event:/Bulletin Board/Pay Bill");
        footingTurfAmbience = RuntimeManager.CreateInstance("event:/Footing Turf/Footing Turf Ambience");
        turfStackComplete = RuntimeManager.CreateInstance("event:/Footing Turf/Turf Stack Complete");
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