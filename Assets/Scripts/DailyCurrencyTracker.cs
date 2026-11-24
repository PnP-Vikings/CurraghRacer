[System.Serializable]
public class DailyCurrencyTracker 
{
    public int euroAtStartOfDay = 0;
    public int euroAtEndOfDay = 0;
    public int teamHappinessStartOfDay = 0;
    public int teamHappinessEndOfDay = 0;
    
    public void SetEuroAtStartOfDay(int value)
    {
        euroAtStartOfDay = value;
    }

    public void SetEuroAtEndOfDay(int value)
    {
        euroAtEndOfDay = value;
    }
    
    public void SetEuroAtStartOfDayAndEndOfDay(int startOfDayValue, int endOfDayValue)
    {
        euroAtStartOfDay = startOfDayValue;
        euroAtEndOfDay = endOfDayValue;
    }
    
    public void SetTeamHappinessStartOfDay(int value)
    {
        teamHappinessStartOfDay = value;
    }
    
    public void SetTeamHappinessEndOfDay(int value)
    {
        teamHappinessEndOfDay = value;
    }

    public void Reset()
    {
        euroAtStartOfDay = 0;
        euroAtEndOfDay = 0;
        teamHappinessStartOfDay = 0;
        teamHappinessEndOfDay = 0;
    }
    
}
