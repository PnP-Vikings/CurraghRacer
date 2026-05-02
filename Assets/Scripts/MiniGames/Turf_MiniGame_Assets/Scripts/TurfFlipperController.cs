using UnityEngine;
using UnityEngine.AI;

public class TurfFlipperController : MonoBehaviour
{
    [SerializeField] private Vector3 defaultPosition;
    [SerializeField] private Transform targetPosition;
    [SerializeField] private NavMeshAgent agent;
    
    private void Start()
    {
        defaultPosition =transform.position;
        agent = GetComponent<NavMeshAgent>();
        
    }
    
    public void GoTowardsTarget(Transform target)
    {
        targetPosition = target;
        agent.SetDestination(targetPosition.position);
    }

    public void ReturnHome()
    {
        agent.SetDestination(defaultPosition);
    }
   
}
