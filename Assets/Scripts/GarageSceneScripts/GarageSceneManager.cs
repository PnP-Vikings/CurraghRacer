using League;
using UnityEngine;

public class GarageSceneManager : MonoBehaviour
{
    public static GarageSceneManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnEnable()
    {
        CheckAndShowLeagueInvite();
    }

    public void CheckAndShowLeagueInvite()
    {
        if(LeagueController.Instance == null)
        {
            Debug.LogWarning("LeagueController instance is null. Cannot check league status.");
            return;
        }
        
        if(LeagueController.Instance.currentLeague == null || !LeagueController.Instance.currentLeague.playerHasJoined)
        {
            Debug.Log("Player not in league, showing join message after delay.");
            StartCoroutine(LeagueController.Instance.StartLeagueInviteMessageAfterDelay(30f));
        }
    }
    
    
    
}
