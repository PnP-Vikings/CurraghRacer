using UnityEngine;

public class MuteParameter : MonoBehaviour
{
    [SerializeField] FMOD.Studio.EventInstance Event;
    [SerializeField] FMODUnity.EventReference EventRef;
    public void MuteEventParameter()
    {
        Event = FMODUnity.RuntimeManager.CreateInstance("event:/Bar/Pouring Pint");
        Event.setParameterByName("Pouring Pint Volume", 0f);
    }
}
