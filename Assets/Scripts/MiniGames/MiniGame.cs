using UnityEngine;

namespace MiniGames
{
    public interface MiniGame 
    {
        void Initialize(MiniGameManager manager, MiniGameData gameData);
        void StartGame();
        void UpdateGame();
        void EndGame();
    }
}
