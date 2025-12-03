using System.Collections;
using League;
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
    public int starRating = 1;
    
    [Header("Shout Boost")]
    public float shoutSpeedBoost = 1f; // Multiplier applied to speed (1 = normal, >1 = boosted)
    [SerializeField] private float starRatingMultiplier = 1f;
    private bool isShoutBoosting = false;
    
    [Header("Ui Elements")]
    public TMPro.TMP_Text shipPositionText;
    
    
    private void Start()
    {
       if(isPlayerShip)
       {
           playerStats = PlayerManager.Instance.GetPlayerStats(); // Get player stats if this is the player's ship
       }
       
       // Calculate star rating and multiplier at start
       CalculateStarRatingMultiplier();
    }
    
    /// <summary>
    /// Calculates the star rating and sets the speed multiplier based on it
    /// Star Rating: 1 = 0.85x, 2 = 0.925x, 3 = 1.0x, 4 = 1.075x, 5 = 1.15x
    /// </summary>
    private void CalculateStarRatingMultiplier()
    {
        if (LeagueController.Instance != null && LeagueController.Instance.currentLeague != null)
        {
            // Find the team by ship name to ensure consistency with TeamUi
            Team team = System.Array.Find(LeagueController.Instance.currentLeague.teams, t => t != null && t.teamName == shipName);
            
            if (team != null)
            {
                // Use the same method as TeamUi for consistency
                starRating = LeagueController.Instance.CalculateTeamStarRating(team);
            }
            else
            {
                // Fallback: calculate from stats if team not found
                Debug.LogWarning($"Could not find team '{shipName}' in league. Using fallback star rating calculation.");
                starRating = LeagueController.Instance.CalculateTeamMemberStarRatingByStats(stats);
            }
            
            // Convert star rating to speed multiplier
            // 1 star: 77.5% speed (0.775x)
            // 2 stars: 85% speed (0.85x)
            // 3 stars: 92.5% speed (0.925x)
            // 4 stars: 100% speed (1.0x) - baseline
            // 5 stars: 107.5% speed (1.075x)
            starRatingMultiplier = 0.7f + (starRating * 0.075f);
            
            Debug.Log($"Ship '{shipName}' has {starRating} stars with {starRatingMultiplier:F2}x speed multiplier");
        }
        else
        {
            starRating = 3;
            starRatingMultiplier = 0.925f;
            Debug.LogWarning("LeagueController or currentLeague is null! Defaulting star rating to 3 and multiplier to 0.925x");
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
    
    public void SetShipPositionText(int position)
    {
        if(shipPositionText != null)
        {
            shipPositionText.text = position.ToString();
        }
    }
    
    void Update()
    {
        if (!raceStarted) return;

        // STAT BREAKDOWN:
        // STRENGTH (0.05x multiplier) - Primary power output, provides the main base speed boost
        // STAMINA (0.03x multiplier) - Endurance, contributes to consistent base speed throughout the race
        // TECHNIQUE (0.2x multiplier on rate) - Rowing efficiency, increases the frequency of speed oscillations for faster rhythm
        // TEAMWORK (0.5x multiplier on amplitude) - Synchronization, increases the power of each rowing stroke (combined with technique)
        
        // Calculate base speed (constant forward momentum)
        // STRENGTH: Provides the majority of base speed (0.05x)
        // STAMINA: Adds sustained energy for consistent performance (0.03x)
        float baseSpeed = stats.strength * 0.05f
            + stats.stamina  * 0.03f;
        
        // Calculate rowing stroke power (oscillation amplitude)
        // TEAMWORK + TECHNIQUE: Combined, they determine how much extra speed each stroke adds (0.5x)
        float amplitude = (stats.teamWork + stats.technique) * 0.5f;
        
        // Calculate rowing stroke frequency (how fast the team rows)
        // TECHNIQUE: Higher technique means faster, more efficient rowing rhythm (0.2x)
        float rate      = 1f + stats.technique * 0.2f;

        // Simulate rowing rhythm (oscillates between strokes)
        float osc = (Mathf.Sin(Time.time * rate * Mathf.PI * 2f) + 1f) / 2f;
        float desiredSpeed = baseSpeed + (osc * amplitude);
        
        // Apply star rating multiplier to final speed
        // Higher star ratings (better overall team quality) get a speed boost
        desiredSpeed *= starRatingMultiplier;
        
        // Apply shout boost multiplier if active
        desiredSpeed *= shoutSpeedBoost;

        // Smooth the speed changes for realistic movement
        currentSpeed = Mathf.Lerp(currentSpeed, desiredSpeed,
            speedSmoothing * Time.deltaTime);

        // Move the ship forward
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }

    
    /*public void HandlePlayerInput()
    {
        StartCoroutine(MovePlayerShip());
    }

    IEnumerator MovePlayerShip()
    {
        WaitForSeconds wait = new WaitForSeconds(0.1f);
        
        yield return wait;
        
        // PLAYER INPUT STAT BREAKDOWN:
        // STRENGTH (0.1x multiplier) - Raw power, provides the biggest boost per button press
        // STAMINA (0.05x multiplier) - Energy efficiency, adds sustained push force to each stroke
        // TECHNIQUE (0.02x multiplier) - Rowing form, improves the effectiveness of each input
        // TEAMWORK (0.01x multiplier) - Crew coordination, provides small bonus to synchronized efforts
        float pushForce = (playerStats.strength* 0.1f) + (playerStats.stamina * 0.05f) + (playerStats.technique * 0.02f) + (playerStats.teamWork * 0.01f);
        
        transform.Translate(Vector3.forward * pushForce * Time.deltaTime); // Move the player ship forward based on stats
    }*/

    /// <summary>
    /// Activates the shout boost for a specified duration
    /// </summary>
    /// <param name="duration">How long the boost lasts in seconds</param>
    public void ActivateShoutBoost(float duration = 3f)
    {
        if (!raceStarted || isShoutBoosting) return;

        StartCoroutine(ShoutBoostCoroutine(duration));
    }
    
    private IEnumerator ShoutBoostCoroutine(float duration)
    {
        isShoutBoosting = true;
        
        // Calculate boost strength based on strength and teamwork stats
        // Higher stats = bigger boost (1.2x to 2.0x multiplier range)
        shoutSpeedBoost = 1f + ((stats.strength + stats.teamWork) * 0.02f);
        
        Debug.Log($"{shipName} activated shout boost! Speed multiplier: {shoutSpeedBoost:F2}x for {duration}s");

        if (AudioManager.instance != null)
        {
            AudioManager.instance.shout.start();
        }
        
        // Wait for the boost duration
        yield return new WaitForSeconds(duration);
        
        // Reset boost after duration expires
        shoutSpeedBoost = 1f;
        isShoutBoosting = false;
        
        Debug.Log($"{shipName} shout boost ended.");
    }
    
    public void SetRaceStarted(bool started)
    {
        raceStarted = started;
    }
}
