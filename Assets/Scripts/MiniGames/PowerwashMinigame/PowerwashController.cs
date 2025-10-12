//using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PowerwashController : MonoBehaviour
{
    [Tooltip("Drag your Powerwasher prefab here")]
    public GameObject powerwashPrefab;

    [Tooltip("Which layers count as 'walls'")]
    public LayerMask plateLayerMask;

    [Tooltip("Maximum distance (in world units) the powerwasher will follow the cursor")]
    public float maxDistance = 5f;

    [Tooltip("How high above the wall surface the powerwasher should hover")]
    public float hoverHeight = 0.01f;

    public GameObject wallPrefab; // Reference to the Wall prefab
    
    public List<WallLogic> walls = new List<WallLogic>(); // List to hold spawned walls
    
    public List<WallLogic> wallsCleaned = new List<WallLogic>(); // List to hold Walls that need cleaning
    
    GameObject powerwashInstance;

    public WallCleanPosition wallCleanPosition; // Reference to the WallCleanPosition script
    
    public Transform spawnPoint,cleanPosition,finishClean; // Point where the powerwash is spawned

    public Camera powerwashCamera;

    public List<GameObject> wallList = new List<GameObject>();

    public GameObject WallPrefab1;

    public GameObject WallPrefab2;

    public GameObject WallPrefab3;

    public GameObject WallPrefab4;


    [SerializeField] private int spawnCount = 5;

    FMOD.Studio.EventInstance MovewallAudio;
    //Coroutine powerwashAudioCoroutine;

    void OnEnable()
    {
        if (powerwashPrefab == null)
        {
            Debug.LogError("Please assign a powerwashPrefab!");
            enabled = false;
            return;
        }

        // Only create one powerwash
        if (powerwashInstance == null)
            powerwashInstance = Instantiate(powerwashPrefab);

        Spawnwalls();
        wallCleanPosition.onWallCleaned.AddListener(wallCleaned); // Subscribe to the BeerDone event
        MovewallToCleanPosition();
        
    }

    void Update()
    {
        if (powerwashInstance == null) return;

        // Ray from camera through cursor/touch
        Ray ray = powerwashCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Raycast against wall colliders, up to maxDistance
        if (Physics.Raycast(ray, out hit, maxDistance, plateLayerMask))
        {
            // Place powerwash at the hit point + hoverHeight
            Vector3 targetPos = hit.point;
            targetPos.y += hoverHeight;
            powerwashInstance.transform.position = targetPos;

            //PlaypowerwashAudio();
            //powerwashAudioCoroutine = StartCoroutine(PlaypowerwashAudio());
            //PlaykapowerwashAudio();
        }
        else
        {
            // Optional: clamp to the farthest point along the ray
            Vector3 fallback = ray.GetPoint(maxDistance);
            fallback.y = Mathf.Max(fallback.y, hoverHeight);
            powerwashInstance.transform.position = fallback;
        }
    }

    //public void PlaykapowerwashAudio()
    //{
    //    FMOD.Studio.EventInstance kapowerwash;
    //    kapowerwash = FMODUnity.RuntimeManager.CreateInstance("event:/Kitchen/powerwash");
    //    kapowerwash.start();
    //}
    //IEnumerator PlaypowerwashAudio()
    //{
    //    FMOD.Studio.EventInstance kapowerwash;
    //    kapowerwash = FMODUnity.RuntimeManager.CreateInstance("event:/Mini Games/kapowerwash");
    //    kapowerwash.start();
    //    yield return new WaitForSeconds(1f);
    //}

    public void MovewallToCleanPosition()
    {
        if (wallCleanPosition.wallLogic == null  && walls.Count > 0)
        {
            walls[0].transform.position = cleanPosition.position; // Move the wall to the clean position
           // walls[0].transform.rotation = cleanPosition.rotation; // Set the rotation to match the clean position
        }
    }
    
    public void Spawnwalls()
    {

        //Select a random wall
        wallList.Add(WallPrefab1);
        wallList.Add(WallPrefab2);
        wallList.Add(WallPrefab3);
        wallList.Add(WallPrefab4);
        int wallIndex = UnityEngine.Random.Range(0, 4);

       /* 
        */

        //Instantiate(wallList[wallIndex], cleanPosition.position, Quaternion.identity);
        wallPrefab = wallList[wallIndex];
        for (int i = 0; i < spawnCount; i++) // Spawn 5 walls
        {
            if (wallPrefab == null)
            {
                return;
            }
            GameObject wallObject = null;
            if (wallIndex == 0)
            {
                wallObject = Instantiate(wallPrefab, cleanPosition.position, Quaternion.RotateTowards(Quaternion.identity, Quaternion.Euler(0, 90, 0), 90));
            }
            else if (wallIndex == 3)
            {
                wallObject = Instantiate(wallPrefab, cleanPosition.position, Quaternion.RotateTowards(Quaternion.identity, Quaternion.Euler(0, -90, 90), 90));
            }
            else
            {
                wallObject= Instantiate(wallPrefab, cleanPosition.position, Quaternion.RotateTowards(Quaternion.identity, Quaternion.Euler(90,0,0),90));
                Debug.Log("Spawned wall with rotation: " + wallObject.transform.rotation.eulerAngles);
            }
            
           // wallObject.transform.rotation = spawnPoint.rotation; // Set the rotation to match the spawn point
            WallLogic wallLogic = wallObject.GetComponent<WallLogic>();
            if (wallLogic != null)
            {
                walls.Add(wallLogic); // Add the wall to the list
                Debug.Log("Spawned wall: " + wallObject.name);
            }
            else
            {
                Debug.LogError("wallLogic component not found on the spawned wall prefab.");
            }
        }
    }
    public void wallCleaned()
    {
        wallCleanPosition.wallLogic.transform.position = finishClean.position; // Move the wall to the finish clean position
        wallsCleaned.Add(wallCleanPosition.wallLogic); // Add the cleaned wall to the list
        walls.Remove(wallCleanPosition.wallLogic); // Remove the cleaned wall from the walls list
        wallCleanPosition.wallLogic = null; // Clear the wallLogic reference in wallCleanPosition
        MovewallToCleanPosition(); // Move the next wall to the clean position
        
        /*MovewallAudio = FMODUnity.RuntimeManager.CreateInstance("event:/Kitchen/Move wall");
        MovewallAudio.start();*/

        MinigameFinished();
    }
    
    public void MinigameFinished()
    {
       if(wallsCleaned.Count == spawnCount)
       {
           Debug.Log("Dishwashing minigame completed!");
           // Let MiniGameManager handle the completion instead of loading scene directly
           // SceneManager.LoadScene("RaceScene"); 
           // if(GameManager.Instance != null)
           // {
           //     GameManager.Instance.PlayerWorked();
           // }
       }
      
    }
}