// Assets/Scripts/MiniGames/IrishMiniGameManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;
using MiniGames.DishWashingMinigame;
using MiniGames.BeerMinigame;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace MiniGames
{
    public class MiniGameManager : MonoBehaviour
    {
        [Header("Game Collections")]
        [SerializeField] private List<MiniGameData> workActivities;
        [SerializeField] private List<MiniGameData> trainingActivities;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        
        // Game state
        private MiniGameData currentGame;
        private MiniGame currentGameInstance;
        [SerializeField] private float gameTimer;
        [SerializeField] private bool gameActive;
        [SerializeField]  private int currentScore;
        private PlayerManager playerManager;
        public UnityEvent MiniGameTimerRunsOutEvent;
        
        public static MiniGameManager Instance { get; private set; }
        
        [Header("Training Activity Settings")]
        [SerializeField] private TeamMember selectedTeamMember;
        [SerializeField] private TeamMember.StatType selectedTrainingStatType;
        
        private Tween currentDelayTween;
        
        
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            playerManager = PlayerManager.Instance;
            if (playerManager == null)
            {
                playerManager = FindFirstObjectByType<PlayerManager>();
            }
        }
        
        void Update()
        {
            if (gameActive)
            {
                gameTimer -= Time.deltaTime;
                
                if (gameTimer <= 0)
                {
                    EndGame();
                    //MiniGameTimerRunsOutEvent.Invoke();
                }
                
                currentGameInstance?.UpdateGame();
            }
        }
        
        public void StartRandomWorkActivity()
        {
            if (workActivities == null || workActivities.Count == 0)
            {
                Debug.LogError("No work activities available!");
                return;
            }
            
            // Randomly select a work activity
            int randomIndex = Random.Range(0, workActivities.Count);
            MiniGameData selectedActivity = workActivities[randomIndex];
            
            Debug.Log($"Starting random work activity: {selectedActivity.gameName}");
            GameManager.Instance.SetPlayerBusy(true);
            // Start the selected activity directly
            StartActivityDirectly(selectedActivity);
        }

        public void StartRandomTrainingActivity()
        {
                if (trainingActivities == null || trainingActivities.Count == 0)
                {
                    Debug.LogError("No training activities available!");
                    return;
                }

                if (PlayerManager.Instance.PlayerHasEnoughEnergy(25))
                {
                    // Randomly select a training activity
                    int randomIndex = Random.Range(0, trainingActivities.Count);
                    MiniGameData selectedActivity = trainingActivities[randomIndex];

                    Debug.Log($"Starting random training activity: {selectedActivity.gameName}");
                    GameManager.Instance.SetPlayerBusy(true);
                    // Start the selected activity directly
                    StartActivityDirectly(selectedActivity);
                }
            
        }
        public void StartRandomTrainingActivityBasedOnStatType(TeamMember.StatType statType, TeamMember teamMember)
        {
            selectedTeamMember = teamMember;
            selectedTrainingStatType = statType;
            if (trainingActivities == null || trainingActivities.Count == 0)
            {
                Debug.LogError("No training activities available!");
                return;
            }

            if (PlayerManager.Instance.PlayerHasEnoughEnergy(25))
            {
                List<MiniGameData>  tempTrainingActivities = new (trainingActivities); // Create a copy to filter
                
                for (int i = tempTrainingActivities.Count - 1; i >= 0; i--)
                {
                    if (tempTrainingActivities[i].trainingAttribute != statType)
                    {
                        tempTrainingActivities.RemoveAt(i);
                    }
                }
                
                
                // Randomly select a training activity
                int randomIndex = Random.Range(0, tempTrainingActivities.Count);
                MiniGameData selectedActivity = tempTrainingActivities[randomIndex];

                Debug.Log($"Starting random training activity: {selectedActivity.gameName}");
                GameManager.Instance.SetPlayerBusy(true);
                // Start the selected activity directly
                StartActivityDirectly(selectedActivity);
            }
            
        }
        private void StartActivityDirectly(MiniGameData activity)
        {
            currentGame = activity;
            
            // Check if we need to load a different scene
            if (activity.CanLoadScene)
            {
                Debug.Log($"Loading scene: {activity.sceneName} for {activity.gameName}");
                
                // Store the current game data before scene transition
                DontDestroyOnLoad(gameObject);
                
                // Load the minigame scene
                SceneManager.LoadScene(activity.sceneName);
                
                // The game will continue in the new scene via OnSceneLoaded
                return;
            }
            
            // Start the minigame in current scene
            StartMiniGameInCurrentScene(activity);
        }
        
        private void StartMiniGameInCurrentScene(MiniGameData activity)
        {
            // Show start quote if available
            if (!string.IsNullOrEmpty(activity.startQuote))
            {
                Debug.Log($"Quote: {activity.startQuote}");
            }
            
            // Check energy cost
            if (playerManager != null && !playerManager.PlayerHasEnoughEnergy(activity.energyCost))
            {
                Debug.LogWarning($"Not enough energy! Need {activity.energyCost} energy to play {activity.gameName}");
                return;
            }
            
            /*// Deduct energy cost
            if (playerManager != null)
            {
                playerManager.ModifyPlayerEnergy(-activity.energyCost);
            }*/
            
            // Reset game state
            gameTimer = activity.timeLimit;
            currentScore = 0;
            gameActive = true;
            
            // Play start sound and background music
            if (activity.startSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(activity.startSound);
            }
            
            if (activity.HasBackgroundMusic && audioSource != null)
            {
                audioSource.clip = activity.backgroundMusic;
                audioSource.loop = true;
                audioSource.Play();
            }
            
            // Instantiate custom controller if specified
            if (activity.HasCustomController)
            {
                GameObject customController = Instantiate(activity.customControllerPrefab);
                Debug.Log($"Instantiated custom controller: {customController.name}");
            }
            
            // Create and start the specific mini game
            currentGameInstance = CreateMiniGameInstance(activity.gameType);
            currentGameInstance?.Initialize(this, activity);
            currentGameInstance?.StartGame();
        }
        
        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // If we have a current game and we're in the right scene, continue the minigame
            if (currentGame != null && currentGame.CanLoadScene && scene.name == currentGame.sceneName)
            {
                Debug.Log($"Scene {scene.name} loaded, starting minigame: {currentGame.gameName}");
                StartMiniGameInCurrentScene(currentGame);
            }
        }
        
        private MiniGame CreateMiniGameInstance(MiniGameType gameType)
        {
            // Remove any existing game instance
            if (currentGameInstance != null)
            {
                Destroy((MonoBehaviour)currentGameInstance);
            }
            
            switch (gameType)
            {
                case MiniGameType.WashingDishes:
                    return gameObject.AddComponent<DishwashingMiniGame>();
                
                case MiniGameType.PouringPint:
                    return gameObject.AddComponent<BeerPouringMiniGame>();
              
                default:
                    Debug.LogWarning($"Mini game {gameType} not implemented yet!");
                    return null;
            }
        }
        
        public void UpdateScore(int score)
        {
            currentScore = score;
            Debug.Log($"Score updated: {currentScore}");
        }
        
        public void UpdateProgress(string progressText)
        {
            Debug.Log($"Progress: {progressText}");
        }
        
        public void CompleteGame(int finalScore, float delayBeforeEnding = 0f)
        {
            currentScore = finalScore;
            Debug.Log($"Game completed with final score: {currentScore}");
            if(currentDelayTween != null)
            {
                currentDelayTween.Kill();
            }
            currentDelayTween = DOVirtual.DelayedCall(delayBeforeEnding, () => EndGame());
        }
        
        
        private void EndGame(bool forceEnd = false)
        {
            gameActive = false;

            if (currentGameInstance != null)
            {
                currentGameInstance.EndGame();
            }


            if (currentGame.DidPlayerPass(currentScore))
            {
                CalculateAndShowResults();
            }
            else
            {
                PlayerFailedMiniGame();
            }
            
           
        }
        
        private void CalculateAndShowResults()
        {
            // Use the enhanced calculation methods from MiniGameData
            bool timedOut = gameTimer <= 0;
            float timeRemaining = Mathf.Max(0, gameTimer);
            
            // Calculate final score with time bonuses and difficulty scaling
            if(currentGame == null)
            {
                Debug.LogError("No current game data available to calculate results!");
                return;
            }
            
            
            
            
            int finalScore = 0;
            float earnings = 0;
            int attribute = 0;
            finalScore=  currentGame.CalculateFinalScore(currentScore, timeRemaining);
            float scorePercentage = 1f;
            if (currentGame.enablePerfectBonus)
            {
                scorePercentage= (float)finalScore / currentGame.perfectScore;
                (earnings, attribute) = currentGame.CalculateRewards(scorePercentage);
            }
            else
            {
                
                (earnings, attribute) = currentGame.CalculateRewards(scorePercentage);
            }
            
           
            
            // Get appropriate quote based on performance
            string resultQuote = currentGame.GetQuoteForPerformance(scorePercentage, timedOut);
            
            // Determine result title
            string resultTitle;
            if (scorePercentage >= 1f)
                resultTitle = "Perfect Job!";
            else if (scorePercentage >= 0.8f)
                resultTitle = "Grand Job!";
            else if (scorePercentage >= 0.6f)
                resultTitle = "Not Bad!";
            else
                resultTitle = "Ah, Feck!";
            
            // Display results
            Debug.Log($"=== {currentGame.gameName} Results ===");
            Debug.Log($"{resultTitle}");
            Debug.Log($"Final Score: {finalScore} (Base: {currentScore})");
            Debug.Log($"Performance: {scorePercentage:P0}");
            Debug.Log($"Time Remaining: {timeRemaining:F1}s");
            Debug.Log($"Earnings: £{earnings:F0}");
            Debug.Log($"Stamina Gained: {attribute}");
            if (!string.IsNullOrEmpty(resultQuote))
            {
                Debug.Log($"Quote: \"{resultQuote}\"");
            }
            
            // Play result sound and particle effects
            AudioClip soundToPlay = scorePercentage >= 0.6f ? currentGame.successSound : currentGame.failureSound;
            if (soundToPlay != null && audioSource != null)
            {
                audioSource.PlayOneShot(soundToPlay);
            }
            
            // Spawn success particle effect if available and performance was good
            if (scorePercentage >= 0.8f && currentGame.successParticleEffect != null)
            {
                GameObject particles = Instantiate(currentGame.successParticleEffect, transform.position, Quaternion.identity);
                Destroy(particles, 5f); // Clean up after 5 seconds
            }
            
            // If we loaded a scene for this minigame, return to the main scene
            if (currentGame.CanLoadScene && !string.IsNullOrEmpty(currentGame.returnSceneName))
            {
                StartCoroutine(ReturnToMainSceneAfterDelay(currentGame.returnDelay)); // Give time to see results
            }
            
            // Call GameManager.PlayerWorked() to handle additional logic like time updates and energy costs
            if (GameManager.Instance != null)
            {
                if (currentGame.category == ActivityCategory.Work)
                {
                    // For work activities, use the GameManager method but with our calculated earnings
                    // This ensures time updates and proper energy deduction
                    GameManager.Instance.PlayerWorked((int)earnings, 0); // 0 energy cost since we already deducted it
                }
                else if( currentGame.category == ActivityCategory.Training)
                {
                    
                    // For training activities, deduct energy cost here
                    if (PlayerManager.Instance != null)
                    {
                        PlayerManager.Instance.ModifyPlayerEnergy(-currentGame.energyCost);
                    }
                    if(selectedTeamMember != null )
                    {
                        // Update time via GameManager
                        GameManager.Instance.PlayerTrained(selectedTeamMember, selectedTrainingStatType, attribute);    
                    }
                    else
                    {
                        Debug.LogWarning("No team member or stat type selected for training.");
                    }
                  
                  
                }
            }
        }
        
        private void PlayerFailedMiniGame()
        {
            Debug.Log("Player failed the mini game.");
            
            // If we loaded a scene for this minigame, return to the main scene
            if (currentGame.CanLoadScene && !string.IsNullOrEmpty(currentGame.returnSceneName))
            {
                StartCoroutine(ReturnToMainSceneAfterDelay(currentGame.returnDelay)); // Give time to see results
            }

            if (GameManager.Instance != null)
            {
                if (currentGame.category == ActivityCategory.Work)
                {
                    // For work activities, use the GameManager method but with our calculated earnings
                    // This ensures time updates and proper energy deduction
                    PlayerStatsView.Instance.DisplayInfo("You Didn't work Hard Enough\nThe Boss is pissed\nYou have not gotten paid", 3);
                }
                else if( currentGame.category == ActivityCategory.Training)
                {
                    
                    // For training activities, deduct energy cost here
                    if (PlayerManager.Instance != null)
                    {
                        PlayerManager.Instance.ModifyPlayerEnergy(-currentGame.energyCost);
                    }
                    if(selectedTeamMember != null )
                    {
                        // Update time via GameManager
                        Debug.LogWarning(selectedTeamMember.memberName + " has failed to improve at " + selectedTrainingStatType);
                        PlayerStatsView.Instance.DisplayInfo($"You Failed\n{selectedTeamMember.memberName} has not improved at {selectedTrainingStatType}", 3);
                    }
                    else
                    {
                        PlayerStatsView.Instance.DisplayInfo($"No Stats have changed", 3);

                    }
                  
                  
                }
            }
            
           
            
        }
        
        private IEnumerator ReturnToMainSceneAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            Debug.Log($"Returning to scene: {currentGame.returnSceneName}");
            SceneManager.LoadScene(currentGame.returnSceneName);
            
            // Clean up after returning
            currentGame = null;
            currentGameInstance = null;
            gameActive = false;
            GameManager.Instance.SetPlayerBusy(false);
        }

        public bool ReturnGameActiveBool() 
        { 
            return gameActive;
        }

        public bool HasMiniGameOfStatType(TeamMember.StatType statType)
        {
            foreach (var activity in trainingActivities)
            {
                if (activity.trainingAttribute == statType)
                {
                    return true;
                }
            }
            return false;
        }
        
    }
}
