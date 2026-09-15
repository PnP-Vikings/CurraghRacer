using System.Collections;
using UnityEngine;
using FMOD.Studio;
using UnityEngine.Localization;

public class StartRaceMenuUi : MonoBehaviour
{
    public TMPro.TMP_Text raceCountdownText;

    [SerializeField] LocalizedString raceGoLocalizedString = new LocalizedString
    {
        TableReference = "RaceScene",
        TableEntryReference = "RaceMessage.Go"
    };

    public void Start()
    {
        if (raceCountdownText != null)
        {
            raceCountdownText.text = "";
            if (RaceManager.Instance != null && RaceManager.Instance.raceStartDelaySeconds > 0)
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
        while( remainingTime > 0 )
        {
            raceCountdownText.text = remainingTime.ToString();
            SetRaceCountdownTextColor();
            yield return new WaitForSeconds(1f);
            remainingTime--;
        }
        string goText = raceGoLocalizedString.GetLocalizedString();
        raceCountdownText.text = !string.IsNullOrEmpty(goText) ? goText : "Go!";
        raceCountdownText.color = Color.green;
        yield return new WaitForSeconds(1f);
        raceCountdownText.text = "";

        if (AudioManager.instance != null)
        {
            AudioManager.instance.loadingScreenSong.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }


    public void SetRaceCountdownTextColor()
    {
        if (raceCountdownText != null)
        {
            if (raceCountdownText.text == "5")
            {
                raceCountdownText.color = Color.red;
            }
            else if (raceCountdownText.text == "4")
            {
                raceCountdownText.color = Color.orangeRed;
            }
            else if (raceCountdownText.text == "3")
            {
                raceCountdownText.color = Color.orange;
            }
            else if (raceCountdownText.text == "2")
            {
                raceCountdownText.color = Color.yellow;
            }
            else if (raceCountdownText.text == "1")
            {
                raceCountdownText.color = Color.darkGreen;
            }
        }
    }
}
