using FMODUnity;
using UnityEngine;

public class Sponge : MonoBehaviour
{
    private FMOD.Studio.EventInstance spongeAudio;
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<DirtShaderLogic>() != null)
        {
            Debug.Log("Sponge collided with dirt: " + other.name);
           DirtShaderLogic dirtShaderLogic = other.GetComponent<DirtShaderLogic>();
              if (dirtShaderLogic != null)
              {
               dirtShaderLogic.CleanDirt();

                spongeAudio = RuntimeManager.CreateInstance("event:/Kitchen/Sponge");
                spongeAudio.start();

                //if (AudioManager.instance != null)
                //{
                //    AudioManager.instance.spongeAudio.start();
                //}
            }
        }
    }
}
