using System.Collections;
using MiniGames;
using UnityEngine;
using UnityEngine.UIElements;

public class TrainingMenu : MonoBehaviour
{
    public UnityEngine.UI.Button _trainStrengthButton, _trainTechniqueButton, _trainStaminaButton, _trainTeamWorkButton,_backButton;
    public UnityEngine.UI.Button _selectTeamMemberButton, _trainSelectedMemberButton;
    [SerializeField] private GameObject startingMenuPrefab;
    public int strengthGainAmount = 2;
    public int techniqueGainAmount = 2;
    public int staminaGainAmount = 2;
    public int teamWorkGainAmount = 2;
    public GameObject teamMemberSelectionMenuPrefab;
    public TeamMemberUiHandler teamManagerUiPrefab;
    public Transform teamManagerUiParent;
    public TeamMember.StatType selectedStatType;
    
    public TrainingSelectionUi oldSelectedTeamMemberUi, newSelectedTeamMemberUi;
    public TeamMember selectedTeamMember;
    
    public bool isTooLateForActivities = false;
    //FMOD.Studio.EventInstance Dumbbell;
    //FMOD.Studio.EventInstance UIClick2;

    void OnEnable()
    {
        if(_trainStrengthButton != null)
            _trainStrengthButton.onClick.AddListener(() => OnClickSelectTeamMemberForTraining(TeamMember.StatType.Strength));
        if(_trainTechniqueButton != null)
            _trainTechniqueButton.onClick.AddListener(() => OnClickSelectTeamMemberForTraining(TeamMember.StatType.Technique));
        if(_trainStaminaButton != null)
            _trainStaminaButton.onClick.AddListener(() => OnClickSelectTeamMemberForTraining(TeamMember.StatType.Stamina));
        if(_trainTeamWorkButton != null)
            _trainTeamWorkButton.onClick.AddListener(() => OnClickSelectTeamMemberForTraining(TeamMember.StatType.TeamWork));
        if(_trainSelectedMemberButton != null)
            _trainSelectedMemberButton.onClick.AddListener(TrainSelectedTeamMember);
         _trainSelectedMemberButton.interactable = false;
    }
    
    public void SetSelectedTeamMember(TeamMember member, TrainingSelectionUi uiHandler)
    {
        selectedTeamMember = member;
        if(oldSelectedTeamMemberUi != null)
        {
            oldSelectedTeamMemberUi.HideSelectionUi();
        }
        oldSelectedTeamMemberUi = uiHandler;
        
        if(selectedTeamMember != null)
        {
            _trainSelectedMemberButton.interactable = true;
        }
        else
        {
            _trainSelectedMemberButton.interactable = false;
        }
      
    }
    
    public void TrainSelectedTeamMember()
    {
        if(selectedTeamMember == null)
        {
            Debug.LogError("No team member selected for training.");
            return;
        }
        
        if (CanTrain(30, 50))
        {
            if (AudioManager.instance != null)
            {
                AudioManager.instance.dumbbell.start();
            } 
            
            ManageClickBasedOnStatType(selectedStatType);

        }
        // Reset selection after training
        selectedTeamMember = null;
        if(oldSelectedTeamMemberUi != null)
        {
            oldSelectedTeamMemberUi.HideSelectionUi();
            oldSelectedTeamMemberUi = null;
        }
        
        /*// Close the selection menu
        if (teamMemberSelectionMenuPrefab != null)
        {
            teamMemberSelectionMenuPrefab.SetActive(false);
        }*/
        
        // Refresh the main training menu UI
        RefreshManagerUi();
    }
    
    public void OnClickSelectTeamMemberForTraining(TeamMember.StatType statType)
    {
        this.selectedStatType = statType;
        if (teamMemberSelectionMenuPrefab != null)
        {
            teamMemberSelectionMenuPrefab.SetActive(true);
            ClearTeamMemberUis();
            RefreshManagerUi();

        }
    }

    public void ManageClickBasedOnStatType(TeamMember.StatType statType)
    {
        if(MiniGameManager.Instance != null)
        {
            MiniGameManager.Instance.StartRandomTrainingActivityBasedOnStatType(statType,selectedTeamMember);
        }
    }
    
     public void RefreshManagerUi()
   {
        ClearTeamMemberUis();
        
         if (teamManagerUiParent != null && teamManagerUiPrefab != null)
         {
             if (TeamManager.Instance != null)
             {
                 if (TeamManager.Instance.activeCrewMembers == null || TeamManager.Instance.activeCrewMembers.Count == 0)
                 {
                     Debug.Log("TeamManagerUi: No crew members to display.");
                     return;
                 }
                 foreach (TeamMember crewMember in TeamManager.Instance.activeCrewMembers)
                 {
                     Debug.Log("TeamManagerUi: Creating UI for crew member: " + crewMember.memberName);
                     if(crewMember.racesAvailableFor <100) return;
                     GameObject crewMemberUi = Instantiate(teamManagerUiPrefab.gameObject, teamManagerUiParent);
                     if (crewMemberUi != null)
                     {
                         TeamMemberUiHandler handler = crewMemberUi.GetComponent<TeamMemberUiHandler>();
                         if (handler != null)
                         {
                             handler.ClearMemberData();
                             handler.SetMemberData(crewMember);
                         }
                         TrainingSelectionUi selectionUi = crewMemberUi.GetComponent<TrainingSelectionUi>();
                         if(selectionUi != null)
                             selectionUi.SetTrainingMenu(this, crewMember);
                     }
                 }
                 foreach (TeamMember benchMember in TeamManager.Instance.benchTeamMembers)
                 {
                     Debug.Log("TeamManagerUi: Creating UI for crew member: " + benchMember.memberName);
                     if(benchMember.racesAvailableFor <100) return;
                     GameObject crewMemberUi = Instantiate(teamManagerUiPrefab.gameObject, teamManagerUiParent);
                     if (crewMemberUi != null)
                     {
                         TeamMemberUiHandler handler = crewMemberUi.GetComponent<TeamMemberUiHandler>();
                         if (handler != null)
                         {
                             handler.ClearMemberData();
                             handler.SetMemberData(benchMember);
                         }
                         TrainingSelectionUi selectionUi = crewMemberUi.GetComponent<TrainingSelectionUi>();
                         if(selectionUi != null)
                             selectionUi.SetTrainingMenu(this, benchMember);
                     }
                 }
               
             }
             else
             {
                 Debug.LogError("TeamManagerUi: TeamManager instance is null.");
                 return;
             }
         }
   }
    

    public void ClearTeamMemberUis()
    {
        foreach (Transform child in teamManagerUiParent)
        {
            Destroy(child.gameObject);
        }
    }
    
    
    public void OnBackButtonClickedOnSelection()
    {
        selectedTeamMember = null;
        if(oldSelectedTeamMemberUi != null)
        {
            oldSelectedTeamMemberUi.HideSelectionUi();
            oldSelectedTeamMemberUi = null;
        }
        ClearTeamMemberUis();
        if (teamMemberSelectionMenuPrefab != null)
        {
            teamMemberSelectionMenuPrefab.SetActive(false);
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.UIClick2.start();
        }
    }
    public void OnCloseTrainingMenuButtonClicked()
    {
        startingMenuPrefab.SetActive(true);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.UIClick2.start();
        }
    }
    
    public bool CanTrain(int energyCost, int currencyCost)
    {
        if (TimeManager.Instance != null)
        {
            isTooLateForActivities = TimeManager.Instance.IsTooLateForActivities();
            
            if (isTooLateForActivities)
            {
                PlayerStatsView.Instance.DisplayInfo("It's too late to Train today. Try again tomorrow.", 3);
                return false;
            }
        }
        
        if(MiniGameManager.Instance != null)
        {
            if (!MiniGameManager.Instance.HasMiniGameOfStatType(selectedStatType))
            {
                PlayerStatsView.Instance.DisplayInfo($"No mini-game available to train {selectedStatType}", 3, Color.red);
                return false;
            }
        }
    
        // Check if the player has enough energy
        if (!PlayerManager.Instance.PlayerHasEnoughEnergy(energyCost))
        {
            PlayerStatsView.Instance.DisplayInfo($"You must have at least {energyCost} Energy to train", 3);
            return false;
        }

        // Check if the player has enough currency WITHOUT deducting yet
        if (PlayerManager.Instance.GetPlayerCoins() < currencyCost)
        {
            PlayerStatsView.Instance.DisplayInfo($"You must have at least {currencyCost} Currency to train", 3);
            return false;
        }

        // Now deduct both currency and energy
        PlayerManager.Instance.PurchaseItem(currencyCost);
        PlayerManager.Instance.ModifyPlayerEnergy(-energyCost);
        
        if(TimeManager.Instance != null){
            if(TimeManager.Instance.IsNight()){
                PlayerStatsView.Instance.DisplayInfo("It's getting late, consider resting soon.", 3, Color.yellow);
            }
            
            if(!TimeManager.Instance.realtimeDayDurationEnabled()) 
            {TimeManager.Instance.AdvanceTimeByHours(3);}
        }
       
    
        Debug.Log($"Player has enough energy {PlayerManager.Instance.energy} and currency {PlayerManager.Instance.coins} to train");
        return true;
    }

    
    private void OnDisable()
    {
        ClearTeamMemberUis();
    }
    
    
}