// Assets/Scripts/Vitor/DefaultMiniGame.cs
using UnityEngine;
using UnityEngine.UIElements;

public class DefaultMiniGame : MonoBehaviour, MiniGame
{
    protected MiniGameManager gameManager;
    protected MiniGameData gameData;
    protected int currentScore;
    
    public virtual void Initialize(MiniGameManager manager, MiniGameData gameData)
    {
        this.gameManager = manager;
        this.gameData = gameData;
        this.currentScore = 0;
    }
    
    public virtual void StartGame()
    {
        Debug.Log($"Starting {gameData.gameName}");
        // Basic implementation - just increment score over time
        InvokeRepeating(nameof(AddScore), 1f, 0.5f);
    }
    
    public virtual void UpdateGame()
    {
        // Override in specific implementations
    }
    
    public virtual void EndGame()
    {
        CancelInvoke();
        Debug.Log($"Ending {gameData.gameName} with score: {currentScore}");
    }
    
    public virtual VisualElement CreateGameUI()
    {
        return null; // Use default UI
    }
    
    protected virtual void AddScore()
    {
        currentScore += Random.Range(1, 5);
        gameManager.UpdateScore(currentScore);
    }
}