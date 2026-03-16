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
        private float gameTimer;
        public TextMeshProUGUI timerText;
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
        }

        public void Update()
        {
            UpdateGame();
        }

        public void UpdateGame()
        {
            gameTimer += Time.deltaTime;
            if (gameTimer <= 30)
            {
                timerText.text = gameTimer.ToString("F2");
            }

            if (gameTimer >= 30 && cutting == true) 
            {
                cutting = false;
                gameTimer = 30;
                timerText.text = gameTimer.ToString("F2");
                grassText.text = "Attach the collector";
            }

            if (collecting == true)
            {
                grassText.text = "Collect the grass";
            }
        }

        public void EndGame()
        {
            gameTimer = 30;
            timerText.text = gameTimer.ToString();
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
