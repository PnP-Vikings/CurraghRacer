//using System.Collections;
using MiniGames;
using System.Collections.Generic;
using DG.Tweening;
using League;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DishwashingController : MonoBehaviour
{
    [Tooltip("Drag your Sponge prefab here")]
    public GameObject spongePrefab;

    [Tooltip("Which layers count as 'plates'")]
    public LayerMask plateLayerMask;

    [Tooltip("Maximum distance (in world units) the sponge will follow the cursor")]
    public float maxDistance = 5f;

    [Tooltip("How high above the plate surface the sponge should hover")]
    public float hoverHeight = 0.01f;

    public GameObject PlatePrefab; // Reference to the Plate prefab
    
    public List<PlateLogic> plates = new List<PlateLogic>(); // List to hold spawned plates
    
    public List<PlateLogic> platesCleaned = new List<PlateLogic>(); // List to hold plates that need cleaning
    
    GameObject spongeInstance;

    public PlateCleanPosition plateCleanPosition; // Reference to the PlateCleanPosition script
    
    public Transform spawnPoint,cleanPosition,finishClean; // Point where the sponge is spawned

    [SerializeField] private int spawnCount = 5;

    public MiniGameManager MiniGameManager;
    
    Sequence _currentTween;

    void OnEnable()
    {
        if (spongePrefab == null)
        {
            Debug.LogError("Please assign a spongePrefab!");
            enabled = false;
            return;
        }

        // Only create one sponge
        if (spongeInstance == null)
            spongeInstance = Instantiate(spongePrefab);
        
        
        SpawnPlates();
        plateCleanPosition.onPlateCleaned.AddListener(PlateCleaned); // Subscribe to the BeerDone event
        
    }

    void Update()
    {
        if (spongeInstance == null) return;

        Vector2 inputPosition = Vector2.zero;
        bool hasInput = false;

        // Check for touch input first (mobile)
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.isPressed)
        {
            inputPosition = ts.primaryTouch.position.ReadValue();
            hasInput = true;
        }
        // Fall back to mouse input (desktop/editor)
        else if (Mouse.current != null)
        {
            inputPosition = Mouse.current.position.ReadValue();
            hasInput = true;
        }

        if (!hasInput) return;
        
        // Ray from camera through cursor/touch
        Ray ray = Camera.main.ScreenPointToRay(inputPosition);
        RaycastHit hit;

        // Raycast against plate colliders, up to maxDistance
        if (Physics.Raycast(ray, out hit, maxDistance, plateLayerMask))
        {
            // Place sponge at the hit point + hoverHeight
            Vector3 targetPos = hit.point;
            targetPos.y += hoverHeight;
            spongeInstance.transform.position = targetPos;
        }
        else
        {
            // Optional: clamp to the farthest point along the ray
            Vector3 fallback = ray.GetPoint(maxDistance);
            fallback.y = Mathf.Max(fallback.y, hoverHeight);
            spongeInstance.transform.position = fallback;
        }
    }

    public void MovePlateToCleanPosition()
    {
        if (plateCleanPosition.plateLogic == null  && plates.Count > 0)
        {
            _currentTween?.Kill(); // Kill any existing tween
            // Move the plate to the clean position and Set the rotation to match the clean position*/
            _currentTween = DOTween.Sequence()
                .Append(plates[0].transform.DOMove(spawnPoint.position + new Vector3(0,1f,0), 2f).SetEase(Ease.InOutSine))
                .Append(plates[0].transform.DOMove(cleanPosition.position, 1f).SetEase(Ease.OutSine))
                .Join(plates[0].transform.DORotateQuaternion(cleanPosition.rotation, 1f).SetEase(Ease.OutSine))
                .SetUpdate(true).SetEase(Ease.OutQuad);
            _currentTween.OnComplete(() =>
            {
                plates[0].SetAllDirtShaderstoCleaning(); // Start cleaning the plate
            });
        }
    }

    public Tween MovePlateToStartPostion(PlateLogic plateLogic)
    {
        Debug.Log("Moving plate to start position: " + plateLogic.gameObject.name);
        Tween tween = DOTween.Sequence()
            .Append(plateLogic.transform.DOMove(spawnPoint.position + new Vector3(0, 0.009f* (plates.Count), 0) , 0.5f).SetEase(Ease.InOutSine))
            .SetUpdate(true)
            .SetEase(Ease.OutQuad);
    
        return tween; // Return the tween so we can track it
    }

    public void SpawnPlates()
    {
        List<Tween> spawnTweens = new List<Tween>();
    
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject plateObject = Instantiate(PlatePrefab, spawnPoint.position + new Vector3(3,1, 0), Quaternion.identity);
            PlateLogic plateLogic = plateObject.GetComponent<PlateLogic>();
            if (plateLogic != null)
            {
                plates.Add(plateLogic);
                Debug.Log("Spawned plate: " + plateObject.name);
                Tween tween = MovePlateToStartPostion(plateLogic);
                spawnTweens.Add(tween);
            }
            else
            {
                Debug.LogError("PlateLogic component not found on the spawned plate prefab.");
            }
        }

        // Wait for all spawn animations to complete
        _currentTween?.Kill();
        _currentTween = DOTween.Sequence();
        foreach (var tween in spawnTweens)
        {
            _currentTween.Append(tween); // Join all tweens so they play simultaneously
        }
    
        _currentTween.OnComplete(() =>
        {
            MovePlateToCleanPosition();
        });
    }
    
    public void PlateCleaned()
    {
        float finishCleanPosX=finishClean.position.x; 
        float finishCleanPosZ=finishClean.position.z; 
        float finishCleanPosY=finishClean.position.y; 
        
        _currentTween?.Kill();
        _currentTween = DOTween.Sequence()
            .Append(plateCleanPosition.plateLogic.transform.DOMove(new Vector3(finishCleanPosX, finishCleanPosY + 1f, finishCleanPosZ), 0.3f).SetEase(Ease.OutSine))
            .Append(plateCleanPosition.plateLogic.transform.DOMove(new Vector3(finishCleanPosX, finishCleanPosY + (0.004f * (platesCleaned.Count + 1)), finishCleanPosZ), 0.3f).SetEase(Ease.OutQuad)) // Reduced to 0.3s
            .Join(plateCleanPosition.plateLogic.transform.DORotateQuaternion(finishClean.rotation, 0.3f).SetEase(Ease.OutQuad)) // Match rotation duration
            .SetUpdate(true);
        
        
        
        /*
        plateCleanPosition.plateLogic.transform.position = new Vector3(finishCleanPosX,finishCleanPosY + (.03f*(platesCleaned.Count+1)),finishCleanPosZ); // Slightly raise the plate to avoid z-fighting
        plateCleanPosition.plateLogic.transform.rotation= finishClean.transform.rotation; // Set the rotation to match the finish clean position
        */
        platesCleaned.Add(plateCleanPosition.plateLogic); // Add the cleaned plate to the list
        plates.Remove(plateCleanPosition.plateLogic); // Remove the cleaned plate from the plates list
        plateCleanPosition.plateLogic = null; // Clear the plateLogic reference in PlateCleanPosition
        
        _currentTween.OnComplete(() =>
        {
            MovePlateToCleanPosition(); // Move the next plate to the clean position
        });
    

        if (AudioManager.instance != null)
        {
            AudioManager.instance.movePlateAudio.start();
        }

        MinigameFinished();
    }
    
    public void MinigameFinished()
    {
       if(platesCleaned.Count == spawnCount)
       {
           Debug.Log("Dishwashing minigame completed!");
           
           int finalScore = platesCleaned.Count * 100; // 100 points per plate cleaned
           
           // Let MiniGameManager handle the completion, rewards, and scene transition
           if (MiniGameManager.Instance != null)
           {
               Debug.Log($"Calling MiniGameManager.CompleteGame with score: {finalScore}");
               MiniGameManager.Instance.CompleteGame(finalScore);
           }
           else
           {
               Debug.LogError("MiniGameManager.Instance is null! Cannot complete minigame properly.");
               
               // Fallback: manually return to main scene if MiniGameManager is missing
               if (GameManager.Instance != null)
               {
                   GameManager.Instance.PlayerWorked();
               }
               SceneManager.LoadScene(GameManager.Instance.mainSceneName);
           }
       }
    }

    public enum PlateDishwashingStages
    {
        NotStarted,
        Sinking,
        Cleaning,
        Finished
    }
}

