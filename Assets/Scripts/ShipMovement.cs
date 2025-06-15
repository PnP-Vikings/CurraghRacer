using System.Collections;
using UnityEngine;

public class ShipMovement : MonoBehaviour
{
    [Header("Stats & Smoothing")]
    public CharacterStats stats;        // now holds *any* ship’s stats, AI or player
    [Range(0f, 10f)]
    public float speedSmoothing = 3f;   // higher = snappier

    private bool raceStarted = false;
    private float currentSpeed = 0f;    // our smoothed velocity
    
    public string shipName;
    public bool isPlayerShip = false; // Flag to identify if this is the player's ship
    private CharacterStats playerStats; // Reference to player stats if needed
    public bool shipIsFinsihed = false;
    
    
    private void Start()
    {
       if(isPlayerShip)
       {
           playerStats = PlayerManager.Instance.GetPlayerStats(); // Get player stats if this is the player's ship
       }
    }
    
    public void SetAiStatsAfterPlayerFinished(float multiplier)
    {
        // Adjust AI stats based on player performance
        stats = new CharacterStats(
            strength : stats.strength * multiplier,
            stamina  : stats.stamina  * multiplier,
            technique: stats.technique * multiplier,
            teamWork : stats.teamWork * multiplier
        );
    }
    
    
    void Update()
    {
        if (!raceStarted) return;

        // 1) compute the *instantaneous* push (same as before)
        float baseSpeed = stats.strength * 0.05f
            + stats.stamina  * 0.03f;
        float amplitude = (stats.teamWork + stats.technique) * 0.5f;
        float rate      = 1f + stats.technique * 0.2f;

        float osc = (Mathf.Sin(Time.time * rate * Mathf.PI * 2f) + 1f) / 2f;
        float desiredSpeed = baseSpeed + (osc * amplitude);

        // 2) smooth toward that target
        currentSpeed = Mathf.Lerp(currentSpeed, desiredSpeed,
            speedSmoothing * Time.deltaTime);

        // 3) move
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }

    
    public void HandlePlayerInput()
    {
        StartCoroutine(MovePlayerShip());
    }

    IEnumerator MovePlayerShip()
    {
        WaitForSeconds wait = new WaitForSeconds(0.1f);
        
        yield return wait;
        
        float pushForce = (playerStats.strength* 0.1f) + (playerStats.stamina * 0.05f) + (playerStats.technique * 0.02f) + (playerStats.teamWork * 0.01f);
        
        transform.Translate(Vector3.forward * pushForce * Time.deltaTime); // Move the player ship forward based on stats
    }

    public void SetRaceStarted(bool started)
    {
        raceStarted = started;
    }
}
