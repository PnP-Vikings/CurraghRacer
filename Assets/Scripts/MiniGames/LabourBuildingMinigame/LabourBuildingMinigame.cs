using UnityEngine;
using MiniGames;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

namespace MiniGames.LabourBuildingMinigame
{
    public class LabourBuildingMinigame : MonoBehaviour, MiniGame
    {
        private MiniGameManager gameManager;
        private MiniGameData gameData;
        public PlayerObject player;
        private float score;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI gameText;

        public void Initialize(MiniGameManager manager, MiniGameData data)
        {
            gameManager = manager;
            gameData = data;

        }

        public void StartGame()
        {
            
        }

        public void Start()
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
