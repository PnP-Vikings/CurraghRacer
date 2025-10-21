using System.Collections;
using UnityEngine;

public class GroundBarrier : MonoBehaviour
{
    public GameObject barrierObject;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(HideBarrierAfterDelay(1f));
        }
    }
    
    public void HideBarrier()
    {
        if (barrierObject != null)
        {
            barrierObject.SetActive(false);
        }
        if( FootRaceMiniGameManager.Instance != null)
        {
            FootRaceMiniGameManager.Instance.MovePassedObstacleToEnd(barrierObject.transform, ObstacleType.GroundObstacle);
        }
    }
    
    IEnumerator HideBarrierAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (barrierObject != null)
        {
            HideBarrier();
        }
    }
}
