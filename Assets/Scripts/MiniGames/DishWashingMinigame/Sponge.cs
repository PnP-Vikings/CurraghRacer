using UnityEngine;

public class Sponge : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<DirtShaderLogic>() != null)
        {
            Debug.Log("Sponge collided with dirt: " + other.name);
           DirtShaderLogic dirtShaderLogic = other.GetComponent<DirtShaderLogic>();
              if (dirtShaderLogic != null)
              {
               dirtShaderLogic.CleanDirt();
              }
        }

    }
    
    

}
