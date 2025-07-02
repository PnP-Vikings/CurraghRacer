using UnityEngine;

public class CalenderButtons : MonoBehaviour
{
    public FMODUnity.EventReference Event;

    public void PlayOneShot()
    {
        FMODUnity.RuntimeManager.PlayOneShotAttached(Event, gameObject);
    }
}

