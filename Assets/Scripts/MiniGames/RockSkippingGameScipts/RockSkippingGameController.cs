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
    public Dictionary<int, (Rock rock, int score)> rockScores = new Dictionary<int, (Rock, int)>();
    public RockCase rockCase;
    
    public void Awake()
    {
        availableRocksForThisSession = new List<Rock>();
        
       int rockCounter = 0;
       while (rockCounter < 4)
       {
              int randomIndex = Random.Range(0, rocksTypes.Count);
              Rock selectedRock = rocksTypes[randomIndex];
              selectedRock.Initialize(rocksTypes[randomIndex].rockType);
              availableRocksForThisSession.Add(selectedRock);
              rockCounter++;
       }
       
       List<RockVisual> rocksToSpawn = new List<RockVisual>();
       foreach (var rock in availableRocksForThisSession)
         {
             rock.rockVisual.Initialize(rock.rockType,rock);
             rocksToSpawn.Add(rock.rockVisual);
         }
       rockCase.SpawnRocksInCase(rocksToSpawn);
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
