using System.Collections;
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
        
        switch (selectedStatType)
        {
            case TeamMember.StatType.Strength:
                if(CanTrain(30, 50))
                {
                    PlayerManager.Instance.ModifyTeamMemberStat(selectedTeamMember, TeamMember.StatType.Strength, strengthGainAmount);
                    PlayerManager.Instance.ModifyPlayerStrength(strengthGainAmount);

                    if (AudioManager.instance != null)
                    {
                        AudioManager.instance.dumbbell.start();
                    }
                }
                break;
            case TeamMember.StatType.Technique:
                if(CanTrain(30, 50))
                {
                    PlayerManager.Instance.ModifyTeamMemberStat(selectedTeamMember, TeamMember.StatType.Technique, techniqueGainAmount);
                }
                break;
            case TeamMember.StatType.Stamina:
                if(CanTrain(30, 50))
                {
                    PlayerManager.Instance.ModifyTeamMemberStat(selectedTeamMember, TeamMember.StatType.Stamina, staminaGainAmount);
                }
                break;
            case TeamMember.StatType.TeamWork:
                if(CanTrain(30, 50))
                {
                    PlayerManager.Instance.ModifyTeamMemberStat(selectedTeamMember, TeamMember.StatType.TeamWork, teamWorkGainAmount);
                }
                break;
            default:
                Debug.LogError("Invalid stat type selected for training.");
                if(PlayerStatsView.Instance != null)
                    PlayerStatsView.Instance.DisplayInfo("Invalid stat type selected for training.", 3);
                break;
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
    
    public void OnTrainStrengthButtonClicked()
    {
       if(CanTrain(30, 50))
        {
            PlayerManager.Instance.ModifyPlayerStrength(strengthGainAmount);
            if (AudioManager.instance != null)
            {
                AudioManager.instance.dumbbell.start();
            } 
        }
        
     
        
    }

    public void OnTrainTechniqueButtonClicked()
    {
        
        if (CanTrain(30, 50))
        {
                PlayerManager.Instance.ModifyPlayerTechnique(techniqueGainAmount);
        }
    }
    
    public void OnTrainStaminaButtonClicked()
    {
        if(CanTrain(30, 50))
        {
            PlayerManager.Instance.ModifyPlayerStamina(staminaGainAmount);
        }

        
       
       
    }
    public void OnTrainTeamWorkButtonClicked()
    { 
        if(CanTrain(30, 50))
        {
            PlayerManager.Instance.ModifyPlayerTeamWork(teamWorkGainAmount);
            
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
        // Check if the player has enough energy
        if (!PlayerManager.Instance.PlayerHasEnoughEnergy(energyCost))
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
        TimeManager.Instance.AdvanceTimeByHours(3); // Advance time by 3 hour
        Debug.Log($"Player has enough energy {PlayerManager.Instance.energy}  and currency {PlayerManager.Instance.coins} to train");
        return true;
    }
    
    private void OnDisable()
    {
        ClearTeamMemberUis();
    }
    
    
}