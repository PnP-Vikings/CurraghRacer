// Assets/Scripts/MiniGames/IrishMiniGameManager.cs
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;

public class MiniGameManager : MonoBehaviour
{
    [Header("Game Collections")]
    [SerializeField] private List<MiniGameData> workActivities;
    [SerializeField] private List<MiniGameData> trainingActivities;
    
    [Header("UI Documents")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset miniGameSelectionTemplate;
    [SerializeField] private VisualTreeAsset gameplayTemplate;
    [SerializeField] private VisualTreeAsset resultsTemplate;
    [SerializeField] private VisualTreeAsset activityButtonTemplate;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    
    // UI Elements
    private VisualElement root;
    private VisualElement activitySelectionPanel;
    private VisualElement gameplayPanel;
    private VisualElement resultsPanel;
    private VisualElement quotesPanel;
    
    // Selection UI
    private Label categoryTitle;
    private VisualElement workContentContainer;
    private VisualElement trainingContentContainer;
    private Button backToMenuButton;
    
    // Gameplay UI
    private Label gameTitle;
    private Label gameInstructions;
    private Label timeDisplay;
    private Label scoreDisplay;
    private Label progressDisplay;
    private Button startGameButton;
    private Button quitGameButton;
    
    // Results UI
    private Label resultsTitle;
    private Label performanceText;
    private Label earningsText;
    private Label staminaGainText;
    private Label quotesText;
    private Button playAgainButton;
    private Button selectNewGameButton;
    private Button exitToMainButton;
    
    // Game state
    private MiniGameData currentGame;
    private MiniGame currentGameInstance;
    private float gameTimer;
    private bool gameActive;
    private int currentScore;
    private PlayerManager playerManager; // Changed from PlayerStats to PlayerManager
    
    public static MiniGameManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        SetupUI();
        playerManager = PlayerManager.Instance; // Use PlayerManager.Instance
        if (playerManager == null)
        {
            playerManager = FindFirstObjectByType<PlayerManager>();
        }
    }
    
    void Update()
    {
        if (gameActive)
        {
            gameTimer -= Time.deltaTime;
            UpdateTimeDisplay();
            
            if (gameTimer <= 0)
            {
                EndGame();
            }
            
            currentGameInstance?.UpdateGame();
        }
    }
    
    private void SetupUI()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UI Document not assigned!");
            return;
        }
        
        root = uiDocument.rootVisualElement;
        CreateUIStructure();
        BindUIElements();
        RegisterCallbacks();
        HideAllPanels();
    }
    
    private void CreateUIStructure()
    {
        // Create main containers
        activitySelectionPanel = new VisualElement();
        activitySelectionPanel.name = "activity-selection-panel";
        activitySelectionPanel.AddToClassList("panel");
        
        gameplayPanel = new VisualElement();
        gameplayPanel.name = "gameplay-panel";
        gameplayPanel.AddToClassList("panel");
        
        resultsPanel = new VisualElement();
        resultsPanel.name = "results-panel";
        resultsPanel.AddToClassList("panel");
        
        quotesPanel = new VisualElement();
        quotesPanel.name = "quotes-panel";
        quotesPanel.AddToClassList("quotes-overlay");
        
        // Add panels to root
        root.Add(activitySelectionPanel);
        root.Add(gameplayPanel);
        root.Add(resultsPanel);
        root.Add(quotesPanel);
        
        CreateSelectionPanel();
        CreateGameplayPanel();
        CreateResultsPanel();
        CreateQuotesPanel();
    }
    
    private void CreateSelectionPanel()
    {
        // Title
        categoryTitle = new Label("Select Activity");
        categoryTitle.name = "category-title";
        categoryTitle.AddToClassList("title");
        activitySelectionPanel.Add(categoryTitle);
        
        // Content containers
        var contentArea = new VisualElement();
        contentArea.name = "content-area";
        contentArea.AddToClassList("content-area");
        
        workContentContainer = new VisualElement();
        workContentContainer.name = "work-content";
        workContentContainer.AddToClassList("activity-grid");
        
        trainingContentContainer = new VisualElement();
        trainingContentContainer.name = "training-content";
        trainingContentContainer.AddToClassList("activity-grid");
        
        contentArea.Add(workContentContainer);
        contentArea.Add(trainingContentContainer);
        activitySelectionPanel.Add(contentArea);
        
        // Back button
        backToMenuButton = new Button();
        backToMenuButton.text = "Back to Menu";
        backToMenuButton.name = "back-button";
        backToMenuButton.AddToClassList("secondary-button");
        activitySelectionPanel.Add(backToMenuButton);
    }
    
    private void CreateGameplayPanel()
    {
        // Game info
        gameTitle = new Label();
        gameTitle.name = "game-title";
        gameTitle.AddToClassList("title");
        gameplayPanel.Add(gameTitle);
        
        gameInstructions = new Label();
        gameInstructions.name = "game-instructions";
        gameInstructions.AddToClassList("instructions");
        gameplayPanel.Add(gameInstructions);
        
        // Game stats
        var statsContainer = new VisualElement();
        statsContainer.name = "stats-container";
        statsContainer.AddToClassList("stats-container");
        
        timeDisplay = new Label();
        timeDisplay.name = "time-display";
        timeDisplay.AddToClassList("stat-display");
        
        scoreDisplay = new Label();
        scoreDisplay.name = "score-display";
        scoreDisplay.AddToClassList("stat-display");
        
        progressDisplay = new Label();
        progressDisplay.name = "progress-display";
        progressDisplay.AddToClassList("stat-display");
        
        statsContainer.Add(timeDisplay);
        statsContainer.Add(scoreDisplay);
        statsContainer.Add(progressDisplay);
        gameplayPanel.Add(statsContainer);
        
        // Game buttons
        var buttonContainer = new VisualElement();
        buttonContainer.name = "button-container";
        buttonContainer.AddToClassList("button-container");
        
        startGameButton = new Button();
        startGameButton.text = "Start Game";
        startGameButton.name = "start-button";
        startGameButton.AddToClassList("primary-button");
        
        quitGameButton = new Button();
        quitGameButton.text = "Quit Game";
        quitGameButton.name = "quit-button";
        quitGameButton.AddToClassList("secondary-button");
        
        buttonContainer.Add(startGameButton);
        buttonContainer.Add(quitGameButton);
        gameplayPanel.Add(buttonContainer);
    }
    
    private void CreateResultsPanel()
    {
        resultsTitle = new Label();
        resultsTitle.name = "results-title";
        resultsTitle.AddToClassList("title");
        resultsPanel.Add(resultsTitle);
        
        performanceText = new Label();
        performanceText.name = "performance-text";
        performanceText.AddToClassList("result-text");
        resultsPanel.Add(performanceText);
        
        earningsText = new Label();
        earningsText.name = "earnings-text";
        earningsText.AddToClassList("result-text");
        resultsPanel.Add(earningsText);
        
        staminaGainText = new Label();
        staminaGainText.name = "stamina-text";
        staminaGainText.AddToClassList("result-text");
        resultsPanel.Add(staminaGainText);
        
        quotesText = new Label();
        quotesText.name = "quotes-text";
        quotesText.AddToClassList("quote-text");
        resultsPanel.Add(quotesText);
        
        // Result buttons
        var resultButtonContainer = new VisualElement();
        resultButtonContainer.name = "result-buttons";
        resultButtonContainer.AddToClassList("button-container");
        
        playAgainButton = new Button();
        playAgainButton.text = "Play Again";
        playAgainButton.name = "play-again-button";
        playAgainButton.AddToClassList("primary-button");
        
        selectNewGameButton = new Button();
        selectNewGameButton.text = "Select New Game";
        selectNewGameButton.name = "select-new-button";
        selectNewGameButton.AddToClassList("secondary-button");
        
        exitToMainButton = new Button();
        exitToMainButton.text = "Exit to Main";
        exitToMainButton.name = "exit-button";
        exitToMainButton.AddToClassList("secondary-button");
        
        resultButtonContainer.Add(playAgainButton);
        resultButtonContainer.Add(selectNewGameButton);
        resultButtonContainer.Add(exitToMainButton);
        resultsPanel.Add(resultButtonContainer);
    }
    
    private void CreateQuotesPanel()
    {
        var quoteContainer = new VisualElement();
        quoteContainer.name = "quote-container";
        quoteContainer.AddToClassList("quote-container");
        
        var quoteText = new Label();
        quoteText.name = "quote-text";
        quoteText.AddToClassList("quote-text");
        
        quoteContainer.Add(quoteText);
        quotesPanel.Add(quoteContainer);
    }
    
    private void BindUIElements()
    {
        // UI elements are already created and referenced above
    }
    
    private void RegisterCallbacks()
    {
        startGameButton.clicked += StartSelectedGame;
        quitGameButton.clicked += QuitCurrentGame;
        backToMenuButton.clicked += CloseActivitySelection;
        playAgainButton.clicked += RestartCurrentGame;
        selectNewGameButton.clicked += BackToSelection;
        exitToMainButton.clicked += ExitToMainMenu;
    }
    
    public void ShowWorkActivities()
    {
        ShowActivitySelection(ActivityCategory.Work, "Work Activities - Earn Some Pounds!");
    }
    
    public void ShowTrainingActivities()
    {
        ShowActivitySelection(ActivityCategory.Training, "Training Activities - Build That Rowing Strength!");
    }
    
    private void ShowActivitySelection(ActivityCategory category, string title)
    {
        HideAllPanels();
        activitySelectionPanel.style.display = DisplayStyle.Flex;
        categoryTitle.text = title;
        
        ClearActivityButtons();
        
        List<MiniGameData> activitiesToShow = category == ActivityCategory.Work ? 
            workActivities : trainingActivities;
        
        VisualElement targetContainer = category == ActivityCategory.Work ? 
            workContentContainer : trainingContentContainer;
        
        foreach (MiniGameData activity in activitiesToShow)
        {
            CreateActivityButton(activity, targetContainer);
        }
    }
    
    private void ClearActivityButtons()
    {
        workContentContainer.Clear();
        trainingContentContainer.Clear();
    }
    
    private void CreateActivityButton(MiniGameData activity, VisualElement container)
    {
        var activityButton = new Button();
        activityButton.name = $"activity-{activity.gameType}";
        activityButton.AddToClassList("activity-button");
        
        // Create button content
        var buttonContent = new VisualElement();
        buttonContent.name = "button-content";
        buttonContent.AddToClassList("button-content");
        
        // Icon (if available)
        if (activity.gameIcon != null)
        {
            var icon = new VisualElement();
            icon.name = "activity-icon";
            icon.AddToClassList("activity-icon");
            icon.style.backgroundImage = new StyleBackground(activity.gameIcon);
            buttonContent.Add(icon);
        }
        
        // Text
        var label = new Label(activity.gameName);
        label.name = "activity-label";
        label.AddToClassList("activity-label");
        buttonContent.Add(label);
        
        activityButton.Add(buttonContent);
        activityButton.clicked += () => SelectActivity(activity);
        
        container.Add(activityButton);
    }
    
    private void SelectActivity(MiniGameData activity)
    {
        currentGame = activity;
        
        HideAllPanels();
        gameplayPanel.style.display = DisplayStyle.Flex;
        
        // Display game info
        gameTitle.text = activity.gameName;
        gameInstructions.text = activity.description;
        
        // Show funny quote
        if (!string.IsNullOrEmpty(activity.startQuote))
        {
            StartCoroutine(ShowQuote(activity.startQuote, 3f));
        }
        
        // Reset game state
        gameTimer = activity.timeLimit;
        currentScore = 0;
        gameActive = false;
        
        UpdateDisplays();
        startGameButton.style.display = DisplayStyle.Flex;
    }
    
    private void StartSelectedGame()
    {
        if (currentGame == null) return;
        
        gameActive = true;
        startGameButton.style.display = DisplayStyle.None;
        
        // Play start sound
        if (currentGame.startSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(currentGame.startSound);
        }
        
        // Create and start the specific mini game
        currentGameInstance = CreateMiniGameInstance(currentGame.gameType);
        currentGameInstance?.Initialize(this, currentGame);
        currentGameInstance?.StartGame();
    }
    
    private MiniGame CreateMiniGameInstance(MiniGameType gameType)
    {
        // Remove any existing game instance
        if (currentGameInstance != null)
        {
            Destroy((MonoBehaviour)currentGameInstance);
        }
        
        switch (gameType)
        {
          
            default:
                Debug.LogWarning($"Mini game {gameType} not implemented yet!");
                return null;
        }
    }
    
    public void UpdateScore(int score)
    {
        currentScore = score;
        UpdateDisplays();
    }
    
    public void EndGame(bool forceEnd = false)
    {
        gameActive = false;
        
        if (currentGameInstance != null)
        {
            currentGameInstance.EndGame();
        }
        
        CalculateAndShowResults();
    }
    
    private void CalculateAndShowResults()
    {
        HideAllPanels();
        resultsPanel.style.display = DisplayStyle.Flex;
        
        // Calculate performance
        float scorePercentage = (float)currentScore / currentGame.perfectScore;
        bool isSuccess = scorePercentage >= 0.6f;
        
        // Calculate rewards
        float earnings = 0;
        int staminaGain = 0;
        
        if (isSuccess)
        {
            if (currentGame.category == ActivityCategory.Work)
            {
                earnings = currentGame.baseEarnings * scorePercentage * currentGame.difficultyMultiplier;
            }
            else
            {
                staminaGain = Mathf.RoundToInt(currentGame.baseGain * scorePercentage * currentGame.difficultyMultiplier);
            }
        }
        
        // Update player stats using PlayerManager
        if (playerManager != null)
        {
            if (earnings > 0) 
            {
                playerManager.ModifyPlayerCoins(earnings);
            }
            
            if (staminaGain > 0) 
            {
                playerManager.ModifyPlayerStamina(staminaGain);
            }
        }
        
        // Display results
        resultsTitle.text = isSuccess ? "Grand Job!" : "Ah, Feck!";
        performanceText.text = $"Performance: {scorePercentage:P0}";
        earningsText.text = $"Earnings: £{earnings:F0}";
        staminaGainText.text = $"Stamina Gained: {staminaGain}";
        
        // Show appropriate quote
        string resultQuote = isSuccess ? currentGame.successQuote : currentGame.failureQuote;
        quotesText.text = !string.IsNullOrEmpty(resultQuote) ? $"\"{resultQuote}\"" : "";
        
        // Play result sound
        AudioClip soundToPlay = isSuccess ? currentGame.successSound : currentGame.failureSound;
        if (soundToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }
    }
    
    private IEnumerator ShowQuote(string quote, float duration)
    {
        quotesPanel.style.display = DisplayStyle.Flex;
        var quoteLabel = quotesPanel.Q<Label>("quote-text");
        if (quoteLabel != null)
        {
            quoteLabel.text = $"\"{quote}\"";
        }
        
        yield return new WaitForSeconds(duration);
        
        quotesPanel.style.display = DisplayStyle.None;
    }
    
    private void UpdateDisplays()
    {
        UpdateTimeDisplay();
        scoreDisplay.text = $"Score: {currentScore}";
        
        if (currentGame != null)
        {
            float progress = (float)currentScore / currentGame.perfectScore;
            progressDisplay.text = $"Progress: {progress:P0}";
        }
    }
    
    private void UpdateTimeDisplay()
    {
        int minutes = Mathf.FloorToInt(gameTimer / 60);
        int seconds = Mathf.FloorToInt(gameTimer % 60);
        timeDisplay.text = $"Time: {minutes:00}:{seconds:00}";
    }
    
    private void HideAllPanels()
    {
        activitySelectionPanel.style.display = DisplayStyle.None;
        gameplayPanel.style.display = DisplayStyle.None;
        resultsPanel.style.display = DisplayStyle.None;
        quotesPanel.style.display = DisplayStyle.None;
    }
    
    // Button event handlers
    private void QuitCurrentGame()
    {
        if (gameActive)
        {
            gameActive = false;
            currentGameInstance?.EndGame();
        }
        BackToSelection();
    }
    
    private void RestartCurrentGame()
    {
        if (currentGame != null)
        {
            SelectActivity(currentGame);
        }
    }
    
    private void BackToSelection()
    {
        HideAllPanels();
        if (currentGame != null)
        {
            if (currentGame.category == ActivityCategory.Work)
                ShowWorkActivities();
            else
                ShowTrainingActivities();
        }
    }
    
    private void CloseActivitySelection()
    {
        HideAllPanels();
    }
    
    private void ExitToMainMenu()
    {
        HideAllPanels();
    }
}
