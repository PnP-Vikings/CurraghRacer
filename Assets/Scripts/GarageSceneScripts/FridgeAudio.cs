using UnityEngine;

public class FridgeAudio : MonoBehaviour
{
    private GameObject gameManagerObject;
    private PlayerManager playerManagerScript;

    public void FindPlayerManagerScript()
    {
        //Debug.Log("Finding player manager script - AudioDebug");
        gameManagerObject = GameObject.Find("GameManager");
        if(gameManagerObject != null)
        {
            //Debug.Log("gameManagerObject found - AudioDebug");
            playerManagerScript = gameManagerObject.GetComponent<PlayerManager>();

            if(playerManagerScript != null)
            {
                PlayConsumeFridgeItemAudio();
            }
        }
    }
    public void PlayConsumeFridgeItemAudio()
    {
        if (playerManagerScript != null)
        {
            //Debug.Log("playerManagerscript found - AudioDebug");
            if (playerManagerScript.GetPlayerEnergy() >= 100)
            {
                
                //Debug.Log("player is at max energy - AudioDebug");
                //playerManagerScript.atMaxEnergy = true;
            }
            else
            {
                //Debug.Log("player is NOT at max energy - AudioDebug");
                if (AudioManager.instance != null)
                {
                    AudioManager.instance.consume.start();
                    //playerManagerScript.atMaxEnergy = true;
                }
            }
        }
        else
        {
            Debug.Log("playerManagerScript NOT found - AudioDebug");
        }
    }
}
