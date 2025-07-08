using UnityEngine;
using UnityEngine.UIElements;

public class TrainingMenu : MonoBehaviour
{
    [SerializeField] private UIDocument uiDoc;
    private Button _trainStrengthButton, _trainTechniqueButton, _trainStaminaButton, _trainTeamWorkButton,_backButton;
    [SerializeField] private GameObject startingMenuPrefab;

    FMOD.Studio.EventInstance Dumbbell;
    FMOD.Studio.EventInstance UIClick2;

    void OnEnable()
    {
        uiDoc = GetComponent<UIDocument>();

        var root = uiDoc.rootVisualElement;
        _trainStrengthButton = root.Q<Button>("TrainStrength");
        _trainTechniqueButton = root.Q<Button>("TrainTechnique");
        _trainStaminaButton = root.Q<Button>("TrainStamina");
        _trainTeamWorkButton = root.Q<Button>("TrainTeamWork");
        _backButton = root.Q<Button>("BackButton");

        _trainStrengthButton.clicked += OnTrainStrengthButtonClicked;
        _trainTechniqueButton.clicked += OnTrainTechniqueButtonClicked;
        _trainStaminaButton.clicked += OnTrainStaminaButtonClicked;
        _trainTeamWorkButton.clicked += OnTrainTeamWorkButtonClicked;
        _backButton.clicked += OnCloseTrainingMenuButtonClicked;
    }

    public void OnTrainStrengthButtonClicked()
    {
        int amountGain = 10; // Define the  gain amount
       if(CanTrain(15, 50))
        {
           
       
            PlayerManager.Instance.ModifyPlayerStrength(amountGain);
            PlayerStatsView.Instance.DisplayInfo($"You gained {amountGain} Strength", 3);

            Dumbbell = FMODUnity.RuntimeManager.CreateInstance("event:/Training/Dumbbell");
            Dumbbell.start();
        }
        
     
        
    }
    public void OnTrainTechniqueButtonClicked()
    { int amountGain = 10; // Define the  gain amount
        if(CanTrain(15, 50))
        {
            PlayerManager.Instance.ModifyPlayerTechnique(amountGain);
            PlayerStatsView.Instance.DisplayInfo($"You gained {amountGain} Technique", 3);
        }

        
       
       
       
      
    

    }
    public void OnTrainStaminaButtonClicked()
    {
        int amountGain = 10; // Define the  gain amount
        if(CanTrain(15, 50))
        {
            PlayerManager.Instance.ModifyPlayerStamina(10);
            PlayerStatsView.Instance.DisplayInfo($"You gained {amountGain} Stamina", 3);
        }

        
       
       
    }
    public void OnTrainTeamWorkButtonClicked()
    { 
        int amountGain = 10; // Define the  gain amount
        if(CanTrain(15, 50))
        {
            PlayerManager.Instance.ModifyPlayerTeamWork(10);
            PlayerStatsView.Instance.DisplayInfo($"You gained {amountGain} TeamWork", 3);
        }
            
    }
    
    public void OnCloseTrainingMenuButtonClicked()
    {
        startingMenuPrefab.SetActive(true);
        uiDoc.gameObject.SetActive(false);

        UIClick2 = FMODUnity.RuntimeManager.CreateInstance("event:/UI/Click 2");
        UIClick2.start();
    }
    
    public bool CanTrain(int energyCost, int currencyCost)
    {
        // Check if the player has enough energy
        if (!PlayerManager.Instance.playerHasEnoughEnergy(energyCost))
        {
            PlayerStatsView.Instance.DisplayInfo($"You must have at least {energyCost} Energy to train", 3);
            return false; // Not enough energy
        }

        // Check if the player has enough currency
        if (!PlayerManager.Instance.PurchaseItem(currencyCost))
        {
            PlayerStatsView.Instance.DisplayInfo($"You must have at least {currencyCost} Currency to train", 3);
            return false; // Not enough currency
        }

        // Deduct the currency cost and allow training
        PlayerManager.Instance.ModifyPlayerEnergy(-energyCost);
        Debug.Log("Player has enough energy and currency to train");
        return true;
    }
    
    private void OnDisable()
    {
        _trainStrengthButton.clicked -= OnTrainStrengthButtonClicked;
        _trainTechniqueButton.clicked -= OnTrainTechniqueButtonClicked;
        _trainStaminaButton.clicked -= OnTrainStaminaButtonClicked;
        _trainTeamWorkButton.clicked -= OnTrainTeamWorkButtonClicked;
        _backButton.clicked -= OnCloseTrainingMenuButtonClicked;
    }
    
    
}