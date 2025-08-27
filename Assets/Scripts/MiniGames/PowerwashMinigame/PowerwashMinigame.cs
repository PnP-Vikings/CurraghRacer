using UnityEngine;
using MiniGames;

namespace MiniGames.PowerwashMinigame
{
    public class PowerwashMiniGame : MonoBehaviour, MiniGame
    {
        private MiniGameManager gameManager;
        private MiniGameData gameData;
        private PowerwashController controller;
        private int initialWallCount;
        private bool gameCompleted;

        public void Initialize(MiniGameManager manager, MiniGameData data)
        {
            gameManager = manager;
            gameData = data;
            controller = FindFirstObjectByType<PowerwashController>();
            
            if (controller == null)
            {
                Debug.LogError("PowerwashController not found!");
                return;
            }

            // Store initial wall count for scoring
            initialWallCount = controller.walls.Count;
        }

        public void StartGame()
        {
            if (controller != null)
            {
                controller.enabled = true;
                gameCompleted = false;
            }
        }

        public void UpdateGame()
        {
            if (controller == null || gameCompleted) return;

            // Update score based on walls cleaned
            int wallsCleaned = controller.wallsCleaned.Count;
            int currentScore = Mathf.RoundToInt((float)wallsCleaned / initialWallCount * gameData.perfectScore);
            
            // Update the manager with current progress
            gameManager.UpdateScore(currentScore);
            gameManager.UpdateProgress($"walls Cleaned: {wallsCleaned}/{initialWallCount}");

            // Check if game is completed
            if (wallsCleaned >= initialWallCount && !gameCompleted)
            {
                gameCompleted = true;
                gameManager.CompleteGame(currentScore);
            }
        }

        public void EndGame()
        {
            if (controller != null)
            {
                controller.enabled = false;
            }
        }

        public int GetCurrentScore()
        {
            if (controller == null) return 0;
            return Mathf.RoundToInt((float)controller.wallsCleaned.Count / initialWallCount * gameData.perfectScore);
        }

        public bool IsGameComplete()
        {
            return controller != null && controller.wallsCleaned.Count >= initialWallCount;
        }
    }
}
