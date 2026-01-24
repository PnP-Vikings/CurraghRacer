using UnityEngine;

[CreateAssetMenu(fileName = "New RockSkippingObject", menuName = "MiniGames/RockSkippingGame/RockSkippingObject")]
public class RockSkippingObject : ScriptableObject 
{
    public string objectName;
    public RockSkippingBounceGameObject prefab;
    public float bounceForce;
    public float movementSpeed;
    
  
    public RockSkippingBounceGameObject CreateInstance(Transform spawnlocation = null)
    {
        RockSkippingBounceGameObject instance = null;
        if (spawnlocation == null)
        {
            instance = Instantiate(prefab);
        }
        else
        {
            instance = Instantiate(prefab, spawnlocation);
        }
        instance.bounceForce = bounceForce;
        instance.movementSpeed = movementSpeed;
        return instance;
    }
    


}
