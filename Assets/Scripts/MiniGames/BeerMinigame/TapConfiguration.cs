using UnityEngine;

public class TapConfiguration : MonoBehaviour
{
    [Tooltip("Visual tap/faucet GameObject")]
    public GameObject tapObject;
    
    [Tooltip("Exact spawn point for liquid stream particles")]
    public Transform tapSpoutPosition;
    
    [Tooltip("Linked pour location for this tap")]
    public BeerPourLocation associatedPourPoint;
    
    [Tooltip("Tap index (0-3)")]
    public int tapIndex;

    public Vector3 GetPourStreamOrigin()
    {
        if (tapSpoutPosition != null)
        {
            return tapSpoutPosition.position;
        }
        
        // Fallback to tap object position if spout not set
        if (tapObject != null)
        {
            return tapObject.transform.position;
        }
        
        return transform.position;
    }
}

