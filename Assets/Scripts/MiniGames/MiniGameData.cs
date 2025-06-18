// Assets/Scripts/MiniGames/MiniGameData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "MiniGameData", menuName = "Curragh Racing/Mini Game Data")]
public class MiniGameData : ScriptableObject
{
    [Header("Basic Info")]
    public string gameName;
    [TextArea(3, 5)]
    public string description;
    [TextArea(2, 3)]
    public string funnyQuote;
    public MiniGameType gameType;
    public ActivityCategory category;
    public Difficulty baseDifficulty;
    
    [Header("Rewards")]
    public int baseEarnings; // Money for work activities
    public int baseGain; // Stamina/strength for training
    public float difficultyMultiplier = 1f;
    
    [Header("Game Settings")]
    public float timeLimit = 60f;
    public int perfectScore = 100;
    public bool hasTimeBonus = true;
    
    [Header("UI & Audio")]
    public Sprite gameIcon;
    public GameObject gameUIPrefab;
    public AudioClip startSound;
    public AudioClip successSound;
    public AudioClip failureSound;
    
    [Header("uotes")]
    [TextArea(2, 3)]
    public string startQuote;
    [TextArea(2, 3)]
    public string successQuote;
    [TextArea(2, 3)]
    public string failureQuote;
}