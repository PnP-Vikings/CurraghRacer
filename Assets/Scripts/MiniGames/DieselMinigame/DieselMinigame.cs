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
        public Slider dieselSlider;
        public Image dieselSliderFill;
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
            if (dieselAmount > 0 && dieselAmount < 10 && timer < 10)
            {
                dieselAmount -= Time.deltaTime;
            }

            dieselText.text = dieselAmount.ToString();
            timerText.text = timer.ToString();
            dieselSlider.value = dieselAmount;
            //dieselSliderFill.color = Color.Lerp(Color.green, Color.red, dieselAmount / 10);

            if (dieselSlider.value > 0 && dieselSlider.value < 5)
            {
                dieselSliderFill.color = Color.yellow;
            }

            else if (dieselSlider.value >= 5 && dieselSlider.value <= 7)
            {
                dieselSliderFill.color = Color.green;
            }

            else
            {
                dieselSliderFill.color = Color.red;
            }

            if (dieselAmount >= 10)
            {
                EndGame();
            }

            if (dieselAmount < 0)
            {
                dieselAmount = 0;
            }

            if (timer < 10 && dieselAmount < 10)
            {
                timer += Time.deltaTime;
            }
            
            if (timer >= 10)
            {
                timer = 10;
                timerText.text = timer.ToString();
                EndGame();
            }
        }

        public void EndGame()
        {
            if (dieselAmount > 7f)
            {
                resultText.text = "Too much pressure";
            }

            else if (dieselAmount < 5f)
            {
                resultText.text = "Not enough pressure";
            }

            else
            {
                resultText.text = "Minigame won";
            }

            dieselButton.interactable = false;
        }

        public void DieselPump()
        {
            dieselAmount++;
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
