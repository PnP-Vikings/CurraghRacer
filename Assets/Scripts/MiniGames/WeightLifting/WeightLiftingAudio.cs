using FMODUnity;
using System.Collections;
using UnityEngine;

public class WeightLiftingAudio : MonoBehaviour
{
    [SerializeField] StudioEventEmitter danceTrackEmitter;

    [SerializeField] WeightLiftingController weightliftingController;

    public void StartPowerMeterAudio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PowerMeter.start();
            //AudioManager.instance.PowerMeter.setParameterByName("Power Meter Pitch", 1f);
            //Debug.Log("Power Meter sound");
        }
    }

    public void StopPowerMeterAudio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PowerMeter.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }

    public IEnumerator StartMiniGameOverWinIEunm()
    {
        AudioManager.instance.miniGame_Win.start();
        danceTrackEmitter.SetParameter("Dance Track Volume", 0.7f, false);
        yield return new WaitForSeconds(1f);
        danceTrackEmitter.SetParameter("Dance Track Volume", 0.8f, false);
    }

    public IEnumerator StartMiniGameOverIEnum()
    {
        AudioManager.instance.miniGame_Over.start();
        danceTrackEmitter.SetParameter("Dance Track Volume", 0.7f, false);
        yield return new WaitForSeconds(1.5f);
        danceTrackEmitter.SetParameter("Dance Track Volume", 0.8f, false);
    }

    void Update()
    {
        if(WeightLiftingController.Instance != null)
        {
            //Debug.Log("Power Meter Position is " + WeightLiftingController.Instance.powerMeterPosition);
            if (WeightLiftingController.Instance.powerMeterPosition <= 0.01)
            {
                if(AudioManager.instance != null)
                {
                    AudioManager.instance.PowerMeter.setParameterByName("Power Meter Pitch", 1f);
                }
            }
            else if (WeightLiftingController.Instance.powerMeterPosition == 1)
            {
                if (AudioManager.instance != null)
                {
                    AudioManager.instance.PowerMeter.setParameterByName("Power Meter Pitch", 0f);
                }
            }
        }
    }

    public void PlayWeightliftingMiniGameOver()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.miniGameProgression.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

            if(weightliftingController != null)
            {
                if (weightliftingController.successfulReps >= weightliftingController.maxSuccessfulReps)
                {
                    StartCoroutine(StartMiniGameOverWinIEunm());
                }
                else
                {
                    StartCoroutine(StartMiniGameOverIEnum());
                }
            } 
        }
    }
}
