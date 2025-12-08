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
        private float dieselPressure;
        private float dieselAmount;
        private float timer;
        public Button dieselButton;
        public Slider dieselSlider;
        public Image dieselSliderFill;
        public Slider dieselAmountSlider;
        public Image dieselAmountSliderFill;
        public TextMeshProUGUI dieselText;
        public TextMeshProUGUI dieselAmountText;
        public TextMeshProUGUI resultText;
        public TextMeshProUGUI timerText;

        public void Initialize(MiniGameManager manager, MiniGameData data)
        {
            gameManager = manager;
            gameData = data;
            dieselPressure = 0;
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
            if (dieselPressure > 0 && dieselPressure < 10 && timer < 10)
            {
                dieselPressure -= Time.deltaTime;
            }

            dieselText.text = dieselPressure.ToString();
            dieselAmountText.text = dieselAmount.ToString();
            timerText.text = timer.ToString();
            dieselSlider.value = dieselPressure;
            dieselAmountSlider.value = dieselAmount;
            dieselAmountSliderFill.color = Color.Lerp(Color.red, Color.green, dieselAmount / 20);

            if (dieselSlider.value >= 5 && dieselSlider.value <= 7)
            {
                dieselSliderFill.color = Color.green;

                if (timer < 10)
                {
                    dieselAmount += Time.deltaTime;
                }
            }

            else
            {
                dieselSliderFill.color = Color.red;
            }

            if (dieselPressure >= 10)
            {
                EndGame();
            }

            if (dieselPressure < 0)
            {
                dieselPressure = 0;
            }

            if (timer < 10 && dieselPressure < 10)
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
            if (dieselPressure >= 10f)
            {
                resultText.text = "Too much pressure";
            }

            else
            {
                resultText.text = "Minigame done";
            }

            dieselButton.interactable = false;
        }

        public void DieselPump()
        {
            dieselPressure++;
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
