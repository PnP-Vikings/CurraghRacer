using FMODUnity;
using System.Collections;
using UnityEngine;

public class WeightLiftingAudio : MonoBehaviour
{
    [SerializeField] StudioEventEmitter danceTrackEmitter;

    public void StartPowerMeterAudio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PowerMeter.start();
            //AudioManager.instance.PowerMeter.setParameterByName("Power Meter Pitch", 1f);
        }
    }

    //public IEnumerator StartPowerMeterAudio()
    //{
    //    AudioManager.instance.PowerMeter.start();
    //    yield return null;
    //    AudioManager.instance.PowerMeter.setParameterByName("Power Meter Pitch", 1f);
    //}

    public void StopPowerMeterAudio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PowerMeter.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

        }
    }

    public IEnumerator PlayBoxingSuccessAudio()
    {
        AudioManager.instance.weightliftingSuccess.start();
        danceTrackEmitter.SetParameter("Dance Track Volume", 0.75f, false);
        yield return new WaitForSeconds(1f);
        danceTrackEmitter.SetParameter("Dance Track Volume", 0.95f, false);
    }

    void Update()
    {
        if(WeightLiftingController.Instance != null)
        {
            Debug.Log("Power Meter Position is " + WeightLiftingController.Instance.powerMeterPosition);
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
}
