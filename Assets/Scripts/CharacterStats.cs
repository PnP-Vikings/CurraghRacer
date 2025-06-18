[System.Serializable]
public struct CharacterStats
{
    public float strength;
    public float stamina ;
    public float technique;
    public float teamWork;
    
    public CharacterStats(float strength, float stamina, float technique, float teamWork)
    {
        this.strength = strength;
        this.stamina = stamina;
        this.technique = technique;
        this.teamWork = teamWork;
    }
}
