using UnityEngine;

namespace MiniGames
{
    public enum TrainingAttribute
    {
        [Tooltip("Improves rowing power and physical strength")]
        Strength,
        
        [Tooltip("Improves rowing endurance and energy capacity")]
        Stamina,
        
        [Tooltip("Improves rowing technique and boat handling")]
        Technique,
        
        [Tooltip("Improves Teamwork and coordination with teammates")]
        Teamwork ,
        
        [Tooltip("General fitness that affects all attributes slightly")]
        Overall,
        
        [Tooltip("None - No specific attribute training")]
        None
    }
}
