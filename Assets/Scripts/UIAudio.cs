using UnityEngine;

public class UIAudio : MonoBehaviour
{
    public FMODUnity.EventReference Event;

    public void PlayOneShot()
    {
        FMODUnity.RuntimeManager.PlayOneShotAttached(Event, gameObject);
    }
}
