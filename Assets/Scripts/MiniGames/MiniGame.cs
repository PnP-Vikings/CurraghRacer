using UnityEngine.UIElements;

public interface  MiniGame 
{
    void Initialize(MiniGameManager manager, MiniGameData gameData);
    void StartGame();
    void UpdateGame();
    void EndGame();
    VisualElement CreateGameUI(); // Optional: for games that need custom UI
}
