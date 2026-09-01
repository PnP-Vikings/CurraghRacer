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
            Instantiate(tractorPrefab, new Vector3(-2f, 0, 5f), Quaternion.Euler(0, 90, 0));
            cutting = true;
            grassText.text = "Cut the grass";
            gameTimer = 180;
        }

        public void Update()
        {
            UpdateGame();
        }

        public void UpdateGame()
        {
            if (collecting == true && gameTimer > 90)
            {
                grassText.text = "Collect the grass";
            }

            if (grassCounter >= 10)
            {
                grassCounter = 0;
                score++;
                scoreText.text = score.ToString();
            }

            gameTimer-= Time.deltaTime;

            if (gameTimer <= 90)
            {
                grassText.text = "Hurry and collect the grass";
            }

            if (FindFirstObjectByType<Tractor>().wallHit == true)
            {
                gameTimer = 0;
                grassText.text = "Tractor crashed into fence. Game Over";
                EndGame();
            }

            if (gameTimer <= 0 && FindFirstObjectByType<Tractor>().wallHit == false)
            {
                gameTimer = 0;
                grassText.text = "Game completed";
                EndGame();
            }
        }

        public void EndGame()
        {
            collecting = false;
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
