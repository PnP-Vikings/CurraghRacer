using System.Collections.Generic;
using JetBrains.Annotations;
using MiniGames;
using UnityEngine;

public class RockSkippingGameController : MonoBehaviour,MiniGame
{
    Stages stage = Stages.RockPicking;
    public int roundsToPlay = 3;
    public int currentRound = 0;
    public List<Rock> rocksTypes;
    public List<Rock> availableRocksForThisSession;
    public Rock currentRock;
    public Transform rockSpawnPoint;
    public Dictionary<Rock,int> rockScores = new Dictionary<Rock, int>();
    
    public void Awake()
    {
        availableRocksForThisSession = new List<Rock>();
        
       int rockCounter = 0;
       while (rockCounter < 4)
       {
              int randomIndex = Random.Range(0, rocksTypes.Count);
              Rock selectedRock = rocksTypes[randomIndex];
              availableRocksForThisSession.Add(selectedRock);
              rockCounter++;
       }

       foreach (Rock rock in availableRocksForThisSession)
       {
           rockScores.Add(rock, 0);
       }
       
       
    }
    
    public void Initialize(MiniGameManager manager, MiniGameData gameData)
    {
        
    }

    public void StartGame()
    {
        throw new System.NotImplementedException();
    }

    public void UpdateGame()
    {
        throw new System.NotImplementedException();
    }

    public void EndGame()
    {
        throw new System.NotImplementedException();
    }


    public enum Stages
    {
        RockPicking,
        Aiming,
        Observing,
        GameOver
    }
}
