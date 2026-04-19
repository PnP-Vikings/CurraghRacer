using FMOD.Studio;
using UnityEngine;

public class TrafficWardenAudio : MonoBehaviour
{
    [SerializeField] TrafficWardenMinigameController trafficWardenMinigameController;
    private PLAYBACK_STATE rainPlaybackState;
    private PLAYBACK_STATE roadworksPlaybackState;

    void Update()
    {
        //Debug.Log("Active event is " + trafficWardenMinigameController.activeEvent + " - AudioDebug");
        

        if (AudioManager.instance != null)
        {
            AudioManager.instance.rain.getPlaybackState(out rainPlaybackState);
            AudioManager.instance.roadworks.getPlaybackState(out roadworksPlaybackState);

            switch (trafficWardenMinigameController.activeEvent)
            {
                case TrafficEventType.None:
                    MuteRainAndRoadworks();
                    break;
                case TrafficEventType.OldPerson:
                    MuteRainAndRoadworks();
                    break;
                case TrafficEventType.Ambulance:
                    MuteRainAndRoadworks();
                    break;
                case TrafficEventType.Rain:
                    if(rainPlaybackState == PLAYBACK_STATE.STOPPED)
                    {
                        AudioManager.instance.rain.start();
                    }
                    AudioManager.instance.roadworks.stop(STOP_MODE.ALLOWFADEOUT);
                    break;
                case TrafficEventType.Roadworks:
                    if (roadworksPlaybackState  == PLAYBACK_STATE.STOPPED)
                    {
                        AudioManager.instance.roadworks.start();
                    }
                    AudioManager.instance.rain.stop(STOP_MODE.ALLOWFADEOUT);
                    break;
            }
        }
    }
    public void MuteRainAndRoadworks()
    {
        AudioManager.instance.rain.stop(STOP_MODE.ALLOWFADEOUT);
        AudioManager.instance.roadworks.stop(STOP_MODE.ALLOWFADEOUT);
    }
}
