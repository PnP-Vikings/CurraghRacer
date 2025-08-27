using UnityEngine;

public class Powerwasher : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<DirtShaderLogic>() != null)
        {
           DirtShaderLogic dirtShaderLogic = other.GetComponent<DirtShaderLogic>();
              if (dirtShaderLogic != null)
              {
               dirtShaderLogic.CleanDirt();
              }
        }

    }
    
    

}
