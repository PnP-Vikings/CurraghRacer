
using UnityEngine;

public class MapController : MonoBehaviour
{
    [SerializeField] private MapSpline mapSpline;
    
    public void StartMovingPlayerTowardsShop()
    {
        if (mapSpline == null)
        {
            mapSpline = GetComponent<MapSpline>();
        }
     
        mapSpline.StartMovingPlayerTowardsShop();
    }
    
    public void StartMovingPlayerTowardsHome()
    {
        if (mapSpline == null)
        {
            mapSpline = GetComponent<MapSpline>();
        }
     
        mapSpline.StartMovingPlayerTowardsHome();
    }
}
