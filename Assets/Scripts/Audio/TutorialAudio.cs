using System.Collections;
using UnityEngine;

public class TutorialAudio : MonoBehaviour
{
    public IEnumerator PlayTutorialCompleteAudio()
    {
        if(AudioManager.instance != null)
        {
            yield return new WaitForSeconds(1.2f);
            AudioManager.instance.miniGame_Win.start();
        }
    }
}
