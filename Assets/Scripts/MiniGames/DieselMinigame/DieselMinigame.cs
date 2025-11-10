using UnityEngine;
using MiniGames;
using UnityEngine.UI;
using TMPro;

namespace MiniGames.DieselMinigame
{
    public class DieselMiniGame : MonoBehaviour, MiniGame
    {
        private MiniGameManager gameManager;
        private MiniGameData gameData;
        private float dieselAmount;
        private float timer;
        public Button dieselButton;
        public TextMeshProUGUI dieselText;
        public TextMeshProUGUI resultText;
        public TextMeshProUGUI timerText;

        public void Initialize(MiniGameManager manager, MiniGameData data)
        {
            gameManager = manager;
            gameData = data;
            dieselAmount = 0;
            dieselButton.interactable = true;
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
            if (dieselAmount > 0 && timer < 10)
            {
                dieselAmount -= Time.deltaTime;
            }
            dieselText.text = dieselAmount.ToString();
            timerText.text = timer.ToString();

            if (timer < 10)
            {
                timer += Time.deltaTime;
            }
            
            if (timer >= 10)
            {
                dieselButton.interactable = false;
                timer = 10;
                EndGame();
            }
        }

        public void EndGame()
        {
            if (dieselAmount >= 8f)
            {
                resultText.text = "Too much pressure";
            }

            else if(dieselAmount <= 2f)
            {
                resultText.text = "Not enough pressure";
            }

            else
            {
                resultText.text = "Minigame won";
            }
        }

        public void DieselPump()
        {
            if (dieselButton.interactable == true)
            {
                if (dieselAmount < 10)
                {
                    dieselAmount++;
                    Debug.Log("Button Pressed");
                }
            }
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
