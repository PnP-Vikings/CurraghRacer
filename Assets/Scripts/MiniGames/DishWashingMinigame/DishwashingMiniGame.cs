using UnityEngine;
using MiniGames;

namespace MiniGames.DishWashingMinigame
{
    public class DishwashingMiniGame : MonoBehaviour, MiniGame
    {
        private MiniGameManager gameManager;
        private MiniGameData gameData;
        private DishwashingController controller;
        private int initialPlateCount;
        private bool gameCompleted;

        public void Initialize(MiniGameManager manager, MiniGameData data)
        {
            gameManager = manager;
            gameData = data;
            controller = FindFirstObjectByType<DishwashingController>();
            
            if (controller == null)
            {
                Debug.LogError("DishwashingController not found!");
                return;
            }

            // Store initial plate count for scoring
            initialPlateCount = controller.plates.Count;
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

            // Update score based on plates cleaned
            int platesCleaned = controller.platesCleaned.Count;
            int currentScore = Mathf.RoundToInt((float)platesCleaned / initialPlateCount * gameData.perfectScore);
            
            // Update the manager with current progress
            gameManager.UpdateScore(currentScore);
            gameManager.UpdateProgress($"Plates Cleaned: {platesCleaned}/{initialPlateCount}");

            // Check if game is completed
            if (platesCleaned >= initialPlateCount && !gameCompleted)
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
            return Mathf.RoundToInt((float)controller.platesCleaned.Count / initialPlateCount * gameData.perfectScore);
        }

        public bool IsGameComplete()
        {
            return controller != null && controller.platesCleaned.Count >= initialPlateCount;
        }
    }
}
