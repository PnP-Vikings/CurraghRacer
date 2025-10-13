using System.Collections;
using UnityEngine;

public class StartRaceMenuUi : MonoBehaviour
{
    public TMPro.TMP_Text raceCountdownText;
    private FMOD.Studio.EventInstance LoadingScreenSong;

    public void Start()
    {
        if (raceCountdownText != null)
        {
            raceCountdownText.text = "";
            if(RaceManager.Instance != null && RaceManager.Instance.raceStartDelaySeconds > 0)
            {
                UpdateRaceCountdown(RaceManager.Instance.raceStartDelaySeconds);

                LoadingScreenSong = FMODUnity.RuntimeManager.CreateInstance("event:/Soundtrack/Loading Screen Song");
                LoadingScreenSong.start();
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

        LoadingScreenSong.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }
}
