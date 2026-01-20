using UnityEngine;

[CreateAssetMenu(fileName = "New RockSkippingObject", menuName = "RockSkippingGame/RockSkippingObject")]
public class RockSkippingObject : ScriptableObject 
{
    public string objectName;
    public RockSkippingBounceGameObject prefab;
    public float bounceForce;
    public float movementSpeed;
    
  
    public RockSkippingBounceGameObject CreateInstance()
    {
        RockSkippingBounceGameObject instance = Instantiate(prefab);
        instance.bounceForce = bounceForce;
        instance.movementSpeed = movementSpeed;
        return instance;
    }
    


}
