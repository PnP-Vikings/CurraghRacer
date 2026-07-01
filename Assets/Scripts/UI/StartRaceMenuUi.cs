using System.Collections;
using UnityEngine;
using FMOD.Studio;

public class StartRaceMenuUi : MonoBehaviour
{
    public TMPro.TMP_Text raceCountdownText;

    public void Start()
    {
        if (raceCountdownText != null)
        {
            raceCountdownText.text = "";
            if(RaceManager.Instance != null && RaceManager.Instance.raceStartDelaySeconds > 0)
            {
                UpdateRaceCountdown(RaceManager.Instance.raceStartDelaySeconds);

                if (AudioManager.instance != null)
                {
                    RadioManager.instance.StopAllRadioSongs();
                    AudioManager.instance.miniGameProgression.stop(STOP_MODE.ALLOWFADEOUT);
                    AudioManager.instance.loadingScreenSong.start();
                }
            }
        }
    }
    
    
    public void UpdateRaceCountdown(int secondsForCountdown)
    {
        if (raceCountdownText != null)
        {
           StartCoroutine(CountdownCoroutine(secondsForCountdown));
        }
    }
    
    IEnumerator CountdownCoroutine(int seconds)
    {
        int remainingTime = seconds;
        while (remainingTime > 0)
        {
            raceCountdownText.text = remainingTime.ToString();
            yield return new WaitForSeconds(1f);
            remainingTime--;
        }
        raceCountdownText.text = "Go!";
        yield return new WaitForSeconds(1f);
        raceCountdownText.text = "";

        if (AudioManager.instance != null)
        {
            AudioManager.instance.loadingScreenSong.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }
}
