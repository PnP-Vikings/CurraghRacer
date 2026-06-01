using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class RockSkippingAudio : MonoBehaviour
{
    [SerializeField] RockCase rockCase;

    public void PlayRockSinkAudio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.rockSkip.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

            AudioManager.instance.rockSink.start();
        }
    }

    public IEnumerator MuteRockSelectSounds()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.rockSelect.setParameterByName("Rock Select Volume", 0f);
            AudioManager.instance.UIClick1.setParameterByName("UI Click 1 Volume", 0f);
            yield return new WaitForSeconds(0.15f);
            AudioManager.instance.rockSelect.setParameterByName("Rock Select Volume", 0f);
            AudioManager.instance.UIClick1.setParameterByName("UI Click 1 Volume", 0f);
            yield return new WaitForSeconds(0.15f);
            AudioManager.instance.rockSelect.setParameterByName("Rock Select Volume", 0f);
            AudioManager.instance.UIClick1.setParameterByName("UI Click 1 Volume", 0f);
            yield return new WaitForSeconds(0.15f);
            AudioManager.instance.rockSelect.setParameterByName("Rock Select Volume", 0f);
            AudioManager.instance.UIClick1.setParameterByName("UI Click 1 Volume", 0f);
            yield return new WaitForSeconds(0.15f);
            AudioManager.instance.rockSelect.setParameterByName("Rock Select Volume", 0f);
            AudioManager.instance.UIClick1.setParameterByName("UI Click 1 Volume", 0f);
            yield return new WaitForSeconds(0.15f);
            AudioManager.instance.rockSelect.setParameterByName("Rock Select Volume", 0f);
            AudioManager.instance.UIClick1.setParameterByName("UI Click 1 Volume", 0f);
            yield return new WaitForSeconds(0.15f);

            AudioManager.instance.rockSelect.setParameterByName("Rock Select Volume", 1f);
            AudioManager.instance.UIClick1.setParameterByName("UI Click 1 Volume", 1f);

            yield return new WaitForSeconds(2.9f);

            rockCase.rockSelectSoundsAreMuted = false;
        }
    }
}
