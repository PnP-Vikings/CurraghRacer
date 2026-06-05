using FMOD.Studio;
using FMODUnity;
using System.Collections;
using UnityEngine;

public class WeightLiftingAudio : MonoBehaviour
{
    [SerializeField] StudioEventEmitter danceTrackEmitter;

    [SerializeField] WeightLiftingController weightliftingController;

    private bool inTheGreen = false;

    private PLAYBACK_STATE weightSelectionResponsePlaybackState;

    public void StartLiftPhaseAudio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PowerMeter.start();
            AudioManager.instance.liftPhaseEncouragement.start();
            //AudioManager.instance.PowerMeter.setParameterByName("Power Meter Pitch", 1f);
            //Debug.Log("Power Meter sound");
        }
    }

    public void StopLiftPhaseAudio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PowerMeter.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            AudioManager.instance.liftPhaseEncouragement.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }

    public void StartHoldPhaseAudio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.holdPhaseEncouragement.start();
        }
    }

    public void StopHoldPhaseAudio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.holdPhaseEncouragement.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
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
            if (WeightLiftingController.Instance.gripBarPosition >= WeightLiftingController.Instance.gripBarTargetMin && WeightLiftingController.Instance.gripBarPosition <= WeightLiftingController.Instance.gripBarTargetMax)
            {
                bool gripPhaseCompleted = weightliftingController.ReturnGripPhaseCompletedBool();
                if (!inTheGreen & !gripPhaseCompleted)
                {
                    if (AudioManager.instance != null)
                    {
                        AudioManager.instance.inTheGreenDialogue.start();
                    }
                    inTheGreen = true;
                }
            }
        }
        //if (WeightLiftingController.Instance != null)
        //{
        //    //Debug.Log("Power Meter Position is " + WeightLiftingController.Instance.powerMeterPosition);
        //    if (WeightLiftingController.Instance.powerMeterPosition <= 0.01)
        //    {
        //        if (AudioManager.instance != null)
        //        {
        //            AudioManager.instance.PowerMeter.setParameterByName("Power Meter Pitch", 1f);
        //        }
        //    }
        //    else if (WeightLiftingController.Instance.powerMeterPosition == 1)
        //    {
        //        if (AudioManager.instance != null)
        //        {
        //            AudioManager.instance.PowerMeter.setParameterByName("Power Meter Pitch", 0f);
        //        }
        //    }
        //}
    }

    public IEnumerator ResetInTheGreenBool()
    {
        yield return null;
        inTheGreen = false;
    }

    public void PlayWeightliftingMiniGameOver()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.miniGameProgression.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

            if (weightliftingController != null)
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

    public void WeightSelectionAudio()
    {
        if(AudioManager.instance != null)
        {
            AudioManager.instance.cementBag.start();

            if (weightliftingController.currentWeight == 20f)
            {
                AudioManager.instance.cementBag.setParameterByName("Cement Bag Weight", 0f);
            }
            else if (weightliftingController.currentWeight == 40f)
            {
                AudioManager.instance.cementBag.setParameterByName("Cement Bag Weight", 0.2f);
            }
            else if (weightliftingController.currentWeight == 60f)
            {
                AudioManager.instance.cementBag.setParameterByName("Cement Bag Weight", 0.4f);
            }
            else if (weightliftingController.currentWeight == 80f)
            {
                AudioManager.instance.cementBag.setParameterByName("Cement Bag Weight", 0.6f);
            }
            else if (weightliftingController.currentWeight == 100f)
            {
                AudioManager.instance.cementBag.setParameterByName("Cement Bag Weight", 0.8f);
            }
            else if (weightliftingController.currentWeight == 120f)
            {
                AudioManager.instance.cementBag.setParameterByName("Cement Bag Weight", 1f);
            }

            AudioManager.instance.weightSelectionResponse.getPlaybackState(out weightSelectionResponsePlaybackState);

            if (weightliftingController.currentWeight <= 40f)
            {
                if (weightSelectionResponsePlaybackState == PLAYBACK_STATE.STOPPED | weightSelectionResponsePlaybackState == PLAYBACK_STATE.STOPPING)
                {
                    AudioManager.instance.weightSelectionResponse.start();
                }
            }
        }   
    }
}
