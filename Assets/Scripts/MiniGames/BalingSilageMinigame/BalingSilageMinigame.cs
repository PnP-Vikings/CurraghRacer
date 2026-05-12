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
        public Tractor tractorPrefab;
        public GameObject Collector;
        private float score;
        public float grassCounter;
        public float gameTimer;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI grassText;
        public bool cutting;
        public bool collecting;

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
            Instantiate(tractorPrefab, new Vector3(0, 0, -0.5f), Quaternion.identity);
            cutting = true;
            grassText.text = "Cut the grass";
            gameTimer = 60;
        }

        public void Update()
        {
            UpdateGame();
        }

        public void UpdateGame()
        {
            if (collecting == true && gameTimer > 30)
            {
                grassText.text = "Collect the grass";
            }

            if (grassCounter > 3)
            {
                grassCounter = 0;
                score++;
                scoreText.text = score.ToString();
            }

            gameTimer-= Time.deltaTime;

            if (gameTimer <= 30)
            {
                grassText.text = "Hurry and collect the grass";
            }

            if (gameTimer <= 0)
            {
                gameTimer = 0;
                EndGame();
            }
        }

        public void EndGame()
        {
            collecting = false;
            grassText.text = "Game completed";
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
