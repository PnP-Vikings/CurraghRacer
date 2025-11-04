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
        private bool buttonActive;
        public Button dieselButton;
        public TextMeshProUGUI dieselText;
        public TextMeshProUGUI resultText;

        public void Initialize(MiniGameManager manager, MiniGameData data)
        {
            gameManager = manager;
            gameData = data;
            dieselAmount = 0;
            buttonActive = false;
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
            if (buttonActive == true)
            {
                dieselAmount += Time.deltaTime;
            }

            dieselText.text = dieselAmount.ToString();

            if (dieselAmount > 10f)
            {
                resultText.text = "Too much pressure";
                buttonActive = false;
            }
        }

        public void EndGame()
        {
            
        }

        public void DieselPump()
        {
            if (buttonActive == false)
            {
                buttonActive = true;
            }

            else
            {
                buttonActive = false;
            }

            Debug.Log("Button Pressed");
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
