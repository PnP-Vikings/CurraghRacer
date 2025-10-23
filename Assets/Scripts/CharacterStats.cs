[System.Serializable]
public struct CharacterStats
{
    [UnityEngine.Tooltip("RAW POWER OUTPUT\n\n" +
        "Racing: Provides the main base speed boost (0.05x multiplier)\n" +
        "Effect: Increases raw power and base speed throughout the race")]
    public float strength;
    
    [UnityEngine.Tooltip("ENDURANCE & ENERGY EFFICIENCY\n\n" +
        "Racing: Contributes to consistent base speed (0.03x multiplier)\n" +
        "Effect: Maintains consistent performance and prevents fatigue")]
    public float stamina;
    
    [UnityEngine.Tooltip("ROWING EFFICIENCY & FORM\n\n" +
        "Racing: Increases rowing frequency/rhythm (0.2x on rate) AND stroke power (0.5x on amplitude with teamwork)\n" +
        "Effect: Improves rowing form, stroke efficiency, and rhythm speed")]
    public float technique;
    
    [UnityEngine.Tooltip("CREW SYNCHRONIZATION & COORDINATION\n\n" +
        "Racing: Increases power of each rowing stroke (0.5x on amplitude with technique)\n" +
        "Effect: Enhances crew coordination for more powerful and synchronized strokes")]
    public float teamWork;
    
    public CharacterStats(float strength, float stamina, float technique, float teamWork)
    {
        this.strength = strength;
        this.stamina = stamina;
        this.technique = technique;
        this.teamWork = teamWork;
    }
}
