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
        private float gameTimer;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI grassText;
        public bool isGrass;

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
            isGrass = true;
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

            if (gameTimer >= 30 && isGrass == true) 
            {
                isGrass = false;
                gameTimer = 0;
                timerText.text = gameTimer.ToString("F2");
                grassText.text = "Collect the grass";
            }

            if (gameTimer >= 30 && isGrass == false)
            {
                EndGame();
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
