// Assets/Scripts/MiniGames/MiniGameData.cs
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniGames
{
    [CreateAssetMenu(fileName = "MiniGameData", menuName = "Curragh Racing/Mini Game Data")]
    public class MiniGameData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Display name of the minigame shown to players")]
        public string gameName;
        [TextArea(3, 5)]
        [Tooltip("Detailed description of how to play this minigame")]
        public string description;
        [TextArea(2, 3)]
        [Tooltip("Humorous Irish quote displayed before the game starts")]
        public string funnyQuote;
        [Tooltip("Type of minigame - determines which controller system to use")]
        public MiniGameType gameType;
        [Tooltip("Category determines if this is a work activity (earns money) or training (improves attributes)")]
        public ActivityCategory category;
        [Tooltip("Base difficulty level affecting scoring and rewards")]
        public Difficulty baseDifficulty;
        
        [Header("Training Attributes")]
        [Tooltip("Which player attribute this training minigame improves (only applies to training activities)")]
        public TrainingAttribute trainingAttribute = TrainingAttribute.None;
        [Tooltip("How much the selected attribute improves per point of performance")]
        public float attributeGainMultiplier = 1f;
        
        [Header("Scene Management")]
        [Tooltip("Scene to load for this minigame (leave empty to use current scene)")]
        public string sceneName;
        [Tooltip("Should this minigame load a separate scene?")]
        public bool useSceneLoading = false;
        [Tooltip("Scene to return to after minigame completion")]
        public string returnSceneName = "RaceScene";
        
        [Header("Rewards")]
        [Tooltip("Base earnings for this minigame (applies to work activities)")]
        public int baseEarnings; // Money for work activities
        [Tooltip("Base gain for this minigame (applies to training activities)")]
        public int baseGain; // Stamina/strength for training
        [Tooltip("Global multiplier applied to all rewards")]
        public float difficultyMultiplier = 1f;
        [Tooltip("Whether to apply a bonus multiplier for perfect completion")]
        public bool enablePerfectBonus = true;
        [Tooltip("Bonus multiplier for perfect completion (100% performance)")]
        public float perfectBonus = 1.5f;
        [Tooltip("Minimum performance required for any reward (0-1)")]
        [Range(0f, 1f)]
        public float minimumPerformanceForReward = 0.3f;
        
        [Header("Game Settings")]
        [Tooltip("Maximum time allowed to complete this minigame (in seconds)")]
        public float timeLimit = 60f;
        [Tooltip("Maximum possible score for perfect performance")]
        public int perfectScore = 100;
        [Tooltip("Whether players get bonus points for finishing with time remaining")]
        public bool hasTimeBonus = true;
        [Tooltip("Time bonus points per second remaining")]
        public float timeBonusMultiplier = 1f;
        [Tooltip("Energy cost to play this minigame")]
        public int energyCost = 25;
        
        [Header("Difficulty Scaling")]
        [Tooltip("Score multiplier based on difficulty level (X=difficulty 0-2, Y=multiplier)")]
        public AnimationCurve difficultyScoreMultiplier = AnimationCurve.Linear(0, 0.5f, 2, 2f);
        [Tooltip("Reward multiplier based on difficulty level (X=difficulty 0-2, Y=multiplier)")]
        public AnimationCurve difficultyRewardMultiplier = AnimationCurve.Linear(0, 0.7f, 2, 1.5f);
        
        [Header("UI & Audio")]
        [Tooltip("Icon displayed in minigame selection menus")]
        public Sprite gameIcon;
        [Tooltip("Custom UI prefab to spawn for this minigame (optional)")]
        public GameObject gameUIPrefab;
        [Tooltip("Background music that plays during this minigame")]
        public AudioClip backgroundMusic;
        [Tooltip("Sound effect played when the minigame starts")]
        public AudioClip startSound;
        [Tooltip("Sound effect played on successful completion")]
        public AudioClip successSound;
        [Tooltip("Sound effect played on failure or poor performance")]
        public AudioClip failureSound;
        [Tooltip("Array of ambient sounds that can play randomly during gameplay")]
        public AudioClip[] ambientSounds;
        
        [Header("Visual Customization")]
        [Tooltip("Primary color theme for UI elements in this minigame")]
        public Color primaryColor = Color.white;
        [Tooltip("Secondary color theme for UI accents and highlights")]
        public Color secondaryColor = Color.gray;
        [Tooltip("Custom background image/sprite for this minigame")]
        public Sprite backgroundImage;
        [Tooltip("Particle effect spawned when player achieves good performance (80%+)")]
        public GameObject successParticleEffect;
        
        [Header("Quotes & Flavor Text")]
        [TextArea(2, 3)]
        [Tooltip("Quote displayed when the minigame starts")]
        public string startQuote;
        [TextArea(2, 3)]
        [Tooltip("Quote displayed on successful completion (60%+ performance)")]
        public string successQuote;
        [TextArea(2, 3)]
        [Tooltip("Quote displayed on failure or poor performance")]
        public string failureQuote;
        [TextArea(2, 3)]
        [Tooltip("Special quote displayed for perfect performance (100%)")]
        public string perfectQuote;
        [TextArea(2, 3)]
        [Tooltip("Quote displayed when time runs out")]
        public string timeoutQuote;
        
        [Header("Unlock Requirements")]
        [Tooltip("Minimum player level required to access this minigame")]
        public int minimumLevel = 1;
        [Tooltip("Minimum coins required to unlock this minigame")]
        public int minimumCoins = 0;
        [Tooltip("Other minigames that must be completed before this one is available")]
        public MiniGameData[] prerequisites;
        
        [Header("Advanced Settings")]
        [Tooltip("Maximum times this minigame can be played per day (-1 for unlimited)")]
        public int maxPlaysPerDay = -1;
        [Tooltip("Cooldown time between plays in minutes (0 for no cooldown)")]
        public float cooldownMinutes = 0f;
        [Tooltip("Custom controller prefab to instantiate for special minigame mechanics")]
        public GameObject customControllerPrefab;
        [Tooltip("Special gameplay modifiers that affect how this minigame behaves")]
        public GameplayModifier[] gameplayModifiers;
        
        // Methods for easy access
        public bool CanLoadScene => useSceneLoading && !string.IsNullOrEmpty(sceneName);
        public bool HasBackgroundMusic => backgroundMusic != null;
        public bool HasCustomController => customControllerPrefab != null;
        
        /// <summary>
        /// Calculate final score based on base score, time remaining, and difficulty
        /// </summary>
        public int CalculateFinalScore(int baseScore, float timeRemaining)
        {
            float finalScore = baseScore;
            
            // Apply time bonus if enabled
            if (hasTimeBonus && timeRemaining > 0)
            {
                finalScore += timeRemaining * timeBonusMultiplier;
            }
            
            // Apply difficulty multiplier
            float difficultyMult = difficultyScoreMultiplier.Evaluate((int)baseDifficulty);
            finalScore *= difficultyMult;
            
            return Mathf.RoundToInt(finalScore);
        }
        
        /// <summary>
        /// Calculate rewards based on performance percentage
        /// </summary>
        public (float earnings, int stamina) CalculateRewards(float performancePercentage)
        {
            float earnings = 0f;
            int stamina = 0;
            
            // Check if performance meets minimum threshold
            if (performancePercentage < minimumPerformanceForReward)
                return (0f, 0);
            
            // Apply difficulty multiplier to rewards
            float difficultyMult = difficultyRewardMultiplier.Evaluate((int)baseDifficulty);
            
            if (category == ActivityCategory.Work)
            {
                earnings = baseEarnings * performancePercentage * difficultyMultiplier * difficultyMult;
                
                // Perfect bonus
                if (performancePercentage >= 1f && enablePerfectBonus)
                {
                    earnings *= perfectBonus;
                }
            }
            else if (category == ActivityCategory.Training)
            {
                stamina = Mathf.RoundToInt(baseGain * performancePercentage * difficultyMultiplier * difficultyMult);
                
                // Perfect bonus
                if (performancePercentage >= 1f && enablePerfectBonus)
                {
                    stamina = Mathf.RoundToInt(stamina * perfectBonus);
                }
            }
            
            return (earnings, stamina);
        }
        
        /// <summary>
        /// Get appropriate quote based on performance
        /// </summary>
        public string GetQuoteForPerformance(float performancePercentage, bool timedOut = false)
        {
            if (timedOut && !string.IsNullOrEmpty(timeoutQuote))
                return timeoutQuote;
            
            if (performancePercentage >= 1f && !string.IsNullOrEmpty(perfectQuote))
                return perfectQuote;
            
            if (performancePercentage >= 0.6f)
                return successQuote;
            
            return failureQuote;
        }
    }
    
    [System.Serializable]
    public class GameplayModifier
    {
        public string modifierName;
        [TextArea(2, 3)]
        public string description;
        public ModifierType type;
        public float value;
    }
    
    public enum ModifierType
    {
        SpeedMultiplier,
        ScoreMultiplier,
        TimeExtension,
        DifficultyIncrease,
        SpecialMechanic
    }
}
