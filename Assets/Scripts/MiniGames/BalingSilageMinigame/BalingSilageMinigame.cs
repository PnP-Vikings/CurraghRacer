using UnityEngine;
using MiniGames;
using UnityEngine.UI;
using TMPro;

namespace MiniGames.BalingSilageMinigame
{
    public class BalingSilageMinigame : MonoBehaviour, MiniGame
    {
        private MiniGameManager gameManager;
        private MiniGameData gameData;

        public void Initialize(MiniGameManager manager, MiniGameData data)
        {
            gameManager = manager;
            gameData = data;
        }

        public void StartGame()
        {
            
        }

        public void Update()
        {
            UpdateGame();
        }

        public void UpdateGame()
        {
            
        }

        public void EndGame()
        {
            
        }

        public int GetCurrentScore()
        {
            return 1;
        }

        public bool IsGameComplete()
        {
            return true;
        }
    }
}
