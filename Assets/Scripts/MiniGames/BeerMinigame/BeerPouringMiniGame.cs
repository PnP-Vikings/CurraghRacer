using UnityEngine;
using MiniGames;

namespace MiniGames.BeerMinigame
{
    public class BeerPouringMiniGame : MonoBehaviour, MiniGame
    {
        private MiniGameManager gameManager;
        private MiniGameData gameData;
        private BeerGameController controller;
        private int targetBeerCount = 5;
        private bool gameCompleted;

        public void Initialize(MiniGameManager manager, MiniGameData data)
        {
            gameManager = manager;
            gameData = data;
            controller = FindFirstObjectByType<BeerGameController>();
            
            if (controller == null)
            {
                Debug.LogError("BeerGameController not found!");
            }
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

            // Update score based on beers completed
            int beersCompleted = controller.Completedbeers.Count;
            int currentScore = Mathf.RoundToInt((float)beersCompleted / targetBeerCount * gameData.perfectScore);
            
            // Update the manager with current progress
            gameManager.UpdateScore(currentScore);
            gameManager.UpdateProgress($"Beers Poured: {beersCompleted}/{targetBeerCount}");

            // Check if game is completed
            if (beersCompleted >= targetBeerCount && !gameCompleted)
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
            return Mathf.RoundToInt((float)controller.Completedbeers.Count / targetBeerCount * gameData.perfectScore);
        }

        public bool IsGameComplete()
        {
            return controller != null && controller.Completedbeers.Count >= targetBeerCount;
        }
    }
}
