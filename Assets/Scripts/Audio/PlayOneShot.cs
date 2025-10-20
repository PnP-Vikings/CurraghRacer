using UnityEngine;

public class PlayOneShot : MonoBehaviour
{
    [SerializeField] FMODUnity.EventReference Event;

    public void PlayOneShotFunction()
    {
        FMODUnity.RuntimeManager.PlayOneShotAttached(Event, gameObject);
    }
}
