using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LabourerAudio : MonoBehaviour
{
    private PLAYBACK_STATE footstepsPlaybackState;
    [System.NonSerialized] public EventInstance labourerFootsteps;
    private void Start()
    {
        labourerFootsteps = RuntimeManager.CreateInstance("event:/Construction Site/Footsteps On Rubble");
    }

    public void LabourerFootsteps()
    {
        labourerFootsteps.getPlaybackState(out footstepsPlaybackState);

        if (footstepsPlaybackState == PLAYBACK_STATE.STOPPING | footstepsPlaybackState == PLAYBACK_STATE.STOPPED)
        {
            labourerFootsteps.start();
        }

        //if (AudioManager.instance != null)
        //{
        //    Debug.Log("AudioManager Found - AudioDebug");
        //    AudioManager.instance.labourerFootsteps.start();
        //}
    }
}
